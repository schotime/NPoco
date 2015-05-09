using System;
using System.Data;

namespace NPoco.RowMappers
{
    public interface IRowMapper
    {
        bool ShouldMap(PocoData pocoData);
        object Map(IDataReader dataReader, RowMapperContext context);
        void Init(IDataReader dataReader);
    }

    public abstract class RowMapper : IRowMapper
    {
        public abstract bool ShouldMap(PocoData pocoData);

        public virtual void Init(IDataReader dataReader)
        {
            
        }

        public abstract object Map(IDataReader dataReader, RowMapperContext context);

        protected static object GetConvertedValue(IDataReader reader, int index, PocoData pocoData, Type desType)
        {
            var value = reader.GetValue(index);
            var converter = MappingFactory.GetConverter(pocoData.Mapper, null, reader.GetFieldType(index), desType);
            var convertedValue = converter != null ? converter(value) : value;
            return convertedValue;
        }
    }
}