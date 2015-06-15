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
            // Get table info
            _tableInfoPlan = GetTableInfo(Type);

            // Work out bound properties
            _memberPlans = GetPocoMembers(Type, Mapper, new List<MemberInfo>()).ToList();

            return this;
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

        protected virtual TableInfoPlan GetTableInfo(Type type)
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
            ColumnInfo ci = ColumnInfo.FromMemberInfo(mi);
            return ci;
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
        
        private IEnumerable<PocoMemberPlan> GetPocoMembers(Type type, IMapper mapper, List<MemberInfo> memberInfos, string prefix = null)
        {
            var capturedMembers = memberInfos.ToArray();
            var capturedPrefix = prefix;
            foreach (var mi in ReflectionUtils.GetFieldsAndPropertiesForClasses(type))
            {
                var ci = GetColumnInfo(mi, memberInfos.ToArray());

                if (ci.IgnoreColumn)
                    continue;

                var memberInfoType = mi.GetMemberInfoType();
                if (ci.ReferenceMappingType == ReferenceMappingType.Many)
                {
                    memberInfoType = memberInfoType.GetGenericArguments().First();
                }

                var childrenPlans = new PocoMemberPlan[0];
                TableInfoPlan childTableInfoPlan = null;
                var members = new List<MemberInfo>();

                if (ci.ComplexMapping || ci.ReferenceMappingType != ReferenceMappingType.None)
                {
                    members.AddRange(capturedMembers);
                    members.Add(mi);

                    if (capturedMembers.GroupBy(x => x.GetMemberInfoType()).Any(x => x.Count() >= 2))
                    {
                        continue;
                    }

                    if (ci.ReferenceMappingType != ReferenceMappingType.None)
                    {
                        childTableInfoPlan = GetTableInfo(memberInfoType);
                    }

                    var newPrefix = GetNewPrefix(capturedPrefix, ci.ReferenceMappingType != ReferenceMappingType.None ? "" : (ci.ComplexPrefix ?? mi.Name));

                    childrenPlans = GetPocoMembers(memberInfoType, mapper, members, newPrefix).ToArray();
                }

                MemberInfo mi1 = mi;
                yield return tableInfo =>
                {
                    var pc = new PocoColumn
                    {
                        ReferenceMappingType = ci.ReferenceMappingType, 
                        TableInfo = tableInfo, 
                        MemberInfo = mi1, 
                        MemberInfoChain = new[] {mi1}.ToList(), 
                        ColumnName = GetColumnName(capturedPrefix, ci.ColumnName), 
                        ResultColumn = ci.ResultColumn, ForceToUtc = ci.ForceToUtc, 
                        ComputedColumn = ci.ComputedColumn, ColumnType = ci.ColumnType, 
                        ColumnAlias = ci.ColumnAlias, VersionColumn = ci.VersionColumn, 
                        VersionColumnType = ci.VersionColumnType, ComplexType = ci.ComplexType
                    };

                    if (memberInfos.Count == 0)
                    {
                        var originalPk = pc.TableInfo.PrimaryKey.Split(',');
                        for (int i = 0; i < originalPk.Length; i++)
                        {
                            if (originalPk[i].Equals(mi1.Name, StringComparison.OrdinalIgnoreCase))
                                originalPk[i] = (ci.ColumnName ?? mi1.Name);
                        }
                        pc.TableInfo.PrimaryKey = string.Join(",", originalPk);
                    }

                    if (mapper != null && !mapper.MapMemberToColumn(mi1, ref pc.ColumnName, ref pc.ResultColumn))
                        return null;

                    var childrenTableInfo = childTableInfoPlan == null ? tableInfo : childTableInfoPlan();
                    var children = childrenPlans.Select(x =>
                    {
                        var member = x(childrenTableInfo);
                        if (member.PocoColumn != null)
                        {
                            member.PocoColumn.MemberInfoChain = new List<MemberInfo>(members.Concat(new[] { member.MemberInfo }));
                        }
                        return member;
                    }).ToList();

                    return new PocoMember()
                    {
                        MemberInfo = mi1,
                        IsList = IsList(mi1),
                        PocoColumn = ci.ComplexMapping ? null : pc,
                        ReferenceMappingType = ci.ReferenceMappingType,
                        ReferenceMemberName = ci.ReferenceMemberName,
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

    public class PocoData
    {
        protected internal Type Type { get; protected set; }
        public IMapper Mapper { get; set; }

        public KeyValuePair<string, PocoColumn>[] QueryColumns { get; protected internal set; }
        public TableInfo TableInfo { get; protected internal set; }
        public Dictionary<string, PocoColumn> Columns { get; protected internal set; }
        public List<PocoMember> Members { get; protected internal set; }
        public List<PocoColumn> AllColumns { get; protected internal set; }

        public PocoData()
        {
        }

        public PocoData(Type type, IMapper mapper) : this()
        {
            Type = type;
            Mapper = mapper;
        }


        public object[] GetPrimaryKeyValues(object obj)
        {
            return PrimaryKeyValues(obj);
        }

        private Func<object, object[]> _primaryKeyValues;
        private Func<object, object[]> PrimaryKeyValues
        {
            get
            {
                if (_primaryKeyValues == null)
                {
                    var multiplePrimaryKeysNames = TableInfo.PrimaryKey.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();
                    var members = multiplePrimaryKeysNames
                        .Select(x => Members.FirstOrDefault(y => y.PocoColumn != null
                                && y.ReferenceMappingType == ReferenceMappingType.None
                                && string.Equals(x, y.PocoColumn.ColumnName, StringComparison.OrdinalIgnoreCase)))
                        .Where(x => x != null);
                    _primaryKeyValues = obj => members.Select(x => x.PocoColumn.GetValue(obj)).ToArray();
                }
                return _primaryKeyValues;
            }
        }

        

        public object CreateObject()
        {
            if (CreateDelegate == null)
                CreateDelegate = new FastCreate(Type);
            return CreateDelegate.Create();
        }

        private FastCreate CreateDelegate;

    }
}
