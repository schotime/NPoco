using System;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace NPoco.RowMappers
{
    public class ArrayMapper : RowMapper
    {
        private PosName[] _posNames;

        public override bool ShouldMap(PocoData pocoData)
        {
            return pocoData.Type.IsArray;
        }

        public override void Init(DbDataReader dataReader, PocoData pocoData)
        {
            _posNames = GetColumnNames(dataReader, pocoData);
        }

        public override object Map(DbDataReader dataReader, RowMapperContext context)
        {
            var arrayType = context.Type.GetElementType();
            var array = Array.CreateInstance(arrayType, _posNames.Length);

            for (int i = 0; i < _posNames.Length; i++)
            {
                if (!dataReader.IsDBNull(_posNames[i].Pos))
                {
                    array.SetValue(dataReader.GetValue(_posNames[i].Pos), i);
                }
            }

            return array;
        }
    }
}