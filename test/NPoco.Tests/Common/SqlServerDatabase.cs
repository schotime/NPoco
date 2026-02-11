using System;
using Microsoft.Data.SqlClient;
using NPoco.DatabaseTypes;

namespace NPoco.Tests.Common
{
    public class SqlServerDatabase : TestDatabase
    {
        private const string DBName = "NPoco_UnitTests";
        private readonly string _masterConnectionString;

        public SqlServerDatabase(string connectionString)
        {
            DbType = new SqlServer2012DatabaseType();
            _masterConnectionString = connectionString;
            ConnectionString = new SqlConnectionStringBuilder(connectionString)
            {
                InitialCatalog = DBName
            }.ToString();
            ProviderName = "Microsoft.Data.SqlClient";

            RecreateDataBase();
            EnsureSharedConnectionConfigured();
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
            base.RecreateDataBase();

            using var conn = new SqlConnection(_masterConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = string.Format(@"
                IF (EXISTS(SELECT name FROM sys.databases WHERE name = '{0}'))
                BEGIN
                    ALTER DATABASE [{0}] SET single_user WITH rollback immediate
                    DROP DATABASE [{0}]
                END
            ", DBName);
            cmd.ExecuteNonQuery();

            cmd.CommandText = string.Format("CREATE DATABASE [{0}]", DBName);
            cmd.ExecuteNonQuery();

            cmd.Connection.ChangeDatabase(DBName);

            SqlServerSchema.CreateSchema(cmd);
        }

        public override void CleanupDataBase()
        {
        }
    }
}
