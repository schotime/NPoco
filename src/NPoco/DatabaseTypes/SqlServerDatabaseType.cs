using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace NPoco.DatabaseTypes
{
	public class SqlServerDatabaseType : DatabaseType
	{
		public override string BuildPageQuery(long skip, long take, PagingHelper.SQLParts parts, ref object[] args)
		{
			var sqlPage = string.Format("SELECT * FROM (SELECT ROW_NUMBER() OVER ({0}) peta_rn, * FROM ({1}) peta_base) peta_paged WHERE peta_rn>{2} AND peta_rn<={3}",
																	parts.sqlOrderBy ?? "ORDER BY (SELECT NULL /*poco_dual*/)", parts.sqlUnordered, skip, skip + take);
			args = args.Concat(new object[] { skip, skip + take }).ToArray();

			return sqlPage;
		}

		public override object ExecuteInsert<T>(Database db, IDbCommand cmd, string primaryKeyName, T poco, object[] args)
		{
				//var pocodata = PocoData.ForType(typeof(T), db.PocoDataFactory);
				//var sql = string.Format("SELECT * FROM {0} WHERE {1} = SCOPE_IDENTITY()", EscapeTableName(pocodata.TableInfo.TableName), EscapeSqlIdentifier(primaryKeyName));
				//return db.SingleInto(poco, ";" + cmd.CommandText + ";" + sql, args);
				cmd.CommandText += ";SELECT SCOPE_IDENTITY();";
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