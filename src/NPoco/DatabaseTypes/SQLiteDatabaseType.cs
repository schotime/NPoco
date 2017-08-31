using System;
using System.Data;
using System.Data.Common;

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

        public override object ExecuteInsert<T>(Database db, DbCommand cmd, string primaryKeyName, bool useOutputClause, T poco, object[] args)
        {
            if (primaryKeyName != null)
            {
                AdjustSqlInsertCommandText(cmd);
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
                AdjustSqlInsertCommandText(cmd);
                return await db.ExecuteScalarHelperAsync(cmd);
            }

            await db.ExecuteNonQueryHelperAsync(cmd);
            return TaskAsyncHelper.FromResult<object>(-1);
        }
#endif

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