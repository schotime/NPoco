using System.Data;

namespace NPoco.DatabaseTypes
{
    public class MySqlDatabaseType : DatabaseType
    {
        public override string GetParameterPrefix(string connectionString)
        {
            if (connectionString != null && connectionString.IndexOf("Allow User Variables=true") >= 0)
                return "?";

            return "@";
        }

        public override void PreExecute(IDbCommand cmd)
        {
            cmd.CommandText = cmd.CommandText.Replace("/*poco_dual*/", "from dual");
        }

        public override string EscapeSqlIdentifier(string str)
        {
            return string.Format("`{0}`", str);
        }

        public override string GetExistsSql()
        {
            return "SELECT EXISTS (SELECT 1 FROM {0} WHERE {1})";
        }

        public override string GetDefaultInsertSql(string tableName, string[] names, string[] parameters)
        {
            return string.Format("INSERT INTO {0} ({1}) VALUES ({2})", EscapeTableName(tableName), string.Join(",", names), string.Join(",", parameters));
        }

        public override string GetProviderName()
        {
            return "MySql.Data.MySQLClient";
        }
    }
}