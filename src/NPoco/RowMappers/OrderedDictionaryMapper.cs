using System;
using System.Collections.Specialized;
using System.Data.Common;

namespace NPoco.RowMappers;

public class OrderedDictionaryMapper : RowMapper
{
    private PosName[] _posNames;

    public override bool ShouldMap(PocoData pocoData)
    {
        return pocoData.Type == typeof(OrderedDictionary);
    }

    public override void Init(DbDataReader dataReader, PocoData pocoData)
    {
        _posNames = GetColumnNames(dataReader, pocoData);
    }

    public override object Map(DbDataReader dataReader, RowMapperContext context)
    {
        var target = new OrderedDictionary(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < _posNames.Length; i++)
        {
            var converter = context.PocoData.Mapper.FindFromDbConverter(typeof(object), dataReader.GetFieldType(_posNames[i].Pos)) ?? (x => x);
            target.Add(_posNames[i].Name, dataReader.IsDBNull(_posNames[i].Pos) ? null : converter(dataReader.GetValue(_posNames[i].Pos)));
        }

        return target;
    }
}