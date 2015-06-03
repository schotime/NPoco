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
            ReferenceMappingType = ReferenceMappingType.None;
        }

        public string Name { get { return MemberInfo.Name; } }
        public MemberInfo MemberInfo { get; set; }
        public PocoColumn PocoColumn { get; set; }
        public List<PocoMember> PocoMemberChildren { get; set; }

        public ReferenceMappingType ReferenceMappingType { get; set; }
        public string ReferenceMemberName { get; set; }

        public bool IsList { get; set; }
        public Type ListType { get; set; }

        private FastCreate _creator;
        public object Create()
        {
            if (_creator == null)
                _creator = new FastCreate(MemberInfo.GetMemberInfoType());
            return _creator.Create();
        }

        private MemberAccessor _memberAccessor;
        public void SetValue(object target, object value)
        {
            if (_memberAccessor == null)
                _memberAccessor= new MemberAccessor(MemberInfo.DeclaringType, Name);
            _memberAccessor.Set(target, value);
        }
    }

    public class PocoData
    {
        public PocoDataFactory PocoDataFactory { get; protected set; }
        protected internal IMapper Mapper { get; set; }
        private readonly Cache<string, Type> aliasToType = Cache<string, Type>.CreateStaticCache();

        protected internal Type Type { get; protected set; }
        public KeyValuePair<string, PocoColumn>[] QueryColumns { get; protected set; }
        public TableInfo TableInfo { get; protected internal set; }
        public Dictionary<string, PocoColumn> Columns { get; protected internal set; }
        public List<PocoMember> Members { get; protected internal set; }
        public List<PocoColumn> AllColumns { get; protected internal set; }

        public PocoData()
        {
        }

        public PocoData(Type type, IMapper mapper, PocoDataFactory pocoDataFactory) : this()
        {
            PocoDataFactory = pocoDataFactory;
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
            QueryColumns = Columns.Where(c => !c.Value.ResultColumn && c.Value.ReferenceMappingType == ReferenceMappingType.None).ToArray();

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
                if (all || (member.ReferenceMappingType != ReferenceMappingType.OneToOne))
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

        private IEnumerable<PocoMember> GetPocoMembers(Type type, TableInfo tableInfo, IMapper mapper, List<MemberInfo> memberInfos, string prefix = null)
        {
            var capturedMembers = memberInfos.ToArray();
            var capturedPrefix = prefix;
            var capturedTableInfo = tableInfo;
            foreach (var mi in ReflectionUtils.GetFieldsAndPropertiesForClasses(type))
            {
                var ci = GetColumnInfo(mi, memberInfos.ToArray());

                if (ci.IgnoreColumn)
                    continue;

                var pocoMemberChildren = new List<PocoMember>();
                
                if (ci.ComplexMapping || ci.ReferenceMappingType != ReferenceMappingType.None)
                {
                    var members = new List<MemberInfo>();
                    members.AddRange(capturedMembers);
                    members.Add(mi);

                    if (capturedMembers.GroupBy(x => x.GetMemberInfoType()).Any(x => x.Count() >= 2))
                    {
                        continue;
                    }

                    var newTableInfo = capturedTableInfo;
                    if (ci.ReferenceMappingType != ReferenceMappingType.None)
                    {
                        newTableInfo = GetTableInfo(mi.GetMemberInfoType());
                    }

                    var newPrefix = GetNewPrefix(capturedPrefix, ci.ReferenceMappingType != ReferenceMappingType.None ? "" : (ci.ComplexPrefix ?? mi.Name));

                    foreach (var pocoMember in GetPocoMembers(mi.GetMemberInfoType(), newTableInfo, mapper, members, newPrefix))
                    {
                        if (pocoMember.PocoColumn != null)
                        {
                            pocoMember.PocoColumn.MemberInfoChain = new List<MemberInfo>(members.Concat(new[] { pocoMember.MemberInfo }));
                        }

                        pocoMemberChildren.Add(pocoMember);
                    }
                }

                var pc = new PocoColumn();
                pc.ReferenceMappingType = ci.ReferenceMappingType;
                pc.TableInfo = capturedTableInfo;
                pc.MemberInfo = mi;
                pc.MemberInfoChain = new[] {mi}.ToList();
                pc.ColumnName = GetColumnName(capturedPrefix, ci.ColumnName);
                pc.ResultColumn = ci.ResultColumn;
                pc.ForceToUtc = ci.ForceToUtc;
                pc.ComputedColumn = ci.ComputedColumn;
                pc.ColumnType = ci.ColumnType;
                pc.ColumnAlias = ci.ColumnAlias;
                pc.VersionColumn = ci.VersionColumn;
                pc.VersionColumnType = ci.VersionColumnType;

                if (mapper != null && !mapper.MapMemberToColumn(mi, ref pc.ColumnName, ref pc.ResultColumn))
                    continue;

                yield return new PocoMember()
                {
                    MemberInfo = mi,
                    PocoColumn = ci.ComplexMapping ? null : pc,
                    ReferenceMappingType = ci.ReferenceMappingType,
                    ReferenceMemberName = ci.ReferenceMemberName,
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
            var list = new List<string>();
            if (!string.IsNullOrWhiteSpace(prefix))
                list.Add(prefix);
            if (!string.IsNullOrWhiteSpace(end))
                list.Add(end);
            return string.Join("__", list);
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
