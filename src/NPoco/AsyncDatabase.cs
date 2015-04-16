#if !POCO_NO_DYNAMIC
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace NPoco
{
    public partial class Database
    {
        /// <summary>
        /// Performs an SQL Insert
        /// </summary>
        /// <param name="tableName">The name of the table to insert into</param>
        /// <param name="primaryKeyName">The name of the primary key column of the table</param>
        /// <param name="poco">The POCO object that specifies the column values to be inserted</param>
        /// <returns>The auto allocated primary key of the new record</returns>
        public Task<object> InsertAsync(string tableName, string primaryKeyName, object poco)
        {
            return InsertAsync(tableName, primaryKeyName, true, poco);
        }


        /// <summary>
        /// Performs an SQL Insert
        /// </summary>
        /// <param name="poco">The POCO object that specifies the column values to be inserted</param>
        /// <returns>The auto allocated primary key of the new record, or null for non-auto-increment tables</returns>
        /// <remarks>The name of the table, it's primary key and whether it's an auto-allocated primary key are retrieved
        /// from the POCO's attributes</remarks>
        public Task<object> InsertAsync<T>(T poco)
        {
            var pd = PocoDataFactory.ForType(poco.GetType());
            return InsertAsync(pd.TableInfo.TableName, pd.TableInfo.PrimaryKey, pd.TableInfo.AutoIncrement, poco);
        }

        /// <summary>
        /// Performs an SQL Insert
        /// </summary>
        /// <param name="tableName">The name of the table to insert into</param>
        /// <param name="primaryKeyName">The name of the primary key column of the table</param>
        /// <param name="autoIncrement">True if the primary key is automatically allocated by the DB</param>
        /// <param name="poco">The POCO object that specifies the column values to be inserted</param>
        /// <returns>The auto allocated primary key of the new record, or null for non-auto-increment tables</returns>
        /// <remarks>Inserts a poco into a table.  If the poco has a property with the same name 
        /// as the primary key the id of the new record is assigned to it.  Either way,
        /// the new id is returned.</remarks>
        public virtual async Task<object> InsertAsync<T>(string tableName, string primaryKeyName, bool autoIncrement, T poco)
        {
            return await InsertImpAsync(tableName, primaryKeyName, autoIncrement, poco);
        }

        public virtual async Task<object> InsertImpAsync<T>(string tableName, string primaryKeyName, bool autoIncrement, T poco)
        {
            if (!OnInserting(new InsertContext(poco, tableName, autoIncrement, primaryKeyName)))
                return 0;

            try
            {
                var preparedSql = InsertStatements.PrepareInsertSql(this, tableName, primaryKeyName, autoIncrement, poco);

                using (var cmd = CreateCommand(_sharedConnection, preparedSql.sql, preparedSql.rawvalues.ToArray()))
                {
                    // Assign the Version column
                    InsertStatements.AssignVersion(poco, preparedSql);

                    if (autoIncrement)
                    {
                        var id = await _dbType.ExecuteInsertAsync(this, cmd, primaryKeyName, poco, preparedSql.rawvalues.ToArray());

                        // Assign the ID back to the primary key property
                        InsertStatements.AssignPrimaryKey(primaryKeyName, poco, id, preparedSql);

                        return id;
                    }
                    else
                    {
                        await _dbType.ExecuteNonQueryAsync(this, cmd);
                        return InsertStatements.AssignNonIncrementPrimaryKey(primaryKeyName, poco, preparedSql);
                    }
                }
            }
            catch (Exception x)
            {
                OnException(x);
                throw;
            }
            finally
            {
                CloseSharedConnectionInternal();
            }
        }

        internal Task<int> ExecuteNonQueryHelperAsync(IDbCommand cmd)
        {
            DoPreExecute(cmd);
            var result = _dbType.ExecuteNonQueryAsync(this, cmd);
            OnExecutedCommand(cmd);
            return result;
        }

        internal Task<object> ExecuteScalarHelperAsync(IDbCommand cmd)
        {
            DoPreExecute(cmd);
            var result = _dbType.ExecuteScalarAsync(this, cmd);
            OnExecutedCommand(cmd);
            return result;
        }

        internal Task<IDataReader> ExecuteReaderHelperAsync(IDbCommand cmd)
        {
            DoPreExecute(cmd);
            var reader = _dbType.ExecuteReaderAsync(this, cmd);
            OnExecutedCommand(cmd);
            return reader;
        }
    }

    public class TaskAsyncHelper
    {
        public static Task<T> FromResult<T>(T value)
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetResult(value);
            return tcs.Task;
        }
    }
}

#endif
