using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using NPoco.FluentMappings;

namespace NPoco
{
    public class DatabaseFactory
    {
        private DatabaseFactoryConfigOptions _options;

        public DatabaseFactory() { }

        public DatabaseFactory(DatabaseFactoryConfigOptions options)
        {
            _options = options;
        }

        public DatabaseFactoryConfig Config()
        {
            _options = new DatabaseFactoryConfigOptions();
            return new DatabaseFactoryConfig(_options);
        }

        public static DatabaseFactory Config(Action<DatabaseFactoryConfig> optionsAction)
        {
            var options = new DatabaseFactoryConfigOptions();
            var databaseFactoryConfig = new DatabaseFactoryConfig(options);
            optionsAction(databaseFactoryConfig);
            var dbFactory = new DatabaseFactory(options);
            return dbFactory;
        }

        public Database Build(Database database)
        {
            ConfigureMappers(database);
            ConfigurePocoDataFactory(database);
            return database;
        }

        private void ConfigurePocoDataFactory(Database database)
        {
            if (_options.PocoDataFactory != null)
                database.PocoDataFactory = _options.PocoDataFactory.Config(database.Mappers);
        }

        private void ConfigureMappers(Database database)
        {
            database.Mappers.InsertRange(0, _options.Mapper);

            foreach (var factory in database.Mappers.Factories)
            {
                database.Mappers.Factories[factory.Key] = factory.Value;
            }
        }

        public Database GetDatabase()
        {
            if (_options.Database == null)
                throw new NullReferenceException("Database cannot be null. Use UsingDatabase()");

            var db = _options.Database();
            Build(db);
            return db; 
        }
    }

    public class DatabaseFactoryConfigOptions
    {
        public DatabaseFactoryConfigOptions()
        {
            Mapper = new MapperCollection();
        }

        public Func<Database> Database { get; set; }
        public MapperCollection Mapper { get; private set; }
        public FluentConfig PocoDataFactory { get; set; }
    }

    public class DatabaseFactoryConfig
    {
        private readonly DatabaseFactoryConfigOptions _options;
        
        public DatabaseFactoryConfig(DatabaseFactoryConfigOptions options)
        {
            _options = options;
        }
        
        public DatabaseFactoryConfig UsingDatabase(Func<Database> database)
        {
            _options.Database = database;
            return this;
        }

        public DatabaseFactoryConfig WithMapper(IMapper mapper)
        {
            _options.Mapper.Add(mapper);
            return this;
        }

        public DatabaseFactoryConfig WithFluentConfig(FluentConfig fluentConfig)
        {
            _options.PocoDataFactory = fluentConfig;
            return this;
        }

        public DatabaseFactoryConfig WithMapperFactory<T>(Func<IDataReader, T> factory)
        {
            _options.Mapper.RegisterFactory(factory);
            return this;
        }
    }
}
