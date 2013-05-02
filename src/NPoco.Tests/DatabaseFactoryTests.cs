using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using NPoco.FluentMappings;
using NUnit.Framework;

namespace NPoco.Tests
{
    [TestFixture]
    public class DatabaseFactoryTests
    {
        [Test]
        public void DatabaseShouldbeReturnedWhenInsertedIntoFactoryConfig()
        {
            var db = new Database(new SqlConnection());

            var factory = DatabaseFactory.Config(x =>
            {
                x.UsingDatabase(() => db);
            });

            Assert.AreEqual(db, factory.GetDatabase());
        }

        [Test]
        public void MapperShouldBePlacedOnDatabaseWhenInsertedIntoFactoryConfig()
        {
            var db = new Database(new SqlConnection());
            var mapper = new Mapper();

            var factory = DatabaseFactory.Config(x =>
            {
                x.WithMapper(mapper);
            });

            Assert.AreEqual(mapper, factory.Build(db).Mapper);
        }

        [Test]
        public void MapperShouldBePlacedOnDatabaseWhenInsertedIntoFactoryConfigWhenCallingGetDatabase()
        {
            var db = new Database(new SqlConnection());
            var mapper = new Mapper();

            var factory = DatabaseFactory.Config(x =>
            {
                x.UsingDatabase(() => db);
                x.WithMapper(mapper);
            });

            Assert.AreEqual(mapper, factory.GetDatabase().Mapper);
        }

        [Test]
        public void FluentConfigShouldBePlacedOnDatabaseWhenInsertedIntoFactoryConfig()
        {
            var db = new Database(new SqlConnection());
            var fluentConfig = new FluentConfig(x=>y=>new PocoData(y, new Mapper()));

            var factory = DatabaseFactory.Config(x =>
            {
                x.UsingDatabase(() => db);
                x.WithFluentConfig(fluentConfig);
            });

            var database = factory.GetDatabase();
            Assert.AreEqual(fluentConfig.Config(null), database.PocoDataFactory);
        }
    }

    public class Mapper : DefaultMapper
    {
    }
}
