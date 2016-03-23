using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;

namespace NPoco.DatabaseTypes
{
    public class SqlServerDatabaseType : DatabaseType
    {
        public bool UseOutputClause { get; set; }

        private static readonly Regex OrderByAlias = new Regex(@"[\""\[\]\w]+\.([\[\]\""\w]+)", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnoreCase);
        private static readonly Regex OrderByColumns = new Regex(@"(?:([\""\[\]\w]+)\.)?([\[\]\""\w]+)(?:\s(asc|desc))?", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnoreCase);
        private static readonly Regex StripOrderBy = new Regex(@"ORDER\s+BY\s+", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnoreCase);

        public override bool UseColumnAliases()
        {
            return true;
        }

        public override string BuildPageQuery(long skip, long take, PagingHelper.SQLParts parts, ref object[] args)
        {
            // fixme - issue: if the 'order by' column is aliased?
            // SELECT table.id, table.name AS whatever FROM table ORDER BY table.name
            // => (SELECT poco_base.*, ROW_NUMBER() OVER (ORDER BY name) poco_rn FROM (SELECT table.id, table.name AS whatever FROM table) poco_base) ...
            // fails, because 'name' is not a column in table poco_base

            var over = "ORDER BY (SELECT NULL /*poco_dual*/)";
            if (parts.sqlOrderBy != null)
            {
                var trimChars = new[] { '[', ']', '"' };
                over = OrderByColumns.Replace(StripOrderBy.Replace(parts.sqlOrderBy, ""), m =>
                {
                    var table = m.Groups[1].Value.Trim(trimChars);
                    var column = m.Groups[2].Value.Trim(trimChars);
                    var dir = m.Groups[3].Success ? (" " + m.Groups[3].Value) : null;

                    // expensive, obviously, but what else?
                    var lookup = new Regex(@"(?:[\[\""]?" + table + @"[\]\""]?\.)?[\[\""]?" + column + @"[\]\""]?\s+AS\s+([\[\]\""\w]+)", RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnoreCase);
                    var matches = lookup.Matches(parts.sqlUnordered);

                    if (matches.Count == 0) return EscapeSqlIdentifier(column) + dir; // obvious

                    var match = matches.Count == 1
                        ? matches[0] // use single match
                        : matches.Cast<Match>().FirstOrDefault(x => x.Groups[1].Success); // use first fq (what else?)
                    if (match == null) match = matches[0]; // use first (what else?)
                    return EscapeSqlIdentifier(match.Groups[1].Value.Trim(trimChars)) + dir; // use the alias
                });
                over = "ORDER BY " + over;
            }

            var sqlPage = string.Format("SELECT {4} FROM (SELECT poco_base.*, ROW_NUMBER() OVER ({0}) poco_rn \nFROM ( \n{1}) poco_base ) poco_paged \nWHERE poco_rn > @{2} AND poco_rn <= @{3} \nORDER BY poco_rn",
                over, parts.sqlUnordered, args.Length, args.Length + 1, parts.sqlColumns);
            args = args.Concat(new object[] { skip, skip + take }).ToArray();

            return sqlPage;
        }

        private void AdjustSqlInsertCommandText(DbCommand cmd, bool useOutputClause)
        {
            if (!UseOutputClause && !useOutputClause)
            {
                cmd.CommandText += ";SELECT SCOPE_IDENTITY();";
            }
        }

        public override string GetInsertOutputClause(string primaryKeyName, bool useOutputClause)
        {
            if (UseOutputClause || useOutputClause)
            {
                return string.Format(" OUTPUT INSERTED.{0}", EscapeSqlIdentifier(primaryKeyName));
            }
            return base.GetInsertOutputClause(primaryKeyName, useOutputClause);
        }

        public override string GetDefaultInsertSql(string tableName, string primaryKeyName, bool useOutputClause, string[] names, string[] parameters)
        {
            return string.Format("INSERT INTO {0}{1} DEFAULT VALUES", EscapeTableName(tableName), GetInsertOutputClause(primaryKeyName, useOutputClause));
        }

        public override object ExecuteInsert<T>(Database db, DbCommand cmd, string primaryKeyName, bool useOutputClause, T poco, object[] args)
        {
            AdjustSqlInsertCommandText(cmd, useOutputClause);
            return db.ExecuteScalarHelper(cmd);
        }

#if !NET35 && !NET40
        public override System.Threading.Tasks.Task<object> ExecuteInsertAsync<T>(Database db, DbCommand cmd, string primaryKeyName, bool useOutputClause, T poco, object[] args)
        {
            AdjustSqlInsertCommandText(cmd, useOutputClause);
            return ExecuteScalarAsync(db, cmd);
        }
#endif

        public override string GetExistsSql()
        {
            return "IF EXISTS (SELECT 1 FROM {0} WHERE {1}) SELECT 1 ELSE SELECT 0";
        }

#if !DNXCORE50
        public override void InsertBulk<T>(IDatabase db, IEnumerable<T> pocos)
        {
            SqlBulkCopyHelper.BulkInsert(db, pocos);
        }
#endif

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

        public override object ProcessDefaultMappings(PocoColumn pocoColumn, object value)
        {
            if (pocoColumn.MemberInfoData.MemberType == typeof (byte[]) && value == null)
            {
                return new SqlParameter("__bytes", SqlDbType.VarBinary, -1) { Value = DBNull.Value };
            }
            return base.ProcessDefaultMappings(pocoColumn, value);
        }
    }
}