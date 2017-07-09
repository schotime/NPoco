using System.Data;
using System.Data.Common;

namespace NPoco.DatabaseTypes
{
    public class PostgreSQLDatabaseType : DatabaseType
    {
        public override object MapParameterValue(object value)
        {
            // Don't map bools to ints in PostgreSQL
            if (value is bool) return value;

            return base.MapParameterValue(value);
        }

        public override string EscapeSqlIdentifier(string str)
        {
            return string.Format("\"{0}\"", str);
        }
        
        private void AdjustSqlInsertCommandText(DbCommand cmd, string primaryKeyName)
        {
            cmd.CommandText += string.Format(" returning {0} as NewID", EscapeSqlIdentifier(primaryKeyName));
        }

        public override object ExecuteInsert<T>(Database db, DbCommand cmd, string primaryKeyName, bool useOutputClause, T poco, object[] args)
        {
            if (primaryKeyName != null)
            {
                AdjustSqlInsertCommandText(cmd, primaryKeyName);
                return db.ExecuteScalarHelper(cmd);
            }

            db.ExecuteNonQueryHelper(cmd);
            return -1;
        }

#if !NET35 && !NET40
        public override async System.Threading.Tasks.Task<object> ExecuteInsertAsync<T>(Database db, DbCommand cmd, string primaryKeyName, bool useOutputClause, T poco, object[] args)
        {
            if (primaryKeyName != null)
            {
                AdjustSqlInsertCommandText(cmd, primaryKeyName);
                return await db.ExecuteScalarHelperAsync(cmd);
            }

            await db.ExecuteNonQueryHelperAsync(cmd);
            return TaskAsyncHelper.FromResult<object>(-1);
        }
#endif

        public override string GetParameterPrefix(string connectionString)
        {
            return "@p";
        }

        public override string GetProviderName()
        {
            return "Npgsql2";
        }
    }
}