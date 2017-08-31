using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using NPoco.FluentMappings;

namespace NPoco
{
    public class DatabaseFactory
    {
        public static IColumnSerializer ColumnSerializer = new FastJsonColumnSerializer();

        private DatabaseFactoryConfigOptions _options;
        private IPocoDataFactory _cachedPocoDataFactory;

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

        public IDatabase Build(IDatabase database)
        {
            var mappers = BuildMapperCollection(database);
            ConfigurePocoDataFactoryAndMappers(database, mappers);
            ConfigureInterceptors(database);
            return database;
        }

        private void ConfigureInterceptors(IDatabase database)
        {
            database.Interceptors.AddRange(_options.Interceptors);
        }

        private void ConfigurePocoDataFactoryAndMappers(IDatabase database, MapperCollection mappers)
        {
            database.Mappers = mappers;
            if (_options.PocoDataFactory != null)
            {
                database.PocoDataFactory = _cachedPocoDataFactory = (_cachedPocoDataFactory == null ? _options.PocoDataFactory.Config(mappers) : _cachedPocoDataFactory);
            }
        }

        private MapperCollection BuildMapperCollection(IDatabase database)
        {
            var mc = new MapperCollection();
            mc.AddRange(database.Mappers);
            mc.AddRange(_options.Mapper);

            foreach (var factory in _options.Mapper.Factories)
            {
                mc.Factories[factory.Key] = factory.Value;
            }

            return mc;
        }

        public IPocoDataFactory GetPocoDataFactory()
        {
            if (_options.PocoDataFactory != null)
            {
                return _options.PocoDataFactory.Config(_options.Mapper);
            }
            throw new Exception("No PocoDataFactory configured");
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
            Interceptors = new List<IInterceptor>();
        }

        public Func<Database> Database { get; set; }
        public MapperCollection Mapper { get; private set; }
        public FluentConfig PocoDataFactory { get; set; }
        public List<IInterceptor> Interceptors { get; private set; }
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

        public DatabaseFactoryConfig WithMapperFactory<T>(Func<DbDataReader, T> factory)
        {
            _options.Mapper.RegisterFactory(factory);
            return this;
        }

        public DatabaseFactoryConfig WithInterceptor(IInterceptor interceptor)
        {
            _options.Interceptors.Add(interceptor);
            return this;
        }
    }
}
