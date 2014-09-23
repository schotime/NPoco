using System;
using System.Linq;
using MySql.Data.MySqlClient;

namespace NPoco.Tests.Common
{
    public class MySqlDatabase : TestDatabase
    {
        private const string Server = "localhost";
        private const string User = "root";
        private const string Password = "1";
        private const string DbName = "NPoco_Tests";

        public MySqlDatabase()
        {
            DbType = DatabaseType.MySQL;
            ProviderName = DatabaseType.MySQL.GetProviderName();

            var sb = new MySqlConnectionStringBuilder();
            sb.Server = Server;
            sb.UserID = User;
            sb.Password = Password;
            ConnectionString = sb.ToString();

            RecreateDataBase();
            EnsureSharedConnectionConfigured();
        }

        public override void EnsureSharedConnectionConfigured()
        {
            if (Connection != null)
                return;

            lock (_syncRoot)
            {
                Connection = new MySqlConnection(ConnectionString);
                Connection.Open();
            }
        }

        public override void RecreateDataBase()
        {
            base.RecreateDataBase();

            using (var conn = new MySqlConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    // Create the new DB
                    cmd.CommandText = String.Format("DROP DATABASE IF EXISTS " + DbName);
                    cmd.ExecuteNonQuery();
                    cmd.CommandText = String.Format("CREATE DATABASE " + DbName);
                    cmd.ExecuteNonQuery();
                    cmd.Connection.ChangeDatabase(DbName);

                    // Create the Schema
                    cmd.CommandText = @"
                        CREATE TABLE Users(
                            UserId int NOT NULL AUTO_INCREMENT PRIMARY KEY,
                            Name nvarchar(200) NULL,
                            Age int NULL,
                            DateOfBirth datetime NULL,
                            Savings decimal(10,5) NULL,
                            Is_Male tinyint,
                            UniqueId CHAR(36) NULL,
                            TimeSpan time NULL,
                            TestEnum varchar(10) NULL,
                            HouseId int NULL,
                            SupervisorId int NULL
                        );";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = @"
                        CREATE TABLE ExtraUserInfos(
                            ExtraUserInfoId int NOT NULL AUTO_INCREMENT PRIMARY KEY,
                            UserId int NOT NULL,
                            Email nvarchar(200) NULL,
                            Children int NULL
                        );";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = @"
                        CREATE TABLE Houses(
                            HouseId int NOT NULL AUTO_INCREMENT PRIMARY KEY,
                            Address nvarchar(200)
                        );";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = @"
                        CREATE TABLE CompositeObjects(
                            Key1ID int NOT NULL PRIMARY KEY,
                            Key2ID int NOT NULL,
                            Key3ID int NOT NULL,
                            TextData nvarchar(512) NULL,
                            DateEntered datetime NOT NULL,
                            DateUpdated datetime NULL
                        );";
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public override void CleanupDataBase()
        {
            using (var conn = new MySqlConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = String.Format("DROP DATABASE IF EXISTS " + DbName);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
