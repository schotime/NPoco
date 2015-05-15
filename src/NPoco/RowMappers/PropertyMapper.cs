using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace NPoco.RowMappers
{
    public class PropertyMapper : RowMapper
    {
        private List<GroupResult<PosName>> _groupedNames;
        private MapPlan _mapPlan;

        public class PosName
        {
            public int Pos { get; set; }
            public string Name { get; set; }
        }

        public override bool ShouldMap(PocoData pocoData)
        {
            return true;
        }

        public override void Init(IDataReader dataReader, PocoData pocoData)
        {
            _groupedNames = Enumerable.Range(0, dataReader.FieldCount)
                .Select(x => new PosName {Pos = x, Name = dataReader.GetName(x)})
                .GroupByMany(x => x.Name, "__")
                .ToList();

            _mapPlan = BuildMapPlan(dataReader, pocoData);
        }

        public override object Map(IDataReader dataReader, RowMapperContext context)
        {
            if (context.Instance == null)
            {
                context.Instance = context.PocoData.CreateObject();
            }

            _mapPlan(dataReader, context.Instance);
           
            return context.Instance;
        }

        public delegate bool MapPlan(IDataReader reader, object instance);

        public MapPlan BuildMapPlan(IDataReader dataReader, PocoData pocoData)
        {
            var plans = _groupedNames.SelectMany(x => BuildMapPlans(x, dataReader, pocoData)).ToArray();
            return (reader, instance) =>
            {
                foreach (MapPlan plan in plans)
                {
                    plan(reader, instance);
                }
                return true;
            };
        }

        public IEnumerable<MapPlan> BuildMapPlans(GroupResult<PosName> groupedName, IDataReader dataReader, PocoData pocoData)
        {
            var pocoColumn = FindPocoColumn(groupedName, pocoData);
            if (groupedName.SubItems.Any() && pocoColumn != null)
            {
                var memberInfoType = pocoColumn.MemberInfo.GetMemberInfoType();
                if (memberInfoType.IsClass && memberInfoType != typeof(string) && !memberInfoType.IsArray)
                {
                    var newPoco = pocoData.PocoDataFactory.ForType(memberInfoType);
                    var subPlans = groupedName.SubItems.SelectMany(x => BuildMapPlans(x, dataReader, newPoco)).ToArray();

                    yield return (reader, instance) =>
                    {
                        var newObject = newPoco.CreateObject();
                        var shouldSetNestedObject = false;
                        
                        foreach (var subPlan in subPlans)
                        {
                            shouldSetNestedObject |= subPlan(reader, newObject);
                        }

                        if (shouldSetNestedObject)
                        {
                            pocoColumn.SetValueFast(instance, newObject);
                        }
                        return false;
                    };
                }
            }
            else if (pocoColumn != null)
            {
                var destType = pocoColumn.MemberInfo.GetMemberInfoType();
                var converter = GetConverter(pocoData, pocoColumn, dataReader.GetFieldType(groupedName.Key.Pos), destType);
                yield return (reader, instance) => MapValue(groupedName.Key.Pos, reader, converter, instance, pocoColumn, destType);
            }
        }

        public static bool MapValue(int index, IDataReader reader, Func<object, object> converter, object instance, PocoColumn pocoColumn, Type destType)
        {
            if (!reader.IsDBNull(index))
            {
                var value = converter != null ? converter(reader.GetValue(index)) : reader.GetValue(index);
                pocoColumn.SetValueFast(instance, value);
                return true;
            }

            return false;
        }

        private static PocoColumn FindPocoColumn(GroupResult<PosName> groupedName, PocoData pocoData)
        {
            PocoColumn pocoColumn;
            MappingFactory.TryGetColumnByName(pocoData.Columns, groupedName.Item, out pocoColumn);
            return pocoColumn;
        }
    }
}