using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;

namespace NPoco
{
#if !DNXCORE50
    public class SqlBulkCopyHelper
    {
        public static Func<DbConnection, SqlConnection> SqlConnectionResolver = dbConn => (SqlConnection)dbConn;
        public static Func<DbTransaction, SqlTransaction> SqlTransactionResolver = dbTran => (SqlTransaction)dbTran;

        public static void BulkInsert<T>(IDatabase db, IEnumerable<T> list)
        {
            BulkInsert(db, list, SqlBulkCopyOptions.Default);
        }

        public static void BulkInsert<T>(IDatabase db, IEnumerable<T> list, SqlBulkCopyOptions sqlBulkCopyOptions)
        {
            using (var bulkCopy = new SqlBulkCopy(SqlConnectionResolver(db.Connection), sqlBulkCopyOptions, SqlTransactionResolver(db.Transaction)))
            {
                var table = BuildBulkInsertDataTable(db, list, bulkCopy, sqlBulkCopyOptions);
                bulkCopy.WriteToServer(table);
            }
        }
#if NET45 || NETSTANDARD20
        public static async System.Threading.Tasks.Task BulkInsertAsync<T>(IDatabase db, IEnumerable<T> list, SqlBulkCopyOptions sqlBulkCopyOptions)
        {
            using (var bulkCopy = new SqlBulkCopy(SqlConnectionResolver(db.Connection), sqlBulkCopyOptions, SqlTransactionResolver(db.Transaction)))
            {
                var table = BuildBulkInsertDataTable(db, list, bulkCopy, sqlBulkCopyOptions);
                await bulkCopy.WriteToServerAsync(table).ConfigureAwait(false);
            }
        }
#endif

        private static DataTable BuildBulkInsertDataTable<T>(IDatabase db, IEnumerable<T> list, SqlBulkCopy bulkCopy, SqlBulkCopyOptions sqlBulkCopyOptions)
        {
            var pocoData = db.PocoDataFactory.ForType(typeof (T));

            bulkCopy.BatchSize = 4096;
            bulkCopy.DestinationTableName = pocoData.TableInfo.TableName;

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
                bulkCopy.ColumnMappings.Add(col.Value.MemberInfoData.Name, col.Value.ColumnName);
                table.Columns.Add(col.Value.MemberInfoData.Name, Nullable.GetUnderlyingType(col.Value.MemberInfoData.MemberType) ?? col.Value.MemberInfoData.MemberType);
            }

            foreach (var item in list)
            {
                var values = new object[cols.Count];
                for (var i = 0; i < values.Length; i++)
                {
                    var value = cols[i].Value.GetValue(item);
                    if (db.Mappers != null)
                    {
                        value = db.Mappers.FindAndExecute(x => x.GetToDbConverter(cols[i].Value.ColumnType, cols[i].Value.MemberInfoData.MemberInfo), value);
                    }

                    value = db.DatabaseType.MapParameterValue(value);

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
#endif
    }
