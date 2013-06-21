using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NPoco.FluentMappings;

namespace NPoco
{
    /// <Summary>
    /// A factory class to build database using mapping or fluency
    /// </Summary>
    public class DatabaseFactory
    {
        private DatabaseFactoryConfigOptions _options;

        /// <Summary>
        /// Get the configuration setting
        /// </Summary>
        public DatabaseFactoryConfig Config()
        {
            _options = new DatabaseFactoryConfigOptions();
            return new DatabaseFactoryConfig(_options);
        }
        /// <Summary>
        /// Build the database item with either mapper or pocodatafactory
        /// </Summary>
        public Database Build(Database database)
        {
            if (_options.Mapper != null)
                database.Mapper = _options.Mapper;

            if (_options.PocoDataFactory != null)
                database.PocoDataFactory = _options.PocoDataFactory;

            return database;
        }
    
        /// <Summary>
        /// Gets the database after building with options
        /// </Summary>
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
        public Func<Database> Database { get; set; }
        public IMapper Mapper { get; set; }
        public Func<Type, PocoData> PocoDataFactory { get; set; }
    }

    public class DatabaseFactoryConfig
    {
        private readonly DatabaseFactoryConfigOptions _options;
        /// <Summary>
        /// Constructor 
        /// </Summary>
        public DatabaseFactoryConfig(DatabaseFactoryConfigOptions options)
        {
            _options = options;
        }
        /// <Summary>
        /// Configure database usign function lambda argument 
        /// </Summary>
        public DatabaseFactoryConfig UsingDatabase(Func<Database> database)
        {
            _options.Database = database;
            return this;
        }
        /// <Summary>
        /// Database mapping using mapper technique
        /// </Summary>
        public DatabaseFactoryConfig WithMapper(IMapper mapper)
        {
            _options.Mapper = mapper;
            return this;
        }
        /// <Summary>
        /// Database mapping using fluent technique
        /// </Summary>
        public DatabaseFactoryConfig WithFluentConfig(FluentConfig pocoDataFactory)
        {
            _options.PocoDataFactory = pocoDataFactory.Config(_options.Mapper);
            return this;
        }
    }
}
