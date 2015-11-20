using System;
using System.Data;
using System.Data.Common;
using System.Reflection;

namespace NPoco.RowMappers
{
    public class ValueTypeMapper : RowMapper
    {
        private Func<object, object> _converter;

        public override bool ShouldMap(PocoData pocoData)
        {
            return pocoData.Type.GetTypeInfo().IsValueType || pocoData.Type == typeof (string) || pocoData.Type == typeof (byte[]);
        }

        public override void Init(DbDataReader dataReader, PocoData pocoData)
        {
            _converter = GetConverter(pocoData, null, dataReader.GetFieldType(0), pocoData.Type) ?? (x => x);
            base.Init(dataReader, pocoData);
        }

        public override object Map(DbDataReader dataReader, RowMapperContext context)
        {
            if (dataReader.IsDBNull(0))
                return null;

            return _converter(dataReader.GetValue(0));
        }
    }
}