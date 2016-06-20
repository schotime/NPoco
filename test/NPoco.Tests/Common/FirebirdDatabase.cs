#if !DNXCORE50
using System;
using System.Data;
using System.IO;
using System.Threading;
using FirebirdSql.Data.FirebirdClient;
using FirebirdSql.Data.Isql;
using FirebirdSql.Data.Services;
using NPoco;
using System.Reflection;
using System.Data.Common;

namespace NPoco.Tests.Common
{
    public class FirebirdDatabase : TestDatabase
    {
        private const string FbUserName = "NPocoTest";
        private const string FbUserPass = "test12345";
        private const string FbRole = "NPoco";

        protected string DBName = "UnitTestsDB";
        protected string DBFileName = "UnitTestsDB.fdb";
        protected string FQDBFile { get; set; }

        public FirebirdDatabase()
        {
            DbType = DatabaseType.Firebird;
            ProviderName = DatabaseType.Firebird.GetProviderName();


            DBFileName = DBName + ".fdb";
            FQDBFile = Path.Combine(DBPath, DBFileName);

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
        }


        protected virtual string DBPath
        {
            get
            {
                string location = Assembly.GetExecutingAssembly().Location;

                UriBuilder uri = new UriBuilder(location);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
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
    SupervisorId integer,
    Version timestamp,
    VersionInt int default '0' NOT NULL,
    YorN char(1),
    Address__Street varchar(50),
    Address__City varchar(50)
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
    Key1_ID integer PRIMARY KEY NOT NULL, 
    Key2ID integer NOT NULL, 
    Key3ID integer NOT NULL, 
    TextData varchar(512), 
    DateEntered timestamp NOT NULL,
    DateUpdated timestamp  
);


CREATE TABLE ComplexMap(
    Id integer PRIMARY KEY NOT NULL, 
    Name varchar(50), 
    Nested__Id integer, 
    Nested__Nested2__Id integer, 
    Nested2__Id integer
);

CREATE TABLE RecursionUser(
    Id integer PRIMARY KEY NOT NULL, 
    Name varchar(50), 
    CreatedById integer, 
    SupervisorId integer
);

CREATE TABLE GuidFromDb(
    Id varchar(32) PRIMARY KEY, 
    Name varchar(30)  
);

CREATE TABLE Ones(
    OneId integer PRIMARY KEY NOT NULL, 
    Name varchar(50)
);

CREATE TABLE Manys(
    ManyId integer PRIMARY KEY NOT NULL, 
    OneId integer NOT NULL, 
    AValue integer, 
    Currency varchar(50)
);

CREATE TABLE UserWithAddress(
    Id integer PRIMARY KEY NOT NULL, 
    Name varchar(100) ,
    Address BLOB SUB_TYPE TEXT 
);

CREATE TABLE JustPrimaryKey(
    Id integer PRIMARY KEY NOT NULL
);

CREATE GENERATOR USERS_USERID_GEN;
CREATE GENERATOR EXTRAUSERINFOS_ID_GEN;
CREATE GENERATOR HOUSES_HOUSEID_GEN;
CREATE GENERATOR COMPLEXMAP_ID_GEN;
CREATE GENERATOR RECURSIONUSER_ID_GEN;
CREATE GENERATOR ONES_ID_GEN;
CREATE GENERATOR MANYS_ID_GEN;
CREATE GENERATOR USERWITHADDRESS_ID_GEN;
CREATE GENERATOR JUSTPRIMARYKEY_ID_GEN;

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

CREATE TRIGGER BI_COMPLEXMAP_ID FOR COMPLEXMAP
ACTIVE BEFORE INSERT
POSITION 0
AS
BEGIN
  IF (NEW.ID IS NULL) THEN
      NEW.ID = GEN_ID(COMPLEXMAP_ID_GEN, 1);
END^

CREATE TRIGGER BI_RECURSIONUSER_ID FOR RECURSIONUSER
ACTIVE BEFORE INSERT
POSITION 0
AS
BEGIN
  IF (NEW.ID IS NULL) THEN
      NEW.ID = GEN_ID(RECURSIONUSER_ID_GEN, 1);
END^

CREATE TRIGGER BI_ONES_ID FOR ONES
ACTIVE BEFORE INSERT
POSITION 0
AS
BEGIN
  IF (NEW.ONEID IS NULL) THEN
      NEW.ONEID = GEN_ID(ONES_ID_GEN, 1);
END^

CREATE TRIGGER BI_MANYS_ID FOR MANYS
ACTIVE BEFORE INSERT
POSITION 0
AS
BEGIN
  IF (NEW.MANYID IS NULL) THEN
      NEW.MANYID = GEN_ID(MANYS_ID_GEN, 1);
END^

CREATE TRIGGER BI_USERWITHADDRESS_ID FOR USERWITHADDRESS
ACTIVE BEFORE INSERT
POSITION 0
AS
BEGIN
  IF (NEW.ID IS NULL) THEN
      NEW.ID = GEN_ID(USERWITHADDRESS_ID_GEN, 1);
END^

CREATE TRIGGER BI_JUSTPRIMARYKEY_ID FOR JUSTPRIMARYKEY
ACTIVE BEFORE INSERT
POSITION 0
AS
BEGIN
  IF (NEW.ID IS NULL) THEN
      NEW.ID = GEN_ID(JUSTPRIMARYKEY_ID_GEN, 1);
END^

CREATE PROCEDURE GET_HEX_UUID
RETURNS(
  REAL_UUID CHAR(16) CHARACTER SET OCTETS,
  HEX_UUID VARCHAR(32))
AS
    DECLARE VARIABLE i INTEGER;
    DECLARE VARIABLE c INTEGER;
BEGIN
    real_uuid = GEN_UUID();
    hex_uuid = '';
    i = 0;
    while (i < 16) do
    begin
        c = ascii_val(substring(real_uuid from i+1 for 1));
        if (c < 0) then c = 256 + c;
        hex_uuid = hex_uuid 
        || substring('0123456789abcdef' from bin_shr(c, 4) + 1 for 1) 
        || substring('0123456789abcdef' from bin_and(c, 15) + 1 for 1); 
        i = i + 1;
    end
    suspend;
END^

CREATE TRIGGER GUIDFROMDB_BI FOR GUIDFROMDB
ACTIVE BEFORE INSERT
POSITION 0
AS
DECLARE VARIABLE HEX_UUID VARCHAR(32);
BEGIN
  IF ((NEW.ID IS NULL) OR CHAR_LENGTH(NEW.ID)=0) THEN
  BEGIN
  	FOR SELECT FIRST 1 HEX_UUID 
    FROM GET_HEX_UUID
	    INTO :hex_uuid DO
  	BEGIN
    	new.id = hex_uuid;
    END
  END
END^

SET TERM ; ^

CREATE ROLE %role%;

GRANT SELECT, UPDATE, INSERT, DELETE ON Users TO ROLE %role%;
GRANT SELECT, UPDATE, INSERT, DELETE ON ExtraUserInfos TO ROLE %role%;
GRANT SELECT, UPDATE, INSERT, DELETE ON Houses TO ROLE %role%;
GRANT SELECT, UPDATE, INSERT, DELETE ON CompositeObjects TO ROLE %role%;
GRANT SELECT, UPDATE, INSERT, DELETE ON ComplexMap TO ROLE %role%;
GRANT SELECT, UPDATE, INSERT, DELETE ON RecursionUser TO ROLE %role%;
GRANT SELECT, UPDATE, INSERT, DELETE ON Ones TO ROLE %role%;
GRANT SELECT, UPDATE, INSERT, DELETE ON Manys TO ROLE %role%;
GRANT SELECT, UPDATE, INSERT, DELETE ON UserWithAddress TO ROLE %role%;
GRANT SELECT, UPDATE, INSERT, DELETE ON GuidFromDb TO ROLE %role%;
GRANT SELECT, UPDATE, INSERT, DELETE ON JustPrimaryKey TO ROLE %role%;

GRANT EXECUTE ON PROCEDURE GET_HEX_UUID TO ROLE %role%;

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

                //conn.Open();
                //Console.WriteLine("Tables (CreateDB): " + Environment.NewLine);
                //var dt = conn.GetSchema("Tables", new[] {null, null, null, "TABLE"});
                //foreach (DataRow row in dt.Rows)
                //{
                //    Console.WriteLine(row[2]);
                //}

                //conn.Close();
            }
        }

        public override void CleanupDataBase()
        {

        }

        public override DbProviderFactory GetProviderFactory()
        {
            return FirebirdClientFactory.Instance;
        }
    }
}
#endif