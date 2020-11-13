using NPoco.Expressions;
using System;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Threading.Tasks;

namespace NPoco.DatabaseTypes
{
    public class OracleDatabaseType : DatabaseType
    {
        public override SqlExpression<T> ExpressionVisitor<T>(IDatabase db, PocoData pocoData, bool prefixTableName)
        {
            return new OracleExpression<T>(db, pocoData, prefixTableName);
        }

        public override string GetParameterPrefix(string connectionString)
        {
            return ":";
        }

        public override void PreExecute(DbCommand cmd)
        {
            cmd.GetType().GetProperty("BindByName").SetValue(cmd, true, null);
            cmd.CommandText = cmd.CommandText.Replace("/*poco_dual*/", "from dual");
        }

        public override string BuildPageQuery(long skip, long take, PagingHelper.SQLParts parts, ref object[] args)
        {
            if (parts.sqlSelectRemoved.StartsWith("*"))
                throw new Exception("Query must alias '*' when performing a paged query.\neg. select t.* from table t order by t.id");

            // Same deal as SQL Server
            return PagingHelper.BuildPaging(skip, take, parts, ref args);
        }

        public override string EscapeSqlIdentifier(string str)
        {
            return string.Format("\"{0}\"", str.ToUpperInvariant());
        }

        public override string GetAutoIncrementExpression(TableInfo ti)
        {
            if (!string.IsNullOrEmpty(ti.SequenceName))
                return string.Format("{0}.nextval", ti.SequenceName);

            return null;
        }

        private DbParameter AdjustSqlInsertCommandText(DbCommand cmd, string primaryKeyName)
        {
            cmd.CommandText += string.Format(" returning {0} into :newid", EscapeSqlIdentifier(primaryKeyName));
            var param = cmd.CreateParameter();
            param.ParameterName = ":newid";
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

        public override async Task<object> ExecuteInsertAsync<T>(Database db, DbCommand cmd, string primaryKeyName, bool useOutputClause, T poco, object[] args)
        {
            if (primaryKeyName != null)
            {
                var param = AdjustSqlInsertCommandText(cmd, primaryKeyName);
                await db.ExecuteNonQueryHelperAsync(cmd).ConfigureAwait(false);
                return param.Value;
            }

            await db.ExecuteNonQueryHelperAsync(cmd).ConfigureAwait(false);
            return -1;
        }

        public override string GetProviderName()
        {
            return "Oracle.DataAccess.Client";
        }
    }
}
