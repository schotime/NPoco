using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;

namespace NPoco.DatabaseTypes
{
    public class SqlServerDatabaseType : DatabaseType
    {
        private static readonly Regex OrderByAlias = new Regex(@"[\""\[\]\w]+\.([\[\]\""\w]+)", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnoreCase);

        public override bool UseColumnAliases()
        {
            return true;
        }

        public override string BuildPageQuery(long skip, long take, PagingHelper.SQLParts parts, ref object[] args)
        {
            parts.sqlOrderBy = string.IsNullOrEmpty(parts.sqlOrderBy) ? null : OrderByAlias.Replace(parts.sqlOrderBy, "$1");
            var sqlPage = string.Format("SELECT {4} FROM (SELECT ROW_NUMBER() OVER ({0}) poco_rn, poco_base.* \nFROM ( \n{1}) poco_base ) poco_paged \nWHERE poco_rn > {2} AND poco_rn <= {3} \nORDER BY poco_rn",
                                                                    parts.sqlOrderBy ?? "ORDER BY (SELECT NULL /*poco_dual*/)", parts.sqlUnordered, skip, skip + take, parts.sqlColumns);
            args = args.Concat(new object[] { skip, skip + take }).ToArray();

            return sqlPage;
        }

        private static void AdjustSqlInsertCommandText(IDbCommand cmd)
        {
            cmd.CommandText += ";SELECT SCOPE_IDENTITY();";
        }

        public override object ExecuteInsert<T>(Database db, IDbCommand cmd, string primaryKeyName, T poco, object[] args)
        {
            //var pocodata = PocoData.ForType(typeof(T), db.PocoDataFactory);
            //var sql = string.Format("SELECT * FROM {0} WHERE {1} = SCOPE_IDENTITY()", EscapeTableName(pocodata.TableInfo.TableName), EscapeSqlIdentifier(primaryKeyName));
            //return db.SingleInto(poco, ";" + cmd.CommandText + ";" + sql, args);
            AdjustSqlInsertCommandText(cmd);
            return db.ExecuteScalarHelper(cmd);
        }

#if NET45
        public override System.Threading.Tasks.Task<object> ExecuteInsertAsync<T>(Database db, IDbCommand cmd, string primaryKeyName, T poco, object[] args)
        {
            AdjustSqlInsertCommandText(cmd);
            return ExecuteScalarAsync(db, cmd);
        }

        public override System.Threading.Tasks.Task<int> ExecuteNonQueryAsync(Database database, IDbCommand cmd)
        {
            var sqlCommand = cmd as SqlCommand;
            if (sqlCommand != null)
                return sqlCommand.ExecuteNonQueryAsync();
            return base.ExecuteNonQueryAsync(database, cmd);
        }

        public override async System.Threading.Tasks.Task<object> ExecuteScalarAsync(Database database, IDbCommand cmd)
        {
            var dbCommand = cmd as SqlCommand;
            
            if (dbCommand != null)
            {
#if NET40ASYNC
                using (var reader = await dbCommand.ExecuteReaderAsync())
                {
                    if (reader.FieldCount > 0 && reader.Read())
                        return await TaskAsyncHelper.FromResult(reader.GetValue(0));
                    return TaskAsyncHelper.FromResult((object)null);
                }
#else
                return await dbCommand.ExecuteScalarAsync().ConfigureAwait(false);
#endif
            }
            return await base.ExecuteScalarAsync(database, cmd).ConfigureAwait(false);
        }

        public override async System.Threading.Tasks.Task<IDataReader> ExecuteReaderAsync(Database database, IDbCommand cmd)
        {
            var dbCommand = cmd as SqlCommand;
            if (dbCommand != null)
            {
                return await dbCommand.ExecuteReaderAsync().ConfigureAwait(false);
            }

            return await base.ExecuteReaderAsync(database, cmd).ConfigureAwait(false);
        }
#endif

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

        public override DbType? LookupDbType(Type type, string name)
        {
            if (type == typeof(TimeSpan) || type == typeof(TimeSpan?))
                return null;

            return base.LookupDbType(type, name);
        }

        public override string GetProviderName()
        {
            return "System.Data.SqlClient";
        }
    }
}