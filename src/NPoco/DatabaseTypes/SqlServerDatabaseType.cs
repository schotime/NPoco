using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace NPoco.DatabaseTypes
{
    public class SqlServerDatabaseType : DatabaseType
    {
        public bool UseOutputClause { get; set; }

        private static readonly Regex OrderByAlias = new Regex(@"[\""\[\]\w]+\.([\[\]\""\w]+)", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnoreCase);

        public override bool UseColumnAliases()
        {
            return true;
        }

        public override string BuildPageQuery(long skip, long take, PagingHelper.SQLParts parts, ref object[] args)
        {
            parts.sqlOrderBy = string.IsNullOrEmpty(parts.sqlOrderBy) ? null : OrderByAlias.Replace(parts.sqlOrderBy, "$1");
            var sqlPage = string.Format("SELECT {4} FROM (SELECT poco_base.*, ROW_NUMBER() OVER ({0}) poco_rn \nFROM ( \n{1}) poco_base ) poco_paged \nWHERE poco_rn > @{2} AND poco_rn <= @{3} \nORDER BY poco_rn",
                parts.sqlOrderBy ?? "ORDER BY (SELECT NULL /*poco_dual*/)", parts.sqlUnordered, args.Length, args.Length + 1, parts.sqlColumns);
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
            return db.ExecuteScalarHelperAsync(cmd);
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

        public override string FormatCommand(string sql, object[] args)
        {            
            if (sql == null)
                return "";

            var sb = new StringBuilder();
            if (args != null && args.Length > 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    var value = args[i];
                    var type = args[i] != null ? args[i].GetType().Name : string.Empty;
                    var formatted = args[i] as FormattedParameter;
                    if (formatted != null)
                    {
                        type = formatted.Type != null ? formatted.Type.Name : string.Format("{0}, {1}", formatted.Parameter.GetType().Name, formatted.Parameter.DbType);
                        value = formatted.Value;
                    }

                    var p = new System.Data.SqlClient.SqlParameter();
                    ParameterHelper.SetParameterValue(this, p, args[i]);
                    if (p.Size == 0 || p.SqlDbType == SqlDbType.UniqueIdentifier)
                    {
                        if (value == null && (p.SqlDbType == SqlDbType.NVarChar || p.SqlDbType == SqlDbType.VarChar))
                        {
                            sb.AppendFormat("DECLARE {0}{1} {2} = null\n", GetParameterPrefix(), i, p.SqlDbType);
                        }
                        else
                        {
                            sb.AppendFormat("DECLARE {0}{1} {2} = '{3}'\n", GetParameterPrefix(), i, p.SqlDbType, value);
                        }
                    }
                    else
                    {
                        sb.AppendFormat("DECLARE {0}{1} {2}[{3}] = '{4}'\n", GetParameterPrefix(), i, p.SqlDbType, p.Size, value);
                    }
                }
            }

            sb.AppendFormat("\n{0}", sql);

            return sb.ToString();
        }
    }
}