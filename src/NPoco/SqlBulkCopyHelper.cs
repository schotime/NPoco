using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace NPoco
{
    public class SqlBulkCopyHelper
    {
        public static Func<IDbConnection, SqlConnection> SqlConnectionResolver = dbConn => (SqlConnection)dbConn;
        public static Func<IDbTransaction, SqlTransaction> SqlTransactionResolver = dbTran => (SqlTransaction)dbTran;

        public static void BulkInsert<T>(IDatabase db, IEnumerable<T> list)
        {
            using (var bulkCopy = new SqlBulkCopy(SqlConnectionResolver(db.Connection), SqlBulkCopyOptions.Default, SqlTransactionResolver(db.Transaction)))
            {
                var pocoData = PocoData.ForType(typeof(T), db.PocoDataFactory);

                bulkCopy.BatchSize = 4096;
                bulkCopy.DestinationTableName = pocoData.TableInfo.TableName;

                var table = new DataTable();
                var cols = pocoData.Columns.Where(x => !pocoData.TableInfo.AutoIncrement || !x.Value.ColumnName.Equals(pocoData.TableInfo.PrimaryKey, StringComparison.OrdinalIgnoreCase)).ToList();

                foreach (var col in cols)
                {
                    bulkCopy.ColumnMappings.Add(col.Value.MemberInfo.Name, col.Value.ColumnName);
                    table.Columns.Add(col.Value.MemberInfo.Name, Nullable.GetUnderlyingType(col.Value.MemberInfo.GetMemberInfoType()) ?? col.Value.MemberInfo.GetMemberInfoType());
                }

                foreach (var item in list)
                {
                    var values = new object[cols.Count];
                    for (var i = 0; i < values.Length; i++)
                    {
                        values[i] = cols[i].Value.GetValue(item);
                    }

                    table.Rows.Add(values);
                }

                bulkCopy.WriteToServer(table);
            }
        }
    }
}
