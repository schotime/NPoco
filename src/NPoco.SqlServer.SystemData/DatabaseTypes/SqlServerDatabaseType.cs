using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NPoco.SqlServer.SystemData;

namespace NPoco.DatabaseTypes
{
    public class SqlServerDatabaseType : DatabaseType
    {
        public bool UseOutputClause { get; set; }

        public override bool UseColumnAliases()
        {
            return true;
        }

        public override string BuildPageQuery(long skip, long take, SQLParts parts, ref object[] args)
        {
            return PagingHelper.BuildPaging(skip, take, parts, ref args);
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

        public override object ExecuteInsert<T>(IDatabase db, DbCommand cmd, string primaryKeyName, bool useOutputClause, T poco, object[] args)
        {
            AdjustSqlInsertCommandText(cmd, useOutputClause);
            return ((IDatabaseHelpers)db).ExecuteScalarHelper(cmd);
        }

        public override Task<object> ExecuteInsertAsync<T>(IDatabase db, DbCommand cmd, string primaryKeyName, bool useOutputClause, T poco, object[] args, CancellationToken cancellationToken = default)
        {
            AdjustSqlInsertCommandText(cmd, useOutputClause);
            return ((IDatabaseHelpers)db).ExecuteScalarHelperAsync(cmd, cancellationToken);
        }

        public override string GetExistsSql()
        {
            return "IF EXISTS (SELECT 1 FROM {0} WHERE {1}) SELECT 1 ELSE SELECT 0";
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

        public override void InsertBulk<T>(IDatabase db, IEnumerable<T> pocos, InsertBulkOptions? options)
        {
            SqlBulkCopyHelper.BulkInsert(db, pocos, options);
        }

        public override Task InsertBulkAsync<T>(IDatabase db, IEnumerable<T> pocos, InsertBulkOptions options, CancellationToken cancellationToken = default)
        {
            return SqlBulkCopyHelper.BulkInsertAsync(db, pocos, options, cancellationToken);
        }

        public override string GetProviderName()
        {
            return "Microsoft.Data.SqlClient";
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
                    var p = args[i] as SqlParameter;
                    if (p is null)
                        continue;

                    if (p.Size == 0 || p.SqlDbType == SqlDbType.UniqueIdentifier)
                    {
                        if ((p.Value == DBNull.Value || p.Value == null) && (p.SqlDbType == SqlDbType.NVarChar || p.SqlDbType == SqlDbType.VarChar))
                        {
                            sb.AppendFormat("DECLARE {0}{1} {2} = null\n", GetParameterPrefix(string.Empty), i, p.SqlDbType);
                        }
                        else
                        {
                            sb.AppendFormat("DECLARE {0}{1} {2} = '{3}'\n", GetParameterPrefix(string.Empty), i, p.SqlDbType, p.Value);
                        }
                    }
                    else
                    {
                        sb.AppendFormat("DECLARE {0}{1} {2}({3}) = '{4}'\n", GetParameterPrefix(string.Empty), i, p.SqlDbType, p.Size, p.Value);
                    }
                }
            }

            sb.AppendFormat("\n{0}", sql);

            return sb.ToString();
        }
    }
}