
using System;
using System.Data;
using System.Text;
using NPoco.Expressions;

namespace NPoco.DatabaseTypes
{
    /// <summary>
    /// Support for Firebird databases
    /// <remarks>
    ///   Firebird doesn’t have some native guid-datatype alike to bool
    ///   Firebird ADO.NET Provider dosen't support batch queries. (Database.FetchMultiple)
    ///  </remarks>
    /// </summary>
    public class FirebirdDatabaseType : DatabaseType
    {

        public override string GetParameterPrefix(string connectionString)
        {
            return "@";
        }

        public override void PreExecute(IDbCommand cmd)
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


        public override string GetDefaultInsertSql(string tableName, string[] names, string[] parameters)
        {
            return string.Format("INSERT INTO {0} ({1}) VALUES ({2})", EscapeTableName(tableName), string.Join(",", names), string.Join(",", parameters));
        }

        public override object ExecuteInsert<T>(Database db, IDbCommand cmd, string primaryKeyName, T poco, object[] args)
        {
            if (primaryKeyName != null)
            {
                cmd.CommandText += string.Format(" returning {0}", EscapeSqlIdentifier(primaryKeyName));
                var param = cmd.CreateParameter();
                param.ParameterName = primaryKeyName;
                param.Value = DBNull.Value;
                param.Direction = ParameterDirection.ReturnValue;
                param.DbType = DbType.Int64;
                cmd.Parameters.Add(param);
                db.ExecuteNonQueryHelper(cmd);
                return param.Value;
            }
            
            db.ExecuteNonQueryHelper(cmd);
            return -1;
        }

        public override SqlExpression<T> ExpressionVisitor<T>(IDatabase db, bool prefixTableName)
        {
            return new FirebirdSqlExpression<T>(db, prefixTableName);
        }

        public override string GetProviderName()
        {
            return "FirebirdSql.Data.FirebirdClient";
        }
    }
}