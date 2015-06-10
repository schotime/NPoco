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

            for (int i = 0; i < dataReader.FieldCount; i++)
            {
                var converter = context.PocoData.Mapper != null ? context.PocoData.Mapper.GetFromDbConverter(null, dataReader.GetFieldType(i)) : (x => x);
                target.Add(dataReader.GetName(i), dataReader.IsDBNull(i) ? null : converter(dataReader.GetValue(i)));
            }

            return target;
        }
    }
}