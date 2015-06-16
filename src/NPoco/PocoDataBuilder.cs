using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NPoco
{
    public class PocoDataBuilder
    {
        private readonly Cache<string, Type> aliasToType = Cache<string, Type>.CreateStaticCache();
        protected internal IMapper Mapper { get; set; }
        protected internal Type Type { get; protected set; }
        public PocoDataFactory PocoDataFactory { get; protected set; }

        private List<PocoMemberPlan> _memberPlans { get; set; }
        private TableInfoPlan _tableInfoPlan { get; set; }
        
        public delegate PocoMember PocoMemberPlan(TableInfo tableInfo);
        public delegate TableInfo TableInfoPlan();

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

            // Get table info
            _tableInfoPlan = GetTableInfo(Type, columnInfos, memberInfos);

            // Work out bound properties
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

            //// Build column list for automatic select
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
            foreach (var ci in columnInfos)
            {
                if (ci.IgnoreColumn)
                    continue;

                var memberInfoType = ci.MemberInfo.GetMemberInfoType();
                if (ci.ReferenceMappingType == ReferenceMappingType.Many)
                {
                    memberInfoType = memberInfoType.GetGenericArguments().First();
                }

                var childrenPlans = new PocoMemberPlan[0];
                TableInfoPlan childTableInfoPlan = null;
                var members = new List<MemberInfo>(capturedMembers) {ci.MemberInfo};

                var name = ci.MemberInfo.Name;
                if (ci.ComplexMapping || ci.ReferenceMappingType != ReferenceMappingType.None)
                {
                    if (capturedMembers.GroupBy(x => x.GetMemberInfoType()).Any(x => x.Count() >= 2))
                    {
                        continue;
                    }

                    var childColumnInfos = GetColumnInfos(memberInfoType, members.ToArray());
                    
                    if (ci.ReferenceMappingType != ReferenceMappingType.None)
                    {
                        childTableInfoPlan = GetTableInfo(memberInfoType, childColumnInfos, members);
                    }

                    var newPrefix = GetNewPrefix(capturedPrefix, ci.ReferenceMappingType != ReferenceMappingType.None ? "" : (ci.ComplexPrefix ?? ci.MemberInfo.Name));

                    childrenPlans = GetPocoMembers(mapper, childColumnInfos, members, newPrefix).ToArray();
                }

                MemberInfo mi1 = ci.MemberInfo;
                var capturedCi = ci;
                yield return tableInfo =>
                {
                    var pc = new PocoColumn
                    {
                        ReferenceMappingType = capturedCi.ReferenceMappingType, 
                        TableInfo = tableInfo, 
                        MemberInfo = mi1,
                        MemberInfoChain = members,
                        ColumnName = GetColumnName(capturedPrefix, capturedCi.ColumnName),
                        ResultColumn = capturedCi.ResultColumn,
                        ForceToUtc = capturedCi.ForceToUtc,
                        ComputedColumn = capturedCi.ComputedColumn,
                        ColumnType = capturedCi.ColumnType,
                        ColumnAlias = capturedCi.ColumnAlias,
                        VersionColumn = capturedCi.VersionColumn,
                        VersionColumnType = capturedCi.VersionColumnType,
                        ComplexType = capturedCi.ComplexType
                    };

                    if (mapper != null && !mapper.MapMemberToColumn(mi1, ref pc.ColumnName, ref pc.ResultColumn))
                        return null;

                    var childrenTableInfo = childTableInfoPlan == null ? tableInfo : childTableInfoPlan();
                    var children = childrenPlans.Select(x => x(childrenTableInfo)).ToList();

                    return new PocoMember()
                    {
                        MemberInfo = mi1,
                        IsList = IsList(mi1),
                        PocoColumn = capturedCi.ComplexMapping ? null : pc,
                        ReferenceMappingType = capturedCi.ReferenceMappingType,
                        ReferenceMemberName = capturedCi.ReferenceMemberName,
                        PocoMemberChildren = children,
                    };
                };
            }
        }

        public static bool IsList(MemberInfo mi)
        {
            return mi.GetMemberInfoType().GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        }

        protected virtual string GetColumnName(string prefix, string columnName)
        {
            return GetNewPrefix(prefix, columnName);
        }

        private static string GetNewPrefix(string prefix, string end)
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

                if (aliasToType.AddIfNotExists(alias, typeIn))
                {
                    continue;
                }

                result = true;
            } while (result == false);

            return alias;
        }
    }
}