using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace NPoco.RowMappers
{
    public class PropertyMapper : RowMapper
    {
        private List<GroupResult<PosName>> _groupedNames;
        private MapPlan _mapPlan;
        private bool _mappingOntoExistingInstance;

        public override bool ShouldMap(PocoData pocoData)
        {
            return true;
        }

        public override void Init(IDataReader dataReader, PocoData pocoData)
        {
            var fields = GetColumnNames(dataReader, pocoData);

            _groupedNames = fields
                .GroupByMany(x => x.Name, PocoData.Separator)
                .ToList();

            _mapPlan = BuildMapPlan(dataReader, pocoData);
        }

        public override object Map(IDataReader dataReader, RowMapperContext context)
        {
            if (context.Instance == null)
            {
                context.Instance = context.PocoData.CreateObject(dataReader);
                if (context.Instance == null)
                    throw new Exception(string.Format("Cannot create POCO '{0}'. It may have no parameterless constructor or be an interface or abstract class without a Mapper factory.", context.Type.FullName));
            }
            else
            {
                _mappingOntoExistingInstance = true;
            }

            _mapPlan(dataReader, context.Instance);

            var result = context.Instance as IOnLoaded;
            if (result != null)
            {
                result.OnLoaded();
            }

            return context.Instance;
        }

        public delegate bool MapPlan(IDataReader reader, object instance);

        private MapPlan BuildMapPlan(IDataReader dataReader, PocoData pocoData)
        {
            var plans = _groupedNames.SelectMany(x => BuildMapPlans(x, dataReader, pocoData, pocoData.Members)).ToArray();
            return (reader, instance) =>
            {
                foreach (MapPlan plan in plans)
                {
                    plan(reader, instance);
                }
                return true;
            };
        }

        private IEnumerable<MapPlan> BuildMapPlans(GroupResult<PosName> groupedName, IDataReader dataReader, PocoData pocoData, List<PocoMember> pocoMembers)
        {
            // find pocomember by property name
            var pocoMember = FindMember(pocoMembers, groupedName.Item);

            if (pocoMember == null)
            {
                yield break;
            }

            if (groupedName.SubItems.Any())
            {
                var memberInfoType = pocoMember.MemberInfo.GetMemberInfoType();
                if (memberInfoType.IsAClass() || pocoMember.IsDynamic)
                {
                    var children = PocoDataBuilder.IsDictionaryType(memberInfoType)
                        ? CreateDynamicDictionaryPocoMembers(groupedName.SubItems, pocoData)
                        : pocoMember.PocoMemberChildren;

                    var subPlans = groupedName.SubItems.SelectMany(x => BuildMapPlans(x, dataReader, pocoData, children)).ToArray();

                    yield return (reader, instance) =>
                    {
                        var newObject = pocoMember.IsList ? pocoMember.Create(dataReader) : (pocoMember.GetValue(instance) ?? pocoMember.Create(dataReader));

                        var shouldSetNestedObject = false;
                        foreach (var subPlan in subPlans)
                        {
                            shouldSetNestedObject |= subPlan(reader, newObject);
                        }

                        if (shouldSetNestedObject)
                        {
                            if (pocoMember.IsList)
                            {
                                var list = pocoMember.CreateList();
                                list.Add(newObject);
                                newObject = list;
                            }

                            pocoMember.SetValue(instance, newObject);
                            return true;
                        }
                        return false;
                    };
                }
            }
            else
            {
                var destType = pocoMember.MemberInfo.GetMemberInfoType();
                var defaultValue = MappingHelper.GetDefault(destType);
                var converter = GetConverter(pocoData, pocoMember.PocoColumn, dataReader.GetFieldType(groupedName.Key.Pos), destType);
                yield return (reader, instance) => MapValue(groupedName, reader, converter, instance, pocoMember.PocoColumn, defaultValue);
            }
        }

        public static PocoMember FindMember(List<PocoMember> pocoMembers, string name)
        {
            return pocoMembers.FirstOrDefault(x => IsEqual(name, x.Name)
                                                   || (x.PocoColumn != null && IsEqual(name, x.PocoColumn.ColumnAlias)));
        }

        private static bool IsEqual(string name, string value)
        {
            return string.Equals(value, name, StringComparison.InvariantCultureIgnoreCase)
                || string.Equals(value, name.Replace("_", ""), StringComparison.InvariantCultureIgnoreCase);
        }

        private bool MapValue(GroupResult<PosName> posName, IDataReader reader, Func<object, object> converter, object instance, PocoColumn pocoColumn, object defaultValue)
        {
            if (!reader.IsDBNull(posName.Key.Pos))
            {
                var value = converter != null ? converter(reader.GetValue(posName.Key.Pos)) : reader.GetValue(posName.Key.Pos);
                pocoColumn.SetValue(instance, value);
                return true;
            }

            if (_mappingOntoExistingInstance && defaultValue == null)
            {
                pocoColumn.SetValue(instance, null);
            }

            return false;
        }

        private static List<PocoMember> CreateDynamicDictionaryPocoMembers(IEnumerable<GroupResult<PosName>> subItems, PocoData pocoData)
        {
            return subItems.Select(x => new DynamicPocoMember(pocoData.Mapper)
            {
                MemberInfo = new DynamicMember(x.Item),
                PocoColumn = new ExpandoColumn
                {
                    ColumnName = x.Item
                }
            }).Cast<PocoMember>().ToList();
        }
    }
}