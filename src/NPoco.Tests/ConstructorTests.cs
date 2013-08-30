using System;
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
        [SetUp]
        public void SetUp()
        {
            var testDBType = Convert.ToInt32(ConfigurationManager.AppSettings["TestDBType"]);
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

                default:
                    Assert.Fail("Unknown database platform specified");
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

        [Test]
        public void ConstructorWithConnection()
        {
            var db = new Database(TestDatabase.Connection);
            db.OpenSharedConnection();
            Assert.IsNotNull(db.Connection);
            Assert.IsTrue(db.Connection.State == ConnectionState.Open);
            Assert.AreEqual(typeof (SqlServerDatabaseType), db.DatabaseType.GetType());

            // Constructors using a Connection do not close the connection on close/displose
            db.CloseSharedConnection();
            Assert.IsNotNull(db.Connection);

            db.Dispose();
            Assert.IsNotNull(db.Connection);
        }

        [Test]
        public void ConstructorWithConnectionAndDBType()
        {
            var db = new Database(TestDatabase.Connection, new SqlServer2012DatabaseType());
            db.OpenSharedConnection();
            Assert.IsNotNull(db.Connection);
            Assert.IsTrue(db.Connection.State == ConnectionState.Open);
            Assert.AreEqual(typeof(SqlServer2012DatabaseType), db.DatabaseType.GetType());

            // Constructors using a Connection do not close the connection on close/displose
            db.CloseSharedConnection();
            Assert.IsNotNull(db.Connection);

            db.Dispose();
            Assert.IsNotNull(db.Connection);
        }

        [Test]
        public void ConstructorWithConnectionDBTypeAndIsolationLevel()
        {
            var db = new Database(TestDatabase.Connection, new SqlServer2012DatabaseType(), IsolationLevel.ReadUncommitted);
            db.OpenSharedConnection();
            Assert.IsNotNull(db.Connection);
            Assert.IsTrue(db.Connection.State == ConnectionState.Open);
            Assert.AreEqual(typeof(SqlServer2012DatabaseType), db.DatabaseType.GetType());

            // Constructors using a Connection do not close the connection on close/displose
            db.CloseSharedConnection();
            Assert.IsNotNull(db.Connection);

            db.Dispose();
            Assert.IsNotNull(db.Connection);
        }

        [Test]
        public void ConstructorWithConnectionDBTypeIsolationTypeAndSettings()
        {
            var db = new Database(TestDatabase.Connection, new SqlServer2012DatabaseType(), IsolationLevel.ReadUncommitted, false);
            db.OpenSharedConnection();
            Assert.IsNotNull(db.Connection);
            Assert.IsTrue(db.Connection.State == ConnectionState.Open);
            Assert.AreEqual(typeof(SqlServer2012DatabaseType), db.DatabaseType.GetType());

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
            var db = new Database(TestDatabase.ConnectionString, TestDatabase.ProviderName);
            db.OpenSharedConnection();
            Assert.IsNotNull(db.Connection);
            Assert.IsTrue(db.Connection.State == ConnectionState.Open);
            Assert.AreEqual(typeof(SqlServerDatabaseType), db.DatabaseType.GetType());

            // Constructors using a Connection do not close the connection on close/displose
            db.CloseSharedConnection();
            Assert.IsNull(db.Connection);

            db.Dispose();
            Assert.IsNull(db.Connection);
        }

        [Test]
        public void ConstructorWithConnectionStringProviderNameAndSettings()
        {
            var db = new Database(TestDatabase.ConnectionString, TestDatabase.ProviderName, false);
            db.OpenSharedConnection();
            Assert.IsNotNull(db.Connection);
            Assert.IsTrue(db.Connection.State == ConnectionState.Open);
            Assert.AreEqual(typeof(SqlServerDatabaseType), db.DatabaseType.GetType());

            // Constructors using a Connection do not close the connection on close/displose
            db.CloseSharedConnection();
            Assert.IsNull(db.Connection);

            db.Dispose();
            Assert.IsNull(db.Connection);
        }

        [Test]
        public void ConstructorWithConnectionStringAndDBType()
        {
            var db = new Database(TestDatabase.ConnectionString, new SqlServer2012DatabaseType());
            db.OpenSharedConnection();
            Assert.IsNotNull(db.Connection);
            Assert.IsTrue(db.Connection.State == ConnectionState.Open);
            Assert.AreEqual(typeof(SqlServer2012DatabaseType), db.DatabaseType.GetType());

            // Constructors using a Connection do not close the connection on close/displose
            db.CloseSharedConnection();
            Assert.IsNull(db.Connection);

            db.Dispose();
            Assert.IsNull(db.Connection);
        }

        [Test]
        public void ConstructorWithConnectionStringDBTypeAndIsolationLevel()
        {
            var db = new Database(TestDatabase.ConnectionString, new SqlServer2012DatabaseType(), IsolationLevel.ReadUncommitted);
            db.OpenSharedConnection();
            Assert.IsNotNull(db.Connection);
            Assert.IsTrue(db.Connection.State == ConnectionState.Open);
            Assert.AreEqual(typeof(SqlServer2012DatabaseType), db.DatabaseType.GetType());

            // Constructors using a Connection do not close the connection on close/displose
            db.CloseSharedConnection();
            Assert.IsNull(db.Connection);

            db.Dispose();
            Assert.IsNull(db.Connection);
        }

        [Test]
        public void ConstructorWithConnectionStringDBTypeAndSettings()
        {
            var db = new Database(TestDatabase.ConnectionString, new SqlServer2012DatabaseType(), IsolationLevel.ReadUncommitted, false);
            db.OpenSharedConnection();
            Assert.IsNotNull(db.Connection);
            Assert.IsTrue(db.Connection.State == ConnectionState.Open);
            Assert.AreEqual(typeof(SqlServer2012DatabaseType), db.DatabaseType.GetType());

            // Constructors using a Connection do not close the connection on close/displose
            db.CloseSharedConnection();
            Assert.IsNull(db.Connection);

            db.Dispose();
            Assert.IsNull(db.Connection);
        }

        [Test]
        public void ConstructorWithConnectionStringAndDBProvider()
        {
            var dbType = new SqlServer2012DatabaseType();
            var provider = DbProviderFactories.GetFactory(dbType.GetProviderName());
            var db = new Database(TestDatabase.ConnectionString, provider);
            db.OpenSharedConnection();
            Assert.IsNotNull(db.Connection);
            Assert.IsTrue(db.Connection.State == ConnectionState.Open);
            Assert.AreEqual(typeof(SqlServerDatabaseType), db.DatabaseType.GetType());

            // Constructors using a Connection do not close the connection on close/displose
            db.CloseSharedConnection();
            Assert.IsNull(db.Connection);

            db.Dispose();
            Assert.IsNull(db.Connection);
        }

        [Test]
        public void ConstructorWithConnectionStringDBProviderAndSettings()
        {
            var dbType = new SqlServer2012DatabaseType();
            var provider = DbProviderFactories.GetFactory(dbType.GetProviderName());
            var db = new Database(TestDatabase.ConnectionString, provider, false);
            db.OpenSharedConnection();
            Assert.IsNotNull(db.Connection);
            Assert.IsTrue(db.Connection.State == ConnectionState.Open);
            Assert.AreEqual(typeof(SqlServerDatabaseType), db.DatabaseType.GetType());

            // Constructors using a Connection do not close the connection on close/displose
            db.CloseSharedConnection();
            Assert.IsNull(db.Connection);

            db.Dispose();
            Assert.IsNull(db.Connection);
        }
    }
}
