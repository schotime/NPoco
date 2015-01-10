using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Linq;

namespace NPoco.DatabaseTypes
{
    public class PostgreSQLDatabaseType : DatabaseType
    {
        public override object MapParameterValue(object value)
        {
            // Don't map bools to ints in PostgreSQL
            if (value is bool) return value;

            return base.MapParameterValue(value);
        }

        public override string EscapeSqlIdentifier(string str)
        {
            return string.Format("\"{0}\"", str);
        }

        public virtual string GetDefaultInsertSql(string tableName, IEnumerable<string> outputColumns, bool selectLastId, string idColumnName)
        {
            var outputClause = GetInsertOutputClause(outputColumns, selectLastId, idColumnName);           
            return string.Format("INSERT INTO {0} DEFAULT VALUES {1}", EscapeTableName(tableName), outputClause);
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

        public override object ExecuteInsert<T>(Database db, IDbCommand cmd, string primaryKeyName, T poco1, object[] args)
        {
            if (primaryKeyName != null)
            {
                //cmd.CommandText += string.Format(" returning {0} as NewID", EscapeSqlIdentifier(primaryKeyName));
                return db.ExecuteScalarHelper(cmd);
            }

            db.ExecuteNonQueryHelper(cmd);
            return -1;
        }

        public override string GetProviderName()
        {
            return "Npgsql2";
        }
        
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
                    builder.Append(EscapeSqlIdentifier(idColumnName));
                    builder.Append(" as NewID");
                }
                return builder.ToString();
            }

            return string.Empty;
        }
    }
}