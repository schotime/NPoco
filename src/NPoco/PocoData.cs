using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace NPoco
{
    public class PocoMember
    {
        public PocoMember()
        {
            PocoMemberChildren = new List<PocoMember>();
        }

        public string Name { get { return MemberInfo.Name; } }
        public MemberInfo MemberInfo { get; set; }
        public PocoColumn PocoColumn { get; set; }
        public List<PocoMember> PocoMemberChildren { get; set; }
        public bool IsReferenceMapping { get; set; }
        public string ReferenceMemberName { get; set; }
    }

    public class PocoData
    {
        public PocoDataFactory PocoDataFactory { get; protected set; }
        protected internal IMapper Mapper;
        internal bool EmptyNestedObjectNull;
        private readonly Cache<string, Type> aliasToType;
     
        protected internal Type Type;
        public KeyValuePair<string, PocoColumn>[] QueryColumns { get; protected set; }
        public TableInfo TableInfo { get; protected internal set; }
        public Dictionary<string, PocoColumn> Columns { get; protected internal set; }
        public List<PocoMember> Members { get; protected internal set; }
        public List<PocoColumn> AllColumns { get; protected internal set; }

        private readonly MappingFactory _mappingFactory;

        public MappingFactory MappingFactory
        {
            get { return _mappingFactory; }
        }

        public PocoData()
        {
            _mappingFactory = new MappingFactory(this);
        }

        public PocoData(Type type, IMapper mapper, Cache<string, Type> aliasToTypeCache, PocoDataFactory pocoDataFactory) : this()
        {
            PocoDataFactory = pocoDataFactory;
            aliasToType = aliasToTypeCache;
            Type = type;
            Mapper = mapper;
        }

        public PocoData Init()
        {
            // Get table info
            TableInfo = GetTableInfo(Type);

            // Call column mapper
            if (Mapper != null)
                Mapper.GetTableInfo(Type, TableInfo);

            // Work out bound properties
            Members = GetPocoMembers(Type, TableInfo, Mapper, new List<MemberInfo>()).ToList();
            Columns = GetPocoColumns(Members, false).Where(x => x != null).ToDictionary(x => x.ColumnName, x => x, StringComparer.OrdinalIgnoreCase);
            AllColumns = GetPocoColumns(Members, true).Where(x => x != null).ToList();

            // Build column list for automatic select
            QueryColumns = Columns.Where(c => !c.Value.ResultColumn && !c.Value.IsReferenceColumn).ToArray();

            return this;
        }

        protected virtual TableInfo GetTableInfo(Type type)
        {
            var tableInfo = TableInfo.FromPoco(type);
            tableInfo.AutoAlias = CreateAlias(type.Name, type);
            return tableInfo;
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
                yield return member.PocoColumn;
                
                var member1 = member;
                foreach (var pocoMemberChild in GetPocoColumns(member.PocoMemberChildren.Where(x => all || !member1.IsReferenceMapping), all))
                {
                    yield return pocoMemberChild;
                }
            }
        } 

        private IEnumerable<PocoMember> GetPocoMembers(Type type, TableInfo tableInfo, IMapper mapper, List<MemberInfo> memberInfos, string prefix = null)
        {
            var index = 0;
            var capturedMembers = memberInfos.ToArray();
            foreach (var mi in ReflectionUtils.GetFieldsAndPropertiesForClasses(type))
            {
                var ci = GetColumnInfo(mi, memberInfos.ToArray());

                if (ci.IgnoreColumn)
                    continue;

                var pocoMemberChildren = new List<PocoMember>();
                var newTableInfo = ci.ReferenceMapping ? PocoDataFactory.ForType(mi.GetMemberInfoType()).TableInfo : tableInfo;

                if (ci.ComplexMapping || ci.ReferenceMapping)
                {
                    var members = new List<MemberInfo>();
                    members.AddRange(capturedMembers);
                    members.Add(mi);
                    
                    foreach (var pocoMember in GetPocoMembers(mi.GetMemberInfoType(), newTableInfo, mapper, members, GetNewPrefix(prefix, ci.ComplexPrefix ?? mi.Name).TrimStart('_')))
                    {
                        if (pocoMember.PocoColumn != null)
                        {
                            pocoMember.PocoColumn.MemberInfoChain = new List<MemberInfo>(members.Concat(new[] { pocoMember.MemberInfo }));
                        }
                        pocoMemberChildren.Add(pocoMember);
                    }
                }

                var pc = new PocoColumn();
                pc.IsReferenceColumn = ci.ReferenceMapping;
                pc.TableInfo = newTableInfo;
                pc.MemberInfo = mi;
                pc.MemberInfoChain = new[] {mi}.ToList();
                pc.ColumnName = GetColumnName(prefix, ci.ColumnName);
                pc.ResultColumn = ci.ResultColumn;
                pc.ForceToUtc = ci.ForceToUtc;
                pc.ComputedColumn = ci.ComputedColumn;
                pc.ColumnType = ci.ColumnType;
                pc.ColumnAlias = ci.ColumnAlias;
                pc.VersionColumn = ci.VersionColumn;
                pc.VersionColumnType = ci.VersionColumnType;
                pc.AutoAlias = tableInfo.AutoAlias + "_" + index++;

                if (mapper != null && !mapper.MapMemberToColumn(mi, ref pc.ColumnName, ref pc.ResultColumn))
                    continue;

                // Store it
                
                yield return new PocoMember()
                {
                    MemberInfo = mi,
                    PocoColumn = ci.ComplexMapping ? null : pc,
                    IsReferenceMapping = ci.ReferenceMapping,
                    ReferenceMemberName = ci.ReferenceMemberName ?? (ci.ReferenceMapping ? pocoMemberChildren.Single(x=>x.PocoColumn.ColumnName.Equals(newTableInfo.PrimaryKey, StringComparison.InvariantCultureIgnoreCase)).Name : null),
                    PocoMemberChildren = pocoMemberChildren,
                };
            }
        }

        protected virtual string GetColumnName(string prefix, string columnName)
        {
            return GetNewPrefix(prefix, columnName);
        }

        private static string GetNewPrefix(string prefix, string end)
        {
            return string.Join("__", new[] {prefix, end}).TrimStart('_');
        }

        public object CreateObject()
        {
            if (CreateDelegate == null)
                CreateDelegate = new FastCreate(Type);
            return CreateDelegate.Create();
        }

        private FastCreate CreateDelegate;

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
