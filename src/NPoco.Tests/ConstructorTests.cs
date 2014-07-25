﻿using System;
using System.Configuration;
using System.Data;
using System.Data.Common;
using NPoco.DatabaseTypes;
using NPoco.Tests.Common;
using NUnit.Framework;

namespace NPoco.Tests
{
    public class ConstructorTests : BaseDBTest
    {
        private int testDBType;

        [SetUp]
        public void SetUp()
        {
            testDBType = Convert.ToInt32(ConfigurationManager.AppSettings["TestDBType"]);
            switch (testDBType)
            {
                case 1: // SQLite In-Memory
                    TestDatabase = new InMemoryDatabase();
                    break;

                case 2: // SQL Local DB
                    TestDatabase = new SQLLocalDatabase();
                    break;

                case 3: // SQL Server
                case 4: // SQL CE
                case 5: // MySQL
                case 6: // Oracle
                case 7: // Postgres
                    Assert.Fail("Database platform not supported for unit testing");
                    return;

                case 8: // Firebird
                    TestDatabase = new FirebirdDatabase();
                    break;

                default:
                    Assert.Fail("Unknown database platform specified: " + testDBType);
                    return;
            }
        }

        [TearDown]
        public void CleanUp()
        {
            if (TestDatabase == null) return;

            TestDatabase.CleanupDataBase();
            TestDatabase.Dispose();
        }

        private DatabaseType GetConfiguredDatabaseType()
        {
            switch (testDBType)
            {
                case 1: // SQLite In-Memory
                    return new SQLiteDatabaseType();
                case 2: // SQL Local DB
                    return new SqlServerDatabaseType();
                case 3: // SQL Server
                    return new SqlServer2012DatabaseType();
                case 4: // SQL CE
                    return new SqlServerCEDatabaseType();
                case 5: // MySQL
                    return new MySqlDatabaseType();
                case 6: // Oracle
                    return new OracleDatabaseType();
                case 7: // Postgres
                    return new PostgreSQLDatabaseType();
                case 8: // Firebird
                    return new FirebirdDatabaseType();
                default:
                    Assert.Fail("Unknown database platform specified : " + testDBType);
                    return null;
            }
        }

        [Test]
        public void ConstructorWithConnection()
        {
            var dbType = GetConfiguredDatabaseType();
            var db = new Database(TestDatabase.Connection);
            db.OpenSharedConnection();
            Assert.IsNotNull(db.Connection);
            Assert.IsTrue(db.Connection.State == ConnectionState.Open);
            Assert.AreEqual(dbType.GetType(), db.DatabaseType.GetType());

            // Constructors using a Connection do not close the connection on close/displose
            db.CloseSharedConnection();
            Assert.IsNotNull(db.Connection);

            db.Dispose();
            Assert.IsNotNull(db.Connection);
        }

        [Test]
        public void ConstructorWithConnectionAndDBType()
        {
            var dbType = GetConfiguredDatabaseType();
            var db = new Database(TestDatabase.Connection,dbType);
            db.OpenSharedConnection();
            Assert.IsNotNull(db.Connection);
            Assert.IsTrue(db.Connection.State == ConnectionState.Open);
            Assert.AreEqual(dbType.GetType(), db.DatabaseType.GetType());

            // Constructors using a Connection do not close the connection on close/displose
            db.CloseSharedConnection();
            Assert.IsNotNull(db.Connection);

            db.Dispose();
            Assert.IsNotNull(db.Connection);
        }

        [Test]
        public void ConstructorWithConnectionDBTypeAndIsolationLevel()
        {
            var dbType = GetConfiguredDatabaseType();
            var db = new Database(TestDatabase.Connection, dbType, IsolationLevel.ReadUncommitted);
            db.OpenSharedConnection();
            Assert.IsNotNull(db.Connection);
            Assert.IsTrue(db.Connection.State == ConnectionState.Open);
            Assert.AreEqual(dbType.GetType(), db.DatabaseType.GetType());

            // Constructors using a Connection do not close the connection on close/displose
            db.CloseSharedConnection();
            Assert.IsNotNull(db.Connection);

            db.Dispose();
            Assert.IsNotNull(db.Connection);
        }

        [Test]
        public void ConstructorWithConnectionDBTypeIsolationTypeAndSettings()
        {
            var dbType = GetConfiguredDatabaseType();
            var db = new Database(TestDatabase.Connection, dbType, IsolationLevel.ReadUncommitted, false);
            db.OpenSharedConnection();
            Assert.IsNotNull(db.Connection);
            Assert.IsTrue(db.Connection.State == ConnectionState.Open);
            Assert.AreEqual(dbType.GetType(), db.DatabaseType.GetType());

            // Constructors using a Connection do not close the connection on close/displose
            db.CloseSharedConnection();
            Assert.IsNotNull(db.Connection);

            db.Dispose();
            Assert.IsNotNull(db.Connection);
        }

        [Test]
        public void ConstructorWithNamedConnectionString()
        {
            /*
            var db = new Database("BLAH");
            db.OpenSharedConnection();
            Assert.IsNotNull(db.Connection);
            Assert.IsTrue(db.Connection.State == ConnectionState.Open);
            Assert.AreEqual(typeof(SqlServerDatabaseType), db.DatabaseType.GetType());

            // Constructors using a Connection do not close the connection on close/displose
            db.CloseSharedConnection();
            Assert.IsNotNull(db.Connection);

            db.Dispose();
            Assert.IsNotNull(db.Connection);
            */
            Assert.Pass("Not sure how to best test this with the dynamic nature of the backend DBs.");
        }

