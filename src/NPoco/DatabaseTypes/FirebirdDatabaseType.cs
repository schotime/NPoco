
using System;
using System.Data;

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

        public override object ExecuteInsert<T>(Database db, IDbCommand cmd, string primaryKeyName, T poco, object[] args)
        {
            if (primaryKeyName != null)
            {
                cmd.CommandText += string.Format(" returning {0}", EscapeSqlIdentifier(primaryKeyName));
                var param = cmd.CreateParameter();
                param.ParameterName = primaryKeyName;
                param.Value = DBNull.Value;
                param.Direction = ParameterDirection.ReturnValue;
                param.DbType = DbType.Int64;
                cmd.Parameters.Add(param);
                db.ExecuteNonQueryHelper(cmd);
                return param.Value;
            }

            db.ExecuteNonQueryHelper(cmd);
            return -1;
        }

        public override string GetProviderName()
        {
            return "FirebirdSql.Data.FirebirdClient";
        }
    }
}