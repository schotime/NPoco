using Microsoft.Data.Sqlite;
using NPoco;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SqliteTest
{
    [TableName("TestTable")]
    [PrimaryKey("TestPocoId", AutoIncrement = false)]
    public class TestPoco
    {
        public Guid TestPocoId { get; set; }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            const string ConnectionString = "Data Source=Test.db";

            // set up database

            using (var connection = SqliteFactory.Instance.CreateConnection())
            using (var command = SqliteFactory.Instance.CreateCommand())
            {
                connection.ConnectionString = ConnectionString;
                connection.Open();
                command.Connection = connection;
                command.CommandText = "CREATE TABLE IF NOT EXISTS TestTable (TestPocoId char(36) NOT NULL PRIMARY KEY)";
                command.ExecuteNonQuery();
            }

            // returns wrong database type
            var dbType = DatabaseType.Resolve(SqliteFactory.Instance.GetType().Name, null);
            if (!(dbType is NPoco.DatabaseTypes.MicrosoftSqliteDatabaseType))
                throw new Exception();

            // test with NPoco

            using (var database = new Database(ConnectionString, null, SqliteFactory.Instance))     // this is how I create it in my real application
            {
                using (var transaction = database.GetTransaction())
                {
                    database.Insert(new TestPoco() { TestPocoId = Guid.NewGuid() });
                    transaction.Complete();
                }

                // this doesn't work
                var data = database.Fetch<TestPoco>();
            }

        }
    }
}