        [Test]
        public void ConstructorWithNamedConnectionStringAndSettings()
        {
            /*
            var db = new Database("BLAH");
            db.OpenSharedConnection();
            Assert.IsNotNull(db.Connection);
            Assert.IsTrue(db.Connection.State == ConnectionState.Open);
            Assert.AreEqual(typeof(SqlServerDatabaseType), db.DatabaseType.GetType());

            // Constructors using a Connection do not close the connection on close/displose
            db.CloseSharedConnection();
            Assert.IsNotNull(db.Connection);

            db.Dispose();
            Assert.IsNotNull(db.Connection);
            */
            Assert.Pass("Not sure how to best test this with the dynamic nature of the backend DBs.");
        }

        [Test]
        public void ConstructorWithConnectionStringAndProviderName()
        {
            var dbType = GetConfiguredDatabaseType();
            var db = new Database(TestDatabase.ConnectionString, TestDatabase.ProviderName);
            db.OpenSharedConnection();
            Assert.IsNotNull(db.Connection);
            Assert.IsTrue(db.Connection.State == ConnectionState.Open);
            Assert.AreEqual(dbType.GetType(), db.DatabaseType.GetType());

            // Constructors using a Connection do not close the connection on close/displose
            db.CloseSharedConnection();
            Assert.IsNull(db.Connection);

            db.Dispose();
            Assert.IsNull(db.Connection);
        }

        [Test]
        public void ConstructorWithConnectionStringProviderNameAndSettings()
        {
            var dbType = GetConfiguredDatabaseType();
            var db = new Database(TestDatabase.ConnectionString, TestDatabase.ProviderName, false);
            db.OpenSharedConnection();
            Assert.IsNotNull(db.Connection);
            Assert.IsTrue(db.Connection.State == ConnectionState.Open);
            Assert.AreEqual(dbType.GetType(), db.DatabaseType.GetType());

            // Constructors using a Connection do not close the connection on close/displose
            db.CloseSharedConnection();
            Assert.IsNull(db.Connection);

            db.Dispose();
            Assert.IsNull(db.Connection);
        }

        [Test]
        public void ConstructorWithConnectionStringAndDBType()
        {
            var dbType = GetConfiguredDatabaseType();
            var db = new Database(TestDatabase.ConnectionString, dbType);
            db.OpenSharedConnection();
            Assert.IsNotNull(db.Connection);
            Assert.IsTrue(db.Connection.State == ConnectionState.Open);
            Assert.AreEqual(dbType.GetType(), db.DatabaseType.GetType());

            // Constructors using a Connection do not close the connection on close/displose
            db.CloseSharedConnection();
            Assert.IsNull(db.Connection);

            db.Dispose();
            Assert.IsNull(db.Connection);
        }

        [Test]
        public void ConstructorWithConnectionStringDBTypeAndIsolationLevel()
        {
            var dbType = GetConfiguredDatabaseType();
            var db = new Database(TestDatabase.ConnectionString, dbType, IsolationLevel.ReadUncommitted);
            db.OpenSharedConnection();
            Assert.IsNotNull(db.Connection);
            Assert.IsTrue(db.Connection.State == ConnectionState.Open);
            Assert.AreEqual(dbType.GetType(), db.DatabaseType.GetType());

            // Constructors using a Connection do not close the connection on close/displose
            db.CloseSharedConnection();
            Assert.IsNull(db.Connection);

            db.Dispose();
            Assert.IsNull(db.Connection);
        }

        [Test]
        public void ConstructorWithConnectionStringDBTypeAndSettings()
        {
            var dbType = GetConfiguredDatabaseType();
            var db = new Database(TestDatabase.ConnectionString, dbType, IsolationLevel.ReadUncommitted, false);
            db.OpenSharedConnection();
            Assert.IsNotNull(db.Connection);
            Assert.IsTrue(db.Connection.State == ConnectionState.Open);
            Assert.AreEqual(dbType.GetType(), db.DatabaseType.GetType());

            // Constructors using a Connection do not close the connection on close/displose
            db.CloseSharedConnection();
            Assert.IsNull(db.Connection);

            db.Dispose();
            Assert.IsNull(db.Connection);
        }

        [Test]
        public void ConstructorWithConnectionStringAndDBProvider()
        {
            var dbType = GetConfiguredDatabaseType();
            var provider = DbProviderFactories.GetFactory(dbType.GetProviderName());
            var db = new Database(TestDatabase.ConnectionString, provider);
            db.OpenSharedConnection();
            Assert.IsNotNull(db.Connection);
            Assert.IsTrue(db.Connection.State == ConnectionState.Open);
            Assert.AreEqual(dbType.GetType(), db.DatabaseType.GetType());

            // Constructors using a Connection do not close the connection on close/displose
            db.CloseSharedConnection();
            Assert.IsNull(db.Connection);

            db.Dispose();
            Assert.IsNull(db.Connection);
        }

        [Test]
        public void ConstructorWithConnectionStringDBProviderAndSettings()
        {
            var dbType = GetConfiguredDatabaseType();
            var provider = DbProviderFactories.GetFactory(dbType.GetProviderName());
            var db = new Database(TestDatabase.ConnectionString, provider, false);
            db.OpenSharedConnection();
            Assert.IsNotNull(db.Connection);
            Assert.IsTrue(db.Connection.State == ConnectionState.Open);
            Assert.AreEqual(dbType.GetType(), db.DatabaseType.GetType());

            // Constructors using a Connection do not close the connection on close/displose
            db.CloseSharedConnection();
            Assert.IsNull(db.Connection);

            db.Dispose();
            Assert.IsNull(db.Connection);
        }
    }
}
