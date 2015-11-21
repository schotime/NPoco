#if !DNXCORE50
using System;
using System.Data;
using System.IO;
using System.Threading;
using FirebirdSql.Data.FirebirdClient;
using FirebirdSql.Data.Isql;
using FirebirdSql.Data.Services;
using NPoco;

namespace NPoco.Tests.Common
{
    public class FirebirdDatabase : TestDatabase
    {
        private const string FbUserName = "NPocoTest";
        private const string FbUserPass = "test12345";
        private const string FbRole = "NPoco";

        protected string DBName = "UnitTestsDB";
        protected string DBFileName = "UnitTestsDB.fdb";

        protected string DBPath { get; set; }

        protected string FQDBFile { get; set; }

        public FirebirdDatabase()
        {
            DbType = DatabaseType.Firebird;
            ProviderName = DatabaseType.Firebird.GetProviderName();
            DBPath = Environment.CurrentDirectory;

#if RUN_ALL_TESTS
            // Create one database for each test. Remember to delete after.
            DBPath = Path.Combine(Path.GetTempPath(), "NPoco", "Fb");
            if (!Directory.Exists(DBPath))
                Directory.CreateDirectory(DBPath);

            DBName = Guid.NewGuid().ToString();
#endif
            DBFileName = DBName + ".fdb";
            FQDBFile = DBPath + "\\" + DBFileName;

            // ConnectionString Builder
            FbConnectionStringBuilder csb = new FbConnectionStringBuilder();
            csb.Database = FQDBFile;
            csb.DataSource = "localhost";
            csb.Dialect = 3;
            csb.Charset = "UTF8";
            csb.Pooling = false;
            csb.Role = FbRole;
            csb.UserID = FbUserName; 
            csb.Password = FbUserPass; 

            ConnectionString = csb.ToString();

            RecreateDataBase();
            EnsureSharedConnectionConfigured();

            Console.WriteLine("Tables (Constructor): " + Environment.NewLine);
            var dt = ((FbConnection)Connection).GetSchema("Tables", new[] { null, null, null, "TABLE" });
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
                Connection = new FbConnection(ConnectionString);
                Connection.Open();
            }
        }

