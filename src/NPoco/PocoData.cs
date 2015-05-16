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
    }

    public class PocoData
    {
        public PocoDataFactory PocoDataFactory { get; protected set; }
        protected internal IMapper Mapper;
        internal bool EmptyNestedObjectNull;
        private readonly Cache<string, Type> aliasToType = Cache<string, Type>.CreateStaticCache();
     
        protected internal Type Type;
        public KeyValuePair<string, PocoColumn>[] QueryColumns { get; protected set; }
        public TableInfo TableInfo { get; protected internal set; }
        public Dictionary<string, PocoColumn> Columns { get; protected internal set; }
        public List<PocoMember> Members { get; protected internal set; }
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

            // Set auto alias
            TableInfo.AutoAlias = CreateAlias(Type.Name, Type);

            // Work out bound properties
            Members = GetPocoMembers(Type, TableInfo, Mapper, new List<MemberInfo>()).ToList();
            Columns = GetPocoColumns(Members).Where(x => x != null)
                .ToDictionary(x => x.ColumnName, x => x, StringComparer.OrdinalIgnoreCase);

            // Build column list for automatic select
            QueryColumns = Columns.Where(c => !c.Value.ResultColumn).ToArray();

            return this;
        }

        protected virtual TableInfo GetTableInfo(Type type)
        {
            return TableInfo.FromPoco(type);
        }

        protected virtual ColumnInfo GetColumnInfo(MemberInfo mi, MemberInfo[] toArray)
        {
            ColumnInfo ci = ColumnInfo.FromMemberInfo(mi);
            return ci;
        }

        private static IEnumerable<PocoColumn> GetPocoColumns(IEnumerable<PocoMember> members)
        {
            foreach (var member in members)
            {
                yield return member.PocoColumn;
                foreach (var pocoMemberChild in GetPocoColumns(member.PocoMemberChildren))
                {
                    yield return pocoMemberChild;
                }
            }
        } 

        private IEnumerable<PocoMember> GetPocoMembers(Type type, TableInfo tableInfo, IMapper mapper, List<MemberInfo> memberInfos, string prefix = null)
        {
            var index = 0;
            foreach (var mi in ReflectionUtils.GetFieldsAndPropertiesForClasses(type))
            {
                var ci = GetColumnInfo(mi, memberInfos.ToArray());

                if (ci.IgnoreColumn)
                    continue;

                if (ci.ComplexMapping || mi.GetMemberInfoType().IsAClass())
                {
                    memberInfos.Add(mi);
                    var members = new List<MemberInfo>(memberInfos.ToArray());
                    var pocoMemberChildren = new List<PocoMember>();
                    foreach (var pocoMember in GetPocoMembers(mi.GetMemberInfoType(), tableInfo, mapper, memberInfos, GetNewPrefix(prefix, ci.ComplexPrefix ?? mi.Name).TrimStart('_')))
                    {
                        if (pocoMember.PocoColumn != null)
                        {
                            pocoMember.PocoColumn.MemberInfoChain = new List<MemberInfo>(members.Concat(new[] { pocoMember.MemberInfo }));
                        }
                        pocoMemberChildren.Add(pocoMember);
                    }
                    yield return new PocoMember()
                    {
                        MemberInfo = mi,
                        PocoColumn = null,
                        PocoMemberChildren = pocoMemberChildren
                    };
                     
                    continue;
                }

                var pc = new PocoColumn();
                pc.TableInfo = tableInfo;
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
                    PocoColumn = pc
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
