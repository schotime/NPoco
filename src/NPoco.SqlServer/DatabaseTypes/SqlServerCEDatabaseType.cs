using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NPoco.DatabaseTypes
{
    public class SqlServerCEDatabaseType : DatabaseType
    {
        public override string BuildPageQuery(long skip, long take, SQLParts parts, ref object[] args)
        {
            var sqlPage = string.Format("{0}\nOFFSET @{1} ROWS FETCH NEXT @{2} ROWS ONLY", parts.sql, args.Length, args.Length + 1);
            args = args.Concat(new object[] { skip, take }).ToArray();
            return sqlPage;
        }

        public override object ExecuteInsert<T>(IDatabase db, DbCommand cmd, string primaryKeyName, bool useOutputClause, T poco, object[] args)
        {
            ((IDatabaseHelpers)db).ExecuteNonQueryHelper(cmd);
            return db.ExecuteScalar<object>("SELECT @@@IDENTITY AS NewID;");
        }

        public override async Task<object> ExecuteInsertAsync<T>(IDatabase db, DbCommand cmd, string primaryKeyName, bool useOutputClause, T poco, object[] args, CancellationToken cancellationToken = default)
        {
            await ((IDatabaseHelpers)db).ExecuteNonQueryHelperAsync(cmd, cancellationToken).ConfigureAwait(false);
            return await db.ExecuteScalarAsync<object>("SELECT @@@IDENTITY AS NewID;", cancellationToken).ConfigureAwait(false);
        }

        public override IsolationLevel GetDefaultTransactionIsolationLevel()
        {
            return IsolationLevel.ReadCommitted;
        }

        public override string GetProviderName()
        {
            return "System.Data.SqlServerCe.4.0";
        }
    }
}