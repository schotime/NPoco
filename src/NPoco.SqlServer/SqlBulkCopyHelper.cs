using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace NPoco.SqlServer
{
    public class SqlBulkCopyHelper
    {
        public static Func<DbConnection, SqlConnection> SqlConnectionResolver = dbConn => (SqlConnection)dbConn;
        public static Func<DbTransaction, SqlTransaction> SqlTransactionResolver = dbTran => (SqlTransaction)dbTran;

        public static void BulkInsert<T>(IDatabase db, IEnumerable<T> list, InsertBulkOptions? insertBulkOptions)
        {
            BulkInsert(db, list, SqlBulkCopyOptions.Default, insertBulkOptions);
        }

        public static void BulkInsert<T>(IDatabase db, IEnumerable<T> list, SqlBulkCopyOptions sqlBulkCopyOptions, InsertBulkOptions? insertBulkOptions)
        {
            using (var bulkCopy = new SqlBulkCopy(SqlConnectionResolver(db.Connection), sqlBulkCopyOptions, SqlTransactionResolver(db.Transaction)))
            {
                var table = BuildBulkInsertDataTable(db, list, bulkCopy, sqlBulkCopyOptions, insertBulkOptions);
                bulkCopy.WriteToServer(table);
            }
        }

        public static Task BulkInsertAsync<T>(IDatabase db, IEnumerable<T> list, InsertBulkOptions sqlBulkCopyOptions)
        {
            return BulkInsertAsync(db, list, SqlBulkCopyOptions.Default, sqlBulkCopyOptions);
        }

        public static async Task BulkInsertAsync<T>(IDatabase db, IEnumerable<T> list, SqlBulkCopyOptions sqlBulkCopyOptions, InsertBulkOptions insertBulkOptions)
        {
            using (var bulkCopy = new SqlBulkCopy(SqlConnectionResolver(db.Connection), sqlBulkCopyOptions, SqlTransactionResolver(db.Transaction)))
            {
                var table = BuildBulkInsertDataTable(db, list, bulkCopy, sqlBulkCopyOptions, insertBulkOptions);
                await bulkCopy.WriteToServerAsync(table).ConfigureAwait(false);
            }
        }


        private static DataTable BuildBulkInsertDataTable<T>(IDatabase db, IEnumerable<T> list, SqlBulkCopy bulkCopy, SqlBulkCopyOptions sqlBulkCopyOptions, InsertBulkOptions? insertBulkOptions)
        {
            var pocoData = db.PocoDataFactory.ForType(typeof (T));

            bulkCopy.BatchSize = 4096;
            bulkCopy.DestinationTableName = db.DatabaseType.EscapeTableName(pocoData.TableInfo.TableName);

            if (insertBulkOptions?.BulkCopyTimeout != null)
                bulkCopy.BulkCopyTimeout = insertBulkOptions.BulkCopyTimeout.Value; 

            var table = new DataTable();
            var cols = pocoData.Columns.Where(x =>
            {
                if (x.Value.ResultColumn) return false;
                if (x.Value.ComputedColumn) return false;
                if (x.Value.ColumnName.Equals(pocoData.TableInfo.PrimaryKey, StringComparison.OrdinalIgnoreCase))
                {
                    if (sqlBulkCopyOptions == SqlBulkCopyOptions.KeepIdentity)
                        return true;

                    return pocoData.TableInfo.AutoIncrement == false;
                }
                return true;
            }).ToList();

            foreach (var col in cols)
            {
                bulkCopy.ColumnMappings.Add(col.Value.MemberInfoKey, col.Value.ColumnName);
                table.Columns.Add(col.Value.MemberInfoKey, Nullable.GetUnderlyingType(col.Value.MemberInfoData.MemberType) ?? col.Value.MemberInfoData.MemberType);
            }

            foreach (var item in list)
            {
                var values = new object[cols.Count];
                for (var i = 0; i < values.Length; i++)
                {
                    var value = db.DatabaseType.MapParameterValue(db.ProcessMapper(cols[i].Value, cols[i].Value.GetValue(item!)));
                    if (value.GetTheType() == typeof (SqlParameter))
                    {
                        value = ((SqlParameter) value).Value;
                    }

                    var newType = value.GetTheType();
                    if (newType != null && newType != typeof (DBNull))
                    {
                        table.Columns[i].DataType = newType;
                    }

                    values[i] = value;
                }

                table.Rows.Add(values);
            }
            return table;
        }
    }
}
