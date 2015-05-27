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

        public IEnumerable<MapPlan> BuildMapPlans(GroupResult<PosName> groupedName, IDataReader dataReader, PocoData pocoData, List<PocoMember> pocoMembers)
        {
            // find pocomember by property name
            var pocoMember = pocoMembers
                .FirstOrDefault(x => x.Name.Equals(groupedName.Item.Replace("_", ""), StringComparison.InvariantCultureIgnoreCase)
                                     || (x.PocoColumn != null && string.Equals(x.PocoColumn.ColumnAlias, groupedName.Item.Replace("_", ""), StringComparison.InvariantCultureIgnoreCase)));

            if (pocoMember == null)
                yield break;

            if (groupedName.SubItems.Any())
            {
                var memberInfoType = pocoMember.MemberInfo.GetMemberInfoType();
                if (memberInfoType.IsAClass())
                {
                    var subPlans = groupedName.SubItems.SelectMany(x => BuildMapPlans(x, dataReader, pocoData, pocoMember.PocoMemberChildren)).ToArray();

                    yield return (reader, instance) =>
                    {
                        var newObject = pocoMember.Create();
                        var shouldSetNestedObject = false;
                        
                        foreach (var subPlan in subPlans)
                        {
                            shouldSetNestedObject |= subPlan(reader, newObject);
                        }

                        if (shouldSetNestedObject)
                        {
                            pocoMember.SetValue(instance, newObject);
                        }
                        return false;
                    };
                }
            }
            else
            {
                var destType = pocoMember.MemberInfo.GetMemberInfoType();
                var converter = GetConverter(pocoData, pocoMember.PocoColumn, dataReader.GetFieldType(groupedName.Key.Pos), destType);
                yield return (reader, instance) => MapValue(groupedName.Key.Pos, reader, converter, instance, pocoMember.PocoColumn);
            }
        }

        public static bool MapValue(int index, IDataReader reader, Func<object, object> converter, object instance, PocoColumn pocoColumn)
        {
            if (!reader.IsDBNull(index))
            {
                var value = converter != null ? converter(reader.GetValue(index)) : reader.GetValue(index);
                pocoColumn.SetValueFast(instance, value);
                return true;
            }

            return false;
        }
    }
}