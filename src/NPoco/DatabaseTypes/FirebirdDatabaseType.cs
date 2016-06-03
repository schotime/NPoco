
using System;
using System.Data;
using System.Data.Common;
using System.Text;
using NPoco.Expressions;

namespace NPoco.DatabaseTypes
{
    public class FirebirdDatabaseType : DatabaseType
    {
        public override string GetParameterPrefix(string connectionString)
        {
            return "@";
        }

        public override void PreExecute(DbCommand cmd)
        {
            cmd.CommandText = cmd.CommandText.Replace("/*poco_dual*/", "from RDB$DATABASE");
        }

        public override string EscapeTableName(string tableName)
        {
            return tableName;
        }

        public override string EscapeSqlIdentifier(string str)
        {
            return str;
        }

        public override string BuildPageQuery(long skip, long take, PagingHelper.SQLParts parts, ref object[] args)
        {
            StringBuilder sql = new StringBuilder("SELECT ");

            if (take > 0)
                sql.AppendFormat("FIRST {0} ", take);

            if (skip > 0)
                sql.AppendFormat("SKIP {0} ", skip);

            sql.Append(parts.sqlSelectRemoved);
            return sql.ToString();
        }


        public override string GetDefaultInsertSql(string tableName, string primaryKeyName, bool useOutputClause, string[] names, string[] parameters)
        {
            return string.Format("INSERT INTO {0} ({1}) VALUES ({2})", EscapeTableName(tableName), string.Join(",", names), string.Join(",", parameters));
        }


        private DbParameter AdjustSqlInsertCommandText(DbCommand cmd, string primaryKeyName)
        {
            cmd.CommandText += string.Format(" returning {0}", EscapeSqlIdentifier(primaryKeyName));
            var param = cmd.CreateParameter();
            param.ParameterName = primaryKeyName;
            param.Value = DBNull.Value;
            param.Direction = ParameterDirection.ReturnValue;
            param.DbType = DbType.Int64;
            cmd.Parameters.Add(param);
            return param;
        }

        public override object ExecuteInsert<T>(Database db, DbCommand cmd, string primaryKeyName, bool useOutputClause, T poco, object[] args)
        {
            if (primaryKeyName != null)
            {
                var param = AdjustSqlInsertCommandText(cmd, primaryKeyName);
                db.ExecuteNonQueryHelper(cmd);
                return param.Value;
            }

            db.ExecuteNonQueryHelper(cmd);
            return -1;
        }

#if !NET35 && !NET40
        public override async System.Threading.Tasks.Task<object> ExecuteInsertAsync<T>(Database db, DbCommand cmd, string primaryKeyName, bool useOutputClause, T poco, object[] args)
        {
            if (primaryKeyName != null)
            {
                var param = AdjustSqlInsertCommandText(cmd, primaryKeyName);
                await db.ExecuteNonQueryHelperAsync(cmd);
                return param.Value;
            }

            await db.ExecuteNonQueryHelperAsync(cmd);
            return TaskAsyncHelper.FromResult<object>(-1);
        }
#endif

        public override SqlExpression<T> ExpressionVisitor<T>(IDatabase db, PocoData pocoData, bool prefixTableName)
        {
            return new FirebirdSqlExpression<T>(db, pocoData, prefixTableName);
        }

        public override string GetProviderName()
        {
            return "FirebirdSql.Data.FirebirdClient";
        }
    }
}