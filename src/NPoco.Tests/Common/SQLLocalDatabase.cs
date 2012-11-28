using System;
using System.Data.SqlClient;
using System.IO;

namespace NPoco.Tests.Common
{
    public class SQLLocalDatabase : TestDatabase
    {
        protected const string DBName = "UnitTestsDB";
        protected const string DBFileName = "UnitTestsDB.mdf";
        protected const string LogFileName = "UnitTestsDB_log.ldf";
        protected string DBPath { get; set; }

        public SQLLocalDatabase()
        {
            DBPath = Environment.CurrentDirectory;
            ConnectionString = "Server=(LocalDB)\\v11.0;Integrated Security=True";
            ProviderName = "System.Data.SqlClient";
            RecreateDataBase();
        }

        public override void EnsureConfigured()
        {
            if (Connection != null) return;

            lock (_syncRoot)
            {
                Connection = new SqlConnection(ConnectionString);
                Connection.Open();
            }
        }

        public override void RecreateDataBase()
        {
            Console.WriteLine("----------------------------");
            Console.WriteLine("Using SQL Server Local DB   ");
            Console.WriteLine("----------------------------");

            base.RecreateDataBase();

            var fqDBFilePath = DBPath + "\\" + DBFileName;
            var fqLogFilePath = DBPath + "\\" + LogFileName;

            if (Connection == null) EnsureConfigured();
            if (Connection == null) throw new Exception("Database conneciton failed.");
            var cmd = Connection.CreateCommand();

            // Try to detach the DB in case the clean up code wasn't called (usually aborted debugging)
            cmd.CommandText = String.Format(@"
                IF (EXISTS(SELECT name FROM master.dbo.sysdatabases WHERE ('[' + name + ']' = '{0}' OR name = '{0}')))
                    DROP DATABASE {0}
            ", DBName);
            cmd.ExecuteNonQuery();
            if (File.Exists(fqDBFilePath)) File.Delete(fqDBFilePath);
            if (File.Exists(fqLogFilePath)) File.Delete(fqLogFilePath);

            // Create the new DB
            cmd.CommandText = String.Format("CREATE DATABASE {0} ON (NAME = N'{0}', FILENAME = '{1}')", DBName, fqDBFilePath);
            cmd.ExecuteNonQuery();
            if (!File.Exists(DBFileName)) throw new Exception("Database failed to create");
            cmd.Connection.ChangeDatabase(DBName);

            // Create the Schema
            cmd.CommandText = @"
                CREATE TABLE Users(
                    UserId int IDENTITY(1,1) PRIMARY KEY NOT NULL, 
                    Name nvarchar(200) NULL, 
                    Age int NULL, 
                    DateOfBirth datetime NULL, 
                    Savings decimal(10,5) NULL
                );
            ";
            cmd.ExecuteNonQuery();

            cmd.CommandText = @"
                CREATE TABLE ExtraInfos(
                    ExtraInfoId int IDENTITY(1,1) PRIMARY KEY NOT NULL, 
                    UserId int NOT NULL, 
                    Email nvarchar(200) NULL, 
                    Children int NULL
                );
            ";
            cmd.ExecuteNonQuery();

            cmd.Dispose();
        }
    }
}