        public override void RecreateDataBase()
        {
            // ConnectionString Builder
            FbConnectionStringBuilder csb = new FbConnectionStringBuilder();
            csb.DataSource = "localhost";
            csb.Dialect = 3;
            csb.Charset = "UTF8";
            csb.Pooling = false;
            csb.UserID = "SYSDBA"; // default user
            csb.Password = "masterkey"; // default password

            string serverConnectionString = csb.ToString();
            csb.Database = csb.Database = FQDBFile;

            string databaseConnectionString = csb.ToString();

            Console.WriteLine("-------------------------");
            Console.WriteLine("Using Firebird Database  ");
            Console.WriteLine("-------------------------");

            base.RecreateDataBase();

            // Create simple user
            FbSecurity security = new FbSecurity();
            security.ConnectionString = serverConnectionString;
            var userData = security.DisplayUser(FbUserName);
            if (userData == null)
            {
                userData = new FbUserData();
                userData.UserName = FbUserName;
                userData.UserPassword = FbUserPass;
                security.AddUser(userData);
            }

            // Try to shutdown & delete database
            if (File.Exists(FQDBFile))
            {
                FbConfiguration configuration = new FbConfiguration();
                configuration.ConnectionString = databaseConnectionString;
                try
                {
                    configuration.DatabaseShutdown(FbShutdownMode.Forced, 0);
                    Thread.Sleep(1000);
                }
                finally
                {
                    File.Delete(FQDBFile);
                }
            }

            // Create the new DB
            FbConnection.CreateDatabase(databaseConnectionString, 4096, true, true);
            if (!File.Exists(FQDBFile)) throw new Exception("Database failed to create");

            // Create the Schema
            string script = @"
CREATE TABLE Users(
    UserId integer PRIMARY KEY NOT NULL, 
    Name varchar(200), 
    Age integer, 
    DateOfBirth timestamp, 
    Savings decimal(10,5),
    Is_Male smallint,
    UniqueId char(38),
    TimeSpan time,
    TestEnum varchar(10),
    HouseId integer,
    SupervisorId integer
                );
          
CREATE TABLE ExtraUserInfos(
    ExtraUserInfoId integer PRIMARY KEY NOT NULL, 
    UserId integer NOT NULL, 
    Email varchar(200), 
    Children integer 
);

CREATE TABLE Houses(
    HouseId integer PRIMARY KEY NOT NULL, 
    Address varchar(200)
);

CREATE TABLE CompositeObjects(
    Key1ID integer PRIMARY KEY NOT NULL, 
    Key2ID integer NOT NULL, 
    Key3ID integer NOT NULL, 
    TextData varchar(512), 
    DateEntered timestamp NOT NULL,
    DateUpdated timestamp  
);

CREATE GENERATOR USERS_USERID_GEN;
CREATE GENERATOR EXTRAUSERINFOS_ID_GEN;
CREATE GENERATOR HOUSES_HOUSEID_GEN;

SET TERM ^ ;

CREATE TRIGGER BI_USERS_USERID FOR USERS
ACTIVE BEFORE INSERT
POSITION 0
AS
BEGIN
  IF (NEW.USERID IS NULL) THEN
      NEW.USERID = GEN_ID(USERS_USERID_GEN, 1);
END^


CREATE TRIGGER BI_EXTRAUSERINFOS_ID1 FOR EXTRAUSERINFOS
ACTIVE BEFORE INSERT
POSITION 0
AS
BEGIN
  IF (NEW.EXTRAUSERINFOID IS NULL) THEN
      NEW.EXTRAUSERINFOID = GEN_ID(EXTRAUSERINFOS_ID_GEN, 1);
END^

CREATE TRIGGER BI_HOUSES_HOUSEID FOR HOUSES
ACTIVE BEFORE INSERT
POSITION 0
AS
BEGIN
  IF (NEW.HOUSEID IS NULL) THEN
      NEW.HOUSEID = GEN_ID(HOUSES_HOUSEID_GEN, 1);
END^

SET TERM ; ^

CREATE ROLE %role%;

GRANT SELECT, UPDATE, INSERT, DELETE ON Users TO ROLE %role%;
GRANT SELECT, UPDATE, INSERT, DELETE ON ExtraUserInfos TO ROLE %role%;
GRANT SELECT, UPDATE, INSERT, DELETE ON Houses TO ROLE %role%;
GRANT SELECT, UPDATE, INSERT, DELETE ON CompositeObjects TO ROLE %role%;

GRANT %role% TO %user%;
".Replace("%role%", FbRole).Replace("%user%", FbUserName);

/* 
 * Using new connection so that when a transaction is bound to Connection if it rolls back 
 * it doesn't blow away the tables
 */

            using (var conn = new FbConnection(databaseConnectionString))
            {
                FbScript fbScript = new FbScript(script);
                fbScript.Parse();

                FbBatchExecution fbBatch = new FbBatchExecution(conn);
                fbBatch.AppendSqlStatements(fbScript);
                fbBatch.Execute(true);

                conn.Open();
                Console.WriteLine("Tables (CreateDB): " + Environment.NewLine);
                var dt = conn.GetSchema("Tables", new[] {null, null, null, "TABLE"});
                foreach (DataRow row in dt.Rows)
                {
                    Console.WriteLine(row[2]);
                }

                conn.Close();
            }
        }

        public override void CleanupDataBase()
        {
#if RUN_ALL_TESTS
    
            // Try to delete all fdb files
            foreach(var file in Directory.EnumerateFiles(DBPath, "*.fdb"))
            {
                try
                {
                    File.Delete(file);
                    Console.WriteLine("database deleted : "+ file);
                }
                catch 
                {
                    
                }
            }
#endif
        }
    }
}
#endif