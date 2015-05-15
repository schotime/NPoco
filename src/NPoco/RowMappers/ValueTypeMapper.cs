using System.Data;

namespace NPoco.RowMappers
{
    public class ValueTypeMapper : RowMapper
    {
        public override bool ShouldMap(PocoData pocoData)
        {
            return pocoData.Type.IsValueType || pocoData.Type == typeof (string) || pocoData.Type == typeof (byte[]);
        }

        public override object Map(IDataReader dataReader, RowMapperContext context)
        {
            if (dataReader.IsDBNull(0))
                return null;

            var converter = GetConverter(context.PocoData, null, dataReader.GetFieldType(0), context.Type) ?? (x=>x);
            return converter(dataReader.GetValue(0));
        }
    }
}