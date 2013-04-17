using System;
using System.Data;
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
        protected string FQDBFile { get; set; }
        protected string FQLogFile { get; set; }

        public SQLLocalDatabase()
        {
            DBPath = Environment.CurrentDirectory;

            FQDBFile = DBPath + "\\" + DBFileName;
            FQLogFile = DBPath + "\\" + LogFileName;

            ConnectionString = String.Format("Data Source=(LocalDB)\\v11.0;Integrated Security=True;AttachDbFileName=\"{0}\";", FQDBFile);
            ProviderName = "System.Data.SqlClient";

            RecreateDataBase();
            EnsureSharedConnectionConfigured();

            Console.WriteLine("Tables (Constructor): " + Environment.NewLine);
            var dt = ((SqlConnection)Connection).GetSchema("Tables");
            foreach (DataRow row in dt.Rows)
            {
                Console.WriteLine((string)row[2]);
            }
        }

        public override void EnsureSharedConnectionConfigured()
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

            /* 
             * Using new connection so that when a transaction is bound to Connection if it rolls back 
             * it doesn't blow away the tables
             */
            var conn = new SqlConnection("Data Source=(LocalDB)\\v11.0;Integrated Security=True;");
            conn.Open();
            var cmd = conn.CreateCommand();

            // Try to detach the DB in case the clean up code wasn't called (usually aborted debugging)
            cmd.CommandText = String.Format(@"
                IF (EXISTS(SELECT name FROM master.dbo.sysdatabases WHERE ('[' + name + ']' = '{0}' OR name = '{0}')))
                BEGIN
                    ALTER DATABASE {0} SET single_user WITH rollback immediate
                    DROP DATABASE {0}
                END
            ", DBName);
            cmd.ExecuteNonQuery();
            if (File.Exists(FQDBFile)) File.Delete(FQDBFile);
            if (File.Exists(FQLogFile)) File.Delete(FQLogFile);

            // Create the new DB
            cmd.CommandText = String.Format("CREATE DATABASE {0} ON (NAME = N'{0}', FILENAME = '{1}')", DBName, FQDBFile);
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
                    Savings decimal(10,5) NULL,
                    Is_Male tinyint
                );
            ";
            cmd.ExecuteNonQuery();

            cmd.CommandText = @"
                CREATE TABLE ExtraUserInfos(
                    ExtraUserInfoId int IDENTITY(1,1) PRIMARY KEY NOT NULL, 
                    UserId int NOT NULL, 
                    Email nvarchar(200) NULL, 
                    Children int NULL
                );
            ";
            cmd.ExecuteNonQuery();

            cmd.CommandText = @"
                CREATE TABLE CompositeObjects(
                    Key1ID int PRIMARY KEY NOT NULL, 
                    Key2ID int NOT NULL, 
                    Key3ID int NOT NULL, 
                    TextData nvarchar(512) NULL, 
                    DateEntered datetime NOT NULL,
                    DateUpdated datetime NULL 
                );
            ";
            cmd.ExecuteNonQuery();

            Console.WriteLine("Tables (CreateDB): " + Environment.NewLine);
            var dt = conn.GetSchema("Tables");
            foreach (DataRow row in dt.Rows)
            {
                Console.WriteLine(row[2]);
            }

            cmd.Dispose();
            conn.Close();
            conn.Dispose();
        }

        public override void CleanupDataBase()
        {
            /* 
             * Trying to do any clean up here fails until the Database object gets disposed.
             * The create deletes and recreates the files anyone so this isn't really necessary
             */
        }
    }
}
