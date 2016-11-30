using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;

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

        public override void Init(DbDataReader dataReader, PocoData pocoData)
        {
            var fields = GetColumnNames(dataReader, pocoData);

            _groupedNames = fields
                .GroupByMany(x => x.Name, PocoData.Separator)
                .ToList();

            _mapPlan = BuildMapPlan(dataReader, pocoData);
        }

        public override object Map(DbDataReader dataReader, RowMapperContext context)
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

            object[] values = new object[dataReader.FieldCount];
            dataReader.GetValues(values);
            _mapPlan(dataReader, values, context.Instance);

            var result = context.Instance as IOnLoaded;
            if (result != null)
            {
                result.OnLoaded();
            }

            return context.Instance;
        }

        public delegate bool MapPlan(DbDataReader dataReader, object[] values, object instance);

        private MapPlan BuildMapPlan(DbDataReader dataReader, PocoData pocoData)
        {
            var plans = _groupedNames.SelectMany(x => BuildMapPlans(x, dataReader, pocoData, pocoData.Members)).ToArray();
            return (reader, values, instance) =>
            {
                foreach (MapPlan plan in plans)
                {
                    plan(reader, values, instance);
                }
                return true;
            };
        }

        private IEnumerable<MapPlan> BuildMapPlans(GroupResult<PosName> groupedName, DbDataReader dataReader, PocoData pocoData, List<PocoMember> pocoMembers)
        {
            // find pocomember by property name
            var pocoMember = pocoMembers.FirstOrDefault(x => IsEqual(groupedName.Item, x.Name));

            if (pocoMember == null)
            {
                yield break;
            }

            if (groupedName.SubItems.Any())
            {
                var memberInfoType = pocoMember.MemberInfoData.MemberType;
                if (memberInfoType.IsAClass() || pocoMember.IsDynamic)
                {
                    var children = PocoDataBuilder.IsDictionaryType(memberInfoType)
                        ? CreateDynamicDictionaryPocoMembers(groupedName.SubItems, pocoData)
                        : pocoMember.PocoMemberChildren;

                    var subPlans = groupedName.SubItems.SelectMany(x => BuildMapPlans(x, dataReader, pocoData, children)).ToArray();

                    yield return (reader, values, instance) =>
                    {
                        var newObject = pocoMember.IsList ? pocoMember.Create(dataReader) : (pocoMember.GetValue(instance) ?? pocoMember.Create(dataReader));

                        var shouldSetNestedObject = false;
                        foreach (var subPlan in subPlans)
                        {
                            shouldSetNestedObject |= subPlan(reader, values, newObject);
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
                var destType = pocoMember.MemberInfoData.MemberType;
                var defaultValue = MappingHelper.GetDefault(destType);
                var converter = GetConverter(pocoData, pocoMember.PocoColumn, dataReader.GetFieldType(groupedName.Key.Pos), destType);
                yield return (reader, values, instance) => MapValue(groupedName, values, converter, instance, pocoMember.PocoColumn, defaultValue);
            }
        }

        public static bool IsEqual(string name, string value)
        {
            return string.Equals(value, name, StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, name.Replace("_", ""), StringComparison.OrdinalIgnoreCase);
        }

        private bool MapValue(GroupResult<PosName> posName, object[] values, Func<object, object> converter, object instance, PocoColumn pocoColumn, object defaultValue)
        {
            var value = values[posName.Key.Pos];
            if (!Equals(value, DBNull.Value))
            {
                pocoColumn.SetValue(instance, converter != null ? converter(value) : value);
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
                MemberInfoData = new MemberInfoData(x.Item, typeof(object), typeof(IDictionary<string, object>)),
                PocoColumn = new ExpandoColumn
                {
                    ColumnName = x.Item
                }
            }).Cast<PocoMember>().ToList();
        }
    }
}