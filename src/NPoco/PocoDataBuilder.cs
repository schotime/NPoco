using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NPoco
{
    public class PocoDataBuilder
    {
        private readonly Cache<string, Type> _aliasToType = Cache<string, Type>.CreateStaticCache();
        
        protected Type Type { get; set; }
        private IMapper Mapper { get; set; }
        private PocoDataFactory PocoDataFactory { get; set; }

        private List<PocoMemberPlan> _memberPlans { get; set; }
        private TableInfoPlan _tableInfoPlan { get; set; }

        private delegate PocoMember PocoMemberPlan(TableInfo tableInfo);
        protected delegate TableInfo TableInfoPlan();

        public PocoDataBuilder(Type type, IMapper mapper, PocoDataFactory pocoDataFactory)
        {
            Type = type;
            Mapper = mapper;
            PocoDataFactory = pocoDataFactory;
        }

        public PocoDataBuilder Init()
        {
            var memberInfos = new List<MemberInfo>();
            var columnInfos = GetColumnInfos(Type, memberInfos.ToArray());

            // Get table info plan
            _tableInfoPlan = GetTableInfo(Type, columnInfos, memberInfos);

            // Get pocomember plan
            _memberPlans = GetPocoMembers(Mapper, columnInfos, memberInfos).ToList();

            return this;
        }

        private ColumnInfo[] GetColumnInfos(Type type, MemberInfo[] memberInfos)
        {
            return ReflectionUtils.GetFieldsAndPropertiesForClasses(type)
                .Select(x => GetColumnInfo(x, memberInfos)).ToArray();
        }

        public PocoData Build()
        {
            var pocoData = new PocoData(Type, Mapper);

            pocoData.TableInfo = _tableInfoPlan();

            if (Mapper != null)
                Mapper.GetTableInfo(Type, pocoData.TableInfo);

            pocoData.Members = _memberPlans.Select(plan => plan(pocoData.TableInfo)).ToList();

            pocoData.Columns = GetPocoColumns(pocoData.Members, false).Where(x => x != null).ToDictionary(x => x.ColumnName, x => x, StringComparer.OrdinalIgnoreCase);
            pocoData.AllColumns = GetPocoColumns(pocoData.Members, true).Where(x => x != null).ToList();

            //Build column list for automatic select
            pocoData.QueryColumns = pocoData.Columns.Where(c => !c.Value.ResultColumn && c.Value.ReferenceMappingType == ReferenceMappingType.None).ToArray();
            return pocoData;
        }

        protected virtual TableInfoPlan GetTableInfo(Type type, ColumnInfo[] columnInfos, List<MemberInfo> memberInfos)
        {
            var alias = CreateAlias(type.Name, type);
            return () =>
            {
                var tableInfo = TableInfo.FromPoco(type);
                tableInfo.AutoAlias = alias;
                return tableInfo;
            };
        }

        protected virtual ColumnInfo GetColumnInfo(MemberInfo mi, MemberInfo[] toArray)
        {
            return ColumnInfo.FromMemberInfo(mi);
        }

        private static IEnumerable<PocoColumn> GetPocoColumns(IEnumerable<PocoMember> members, bool all)
        {
            foreach (var member in members)
            {
                if (all || (member.ReferenceMappingType != ReferenceMappingType.OneToOne
                            && member.ReferenceMappingType != ReferenceMappingType.Many))
                {
                    yield return member.PocoColumn;
                }

                if (all || (member.ReferenceMappingType == ReferenceMappingType.None))
                {
                    foreach (var pocoMemberChild in GetPocoColumns(member.PocoMemberChildren, all))
                    {
                        yield return pocoMemberChild;
                    }
                }
            }
        }
        
        private IEnumerable<PocoMemberPlan> GetPocoMembers(IMapper mapper, ColumnInfo[] columnInfos, List<MemberInfo> memberInfos, string prefix = null)
        {
            var capturedMembers = memberInfos.ToArray();
            var capturedPrefix = prefix;
            foreach (var columnInfo in columnInfos)
            {
                if (columnInfo.IgnoreColumn)
                    continue;

                var memberInfoType = columnInfo.MemberInfo.GetMemberInfoType();
                if (columnInfo.ReferenceMappingType == ReferenceMappingType.Many)
                {
                    memberInfoType = memberInfoType.GetGenericArguments().First();
                }

                var childrenPlans = new PocoMemberPlan[0];
                TableInfoPlan childTableInfoPlan = null;
                var members = new List<MemberInfo>(capturedMembers) {columnInfo.MemberInfo};

                if (columnInfo.ComplexMapping || columnInfo.ReferenceMappingType != ReferenceMappingType.None)
                {
                    if (capturedMembers.GroupBy(x => x.GetMemberInfoType()).Any(x => x.Count() >= 2))
                    {
                        continue;
                    }

                    var childColumnInfos = GetColumnInfos(memberInfoType, members.ToArray());
                    
                    if (columnInfo.ReferenceMappingType != ReferenceMappingType.None)
                    {
                        childTableInfoPlan = GetTableInfo(memberInfoType, childColumnInfos, members);
                    }

                    var newPrefix = JoinStrings(capturedPrefix, columnInfo.ReferenceMappingType != ReferenceMappingType.None ? "" : (columnInfo.ComplexPrefix ?? columnInfo.MemberInfo.Name));

                    childrenPlans = GetPocoMembers(mapper, childColumnInfos, members, newPrefix).ToArray();
                }

                MemberInfo capturedMemberInfo = columnInfo.MemberInfo;
                ColumnInfo capturedColumnInfo = columnInfo;

                var accessors = GetMemberAccessors(members);
                var memberType = capturedMemberInfo.GetMemberInfoType();
                var isList = IsList(capturedMemberInfo);
                var listType = GetListType(memberType, isList);
                var fastCreate = GetFastCreate(memberType, isList);

                yield return tableInfo =>
                {
                    var pc = new PocoColumn
                    {
                        ReferenceMappingType = capturedColumnInfo.ReferenceMappingType, 
                        TableInfo = tableInfo, 
                        MemberInfo = capturedMemberInfo,
                        MemberInfoChain = members,
                        ColumnName = GetColumnName(capturedPrefix, capturedColumnInfo.ColumnName),
                        ResultColumn = capturedColumnInfo.ResultColumn,
                        ForceToUtc = capturedColumnInfo.ForceToUtc,
                        ComputedColumn = capturedColumnInfo.ComputedColumn,
                        ColumnType = capturedColumnInfo.ColumnType,
                        ColumnAlias = capturedColumnInfo.ColumnAlias,
                        VersionColumn = capturedColumnInfo.VersionColumn,
                        VersionColumnType = capturedColumnInfo.VersionColumnType,
                        StoredAsJson = capturedColumnInfo.StoredAsJson
                    };

                    pc.SetMemberAccessors(accessors);

                    if (mapper != null && !mapper.MapMemberToColumn(capturedMemberInfo, ref pc.ColumnName, ref pc.ResultColumn))
                        return null;

                    var childrenTableInfo = childTableInfoPlan == null ? tableInfo : childTableInfoPlan();
                    var children = childrenPlans.Select(plan => plan(childrenTableInfo)).ToList();

                    // Cascade ResultColumn down
                    foreach (var child in children.Where(child => child.PocoColumn != null && pc.ResultColumn))
                    {
                        child.PocoColumn.ResultColumn = true;
                    }

                    var pocoMember = new PocoMember()
                    {
                        MemberInfo = capturedMemberInfo,
                        IsList = isList,
                        PocoColumn = capturedColumnInfo.ComplexMapping ? null : pc,
                        ReferenceMappingType = capturedColumnInfo.ReferenceMappingType,
                        ReferenceMemberName = capturedColumnInfo.ReferenceMemberName,
                        PocoMemberChildren = children,
                    };

                    pocoMember.SetMemberAccessor(accessors[accessors.Count-1], fastCreate, listType);

                    return pocoMember;
                };
            }
        }

        private static FastCreate GetFastCreate(Type memberType, bool isList)
        {
            return memberType.IsAClass()
                       ? (new FastCreate(isList
                            ? memberType.GetGenericArguments().First()
                            : memberType))
                       : null;
        }

        private static Type GetListType(Type memberType, bool isList)
        {
            return memberType.IsAClass()
                       ? (isList 
                            ? typeof(List<>).MakeGenericType(memberType.GetGenericArguments().First()) 
                            : null)
                       : null;
        }

        public List<MemberAccessor> GetMemberAccessors(IEnumerable<MemberInfo> memberInfos)
        {
            return memberInfos
                .Select(memberInfo => new MemberAccessor(memberInfo.DeclaringType, memberInfo.Name))
                .ToList();
        }

        public static bool IsList(MemberInfo mi)
        {
            return mi.GetMemberInfoType() != typeof(string) && mi.GetMemberInfoType() != typeof(byte[]) && mi.GetMemberInfoType().GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        }

        protected virtual string GetColumnName(string prefix, string columnName)
        {
            return JoinStrings(prefix, columnName);
        }

        public static string JoinStrings(string prefix, string end)
        {
            var list = new List<string>();
            if (!string.IsNullOrEmpty(prefix))
                list.Add(prefix);
            if (!string.IsNullOrEmpty(end))
                list.Add(end);
            return string.Join("__", list.ToArray());
        }

        protected string CreateAlias(string typeName, Type typeIn)
        {
            string alias;
            int i = 0;
            bool result = false;
            string name = string.Join(string.Empty, typeName.BreakUpCamelCase().Split(' ').Select(x => x.Substring(0, 1)).ToArray());
            do
            {
                alias = name + (i == 0 ? string.Empty : i.ToString());
                i++;

                if (_aliasToType.AddIfNotExists(alias, typeIn))
                {
                    continue;
                }

                result = true;
            } while (result == false);

            return alias;
        }
    }
}