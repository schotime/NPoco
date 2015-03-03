﻿using System;
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
            var pocoDataFactory = new PocoDataFactory(y => new PocoData(y, new Mapper()));
            var fluentConfig = new FluentConfig(x=>pocoDataFactory);

            var factory = DatabaseFactory.Config(x =>
            {
                x.UsingDatabase(() => db);
                x.WithFluentConfig(fluentConfig);
            });

            var database = factory.GetDatabase();
            Assert.AreEqual(fluentConfig.Config(null), database.PocoDataFactory);
        }

        class WanderingPoco
        {
            public int Id { get; set; }
        }

        class Mapping : Map<WanderingPoco>
        {
            public Mapping()
            {
                PrimaryKey(x => x.Id, true);
                TableName("Table1");
            }
        }

        class AnotherMapping : Mapping
        {
            public AnotherMapping()
            {
                TableName("Table2");
            }
        }

        [Test]
        public void DifferentFactoriesWithDifferentMappingsGetDifferentPocoDataForSamePoco()
        {
            // Assume these factories connect to different data sources
            var factory1 = DatabaseFactory.Config(x =>
            {
                x.UsingDatabase(() => new Database(new SqlConnection()));
                x.WithFluentConfig(FluentMappingConfiguration.Configure(new Mapping()));
            });
            var factory2 = DatabaseFactory.Config(x =>
            {
                x.UsingDatabase(() => new Database(new SqlConnection()));
                x.WithFluentConfig(FluentMappingConfiguration.Configure(new AnotherMapping()));
            });

            var db1 = factory1.GetDatabase();
            var db2 = factory2.GetDatabase();

            var pocoData1 = db1.PocoDataFactory.ForType(typeof (WanderingPoco));
            var pocoData2 = db2.PocoDataFactory.ForType(typeof (WanderingPoco));

            Assert.AreEqual("Table1", pocoData1.TableInfo.TableName);
            Assert.AreEqual("Table2", pocoData2.TableInfo.TableName);
        }
    }

    public class Mapper : DefaultMapper
    {
    }
}
