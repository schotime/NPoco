using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPoco
{
    public class DatabaseFactory
    {
        private Database _database;

        public DatabaseFactoryConfig UsingDatabase(Database database)
        {
            _database = database;
            return new DatabaseFactoryConfig(database);
        }

        public IDatabase GetDatabase()
        {
            return _database; 
        }
    }

    public class DatabaseFactoryConfig
    {
        private readonly Database _database;

        public DatabaseFactoryConfig(Database database)
        {
            _database = database;
        }

        public DatabaseFactoryConfig WithMapper(IMapper mapper)
        {
            _database.Mapper = mapper;
            return this;
        }

        public DatabaseFactoryConfig WithFluentConfig(Func<IMapper, Func<Type, PocoData>> pocoDataFactory)
        {
            _database.PocoDataFactory = pocoDataFactory(_database.Mapper);
            return this;
        }
    }
}
