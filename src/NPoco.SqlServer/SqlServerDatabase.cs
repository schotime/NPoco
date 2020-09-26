using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace NPoco.DatabaseTypes
{
    public class SqlServerDatabase : Database
    {
        public SqlServerDatabase(string connectionString) 
            : base(connectionString, new SqlServer2012DatabaseType(), Microsoft.Data.SqlClient.SqlClientFactory.Instance)
        {
        }

        public SqlServerDatabase(string connectionString, SqlServerDatabaseType databaseType) 
            : base(connectionString, databaseType, Microsoft.Data.SqlClient.SqlClientFactory.Instance)
        {
        }
    }
}
