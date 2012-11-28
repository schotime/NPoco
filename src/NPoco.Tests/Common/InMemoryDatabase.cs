using System;
using System.Data.SQLite;

namespace NPoco.Tests.Common
{
    public class InMemoryDatabase : TestDatabase
    {
        public InMemoryDatabase()
        {
            ConnectionString = "Data Source=:memory:;Version=3;";
            RecreateDataBase();
        }

        public override void EnsureConfigured()
        {
            if (Connection != null) return;

            lock (_syncRoot)
            {
                Connection = new SQLiteConnection(ConnectionString);
                Connection.Open();
            }
        }

        public override void RecreateDataBase()
        {
            Console.WriteLine("----------------------------");
            Console.WriteLine("Using SQLite In-Memory DB   ");
            Console.WriteLine("----------------------------");

            base.RecreateDataBase();

            if (Connection == null) EnsureConfigured();
            if (Connection == null) throw new Exception("Database conneciton failed.");

            var cmd = Connection.CreateCommand();
            cmd.CommandText = "CREATE TABLE Users(UserId INTEGER PRIMARY KEY, Name nvarchar(200), Age int, DateOfBirth datetime, Savings Decimal(10,5));";
            cmd.ExecuteNonQuery();

            cmd.CommandText = "CREATE TABLE ExtraUserInfos(ExtraUserInfoId INTEGER PRIMARY KEY, UserId int, Email nvarchar(200), Children int);";
            cmd.ExecuteNonQuery();

            cmd.Dispose();
        }

        public override void CleanupDataBase()
        {
            base.CleanupDataBase();

            if (Connection == null) return;

            var cmd = Connection.CreateCommand();
            cmd.CommandText = "DROP TABLE Users;";
            cmd.ExecuteNonQuery();

            cmd.CommandText = "DROP TABLE ExtraUserInfos;";
            cmd.ExecuteNonQuery();

            cmd.Dispose();
        }
    }
}
