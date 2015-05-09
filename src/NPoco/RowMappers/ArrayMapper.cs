using System;
using System.Data;

namespace NPoco.RowMappers
{
    public class ArrayMapper : RowMapper
    {
        public override bool ShouldMap(PocoData pocoData)
        {
            return pocoData.Type.IsArray;
        }

        public override object Map(IDataReader dataReader, RowMapperContext context)
        {
            var arrayType = context.Type.GetElementType();
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
    }
}