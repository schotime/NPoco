#if NET45
using System.Linq.Expressions;
using NPoco.Expressions;
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
        public virtual Task<object> InsertAsync<T>(string tableName, string primaryKeyName, bool autoIncrement, T poco)
        {
            return InsertImp<T, object, Task<object>>(tableName, primaryKeyName, autoIncrement, poco,
                async (cmd, pkname, thepoco, rawvalues, next) => 
                    next(await _dbType.ExecuteInsertAsync(this, cmd, pkname, thepoco, rawvalues.ToArray())), 
                async (cmd, next) => {
                     await _dbType.ExecuteNonQueryAsync(this, cmd);
                     return next();
                });
        }

        public Task<int> UpdateAsync<T>(T poco, Expression<Func<T, object>> fields)
        {
            var expression = DatabaseType.ExpressionVisitor<T>(this);
            expression = expression.Select(fields);
            var columnNames = ((ISqlExpression)expression).SelectMembers.Select(x => x.PocoColumn.ColumnName);
            var otherNames = ((ISqlExpression)expression).GeneralMembers.Select(x => x.PocoColumn.ColumnName);
            return UpdateAsync(poco, columnNames.Union(otherNames));
        }

        public Task<int> UpdateAsync(object poco)
        {
            return UpdateAsync(poco, null, null);
        }

        public Task<int> UpdateAsync(object poco, IEnumerable<string> columns)
        {
            return UpdateAsync(poco, null, columns);
        }

        public Task<int> UpdateAsync(object poco, object primaryKeyValue, IEnumerable<string> columns)
        {
            var pd = PocoDataFactory.ForType(poco.GetType());
            return UpdateAsync(pd.TableInfo.TableName, pd.TableInfo.PrimaryKey, poco, primaryKeyValue, columns);
        }

        public virtual Task<int> UpdateAsync(string tableName, string primaryKeyName, object poco, object primaryKeyValue, IEnumerable<string> columns)
        {
            return UpdateImp (tableName, primaryKeyName, poco, primaryKeyValue, columns,
                async (sql, args, next) => next(await ExecuteAsync(sql, args)), TaskAsyncHelper.FromResult(0));
        }

        internal async Task<int> ExecuteNonQueryHelperAsync(IDbCommand cmd)
        {
            DoPreExecute(cmd);
            var result = await _dbType.ExecuteNonQueryAsync(this, cmd);
            OnExecutedCommand(cmd);
            return result;
        }

        internal async Task<object> ExecuteScalarHelperAsync(IDbCommand cmd)
        {
            DoPreExecute(cmd);
            var result = await _dbType.ExecuteScalarAsync(this, cmd);
            OnExecutedCommand(cmd);
            return result;
        }

        internal async Task<IDataReader> ExecuteReaderHelperAsync(IDbCommand cmd)
        {
            DoPreExecute(cmd);
            var reader = await _dbType.ExecuteReaderAsync(this, cmd);
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
