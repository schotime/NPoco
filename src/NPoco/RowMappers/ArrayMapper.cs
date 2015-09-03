using System;
using System.Data;
using System.Linq;

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
            var columnNames = GetColumnNames(dataReader, context.PocoData);

            var array = Array.CreateInstance(arrayType, columnNames.Length);

            for (int i = 0; i < columnNames.Length; i++)
            {
                if (!dataReader.IsDBNull(columnNames[i].Pos))
                {
                    array.SetValue(dataReader.GetValue(columnNames[i].Pos), i);
                }
            }

            return array;
        }
    }
}