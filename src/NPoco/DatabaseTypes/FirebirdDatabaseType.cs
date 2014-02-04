
namespace NPoco.DatabaseTypes
{
    public class FirebirdDatabaseType : DatabaseType
    {
        public override string GetParameterPrefix(string connectionString)
        {
            return "@";
        }

        public override string EscapeSqlIdentifier(string str)
        {
            return string.Format("\"{0}\"", str);
        }

        /*
        public override string GetExistsSql()
        {
            return "SELECT (SELECT 1 FROM {0} WHERE {1}) AS id FROM RDB$DATABASE";
        }
        */
        public override string GetDefaultInsertSql(string tableName, string[] names, string[] parameters)
        {
            return string.Format("INSERT INTO {0} ({1}) VALUES ({2})", EscapeTableName(tableName), string.Join(",", names), string.Join(",", parameters));
        }

        public override string GetProviderName()
        {
            return "FirebirdSql.Data.FirebirdClient";
        }
    }
}