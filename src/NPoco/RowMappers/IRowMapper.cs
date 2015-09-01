using System;
using System.Data;
using System.Linq;

namespace NPoco.RowMappers
{
    public interface IRowMapper
    {
        bool ShouldMap(PocoData pocoData);
        object Map(IDataReader dataReader, RowMapperContext context);
        void Init(IDataReader dataReader, PocoData pocoData);
    }

    public abstract class RowMapper : IRowMapper
    {
        public abstract bool ShouldMap(PocoData pocoData);

        public virtual void Init(IDataReader dataReader, PocoData pocoData)
        {
        }

        private PosName[] _columnNames;
        protected PosName[] GetColumnNames(IDataReader dataReader)
        {
            return _columnNames ?? (_columnNames = Enumerable.Range(0, dataReader.FieldCount)
                .Select(x => new PosName {Pos = x, Name = dataReader.GetName(x)})
                .Where(x => !string.Equals("poco_rn", x.Name))
                .ConvertFromConvention()
                .ToArray());
        }

        public abstract object Map(IDataReader dataReader, RowMapperContext context);

        public static Func<object, object> GetConverter(PocoData pocoData, PocoColumn pocoColumn, Type sourceType, Type desType)
        {
            var converter = MappingFactory.GetConverter(pocoData.Mapper, pocoColumn, sourceType, desType);
            return converter;
        }
    }
}