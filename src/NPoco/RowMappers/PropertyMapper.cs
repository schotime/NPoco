using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace NPoco.RowMappers
{
    public class PropertyMapper : RowMapper
    {
        private List<GroupResult<PosName>> _groupedNames;
        private MapPlan _mapPlan;
        private bool _mappingOntoExistingInstance;

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
            var fields = Enumerable.Range(0, dataReader.FieldCount)
                .Select(x => new PosName {Pos = x, Name = dataReader.GetName(x)})
                .ConvertFromConvention();
            
            _groupedNames = fields
                .GroupByMany(x => x.Name, PocoData.Separator)
                .ToList();

            _mapPlan = BuildMapPlan(dataReader, pocoData);
        }

        public override object Map(IDataReader dataReader, RowMapperContext context)
        {
            if (context.Instance == null)
            {
                context.Instance = context.PocoData.CreateObject();
                if (context.Instance == null)
                    throw new Exception(string.Format("Poco '{0}' has no parameterless constructor", context.Type.FullName));
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
            var pocoMember = FindMember(pocoMembers, groupedName);

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
                        var newObject = pocoMember.GetValue(instance) ?? pocoMember.Create();

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
                var defaultValue = MappingFactory.GetDefault(destType);
                var converter = GetConverter(pocoData, pocoMember.PocoColumn, dataReader.GetFieldType(groupedName.Key.Pos), destType);
                yield return (reader, instance) => MapValue(groupedName.Key.Pos, reader, converter, instance, pocoMember.PocoColumn, defaultValue);
            }
        }

        private static PocoMember FindMember(List<PocoMember> pocoMembers, GroupResult<PosName> groupedName)
        {
            return pocoMembers.FirstOrDefault(x => IsEqual(groupedName, x.Name)
                                                   || (x.PocoColumn != null && IsEqual(groupedName, x.PocoColumn.ColumnAlias)));
        }

        private static bool IsEqual(GroupResult<PosName> groupedName, string value)
        {
            return string.Equals(value, groupedName.Item, StringComparison.InvariantCultureIgnoreCase)
                || string.Equals(value, groupedName.Item.Replace("_", ""), StringComparison.InvariantCultureIgnoreCase);
        }

        private bool MapValue(int index, IDataReader reader, Func<object, object> converter, object instance, PocoColumn pocoColumn, object defaultValue)
        {
            if (!reader.IsDBNull(index))
            {
                var value = converter != null ? converter(reader.GetValue(index)) : reader.GetValue(index);
                pocoColumn.SetValue(instance, value);
                return true;
            }

            if (_mappingOntoExistingInstance && defaultValue == null)
            {
                pocoColumn.SetValue(instance, null);
            }

            return false;
        }
    }
}