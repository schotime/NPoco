using System;
using System.Collections.Generic;
using System.Data;

namespace NPoco.DatabaseTypes
{
    public class SQLiteDatabaseType : DatabaseType
    {
        public override object MapParameterValue(object value)
        {
            if (value is uint)
                return (long)((uint)value);

            return base.MapParameterValue(value);
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
            string selectIdSql = string.Empty;
            if (selectLastId)
            {
                selectIdSql = GetSelectIdSql();
            }
            return string.Format("INSERT INTO {0} DEFAULT VALUES {2}", EscapeTableName(tableName), selectIdSql);
        }

        public override string GetInsertSql(string tableName, IEnumerable<string> columnNames, IEnumerable<string> outputColumns, IEnumerable<string> values, bool selectLastId, string idColumnName)
        {
            if (outputColumns != null)
            {
                foreach (var item in outputColumns)
                {
                    throw new NotSupportedException("SQL Compact does not support OUTPUT columns");
                }
            }
            string selectIdSql = string.Empty;
            if (selectLastId)
            {
                selectIdSql = GetSelectIdSql();
            }
            var sql = string.Format("INSERT INTO {0} ({1}) VALUES ({2}){3}",
                                   EscapeTableName(tableName),
                                   string.Join(",", columnNames),                                  
                                   string.Join(",", values),
                                   selectIdSql
                                   );
            return sql;
        }

        private string GetSelectIdSql()
        {
            return ";\nSELECT last_insert_rowid();";
        }

        public override object ExecuteInsert<T>(Database db, IDbCommand cmd, string primaryKeyName, T poco1, object[] args)
        {
            if (primaryKeyName != null)
            {              
                return db.ExecuteScalarHelper(cmd);
            }
            db.ExecuteNonQueryHelper(cmd);
            return -1;
        }

        public override string GetExistsSql()
        {
            return "SELECT EXISTS (SELECT 1 FROM {0} WHERE {1})";
        }

        public override IsolationLevel GetDefaultTransactionIsolationLevel()
        {
            return IsolationLevel.ReadCommitted;
        }

        public override string GetSQLForTransactionLevel(IsolationLevel isolationLevel)
        {
            switch (isolationLevel)
            {
                case IsolationLevel.ReadCommitted:
                    return "SET TRANSACTION ISOLATION LEVEL READ COMMITTED;";

                case IsolationLevel.Serializable:
                    return "SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;";

                default:
                    return "SET TRANSACTION ISOLATION LEVEL READ COMMITTED;";
            }
        }

        public override string GetProviderName()
        {
            return "System.Data.SQLite";
        }
    }
}