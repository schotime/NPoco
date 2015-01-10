using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace NPoco.DatabaseTypes
{
    public class SqlServerCEDatabaseType : DatabaseType
    {
        public override string BuildPageQuery(long skip, long take, PagingHelper.SQLParts parts, ref object[] args)
        {
            var sqlPage = string.Format("{0}\nOFFSET @{1} ROWS FETCH NEXT @{2} ROWS ONLY", parts.sql, args.Length, args.Length + 1);
            args = args.Concat(new object[] { skip, take }).ToArray();
            return sqlPage;
        }

        public override object ExecuteInsert<T>(Database db, IDbCommand cmd, string primaryKeyName, T poco1, object[] args)
        {
            db.ExecuteNonQueryHelper(cmd);
            return db.ExecuteScalar<object>("SELECT @@@IDENTITY AS NewID;");
        }

        public override string GetDefaultInsertSql(string tableName, IEnumerable<string> outputColumns, bool selectLastId, string idColumnName)
        {
            if (outputColumns != null)
            {
                foreach (var item in outputColumns)
                {
                    throw new NotSupportedException("SQL Compact does not support OUTPUT columns");
                }
            }
            throw new NotSupportedException("SQL Compact does not support INSERT of all default values, atleast one column must be included in the INSERT.");         
        
        }

        public override string GetInsertSql(string tableName, IEnumerable<string> columnNames, IEnumerable<string> outputColumns, IEnumerable<string> values, bool selectLastId, string idColumnName)
        {
            // INSERT INTO my_table VALUES ()           
            if (outputColumns != null)
            {
                foreach (var item in outputColumns)
                {
                    throw new NotSupportedException("SQL Compact does not support OUTPUT columns");
                }
            }
          
            var sql = string.Format("INSERT INTO {0} ({1}) VALUES ({2})",
                                   EscapeTableName(tableName),
                                   string.Join(",", columnNames),                                  
                                   string.Join(",", values)
                                   );
            return sql;
        }

        public override IsolationLevel GetDefaultTransactionIsolationLevel()
        {
            return IsolationLevel.ReadCommitted;
        }

        public override string GetProviderName()
        {
            return "System.Data.SqlServerCe.4.0";
        }
    }
}