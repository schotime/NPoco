using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;

namespace NPoco.Tests.Common
{
    public class SQLServerDatabase : TestDatabase
    {
        protected const string DBName = "NPocoUnitTestsDB";
        protected const string DBFileName = "NPocoUnitTestsDB.mdf";
        protected string DBPath { get; set; }
        protected string FQDBFile { get; set; }
         
        public SQLServerDatabase()
        {
            DBPath = Environment.CurrentDirectory;

            FQDBFile = DBPath + "\\" + DBFileName;

            ConnectionString = System.Configuration.ConfigurationManager.AppSettings["TestDBConnectionString"];
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
                Connection.ChangeDatabase(DBName);
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
            var conn = new SqlConnection(ConnectionString);
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

            // Create the new DB
            cmd.CommandText = String.Format("CREATE DATABASE {0} ON (NAME = N'{0}', FILENAME = '{1}')", DBName, FQDBFile);
            cmd.ExecuteNonQuery();
            cmd.Connection.ChangeDatabase(DBName);
              
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
