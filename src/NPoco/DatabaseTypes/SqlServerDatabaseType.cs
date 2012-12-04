using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace NPoco.DatabaseTypes
{
    public class SqlServerDatabaseType : DatabaseType
    {
        public override string BuildPageQuery(long skip, long take, PagingHelper.SQLParts parts, ref object[] args)
        {
            parts.sqlSelectRemoved = PagingHelper.rxOrderBy.Replace(parts.sqlSelectRemoved, "", 1);
            var sqlPage = string.Format("SELECT * FROM (SELECT ROW_NUMBER() OVER ({0}) peta_rn, {1}) peta_paged WHERE peta_rn>@{2} AND peta_rn<=@{3}",
                                        parts.sqlOrderBy ?? "ORDER BY (SELECT NULL /*poco_dual*/)", parts.sqlSelectRemoved, args.Length, args.Length + 1);
            args = args.Concat(new object[] { skip, skip + take }).ToArray();

            return sqlPage;
        }

        public override object ExecuteInsert(Database db, IDbCommand cmd, string primaryKeyName)
        {
            cmd.CommandText = cmd.CommandText + ";SELECT SCOPE_IDENTITY();";
            return db.ExecuteScalarHelper(cmd);
        }

        public override string GetExistsSql()
        {
            return "IF EXISTS (SELECT 1 FROM {0} WHERE {1}) SELECT 1 ELSE SELECT 0";
        }

        public override void InsertBulk<T>(IDatabase db, IEnumerable<T> pocos)
        {
            SqlBulkCopyHelper.BulkInsert(db, pocos);
        }

        public override IsolationLevel GetDefaultTransactionIsolationLevel()
        {
            return IsolationLevel.ReadCommitted;
        }

        public override string GetProviderName()
        {
            return "System.Data.SqlClient";
        }
    }
}