
using System;
using System.Data;
using System.Text;
using NPoco.Expressions;
using System.Collections.Generic;
using System.Linq;

namespace NPoco.DatabaseTypes
{
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

        public override string GetInsertSql(string tableName, IEnumerable<string> columnNames, IEnumerable<string> outputColumns, IEnumerable<string> values, bool selectLastId, string idColumnName)
        {
            var outputClause = GetInsertOutputClause(outputColumns, selectLastId, idColumnName);
            var sql = string.Format("INSERT INTO {0} ({1}) VALUES ({3}) {2}",
                                   EscapeTableName(tableName),
                                   string.Join(",", columnNames),
                                   outputClause,
                                   string.Join(",", values)
                                   );
            return sql;
        }
        
        public override string GetDefaultInsertSql(string tableName, IEnumerable<string> outputColumns, bool selectLastId, string idColumnName)
        {
            var outputClause = GetInsertOutputClause(outputColumns, selectLastId, idColumnName);
            return string.Format("INSERT INTO {0} DEFAULT VALUES {1}", EscapeTableName(tableName), outputClause);
        }

        public override object ExecuteInsert<T>(Database db, IDbCommand cmd, string primaryKeyName, T poco1, object[] args)
        {
            if (primaryKeyName != null)
            {
                // cmd.CommandText += string.Format(" returning {0}", EscapeSqlIdentifier(primaryKeyName));
                var param = cmd.CreateParameter();
                param.ParameterName = primaryKeyName;
                param.Value = DBNull.Value;
                param.Direction = ParameterDirection.ReturnValue;
                param.DbType = DbType.Int64;
                cmd.Parameters.Add(param);


                // TODO: ALSO ADD RETURN VALUES FOR OUTPUT COLUMNS?
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

        #region private methods

        private string GetInsertOutputClause(IEnumerable<string> outputColumnNames, bool selectLastId, string idColumnName)
        {
            bool hasOutputColumns = outputColumnNames != null && outputColumnNames.Any();

            if (hasOutputColumns || selectLastId)
            {
                var builder = new StringBuilder("returning ");
                if (hasOutputColumns)
                {
                    builder.Append(string.Join(", ", outputColumnNames));
                }

                if (selectLastId)
                {
                    if (hasOutputColumns)
                    {
                        builder.Append(", ");
                    }
                    builder.Append(idColumnName);
                    //builder.Append(" as NewID");
                }
                return builder.ToString();
            }

            return string.Empty;
        }

        #endregion
    }
}