using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
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

        public override string GetDefaultInsertSql(string tableName, IEnumerable<string> outputColumns, bool selectLastId, string idColumnName)
        {
            var outputClause = GetInsertOutputClause(outputColumns);
            string selectIdSql = string.Empty;
            if (selectLastId)
            {
                selectIdSql = GetSelectIdSql();
            }
            return string.Format("INSERT INTO {0} {1} DEFAULT VALUES {2}", EscapeTableName(tableName), outputClause, selectIdSql);

        }

        public override string GetInsertSql(string tableName, IEnumerable<string> columnNames, IEnumerable<string> outputColumns, IEnumerable<string> values, bool selectLastId, string idColumnName)
        {
            var outputClause = GetInsertOutputClause(outputColumns);
            string selectIdSql = string.Empty;
            if (selectLastId)
            {
                selectIdSql = GetSelectIdSql();
            }
            var sql = string.Format("INSERT INTO {0} ({1}){2} VALUES ({3}){4}",
                                   EscapeTableName(tableName),
                                   string.Join(",", columnNames.ToArray()),
                                   outputClause,
                                   string.Join(",", values.ToArray()),
                                   selectIdSql
                                   );

            return sql;
        }

        public override object ExecuteInsert<T>(Database db, IDbCommand cmd, string primaryKeyName, T poco1, object[] args)
        {
            //var pocodata = PocoData.ForType(typeof(T), db.PocoDataFactory);
            //var sql = string.Format("SELECT * FROM {0} WHERE {1} = SCOPE_IDENTITY()", EscapeTableName(pocodata.TableInfo.TableName), EscapeSqlIdentifier(primaryKeyName));
            //return db.SingleInto(poco, ";" + cmd.CommandText + ";" + sql, args);
            // cmd.CommandText += ";SELECT SCOPE_IDENTITY();";
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

        #region private methods

        private string GetSelectIdSql()
        {
            return ";SELECT SCOPE_IDENTITY();";
        }

        private string GetInsertOutputClause(IEnumerable<string> outputColumnNames)
        {
            if (outputColumnNames != null && outputColumnNames.Any())
            {
                var builder = new StringBuilder("OUTPUT ");

                foreach (var item in outputColumnNames)
                {
                    builder.Append("INSERTED.");
                    builder.Append(item);
                    builder.Append(",");
                }

                builder.Remove(builder.Length - 1, 1);
                return builder.ToString();
            }

            return string.Empty;
            //return base.GetInsertOutputClause(outputColumnNames);
        }

        #endregion


    }
}