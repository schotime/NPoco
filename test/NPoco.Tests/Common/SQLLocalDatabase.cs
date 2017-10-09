using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using NPoco.DatabaseTypes;

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
        protected string ConnectionStringBase { get; set; }

        public SQLLocalDatabase(string dataSource)
        {
            DbType = new SqlServer2012DatabaseType();
            DBPath = Directory.GetCurrentDirectory();

            FQDBFile = DBPath + "\\" + DBFileName;
            FQLogFile = DBPath + "\\" + LogFileName;

            ConnectionStringBase = String.Format("Data Source={0};Integrated Security=True;", dataSource);
            ConnectionString = String.Format("{0}AttachDbFileName=\"{1}\";", ConnectionStringBase, FQDBFile);
            ProviderName = "System.Data.SqlClient";

            RecreateDataBase();
            EnsureSharedConnectionConfigured();

//            Console.WriteLine("Tables (Constructor): " + Environment.NewLine);
//#if !DNXCORE50
//            var dt = ((SqlConnection)Connection).GetSchema("Tables");
//            foreach (DataRow row in dt.Rows)
//            {
//                Console.WriteLine((string)row[2]);
//            }
//#endif
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
            //Console.WriteLine("----------------------------");
            //Console.WriteLine("Using SQL Server Local DB   ");
            //Console.WriteLine("----------------------------");

            base.RecreateDataBase();

            /* 
             * Using new connection so that when a transaction is bound to Connection if it rolls back 
             * it doesn't blow away the tables
             */
            var conn = new SqlConnection(ConnectionStringBase);
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
                    Is_Male tinyint,
                    UniqueId uniqueidentifier NULL,
                    TimeSpan time NULL,
                    TestEnum varchar(10) NULL,
                    HouseId int NULL,
                    SupervisorId int NULL,
                    Version rowversion,
                    VersionInt int default(0) NOT NULL,
                    YorN char NULL,
                    Address__Street nvarchar(50) NULL,
                    Address__City nvarchar(50) NULL
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
                CREATE TABLE Houses(
                    HouseId int IDENTITY(1,1) PRIMARY KEY NOT NULL, 
                    Address nvarchar(200)
                );
            ";
            cmd.ExecuteNonQuery();

            cmd.CommandText = @"
                CREATE TABLE CompositeObjects(
                    Key1_ID int PRIMARY KEY NOT NULL, 
                    Key2ID int NOT NULL, 
                    Key3ID int NOT NULL, 
                    TextData nvarchar(512) NULL, 
                    DateEntered datetime NOT NULL,
                    DateUpdated datetime NULL 
                );
            ";
            cmd.ExecuteNonQuery();

            cmd.CommandText = @"
                CREATE TABLE ComplexMap(
                    Id int Identity(1,1) PRIMARY KEY NOT NULL, 
                    Name nvarchar(50) NULL, 
                    NestedComplexMap__Id int NULL, 
                    NestedComplexMap__NestedComplexMap2__Id int NULL, 
                    NestedComplexMap__NestedComplexMap2__Name nvarchar(50) NULL, 
                    NestedComplexMap2__Id int NULL,
                    NestedComplexMap2__Name nvarchar(50) NULL
                );
            ";
            cmd.ExecuteNonQuery();

            cmd.CommandText = @"
                CREATE TABLE RecursionUser(
                    Id int Identity(1,1) PRIMARY KEY NOT NULL, 
                    Name nvarchar(50) NULL, 
                    CreatedById int NULL, 
                    SupervisorId int NULL
                );
            ";
            cmd.ExecuteNonQuery();

            cmd.CommandText = @"
                CREATE TABLE Ones(
                    OneId int Identity(1,1) PRIMARY KEY NOT NULL, 
                    Name nvarchar(50) NULL
                );
            ";
            cmd.ExecuteNonQuery();

            cmd.CommandText = @"
                CREATE TABLE Manys(
                    ManyId int Identity(1,1) PRIMARY KEY NOT NULL, 
                    OneId int NOT NULL, 
                    Value int NULL, 
                    Currency nvarchar(50) NULL
                );
            ";
            cmd.ExecuteNonQuery();

            cmd.CommandText = @"
                CREATE TABLE UserWithAddress(
                    Id int Identity(1,1) PRIMARY KEY NOT NULL, 
                    Name nvarchar(100) NULL,
                    Address nvarchar(max) NULL
                );
            ";
            cmd.ExecuteNonQuery();

            cmd.CommandText = @"
                CREATE TABLE GuidFromDb(
                    Id uniqueidentifier PRIMARY KEY DEFAULT newid(), 
                    Name nvarchar(30)  
                );
            ";
            cmd.ExecuteNonQuery();

            cmd.CommandText = @"
                CREATE TABLE JustPrimaryKey(
                    Id int IDENTITY(1, 1) PRIMARY KEY NOT NULL
                );
            ";
            cmd.ExecuteNonQuery();

            cmd.CommandText = @"
                CREATE TABLE NoPrimaryKey(
                    Name nvarchar(50) NULL
                );
            ";
            cmd.ExecuteNonQuery();

            cmd.CommandText = @"
                CREATE procedure TestProc (@Name nvarchar(50)) 
                AS
                BEGIN 
                    select @Name
                END
            ";
            cmd.ExecuteNonQuery();

            //            Console.WriteLine("Tables (CreateDB): " + Environment.NewLine);
            //#if !DNXCORE50
            //            var dt = conn.GetSchema("Tables");
            //            foreach (DataRow row in dt.Rows)
            //            {
            //                Console.WriteLine(row[2]);
            //            }
            //#endif

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
