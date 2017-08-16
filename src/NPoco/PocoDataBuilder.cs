using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NPoco.FluentMappings;
using NPoco.RowMappers;

namespace NPoco
{
    public interface InitializedPocoDataBuilder
    {
        TableInfo BuildTableInfo();
        PocoData Build();
    }

    public class PocoDataBuilder : InitializedPocoDataBuilder
    {
        private readonly Cache<string, Type> _aliasToType = Cache<string, Type>.CreateStaticCache();

        protected Type Type { get; set; }
        private MapperCollection Mapper { get; set; }

        private List<PocoMemberPlan> _memberPlans { get; set; }
        private TableInfoPlan _tableInfoPlan { get; set; }

        private delegate PocoMember PocoMemberPlan(TableInfo tableInfo);
        protected delegate TableInfo TableInfoPlan();

        public PocoDataBuilder(Type type, MapperCollection mapper)
        {
            Type = type;
            Mapper = mapper;
        }

        public InitializedPocoDataBuilder Init()
        {
            var memberInfos = new List<MemberInfo>();
            var columnInfos = GetColumnInfos(Type);

            // Get table info plan
            _tableInfoPlan = GetTableInfo(Type, columnInfos, memberInfos);

            // Get pocomember plan
            _memberPlans = GetPocoMembers(Mapper, columnInfos, memberInfos).ToList();

            return this;
        }

        private ColumnInfo[] GetColumnInfos(Type type)
        {
            return ReflectionUtils.GetFieldsAndPropertiesForClasses(type)
                .Where(x => !IsDictionaryType(x.DeclaringType))
                .Select(x => GetColumnInfo(x, type)).ToArray();
        }

        public static bool IsDictionaryType(Type type)
        {
            return new[] { typeof(object), typeof(IDictionary<string, object>), typeof(Dictionary<string, object>) }.Contains(type);
        }

        TableInfo InitializedPocoDataBuilder.BuildTableInfo()
        {
            return _tableInfoPlan();
        }

        PocoData InitializedPocoDataBuilder.Build()
        {
            var pocoData = new PocoData(Type, Mapper);

            pocoData.TableInfo = _tableInfoPlan();

            pocoData.Members = _memberPlans.Select(plan => plan(pocoData.TableInfo)).ToList();

            pocoData.Columns = GetPocoColumns(pocoData.Members, false).Where(x => x != null).ToDictionary(x => x.ColumnName, x => x, StringComparer.OrdinalIgnoreCase);
            pocoData.AllColumns = GetPocoColumns(pocoData.Members, true).Where(x => x != null).ToList();

            //Build column list for automatic select
            pocoData.QueryColumns = pocoData.Columns.Where(c => !c.Value.ResultColumn && c.Value.ReferenceType == ReferenceType.None).ToArray();

            return pocoData;
        }

        protected virtual TableInfoPlan GetTableInfo(Type type, ColumnInfo[] columnInfos, List<MemberInfo> memberInfos)
        {
            var alias = CreateAlias(type.Name, type);
            var tableInfo = TableInfo.FromPoco(type);
            tableInfo.AutoAlias = alias;
            return () => { return tableInfo.Clone(); };
        }

        protected virtual ColumnInfo GetColumnInfo(MemberInfo mi, Type type)
        {
            return ColumnInfo.FromMemberInfo(mi);
        }

        private static IEnumerable<PocoColumn> GetPocoColumns(IEnumerable<PocoMember> members, bool all)
        {
            foreach (var member in members)
            {
                if (all || (member.ReferenceType != ReferenceType.OneToOne
                            && member.ReferenceType != ReferenceType.Many))
                {
                    yield return member.PocoColumn;
                }

                if (all || (member.ReferenceType == ReferenceType.None))
                {
                    foreach (var pocoMemberChild in GetPocoColumns(member.PocoMemberChildren, all))
                    {
                        yield return pocoMemberChild;
                    }
                }
            }
        }

