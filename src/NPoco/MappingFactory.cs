using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using NPoco.RowMappers;

namespace NPoco
{
    public class MappingFactory
    {
        public static List<Func<MapperCollection, IRowMapper>> RowMappers { get; private set; } 
        private readonly PocoData _pocoData;
        private readonly IRowMapper _rowMapper;

        static MappingFactory()
        {
            RowMappers = new List<Func<MapperCollection, IRowMapper>>()
            {
                x => new ValueTupleRowMapper(x),
                _ => new DictionaryMapper(),
                _ => new ValueTypeMapper(),
                _ => new ArrayMapper(),
                _ => new PropertyMapper()
            };
        }

        public MappingFactory(PocoData pocoData, DbDataReader dataReader)
        {
            _pocoData = pocoData;
            _rowMapper = RowMappers.Select(mapper => mapper(_pocoData.Mapper)).First(x => x.ShouldMap(pocoData));
            _rowMapper.Init(dataReader, pocoData);
        }

        public object Map(DbDataReader dataReader, object instance)
        {
            return _rowMapper.Map(dataReader, new RowMapperContext()
            {
                Instance = instance,
                PocoData = _pocoData
            });
        }
    }
}
