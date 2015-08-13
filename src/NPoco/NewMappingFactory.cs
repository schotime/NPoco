using System.Collections.Generic;
using System.Data;
using System.Linq;
using NPoco.RowMappers;

namespace NPoco
{
    public class NewMappingFactory
    {
        public static List<IRowMapper> RowMappers { get; private set; } 
        private readonly PocoData _pocoData;
        private readonly IRowMapper _rowMapper;

        static NewMappingFactory()
        {
            RowMappers = new List<IRowMapper>()
            {
                new DictionaryMapper(),
                new ValueTypeMapper(),
                new ArrayMapper(),
                new PropertyMapper()
            };
        }

        public NewMappingFactory(PocoData pocoData, IDataReader dataReader)
        {
            _pocoData = pocoData;
            _rowMapper = RowMappers.First(x => x.ShouldMap(pocoData));
            _rowMapper.Init(dataReader, pocoData);
        }

        public object Map(IDataReader dataReader, object instance)
        {
            return _rowMapper.Map(dataReader, new RowMapperContext()
            {
                Instance = instance,
                PocoData = _pocoData
            });
        }
    }
}
