using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace NPoco.RowMappers
{
    public class PropertyMapper : RowMapper
    {
        private List<GroupResult<PosName>> _groupedNames;
        private Lazy<MapPlan> _mapPlan;

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
              .Select(x => new PosName { Pos = x, Name = dataReader.GetName(x) })
              .GroupByMany(x => x.Name, "__")
              .ToList();

            _mapPlan = new Lazy<MapPlan>(() => BuildMapPlan(pocoData));
        }

        public override object Map(IDataReader dataReader, RowMapperContext context)
        {
            if (context.Instance == null)
            {
                context.Instance = Activator.CreateInstance(context.Type);
                //instance = _pocoData.CreateObject();
            }

            _mapPlan.Value(dataReader, context.Instance);
           
            return context.Instance;
        }

        public class AssignResult
        {
            public bool IsSet { get; set; }
        }

        public delegate AssignResult MapPlan(IDataReader reader, object instance);

        public MapPlan BuildMapPlan(PocoData pocoData)
        {
            var plans = _groupedNames.SelectMany(x => BuildMapPlans(x, pocoData)).ToArray();
            return (reader, instance) => plans.Select(x => x(reader, instance)).LastOrDefault();
        }

        public IEnumerable<MapPlan> BuildMapPlans(GroupResult<PosName> groupedName, PocoData pocoData)
        {
            var pocoColumn = FindPocoColumn(groupedName, pocoData);
            if (groupedName.SubItems.Any() && pocoColumn != null)
            {
                var memberInfoType = pocoColumn.MemberInfo.GetMemberInfoType();
                if (memberInfoType.IsClass && memberInfoType != typeof(string) && memberInfoType != typeof(byte[]))
                {
                    var newPoco = pocoData.PocoDataFactory.ForType(memberInfoType);
                    //var newObject = newPoco.CreateObject();
                    var subPlans = groupedName.SubItems.SelectMany(x => BuildMapPlans(x, newPoco)).ToArray();

                    yield return (reader, instance) =>
                    {
                        var newObject = Activator.CreateInstance(memberInfoType);
                        var results = subPlans.Select(x => x(reader, newObject)).ToArray();
                        
                        if (results.Any(x => x.IsSet))
                           pocoColumn.SetValue(instance, newObject);
                        return new AssignResult();
                    };
                }
            }
            else if (pocoColumn != null)
            {
                yield return (reader, instance)=> MapValue(groupedName, reader, pocoData, instance, pocoColumn);
            }
        }

        private static AssignResult MapValue(GroupResult<PosName> groupedName, IDataReader reader, PocoData pocoData, object instance, PocoColumn pocoColumn)
        {
            var isDbNull = reader.IsDBNull(groupedName.Key.Pos);
            if (!isDbNull)
            {
                var convertedValue = GetConvertedValue(reader, groupedName.Key.Pos, pocoData, pocoColumn.MemberInfo.GetMemberInfoType());
                pocoColumn.SetValue(instance, convertedValue);
                //pocoColumn.SetValueFast(instance, convertedValue);

                return new AssignResult
                {
                    IsSet = true
                };
            }

            return new AssignResult();
        }

        private static PocoColumn FindPocoColumn(GroupResult<PosName> groupedName, PocoData pocoData)
        {
            PocoColumn pocoColumn;
            MappingFactory.TryGetColumnByName(pocoData.Columns, groupedName.Item, out pocoColumn);
            return pocoColumn;
        }
    }
}