using System;
using System.Collections.Generic;
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

        protected PosName[] GetColumnNames(IDataReader dataReader, PocoData pocoData)
        {
            if (_columnNames != null)
                return _columnNames;

            var cols = Enumerable.Range(0, dataReader.FieldCount)
                .Select(x => new PosName { Pos = x, Name = dataReader.GetName(x) })
                .Where(x => !string.Equals("poco_rn", x.Name))
                .ToList();

            if (cols.Any(x => x.Name.StartsWith(PropertyMapperNameConvention.SplitPrefix)))
            {
                return (_columnNames = cols.ConvertFromNewConvention().ToArray());
            }

            return (_columnNames = cols.ConvertFromOldConvention(pocoData.Members).ToArray());
        }

        public abstract object Map(IDataReader dataReader, RowMapperContext context);

        public static Func<object, object> GetConverter(PocoData pocoData, PocoColumn pocoColumn, Type sourceType, Type desType)
        {
            var converter = MappingHelper.GetConverter(pocoData.Mapper, pocoColumn, sourceType, desType);
            return converter;
        }
    }
}