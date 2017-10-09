using System;
using System.Collections.Generic;
using System.Text;

namespace NPoco.Tests.Common
{
    public class SqliteDatabase : TestDatabase
    {
        public SqliteDatabase()
        {
            DbType = DatabaseType.MicrosoftSqlite;
            ConnectionString = "Data Source=:memory:";
            ProviderName = DatabaseType.MicrosoftSqlite.GetProviderName();
            Factory = Microsoft.Data.Sqlite.SqliteFactory.Instance;

            EnsureSharedConnectionConfigured();
            RecreateDataBase();
        }

        public override void EnsureSharedConnectionConfigured()
        {
            if (Connection != null) return;


            lock (_syncRoot)
            {
                Connection = new Microsoft.Data.Sqlite.SqliteConnection(ConnectionString);
                Connection.Open();
            }
        }

        public override void RecreateDataBase()
        {
            Console.WriteLine("----------------------------");
            Console.WriteLine("Using Microsoft SQLite In-Memory DB   ");
            Console.WriteLine("----------------------------");

            base.RecreateDataBase();

            var cmd = Connection.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE Users(
                    UserId INTEGER PRIMARY KEY AUTOINCREMENT  NOT NULL, 
                    Name TEXT NULL, 
                    Age INTEGER NULL, 
                    DateOfBirth datetime NULL, 
                    Savings REAL NULL,
                    Is_Male INTEGER,
                    UniqueId TEXT NULL,
                    TimeSpan INTEGER NULL,
                    TestEnum TEXT NULL,
                    HouseId INTEGER NULL,
                    SupervisorId INTEGER NULL,
                    Version TEXT,
                    VersionInt INTEGER NOT NULL DEFAULT 0,
                    YorN TEXT NULL,
                    Address__Street TEXT NULL,
                    Address__City TEXT NULL
                );
            ";
            cmd.ExecuteNonQuery();

            cmd.CommandText = @"
                CREATE TABLE ExtraUserInfos(
                    ExtraUserInfoId INTEGER PRIMARY KEY AUTOINCREMENT  NOT NULL, 
                    UserId INTEGER NOT NULL, 
                    Email TEXT(200) NULL, 
                    Children INTEGER NULL
                );
            ";
            cmd.ExecuteNonQuery();

            cmd.CommandText = @"
                CREATE TABLE Houses(
                    HouseId INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, 
                    Address TEXT
                );
            ";
            cmd.ExecuteNonQuery();

            cmd.CommandText = @"
                CREATE TABLE CompositeObjects(
                    Key1_ID INTEGER PRIMARY KEY NOT NULL, 
                    Key2ID INTEGER NOT NULL, 
                    Key3ID INTEGER NOT NULL, 
                    TextData TEXT NULL, 
                    DateEntered TEXT NOT NULL,
                    DateUpdated TEXT NULL 
                );
            ";
            cmd.ExecuteNonQuery();

            cmd.CommandText = @"
                CREATE TABLE ComplexMap(
                    Id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, 
                    Name TEXT NULL, 
                    NestedComplexMap__Id INTEGER NULL, 
                    NestedComplexMap__NestedComplexMap2__Id INTEGER NULL, 
                    NestedComplexMap__NestedComplexMap2__Name TEXT NULL, 
                    NestedComplexMap2__Id INTEGER NULL,
                    NestedComplexMap2__Name TEXT NULL
                );
            ";
            cmd.ExecuteNonQuery();

            cmd.CommandText = @"
                CREATE TABLE RecursionUser(
                    Id INTEGER PRIMARY KEY AUTOINCREMENT  NOT NULL, 
                    Name TEXT NULL, 
                    CreatedById INTEGER NULL, 
                    SupervisorId INTEGER NULL
                );
            ";
            cmd.ExecuteNonQuery();

            cmd.CommandText = @"
                CREATE TABLE Ones(
                    OneId INTEGER PRIMARY KEY AUTOINCREMENT  NOT NULL, 
                    Name TEXT NULL
                );
            ";
            cmd.ExecuteNonQuery();

            cmd.CommandText = @"
                CREATE TABLE Manys(
                    ManyId INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, 
                    OneId INTEGER NOT NULL, 
                    Value INTEGER NULL, 
                    Currency TEXT NULL
                );
            ";
            cmd.ExecuteNonQuery();

            cmd.CommandText = @"
                CREATE TABLE UserWithAddress(
                    Id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, 
                    Name TEXT NULL,
                    Address TEXT NULL
                );
            ";
            cmd.ExecuteNonQuery();

            cmd.CommandText = @"
                CREATE TABLE GuidFromDb(
                    Id TEXT PRIMARY KEY, 
                    Name TEXT  
                );
            ";
            cmd.ExecuteNonQuery();

            cmd.CommandText = @"
                CREATE TABLE JustPrimaryKey(
                    Id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL
                );
            ";
            cmd.ExecuteNonQuery();

            cmd.CommandText = @"
                CREATE TABLE NoPrimaryKey(
                    Name TEXT NULL
                );
            ";
            cmd.ExecuteNonQuery();

            cmd.Dispose();
        }

        public override void CleanupDataBase()
        {
            base.CleanupDataBase();

            if (Connection == null) return;

            //var cmd = Connection.CreateCommand();
            //cmd.CommandText = "DROP TABLE Users;";
            //cmd.ExecuteNonQuery();

            //cmd.CommandText = "DROP TABLE ExtraUserInfos;";
            //cmd.ExecuteNonQuery();

            //cmd.Dispose();
        }
    }
}
