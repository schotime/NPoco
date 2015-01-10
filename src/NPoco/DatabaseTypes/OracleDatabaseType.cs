using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace NPoco.DatabaseTypes
{
    public class OracleDatabaseType : DatabaseType
    {
        public override string GetParameterPrefix(string connectionString)
        {
            return ":";
        }

        public override void PreExecute(IDbCommand cmd)
        {
            cmd.GetType().GetProperty("BindByName").SetValue(cmd, true, null);
            cmd.CommandText = cmd.CommandText.Replace("/*poco_dual*/", "from dual");
        }

        public override string BuildPageQuery(long skip, long take, PagingHelper.SQLParts parts, ref object[] args)
        {
            if (parts.sqlSelectRemoved.StartsWith("*"))
                throw new Exception("Query must alias '*' when performing a paged query.\neg. select t.* from table t order by t.id");

            // Same deal as SQL Server
            return Singleton<SqlServerDatabaseType>.Instance.BuildPageQuery(skip, take, parts, ref args);
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

        public virtual string GetDefaultInsertSql(string tableName, IEnumerable<string> outputColumns, bool selectLastId, string idColumnName)
        {
            var outputClause = GetInsertOutputClause(outputColumns, selectLastId, idColumnName);
            return string.Format("INSERT INTO {0} DEFAULT VALUES {1}", EscapeTableName(tableName), outputClause);
        }

        public override object ExecuteInsert<T>(Database db, IDbCommand cmd, string primaryKeyName, T poco1, object[] args)
        {
            if (primaryKeyName != null)
            {
              //  cmd.CommandText += string.Format(" returning {0} into :newid", EscapeSqlIdentifier(primaryKeyName));
                var param = cmd.CreateParameter();
                param.ParameterName = ":newid";
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

        public override string GetProviderName()
        {
            return "Oracle.DataAccess.Client";
        }


        private string GetInsertOutputClause(IEnumerable<string> outputColumnNames, bool selectLastId, string idColumnName)
        {
            bool hasOutputColumns = outputColumnNames != null && outputColumnNames.Any();

            if (hasOutputColumns || selectLastId)
            {
                var builder = new StringBuilder("returning ");
                if (hasOutputColumns)
                {
                    foreach (var item in outputColumnNames)
                    {
                        builder.AppendFormat("{0} into :{0}", EscapeSqlIdentifier(item));
                        builder.Append(", ");
                    }


                    builder.Remove(builder.Length - 1, 1);

                }

                if (selectLastId)
                {                
                    builder.AppendFormat("{0} into :newid", EscapeSqlIdentifier(idColumnName));
                }
                return builder.ToString();
            }

            return string.Empty;
        }
    }
}
