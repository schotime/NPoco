using System.Collections.Generic;
using System.Data;

namespace NPoco.RowMappers
{
    public class DictionaryMapper : RowMapper
    {
        public override bool ShouldMap(PocoData pocoData)
        {
            return pocoData.Type == typeof (object)
                   || pocoData.Type == typeof (Dictionary<string, object>)
                   || pocoData.Type == typeof (IDictionary<string, object>);
        }

        public override object Map(IDataReader dataReader, RowMapperContext context)
        {
            IDictionary<string, object> target = new Dictionary<string, object>();

#if !POCO_NO_DYNAMIC
            if (context.Type == typeof(object))
                target = new PocoExpando();
#endif

            var columnNames = GetColumnNames(dataReader, context.PocoData);

            for (int i = 0; i < columnNames.Length; i++)
            {
                var converter = context.PocoData.Mapper.Find(x => x.GetFromDbConverter(typeof(object), dataReader.GetFieldType(columnNames[i].Pos))) ?? (x => x);
                target.Add(columnNames[i].Name, dataReader.IsDBNull(columnNames[i].Pos) ? null : converter(dataReader.GetValue(columnNames[i].Pos)));
            }

            return target;
        }
    }
}