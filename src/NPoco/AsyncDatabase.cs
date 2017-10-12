#if !NET35 && !NET40
using System.Collections;
using System.Linq.Expressions;
using NPoco.Expressions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
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
            var tableInfo = PocoDataFactory.TableInfoForType(poco.GetType());
            return InsertAsync(tableInfo.TableName, tableInfo.PrimaryKey, tableInfo.AutoIncrement, poco);
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
            var pd = PocoDataFactory.ForObject(poco, primaryKeyName, autoIncrement);
            return InsertAsyncImp(pd, tableName, primaryKeyName, autoIncrement, poco);
        }

        private async Task<object> InsertAsyncImp<T>(PocoData pocoData, string tableName, string primaryKeyName, bool autoIncrement, T poco)
        {
            if (!OnInsertingInternal(new InsertContext(poco, tableName, autoIncrement, primaryKeyName)))
                return 0;

            try
            {
                OpenSharedConnectionInternal();

                var preparedInsert = InsertStatements.PrepareInsertSql(this, pocoData, tableName, primaryKeyName, autoIncrement, poco);

                using (var cmd = CreateCommand(_sharedConnection, preparedInsert.Sql, preparedInsert.Rawvalues.ToArray()))
                {
                    // Assign the Version column
                    InsertStatements.AssignVersion(poco, preparedInsert);

                    object id;
                    if (!autoIncrement)
                    {
                        await _dbType.ExecuteNonQueryAsync(this, cmd).ConfigureAwait(false);
                        id = InsertStatements.AssignNonIncrementPrimaryKey(primaryKeyName, poco, preparedInsert);
                    }
                    else
                    {
                        id = await _dbType.ExecuteInsertAsync(this, cmd, primaryKeyName, preparedInsert.PocoData.TableInfo.UseOutputClause, poco, preparedInsert.Rawvalues.ToArray()).ConfigureAwait(false);
                        InsertStatements.AssignPrimaryKey(primaryKeyName, poco, id, preparedInsert);
                    }

                    return id;
                }
            }
            catch (Exception x)
            {
                OnExceptionInternal(x);
                throw;
            }
            finally
            {
                CloseSharedConnectionInternal();
            }
        }

        public Task<int> UpdateAsync<T>(T poco, Expression<Func<T, object>> fields)
        {
            var expression = DatabaseType.ExpressionVisitor<T>(this, PocoDataFactory.ForType(typeof(T)));
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
            var tableInfo = PocoDataFactory.TableInfoForType(poco.GetType());
            return UpdateAsync(tableInfo.TableName, tableInfo.PrimaryKey, poco, primaryKeyValue, columns);
        }

        public virtual Task<int> UpdateAsync(string tableName, string primaryKeyName, object poco, object primaryKeyValue, IEnumerable<string> columns)
        {
            return UpdateImp (tableName, primaryKeyName, poco, primaryKeyValue, columns,
                async (sql, args, next) => next(await ExecuteAsync(sql, args).ConfigureAwait(false)), TaskAsyncHelper.FromResult(0));
        }

        public Task<int> DeleteAsync(object poco)
        {
            var tableInfo = PocoDataFactory.TableInfoForType(poco.GetType());
            return DeleteAsync(tableInfo.TableName, tableInfo.PrimaryKey, poco);
        }

        public Task<int> DeleteAsync(string tableName, string primaryKeyName, object poco)
        {
            return DeleteAsync(tableName, primaryKeyName, poco, null);
        }

        public virtual Task<int> DeleteAsync(string tableName, string primaryKeyName, object poco, object primaryKeyValue)
        {
            return DeleteImp(tableName, primaryKeyName, poco, primaryKeyValue, ExecuteAsync, TaskAsyncHelper.FromResult(0));
        }

        public Task<Page<T>> PageAsync<T>(long page, long itemsPerPage, Sql sql)
        {
            return PageAsync<T>(page, itemsPerPage, sql.SQL, sql.Arguments);
        }

        public Task<Page<T>> PageAsync<T>(long page, long itemsPerPage, string sql, params object[] args)
        {
            return PageImp<T, Task<Page<T>>>(page, itemsPerPage, sql, args,
                async (paged, thesql) =>
                {
                    paged.Items = (await QueryAsync<T>(thesql).ConfigureAwait(false)).ToList();
                    return paged;
                });
        }

        public Task<List<T>> FetchAsync<T>(long page, long itemsPerPage, string sql, params object[] args)
        {
            return SkipTakeAsync<T>((page - 1) * itemsPerPage, itemsPerPage, sql, args);
        }

        public Task<List<T>> FetchAsync<T>(long page, long itemsPerPage, Sql sql)
        {
            return SkipTakeAsync<T>((page - 1) * itemsPerPage, itemsPerPage, sql.SQL, sql.Arguments);
        }

        public Task<List<T>> SkipTakeAsync<T>(long skip, long take, string sql, params object[] args)
        {
            string sqlCount, sqlPage;
            BuildPageQueries<T>(skip, take, sql, ref args, out sqlCount, out sqlPage);
            return FetchAsync<T>(sqlPage, args);
        }

        public Task<List<T>> SkipTakeAsync<T>(long skip, long take, Sql sql)
        {
            return SkipTakeAsync<T>(skip, take, sql.SQL, sql.Arguments);
        }

        public Task<List<T>> FetchAsync<T>()
        {
            return FetchAsync<T>("");
        }

        public async Task<List<T>> FetchAsync<T>(string sql, params object[] args)
        {
            return (await QueryAsync<T>(sql, args).ConfigureAwait(false)).ToList();
        }

        public async Task<List<T>> FetchAsync<T>(Sql sql)
        {
            return (await QueryAsync<T>(sql).ConfigureAwait(false)).ToList();
        }

        public Task<IEnumerable<T>> QueryAsync<T>(string sql, params object[] args)
        {
            return QueryAsync<T>(new Sql(sql, args));
        }

        public Task<IEnumerable<T>> QueryAsync<T>(Sql sql)
        {
            return QueryAsync(default(T), null, null, sql);
        }

        internal async Task<IEnumerable<T>> QueryAsync<T>(T instance, Expression<Func<T, IList>> listExpression, Func<T, object[]> idFunc, Sql Sql)
        {
            var sql = Sql.SQL;
            var args = Sql.Arguments;

            if (EnableAutoSelect) sql = AutoSelectHelper.AddSelectClause(this, typeof(T), sql);

            try
            {
                OpenSharedConnectionInternal();
                DbCommand cmd = CreateCommand(_sharedConnection, sql, args); 
                DbDataReader r;
                try
                {
                    r = await ExecuteReaderHelperAsync(cmd).ConfigureAwait(false);
                }
                catch (Exception x)
                {
                    cmd.Dispose();
                    OnExceptionInternal(x);
                    throw;
                }

                return listExpression != null ? ReadOneToMany(instance, r, cmd, listExpression, idFunc) : Read<T>(typeof(T), instance, r, cmd);
            }
            catch
            {
                CloseSharedConnectionInternal();
                throw;
            }
        }

        public async Task<T> SingleAsync<T>(string sql, params object[] args)
        {
            return (await QueryAsync<T>(sql, args).ConfigureAwait(false)).Single();
        }

        public async Task<T> SingleAsync<T>(Sql sql)
        {
            return (await QueryAsync<T>(sql).ConfigureAwait(false)).Single();
        }

        public async Task<T> SingleOrDefaultAsync<T>(string sql, params object[] args)
        {
            return (await QueryAsync<T>(sql, args).ConfigureAwait(false)).SingleOrDefault();
        }

        public async Task<T> SingleOrDefaultAsync<T>(Sql sql)
        {
            return (await QueryAsync<T>(sql).ConfigureAwait(false)).SingleOrDefault();
        }

        public async Task<T> SingleByIdAsync<T>(object primaryKey)
        {
            var sql = GenerateSingleByIdSql<T>(primaryKey);
            return (await QueryAsync<T>(sql).ConfigureAwait(false)).Single();
        }

        public async Task<T> SingleOrDefaultByIdAsync<T>(object primaryKey)
        {
            var sql = GenerateSingleByIdSql<T>(primaryKey);
            return (await QueryAsync<T>(sql).ConfigureAwait(false)).SingleOrDefault();
        }

        public async Task<T> FirstAsync<T>(string sql, params object[] args)
        {
            return (await QueryAsync<T>(sql, args).ConfigureAwait(false)).First();
        }

        public async Task<T> FirstAsync<T>(Sql sql)
        {
            return (await QueryAsync<T>(sql).ConfigureAwait(false)).First();
        }

        public async Task<T> FirstOrDefaultAsync<T>(string sql, params object[] args)
        {
            return (await QueryAsync<T>(sql, args).ConfigureAwait(false)).FirstOrDefault();
        }

        public async Task<T> FirstOrDefaultAsync<T>(Sql sql)
        {
            return (await QueryAsync<T>(sql).ConfigureAwait(false)).FirstOrDefault();
        }

        public Task<int> ExecuteAsync(string sql, params object[] args)
        {
            return ExecuteAsync(new Sql(sql, args));
        }

        public async Task<int> ExecuteAsync(Sql Sql)
        {
            var sql = Sql.SQL;
            var args = Sql.Arguments;

            try
            {
                OpenSharedConnectionInternal();
                using (var cmd = CreateCommand(_sharedConnection, sql, args))
                {
                    var result = await ExecuteNonQueryHelperAsync(cmd).ConfigureAwait(false);
                    return result;
                }
            }
            catch (Exception x)
            {
                OnExceptionInternal(x);
                throw;
            }
            finally
            {
                CloseSharedConnectionInternal();
            }
        }

        public Task<T> ExecuteScalarAsync<T>(string sql, params object[] args)
        {
            return ExecuteScalarAsync<T>(new Sql(sql, args));
        }

        public async Task<T> ExecuteScalarAsync<T>(Sql Sql)
        {
            var sql = Sql.SQL;
            var args = Sql.Arguments;

            try
            {
                OpenSharedConnectionInternal();
                using (var cmd = CreateCommand(_sharedConnection, sql, args))
                {
                    object val = await ExecuteScalarHelperAsync(cmd).ConfigureAwait(false);

                    if (val == null || val == DBNull.Value)
                        return await TaskAsyncHelper.FromResult(default(T)).ConfigureAwait(false);

                    Type t = typeof(T);
                    Type u = Nullable.GetUnderlyingType(t);

                    return (T)Convert.ChangeType(val, u ?? t);
                }
            }
            catch (Exception x)
            {
                OnExceptionInternal(x);
                throw;
            }
            finally
            {
                CloseSharedConnectionInternal();
            }
        }

        internal async Task<int> ExecuteNonQueryHelperAsync(DbCommand cmd)
        {
            DoPreExecute(cmd);
            var result = await _dbType.ExecuteNonQueryAsync(this, cmd).ConfigureAwait(false);
            OnExecutedCommandInternal(cmd);
            return result;
        }

        internal async Task<object> ExecuteScalarHelperAsync(DbCommand cmd)
        {
            DoPreExecute(cmd);
            var result = await _dbType.ExecuteScalarAsync(this, cmd).ConfigureAwait(false);
            OnExecutedCommandInternal(cmd);
            return result;
        }

        internal async Task<DbDataReader> ExecuteReaderHelperAsync(DbCommand cmd)
        {
            DoPreExecute(cmd);
            var reader = await _dbType.ExecuteReaderAsync(this, cmd).ConfigureAwait(false);
            OnExecutedCommandInternal(cmd);
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
