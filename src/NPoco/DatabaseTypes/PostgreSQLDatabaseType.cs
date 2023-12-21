using NPoco.Expressions;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace NPoco.DatabaseTypes
{
    public class PostgreSQLDatabaseType : DatabaseType
    {
        public override ISqlExpression<T> ExpressionVisitor<T>(IDatabase db, PocoData pocoData, bool prefixTableName)
        {
            return new PostgreSQLExpression<T>(db, pocoData, prefixTableName);
        }

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

        public override object ExecuteInsert<T>(IDatabase db, DbCommand cmd, string primaryKeyName, bool useOutputClause, T poco, object[] args)
        {
            if (primaryKeyName != null)
            {
                AdjustSqlInsertCommandText(cmd, primaryKeyName);
                return ((IDatabaseHelpers)db).ExecuteScalarHelper(cmd);
            }

            ((IDatabaseHelpers)db).ExecuteNonQueryHelper(cmd);
            return -1;
        }

        public override async Task<object> ExecuteInsertAsync<T>(IDatabase db, DbCommand cmd, string primaryKeyName, bool useOutputClause, T poco, object[] args, CancellationToken cancellationToken = default)
        {
            if (primaryKeyName != null)
            {
                AdjustSqlInsertCommandText(cmd, primaryKeyName);
                return await ((IDatabaseHelpers)db).ExecuteScalarHelperAsync(cmd, cancellationToken).ConfigureAwait(false);
            }

            await ((IDatabaseHelpers)db).ExecuteNonQueryHelperAsync(cmd, cancellationToken).ConfigureAwait(false);
            return -1;
        }

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