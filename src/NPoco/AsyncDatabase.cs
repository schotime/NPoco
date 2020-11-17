using System.Collections;
using System.Linq.Expressions;
using NPoco.Expressions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using NPoco.Extensions;
using NPoco.Linq;

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
            return InsertAsyncImp(pd, tableName, primaryKeyName, autoIncrement, poco, false);
        }

        private async Task<object> InsertAsyncImp<T>(PocoData pocoData, string tableName, string primaryKeyName, bool autoIncrement, T poco, bool sync)
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
                        _ = sync
                            ? ExecuteNonQueryHelper(cmd)
                            : await ExecuteNonQueryHelperAsync(cmd).ConfigureAwait(false);

                        id = InsertStatements.AssignNonIncrementPrimaryKey(primaryKeyName, poco, preparedInsert);
                    }
                    else
                    {
                        id = sync 
                            ? _dbType.ExecuteInsert(this, cmd, primaryKeyName, preparedInsert.PocoData.TableInfo.UseOutputClause, poco, preparedInsert.Rawvalues.ToArray())
                            : await _dbType.ExecuteInsertAsync(this, cmd, primaryKeyName, preparedInsert.PocoData.TableInfo.UseOutputClause, poco, preparedInsert.Rawvalues.ToArray()).ConfigureAwait(false);
                        
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

        public async Task InsertBulkAsync<T>(IEnumerable<T> pocos, InsertBulkOptions options = null)
        {
            try
            {
                OpenSharedConnectionInternal();
                await _dbType.InsertBulkAsync(this, pocos, options).ConfigureAwait(false);
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

        public Task<int> InsertBatchAsync<T>(IEnumerable<T> pocos, BatchOptions options = null)
        {
            return InsertBatchAsyncImp(pocos, options, false);
        }

        private async Task<int> InsertBatchAsyncImp<T>(IEnumerable<T> pocos, BatchOptions options, bool sync)
        {
            options = options ?? new BatchOptions();
            var result = 0;

            try
            {
                OpenSharedConnectionInternal();
                PocoData pd = null;

                foreach (var batchedPocos in pocos.Chunkify(options.BatchSize))
                {
                    var preparedInserts = batchedPocos.Select(x =>
                    {
                        if (pd == null) pd = PocoDataFactory.ForType(x.GetType());
                        return InsertStatements.PrepareInsertSql(this, pd, pd.TableInfo.TableName, pd.TableInfo.PrimaryKey, pd.TableInfo.AutoIncrement, x);
                    }).ToArray();

                    var sql = new Sql();
                    foreach (var preparedInsertSql in preparedInserts)
                    {
                        sql.Append(preparedInsertSql.Sql + options.StatementSeperator, preparedInsertSql.Rawvalues.ToArray());
                    }

                    using (var cmd = CreateCommand(_sharedConnection, sql.SQL, sql.Arguments))
                    {
                        result += sync
                            ? ExecuteNonQueryHelper(cmd)
                            : await ExecuteNonQueryHelperAsync(cmd).ConfigureAwait(false);
                    }
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

            return result;
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
            return UpdateImpAsync(tableName, primaryKeyName, poco, primaryKeyValue, columns, false);
        }

        public Task<int> UpdateBatchAsync<T>(IEnumerable<UpdateBatch<T>> pocos, BatchOptions options = null)
        {
            return UpdateBatchAsyncImp(pocos, options, false);
        }

        private async Task<int> UpdateBatchAsyncImp<T>(IEnumerable<UpdateBatch<T>> pocos, BatchOptions options, bool sync)
        {
            options = options ?? new BatchOptions();
            int result = 0;

            try
            {
                OpenSharedConnectionInternal();
                PocoData pd = null;

                foreach (var batchedPocos in pocos.Chunkify(options.BatchSize))
                {
                    var preparedUpdates = batchedPocos.Select(x =>
                    {
                        if (pd == null) pd = PocoDataFactory.ForType(x.Poco.GetType());
                        return UpdateStatements.PrepareUpdate(this, pd, pd.TableInfo.TableName, pd.TableInfo.PrimaryKey, x.Poco, null, x.Snapshot?.UpdatedColumns());
                    }).ToArray();

                    var sql = new Sql();
                    foreach (var preparedUpdate in preparedUpdates)
                    {
                        if (preparedUpdate.Sql != null)
                        {
                            sql.Append(preparedUpdate.Sql + options.StatementSeperator, preparedUpdate.Rawvalues.ToArray());
                        }
                    }

                    using (var cmd = CreateCommand(_sharedConnection, sql.SQL, sql.Arguments))
                    {
                        result += sync
                            ? ExecuteNonQueryHelper(cmd)
                            : await ExecuteNonQueryHelperAsync(cmd).ConfigureAwait(false);
                    }
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

            return result;
        }

        public IAsyncUpdateQueryProvider<T> UpdateManyAsync<T>()
        {
            return new AsyncUpdateQueryProvider<T>(this);
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
            return DeleteImpAsync(tableName, primaryKeyName, poco, primaryKeyValue, false);
        }

        public IAsyncDeleteQueryProvider<T> DeleteManyAsync<T>()
        {
            return new AsyncDeleteQueryProvider<T>(this);
        }

        public Task<Page<T>> PageAsync<T>(long page, long itemsPerPage, Sql sql)
        {
            return PageAsync<T>(page, itemsPerPage, sql.SQL, sql.Arguments);
        }

        public Task<Page<T>> PageAsync<T>(long page, long itemsPerPage, string sql, params object[] args)
        {
            return PageImpAsync<T>(page, itemsPerPage, sql, args, false);
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

        /// <summary>Checks if a poco represents a new record.</summary>
        public Task<bool> IsNewAsync<T>(T poco)
        {
            return IsNewAsync(poco, false);
        }

        public async Task SaveAsync<T>(T poco)
        {
            var tableInfo = PocoDataFactory.TableInfoForType(poco.GetType());
            if (await IsNewAsync(poco).ConfigureAwait(false))
            {
                await InsertAsync(tableInfo.TableName, tableInfo.PrimaryKey, tableInfo.AutoIncrement, poco).ConfigureAwait(false);
            }
            else
            {
                await UpdateAsync(tableInfo.TableName, tableInfo.PrimaryKey, poco, null, null).ConfigureAwait(false);
            }
        }

        private async Task<bool> PocoExistsAsync<T>(T poco, bool sync)
        {
            var sql = GetExistsSql<T>(poco, true);
            var result = sync 
                ? ExecuteScalar<int>(sql)
                : await ExecuteScalarAsync<int>(sql).ConfigureAwait(false);
            return result > 0;
        }

        private async Task<bool> ExistsAsync<T>(object primaryKey, bool sync)
        {
            var sql = GetExistsSql<T>(primaryKey, false);
            var result = sync 
                ? ExecuteScalar<int>(sql)
                : await ExecuteScalarAsync<int>(sql).ConfigureAwait(false);
            return result > 0;
        }

        private Sql GetExistsSql<T>(object primaryKeyorPoco, bool isPoco)
        {
            var index = 0;
            var pd = PocoDataFactory.ForType(typeof(T));
            var primaryKeyValuePairs = GetPrimaryKeyValues(this, pd, pd.TableInfo.PrimaryKey, primaryKeyorPoco, isPoco);
            var sql = string.Format(DatabaseType.GetExistsSql(), DatabaseType.EscapeTableName(pd.TableInfo.TableName), BuildPrimaryKeySql(this, primaryKeyValuePairs, ref index));
            var args = primaryKeyValuePairs.Select(x => x.Value).ToArray();
            return new Sql(sql, args);
        }

        public Task<List<T>> FetchAsync<T>()
        {
            return FetchAsync<T>("");
        }

        public Task<List<T>> FetchAsync<T>(string sql, params object[] args)
        {
            return QueryAsync<T>(sql, args).ToListAsync().AsTask();
        }

        public Task<List<T>> FetchAsync<T>(Sql sql)
        {
            return QueryAsync<T>(sql).ToListAsync().AsTask();
        }

        public Task<TRet> FetchMultipleAsync<T1, T2, TRet>(Func<List<T1>, List<T2>, TRet> cb, string sql, params object[] args) { return FetchMultipleImp<T1, T2, DontMap, DontMap, TRet>(new[] { typeof(T1), typeof(T2) }, cb, new Sql(sql, args), false); }
        public Task<TRet> FetchMultipleAsync<T1, T2, T3, TRet>(Func<List<T1>, List<T2>, List<T3>, TRet> cb, string sql, params object[] args) { return FetchMultipleImp<T1, T2, T3, DontMap, TRet>(new[] { typeof(T1), typeof(T2), typeof(T3) }, cb, new Sql(sql, args), false); }
        public Task<TRet> FetchMultipleAsync<T1, T2, T3, T4, TRet>(Func<List<T1>, List<T2>, List<T3>, List<T4>, TRet> cb, string sql, params object[] args) { return FetchMultipleImp<T1, T2, T3, T4, TRet>(new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) }, cb, new Sql(sql, args), false); }
        public Task<TRet> FetchMultipleAsync<T1, T2, TRet>(Func<List<T1>, List<T2>, TRet> cb, Sql sql) { return FetchMultipleImp<T1, T2, DontMap, DontMap, TRet>(new[] { typeof(T1), typeof(T2) }, cb, sql, false); }
        public Task<TRet> FetchMultipleAsync<T1, T2, T3, TRet>(Func<List<T1>, List<T2>, List<T3>, TRet> cb, Sql sql) { return FetchMultipleImp<T1, T2, T3, DontMap, TRet>(new[] { typeof(T1), typeof(T2), typeof(T3) }, cb, sql, false); }
        public Task<TRet> FetchMultipleAsync<T1, T2, T3, T4, TRet>(Func<List<T1>, List<T2>, List<T3>, List<T4>, TRet> cb, Sql sql) { return FetchMultipleImp<T1, T2, T3, T4, TRet>(new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) }, cb, sql, false); }

        public Task<(List<T1>, List<T2>)> FetchMultipleAsync<T1, T2>(string sql, params object[] args) { return FetchMultipleImp<T1, T2, DontMap, DontMap, (List<T1>, List<T2>)>(new[] { typeof(T1), typeof(T2) }, new Func<List<T1>, List<T2>, (List<T1>, List<T2>)>((y, z) => (y, z)), new Sql(sql, args), false); }
        public Task<(List<T1>, List<T2>, List<T3>)> FetchMultipleAsync<T1, T2, T3>(string sql, params object[] args) { return FetchMultipleImp<T1, T2, T3, DontMap, (List<T1>, List<T2>, List<T3>)>(new[] { typeof(T1), typeof(T2), typeof(T3) }, new Func<List<T1>, List<T2>, List<T3>, (List<T1>, List<T2>, List<T3>)>((x, y, z) => (x, y, z)), new Sql(sql, args), false); }
        public Task<(List<T1>, List<T2>, List<T3>, List<T4>)> FetchMultipleAsync<T1, T2, T3, T4>(string sql, params object[] args) { return FetchMultipleImp<T1, T2, T3, T4, (List<T1>, List<T2>, List<T3>, List<T4>)>(new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) }, new Func<List<T1>, List<T2>, List<T3>, List<T4>, (List<T1>, List<T2>, List<T3>, List<T4>)>((w, x, y, z) => (w, x, y, z)), new Sql(sql, args), false); }
        public Task<(List<T1>, List<T2>)> FetchMultipleAsync<T1, T2>(Sql sql) { return FetchMultipleImp<T1, T2, DontMap, DontMap, (List<T1>, List<T2>)>(new[] { typeof(T1), typeof(T2) }, new Func<List<T1>, List<T2>, (List<T1>, List<T2>)>((y, z) => (y, z)), sql, false); }
        public Task<(List<T1>, List<T2>, List<T3>)> FetchMultipleAsync<T1, T2, T3>(Sql sql) { return FetchMultipleImp<T1, T2, T3, DontMap, (List<T1>, List<T2>, List<T3>)>(new[] { typeof(T1), typeof(T2), typeof(T3) }, new Func<List<T1>, List<T2>, List<T3>, (List<T1>, List<T2>, List<T3>)>((x, y, z) => (x, y, z)), sql, false); }
        public Task<(List<T1>, List<T2>, List<T3>, List<T4>)> FetchMultipleAsync<T1, T2, T3, T4>(Sql sql) { return FetchMultipleImp<T1, T2, T3, T4, (List<T1>, List<T2>, List<T3>, List<T4>)>(new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) }, new Func<List<T1>, List<T2>, List<T3>, List<T4>, (List<T1>, List<T2>, List<T3>, List<T4>)>((w, x, y, z) => (w, x, y, z)), sql, false); }

        public IAsyncQueryProviderWithIncludes<T> QueryAsync<T>()
        {
            return new AsyncQueryProvider<T>(this);
        }

        public IAsyncEnumerable<T> QueryAsync<T>(string sql, params object[] args)
        {
            return QueryAsync<T>(new Sql(sql, args));
        }

        public IAsyncEnumerable<T> QueryAsync<T>(Sql sql)
        {
            return QueryAsync(default(T), null, null, sql);
        }

        internal async IAsyncEnumerable<T> QueryAsync<T>(T instance, Expression<Func<T, IList>> listExpression, Func<T, object[]> idFunc, Sql Sql, PocoData pocoData = null)
        {
            pocoData ??= PocoDataFactory.ForType(typeof(T));

            var sql = Sql.SQL;
            var args = Sql.Arguments;

            if (EnableAutoSelect) sql = AutoSelectHelper.AddSelectClause(this, typeof(T), sql);

            try
            {
                OpenSharedConnectionInternal();
                using var cmd = CreateCommand(_sharedConnection, sql, args); 
                using var reader = await ExecuteDataReader(cmd, false).ConfigureAwait(false);
                var read = (listExpression != null ? ReadOneToManyAsync(instance, reader, listExpression, idFunc, pocoData) : ReadAsync<T>(instance, reader, pocoData));
                await foreach (var item in read)
                {
                    yield return item;
                }
            }
            finally
            {
                CloseSharedConnectionInternal();
            }
        }

        public Task<T> SingleAsync<T>(string sql, params object[] args)
        {
            return QueryAsync<T>(sql, args).SingleAsync().AsTask();
        }

        public Task<T> SingleAsync<T>(Sql sql)
        {
            return QueryAsync<T>(sql).SingleAsync().AsTask();
        }

        public Task<T> SingleOrDefaultAsync<T>(string sql, params object[] args)
        {
            return QueryAsync<T>(sql, args).SingleOrDefaultAsync().AsTask();
        }

        public Task<T> SingleOrDefaultAsync<T>(Sql sql)
        {
            return QueryAsync<T>(sql).SingleOrDefaultAsync().AsTask();
        }

        public Task<T> SingleByIdAsync<T>(object primaryKey)
        {
            var sql = GenerateSingleByIdSql<T>(primaryKey);
            return QueryAsync<T>(sql).SingleAsync().AsTask();
        }

        public Task<T> SingleOrDefaultByIdAsync<T>(object primaryKey)
        {
            var sql = GenerateSingleByIdSql<T>(primaryKey);
            return QueryAsync<T>(sql).SingleOrDefaultAsync().AsTask();
        }

        public Task<T> FirstAsync<T>(string sql, params object[] args)
        {
            return QueryAsync<T>(sql, args).FirstAsync().AsTask();
        }

        public Task<T> FirstAsync<T>(Sql sql)
        {
            return QueryAsync<T>(sql).FirstAsync().AsTask();
        }

        public Task<T> FirstOrDefaultAsync<T>(string sql, params object[] args)
        {
            return QueryAsync<T>(sql, args).FirstOrDefaultAsync().AsTask();
        }

        public Task<T> FirstOrDefaultAsync<T>(Sql sql)
        {
            return QueryAsync<T>(sql).FirstOrDefaultAsync().AsTask();
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
                        return default(T);

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
            var result = await ExecutionHookAsync(() => _dbType.ExecuteNonQueryAsync(this, cmd)).ConfigureAwait(false);
            OnExecutedCommandInternal(cmd);
            return result;
        }

        internal async Task<object> ExecuteScalarHelperAsync(DbCommand cmd)
        {
            DoPreExecute(cmd);
            var result = await ExecutionHookAsync(() => _dbType.ExecuteScalarAsync(this, cmd)).ConfigureAwait(false);
            OnExecutedCommandInternal(cmd);
            return result;
        }

        internal async Task<DbDataReader> ExecuteReaderHelperAsync(DbCommand cmd)
        {
            DoPreExecute(cmd);
            var reader = await ExecutionHookAsync(() => _dbType.ExecuteReaderAsync(this, cmd)).ConfigureAwait(false);
            OnExecutedCommandInternal(cmd);
            return reader;
        }
    }
}