        private IEnumerable<PocoMemberPlan> GetPocoMembers(MapperCollection mapper, ColumnInfo[] columnInfos, List<MemberInfo> memberInfos, string prefix = null)
        {
            var capturedMembers = memberInfos.ToArray();
            var capturedPrefix = prefix;
            foreach (var columnInfo in columnInfos)
            {
                if (columnInfo.IgnoreColumn)
                    continue;

                var memberInfoType = columnInfo.MemberInfo.GetMemberInfoType();
                if (columnInfo.ReferenceType == ReferenceType.Many)
                {
                    var genericArguments = memberInfoType.GetGenericArguments();
                    memberInfoType = genericArguments.Any() 
                        ? genericArguments.First() 
                        : memberInfoType.GetTypeWithGenericTypeDefinitionOf(typeof(IList<>)).GetGenericArguments().First();
                }

                var childrenPlans = new PocoMemberPlan[0];
                TableInfoPlan childTableInfoPlan = null;
                var members = new List<MemberInfo>(capturedMembers) { columnInfo.MemberInfo };

                if (columnInfo.ComplexMapping || columnInfo.ReferenceType != ReferenceType.None)
                {
                    if (capturedMembers.GroupBy(x => x.GetMemberInfoType()).Any(x => x.Count() >= 2))
                    {
                        continue;
                    }

                    var childColumnInfos = GetColumnInfos(memberInfoType);

                    if (columnInfo.ReferenceType != ReferenceType.None)
                    {
                        childTableInfoPlan = GetTableInfo(memberInfoType, childColumnInfos, members);
                    }

                    var newPrefix = JoinStrings(capturedPrefix, columnInfo.ReferenceType != ReferenceType.None ? "" : (columnInfo.ComplexPrefix ?? columnInfo.MemberInfo.Name));

                    childrenPlans = GetPocoMembers(mapper, childColumnInfos, members, newPrefix).ToArray();
                }

                MemberInfo capturedMemberInfo = columnInfo.MemberInfo;
                ColumnInfo capturedColumnInfo = columnInfo;

                var accessors = GetMemberAccessors(members);
                var memberType = capturedMemberInfo.GetMemberInfoType();
                var isList = IsList(capturedMemberInfo);
                var listType = GetListType(memberType, isList);
                var isDynamic = capturedMemberInfo.IsDynamic();
                var fastCreate = GetFastCreate(memberType, mapper, isList, isDynamic);
                var columnName = GetColumnName(capturedPrefix, capturedColumnInfo.ColumnName ?? capturedMemberInfo.Name);
                var memberInfoData = new MemberInfoData(capturedMemberInfo);
                
                yield return tableInfo =>
                {
                    var pc = new PocoColumn
                    {
                        ReferenceType = capturedColumnInfo.ReferenceType,
                        TableInfo = tableInfo,
                        MemberInfoData = memberInfoData,
                        MemberInfoChain = members,
                        ColumnName = columnName,
                        ResultColumn = capturedColumnInfo.ResultColumn,
                        ForceToUtc = capturedColumnInfo.ForceToUtc,
                        ComputedColumn = capturedColumnInfo.ComputedColumn,
                        ComputedColumnType = capturedColumnInfo.ComputedColumnType,
                        ColumnType = capturedColumnInfo.ColumnType,
                        ColumnAlias = capturedColumnInfo.ColumnAlias,
                        VersionColumn = capturedColumnInfo.VersionColumn,
                        VersionColumnType = capturedColumnInfo.VersionColumnType,
                        SerializedColumn = capturedColumnInfo.SerializedColumn,
                        ValueObjectColumn = capturedColumnInfo.ValueObjectColumn,
                    };

                    if (pc.ValueObjectColumn)
                    {
                        SetupValueObject(pc, fastCreate);
                    }

                    pc.SetMemberAccessors(accessors);

                    var childrenTableInfo = childTableInfoPlan == null ? tableInfo : childTableInfoPlan();
                    var children = childrenPlans.Select(plan => plan(childrenTableInfo)).ToList();

                    // Cascade ResultColumn down
                    foreach (var child in children.Where(child => child.PocoColumn != null && pc.ResultColumn))
                    {
                        child.PocoColumn.ResultColumn = true;
                    }

                    var pocoMember = new PocoMember()
                    {
                        MemberInfoData = memberInfoData,
                        MemberInfoChain = members,
                        IsList = isList,
                        IsDynamic = isDynamic,
                        PocoColumn = capturedColumnInfo.ComplexMapping ? null : pc,
                        ReferenceType = capturedColumnInfo.ReferenceType,
                        ReferenceMemberName = capturedColumnInfo.ReferenceMemberName,
                        PocoMemberChildren = children,
                    };

                    pocoMember.SetMemberAccessor(accessors[accessors.Count - 1], fastCreate, listType);

                    return pocoMember;
                };
            }
        }

        private static void SetupValueObject(PocoColumn pc, FastCreate fastCreate)
        {
            var memberName = "Value";
            var hasIValueObject = pc.MemberInfoData.MemberType.GetTypeWithGenericTypeDefinitionOf(typeof(IValueObject<>));
            MemberInfo property = string.IsNullOrEmpty(pc.ValueObjectColumnName)
                ? pc.MemberInfoData.MemberType.GetProperties().FirstOrDefault(x => x.Name.IndexOf(memberName, StringComparison.OrdinalIgnoreCase) >= 0)
                  ?? pc.MemberInfoData.MemberType.GetProperties().First()
                : ReflectionUtils.GetFieldsAndProperties(pc.MemberInfoData.MemberType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).First(x => x.Name == pc.ValueObjectColumnName);
            var type = hasIValueObject != null ? hasIValueObject.GetGenericArguments().First() : property.GetMemberInfoType();
            var memberAccessor = hasIValueObject != null ? new MemberAccessor(typeof(IValueObject<>).MakeGenericType(type), memberName) : new MemberAccessor(pc.MemberInfoData.MemberType, property.Name);
            pc.SetValueObjectAccessors(fastCreate, (target, value) => memberAccessor.Set(target, value), target => memberAccessor.Get(target));
            pc.ColumnType = type;
        }

        private static FastCreate GetFastCreate(Type memberType, MapperCollection mapperCollection, bool isList, bool isDynamic)
        {
            return memberType.IsAClass() || isDynamic
                       ? (new FastCreate(isList
                            ? (memberType.GetGenericArguments().Any() ? memberType.GetGenericArguments().First() : memberType.GetTypeWithGenericTypeDefinitionOf(typeof(IList<>)).GetGenericArguments().First())
                            : memberType, mapperCollection))
                       : null;
        }

        private static Type GetListType(Type memberType, bool isList)
        {
            return isList
                ? (memberType.GetGenericArguments().Length > 0
                    ? typeof(List<>).MakeGenericType(memberType.GetGenericArguments().First())
                    : memberType)
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
            return mi.GetMemberInfoType().IsOfGenericType(typeof(IList<>)) && !mi.GetMemberInfoType().IsArray;
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
            return string.Join(PocoData.Separator, list.ToArray());
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