using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace NPoco.RowMappers
{
    public class DictionaryMapper : RowMapper
    {
        private PosName[] _posNames;

        public override bool ShouldMap(PocoData pocoData)
        {
            return pocoData.Type == typeof (object)
                   || pocoData.Type == typeof (Dictionary<string, object>)
                   || pocoData.Type == typeof (IDictionary<string, object>);
        }

        public override void Init(DbDataReader dataReader, PocoData pocoData)
        {
            _posNames = GetColumnNames(dataReader, pocoData);
        }

        public override object Map(DbDataReader dataReader, RowMapperContext context)
        {
            IDictionary<string, object> target = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

#if !NET35
            if (context.Type == typeof(object))
                target = new PocoExpando();
#endif

            for (int i = 0; i < _posNames.Length; i++)
            {
                var converter = context.PocoData.Mapper.Find(x => x.GetFromDbConverter(typeof(object), dataReader.GetFieldType(_posNames[i].Pos))) ?? (x => x);
                target.Add(_posNames[i].Name, dataReader.IsDBNull(_posNames[i].Pos) ? null : converter(dataReader.GetValue(_posNames[i].Pos)));
            }

            return target;
        }
    }
}