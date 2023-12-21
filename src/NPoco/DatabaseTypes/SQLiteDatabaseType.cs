using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace NPoco.DatabaseTypes
{
    public class SQLiteDatabaseType : DatabaseType
    {
        public override object MapParameterValue(object value)
        {
            if (value is uint)
                return (long)((uint)value);

            return base.MapParameterValue(value);
        }

        private void AdjustSqlInsertCommandText(DbCommand cmd)
        {
            cmd.CommandText += ";\nSELECT last_insert_rowid();";
        }

        public override object ExecuteInsert<T>(IDatabase db, DbCommand cmd, string primaryKeyName, bool useOutputClause, T poco, object[] args)
        {
            if (primaryKeyName != null)
            {
                AdjustSqlInsertCommandText(cmd);
                return ((IDatabaseHelpers)db).ExecuteScalarHelper(cmd);
            }

            ((IDatabaseHelpers)db).ExecuteNonQueryHelper(cmd);
            return -1;
        }

        public override async Task<object> ExecuteInsertAsync<T>(IDatabase db, DbCommand cmd, string primaryKeyName, bool useOutputClause, T poco, object[] args, CancellationToken cancellationToken = default)
        {
            if (primaryKeyName != null)
            {
                AdjustSqlInsertCommandText(cmd);
                return await ((IDatabaseHelpers)db).ExecuteScalarHelperAsync(cmd, cancellationToken).ConfigureAwait(false);
            }

            await ((IDatabaseHelpers)db).ExecuteNonQueryHelperAsync(cmd, cancellationToken).ConfigureAwait(false);
            return -1;
        }

        public override string GetExistsSql()
        {
            return "SELECT EXISTS (SELECT 1 FROM {0} WHERE {1})";
        }

        public override IsolationLevel GetDefaultTransactionIsolationLevel()
        {
            return IsolationLevel.ReadCommitted;
        }

        public override string GetSQLForTransactionLevel(IsolationLevel isolationLevel)
        {
            switch (isolationLevel)
            {
                case IsolationLevel.ReadCommitted:
                    return "SET TRANSACTION ISOLATION LEVEL READ COMMITTED;";

                case IsolationLevel.Serializable:
                    return "SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;";

                default:
                    return "SET TRANSACTION ISOLATION LEVEL READ COMMITTED;";
            }
        }

        public override string GetProviderName()
        {
            return "System.Data.SQLite";
        }
    }
}