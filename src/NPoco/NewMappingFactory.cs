using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace NPoco
{
    public class NewMappingFactory
    {
        public class PosName
        {
            public int Pos { get; set; }
            public string Name { get; set; }
        }

        private readonly PocoData _pocoData;
        private readonly List<GroupResult<PosName>> _groupedNames;

        public NewMappingFactory(PocoData pocoData, IDataReader dataReader)
        {
            _pocoData = pocoData;

            _groupedNames = Enumerable.Range(0, dataReader.FieldCount)
                .Select(x => new PosName {Pos = x, Name = dataReader.GetName(x)})
                .GroupByMany(x => x.Name, "__")
                .ToList();
        }

        public object Map(IDataReader dataReader, object instance)
        {
            if (_pocoData.type == typeof(object)
                || _pocoData.type == typeof(Dictionary<string, object>) 
                || _pocoData.type == typeof(IDictionary<string, object>))
            {
                return MapDictionaryTypes(dataReader);
            }

            if (_pocoData.type.IsValueType || _pocoData.type == typeof (string) || _pocoData.type == typeof (byte[]))
            {
                return MapValueTypes(dataReader);
            }
            
            if (_pocoData.type.IsArray)
            {
                return MapArray(dataReader);
            }

            return MapObject(dataReader, instance);
        }

        private object MapDictionaryTypes(IDataReader dataReader)
        {
            var target = _pocoData.type == typeof (object)
                ? (IDictionary<string, object>) new PocoExpando()
                : new Dictionary<string, object>();

            for (int i = 0; i < dataReader.FieldCount; i++)
            {
                var converter = _pocoData.Mapper != null ? _pocoData.Mapper.GetFromDbConverter(null, dataReader.GetFieldType(i)) : (x => x);
                target.Add(dataReader.GetName(i), dataReader.IsDBNull(i) ? null : converter(dataReader.GetValue(i)));
            }

            return target;
        }

        private object MapValueTypes(IDataReader dataReader)
        {
            if (dataReader.IsDBNull(0))
                return null;

            var convertedValue = GetConvertedValue(0, dataReader, _pocoData, _pocoData.type);
            return convertedValue;
        }

        private object MapArray(IDataReader dataReader)
        {
            var arrayType = _pocoData.type.GetElementType();
            var array = Array.CreateInstance(arrayType, dataReader.FieldCount);
            for (int i = 0; i < dataReader.FieldCount; i++)
            {
                if (!dataReader.IsDBNull(i))
                {
                    array.SetValue(dataReader.GetValue(i), i);
                }
            }
            return array;
        }

        private object MapObject(IDataReader dataReader, object instance)
        {
            if (instance == null)
            {
                instance = Activator.CreateInstance(_pocoData.type);
                //instance = _pocoData.CreateObject();
            }

            foreach (var groupedName in _groupedNames)
            {
                AssignFromDataReader(groupedName, dataReader, _pocoData, instance);
            }

            return instance;
        }

        public class AssignResult
        {
            public bool IsSet { get; set; }
        }

        private static AssignResult AssignFromDataReader(GroupResult<PosName> groupedName, IDataReader reader, PocoData pocoData, object instance)
        {
            var pocoColumn = FindPocoColumn(groupedName, pocoData);
            if (pocoColumn == null)
                return new AssignResult();

            if (groupedName.SubItems.Any())
            {
                var memberInfoType = pocoColumn.MemberInfo.GetMemberInfoType();
                if (memberInfoType.IsClass && memberInfoType != typeof(string) && memberInfoType != typeof(byte[]))
                {
                    var newPoco = pocoData.PocoDataFactory.ForType(memberInfoType);
                    var newObject = Activator.CreateInstance(memberInfoType);
                    //var newObject = newPoco.CreateObject();

                    var results = groupedName.SubItems.Select(x => AssignFromDataReader(x, reader, newPoco, newObject)).ToArray();
                    if (results.Any(x => x.IsSet))
                    {
                        pocoColumn.SetValue(instance, newObject);
                        //pocoColumn.SetValueFast(instance, newObject);
                    }
                }
                return new AssignResult();
            }
            
            return MapValue(groupedName, reader, pocoData, instance, pocoColumn);
        }

        private static AssignResult MapValue(GroupResult<PosName> groupedName, IDataReader reader, PocoData pocoData, object instance, PocoColumn pocoColumn)
        {
            var isDbNull = reader.IsDBNull(groupedName.Key.Pos);
            if (!isDbNull)
            {
                var convertedValue = GetConvertedValue(groupedName.Key.Pos, reader, pocoData, pocoColumn.MemberInfo.GetMemberInfoType());
                pocoColumn.SetValue(instance, convertedValue);
                //pocoColumn.SetValueFast(instance, convertedValue);

                return new AssignResult
                {
                    IsSet = true
                };
            }

            return new AssignResult();
        }

        private static object GetConvertedValue(int index, IDataReader reader, PocoData pocoData, Type desType)
        {
            var value = reader.GetValue(index);
            var converter = MappingFactory.GetConverter(pocoData.Mapper, null, reader.GetFieldType(index), desType);
            var convertedValue = converter != null ? converter(value) : value;
            return convertedValue;
        }

        private static PocoColumn FindPocoColumn(GroupResult<PosName> groupedName, PocoData pocoData)
        {
            PocoColumn pocoColumn;
            MappingFactory.TryGetColumnByName(pocoData.Columns, groupedName.Item, out pocoColumn);
            return pocoColumn;
        }
    }

    public class GroupResult<TKey>
    {
        public TKey Key { get; set; }
        public string Item { get; set; }
        public int Count { get; set; }
        public IEnumerable<GroupResult<TKey>> SubItems { get; set; }
        public override string ToString()
        {
            return string.Format("{0} ({1})", Item, Count);
        }
    }
    
    public static class MyEnumerableExtensions
    {
        public static IEnumerable<GroupResult<TKey>> GroupByMany<TKey>(this IEnumerable<TKey> elements, Func<TKey, string> stringFunc, string splitBy, int i = 0)
        {
            return elements
                .Select(x => new { Item = x, Parts = stringFunc(x).Split(new[] { splitBy }, StringSplitOptions.RemoveEmptyEntries) })
                .GroupBy(x => x.Parts.Skip(i).FirstOrDefault())
                .Where(x => x.Key != null)
                .Select(g => new GroupResult<TKey>
                {
                    Item = g.Key,
                    Key = g.Select(x => x.Item).First(),
                    Count = g.Count(),
                    SubItems = g.Select(x => x.Item).GroupByMany(stringFunc, splitBy, i + 1).ToList()
                });
        }
    }
}
