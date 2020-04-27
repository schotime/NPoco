﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
#if !NET35 && !NET40
using System.Threading.Tasks;
#endif
using NPoco.Expressions;

namespace NPoco.Linq
{
    public interface IAsyncQueryResultProvider<T>
    {
#if !NET35 && !NET40
        Task<List<T>> ToList();
        Task<T[]> ToArray();
        Task<IEnumerable<T>> ToEnumerable();
        Task<T> FirstOrDefault();
        Task<T> FirstOrDefault(Expression<Func<T, bool>> whereExpression);
        Task<T> First();
        Task<T> First(Expression<Func<T, bool>> whereExpression);
        Task<T> SingleOrDefault();
        Task<T> SingleOrDefault(Expression<Func<T, bool>> whereExpression);
        Task<T> Single();
        Task<T> Single(Expression<Func<T, bool>> whereExpression);
        Task<int> Count();
        Task<int> Count(Expression<Func<T, bool>> whereExpression);
        Task<bool> Any();
        Task<bool> Any(Expression<Func<T, bool>> whereExpression);
        Task<Page<T>> ToPage(int page, int pageSize);
        Task<List<T2>> ProjectTo<T2>(Expression<Func<T, T2>> projectionExpression);
        Task<List<T2>> Distinct<T2>(Expression<Func<T, T2>> projectionExpression);
        Task<List<T>> Distinct();
#endif
    }

    public interface IQueryResultProvider<T>
    {
        T FirstOrDefault();
        T FirstOrDefault(Expression<Func<T, bool>> whereExpression);
        T First();
        T First(Expression<Func<T, bool>> whereExpression);
        T SingleOrDefault();
        T SingleOrDefault(Expression<Func<T, bool>> whereExpression);
        T Single();
        T Single(Expression<Func<T, bool>> whereExpression);
        int Count();
        int Count(Expression<Func<T, bool>> whereExpression);
        bool Any();
        bool Any(Expression<Func<T, bool>> whereExpression);
        List<T> ToList();
        T[] ToArray();
        IEnumerable<T> ToEnumerable();
#if !NET35
        List<dynamic> ToDynamicList();
        IEnumerable<dynamic> ToDynamicEnumerable();
#endif
        Page<T> ToPage(int page, int pageSize);
        List<T2> ProjectTo<T2>(Expression<Func<T, T2>> projectionExpression);
        List<T2> Distinct<T2>(Expression<Func<T, T2>> projectionExpression);
        List<T> Distinct();
#if !NET35 && !NET40
        Task<List<T>> ToListAsync();
        Task<T[]> ToArrayAsync();
        Task<IEnumerable<T>> ToEnumerableAsync();
        Task<T> FirstOrDefaultAsync();
        Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> whereExpression);
        Task<T> FirstAsync();
        Task<T> FirstAsync(Expression<Func<T, bool>> whereExpression);
        Task<T> SingleOrDefaultAsync();
        Task<T> SingleOrDefaultAsync(Expression<Func<T, bool>> whereExpression);
        Task<T> SingleAsync();
        Task<T> SingleAsync(Expression<Func<T, bool>> whereExpression);
        Task<int> CountAsync();
        Task<int> CountAsync(Expression<Func<T, bool>> whereExpression);
        Task<bool> AnyAsync();
        Task<bool> AnyAsync(Expression<Func<T, bool>> whereExpression);
        Task<Page<T>> ToPageAsync(int page, int pageSize);
        Task<List<T2>> ProjectToAsync<T2>(Expression<Func<T, T2>> projectionExpression);
        Task<List<T2>> DistinctAsync<T2>(Expression<Func<T, T2>> projectionExpression);
        Task<List<T>> DistinctAsync();
#endif
    }

    public interface IQueryProvider<T> : IQueryResultProvider<T>
    {
        IQueryProvider<T> Where(Expression<Func<T, bool>> whereExpression);
        IQueryProvider<T> WhereSql(string sql, params object[] args);
        IQueryProvider<T> WhereSql(Sql sql);
        IQueryProvider<T> WhereSql(Func<QueryContext<T>, Sql> queryBuilder);
        IQueryProvider<T> OrderBy(Expression<Func<T, object>> column);
        IQueryProvider<T> OrderByDescending(Expression<Func<T, object>> column);
        IQueryProvider<T> ThenBy(Expression<Func<T, object>> column);
        IQueryProvider<T> ThenByDescending(Expression<Func<T, object>> column);
        IQueryProvider<T> Limit(int rows);
        IQueryProvider<T> Limit(int skip, int rows);
        IQueryProvider<T> From(QueryBuilder<T> builder);
    }

    public interface IAsyncQueryProvider<T> : IAsyncQueryResultProvider<T>
    {
        IAsyncQueryProvider<T> Where(Expression<Func<T, bool>> whereExpression);
        IAsyncQueryProvider<T> WhereSql(string sql, params object[] args);
        IAsyncQueryProvider<T> WhereSql(Sql sql);
        IAsyncQueryProvider<T> WhereSql(Func<QueryContext<T>, Sql> queryBuilder);
        IAsyncQueryProvider<T> OrderBy(Expression<Func<T, object>> column);
        IAsyncQueryProvider<T> OrderByDescending(Expression<Func<T, object>> column);
        IAsyncQueryProvider<T> ThenBy(Expression<Func<T, object>> column);
        IAsyncQueryProvider<T> ThenByDescending(Expression<Func<T, object>> column);
        IAsyncQueryProvider<T> Limit(int rows);
        IAsyncQueryProvider<T> Limit(int skip, int rows);
        IAsyncQueryProvider<T> From(QueryBuilder<T> builder);
    }

    public interface IAsyncQueryProviderWithIncludes<T> : IAsyncQueryProvider<T>
    {
        IAsyncQueryProvider<T> IncludeMany(Expression<Func<T, IList>> expression, JoinType joinType = JoinType.Left);
        IAsyncQueryProviderWithIncludes<T> Include<T2>(Expression<Func<T, T2>> expression, JoinType joinType = JoinType.Left) where T2 : class;
        IAsyncQueryProviderWithIncludes<T> Include<T2>(Expression<Func<T, T2>> expression, string tableAlias, JoinType joinType = JoinType.Left) where T2 : class;
        IAsyncQueryProviderWithIncludes<T> UsingAlias(string empty);
    }

    public interface IQueryProviderWithIncludes<T> : IQueryProvider<T>
    {
        IQueryProvider<T> IncludeMany(Expression<Func<T, IList>> expression, JoinType joinType = JoinType.Left);
        IQueryProviderWithIncludes<T> Include<T2>(Expression<Func<T, T2>> expression, JoinType joinType = JoinType.Left) where T2 : class;
        IQueryProviderWithIncludes<T> Include<T2>(Expression<Func<T, T2>> expression, string tableAlias, JoinType joinType = JoinType.Left) where T2 : class;
        IQueryProviderWithIncludes<T> UsingAlias(string empty);
    }

    public class AsyncQueryProvider<T> : IAsyncQueryProviderWithIncludes<T>, ISimpleQueryProviderExpression<T>, INeedDatabase
    {
        protected readonly Database _database;
        protected SqlExpression<T> _sqlExpression;
        protected Dictionary<string, JoinData> _joinSqlExpressions = new Dictionary<string, JoinData>();
        protected readonly ComplexSqlBuilder<T> _buildComplexSql;
        protected Expression<Func<T, IList>> _listExpression = null;
        protected PocoData _pocoData;

        public AsyncQueryProvider(Database database, Expression<Func<T, bool>> whereExpression)
        {
            _database = database;
            _pocoData = database.PocoDataFactory.ForType(typeof(T));
            _sqlExpression = database.DatabaseType.ExpressionVisitor<T>(database, _pocoData, true);
            _buildComplexSql = new ComplexSqlBuilder<T>(database, _pocoData, _sqlExpression, _joinSqlExpressions);
            _sqlExpression = _sqlExpression.Where(whereExpression);
        }

        SqlExpression<T> ISimpleQueryProviderExpression<T>.AtlasSqlExpression { get { return _sqlExpression; } }

        public AsyncQueryProvider(Database database) : this(database, null)
        {
        }

        protected void AddWhere(Expression<Func<T, bool>> whereExpression)
        {
            if (whereExpression != null)
                _sqlExpression = _sqlExpression.Where(whereExpression);
        }

        protected Sql BuildSql()
        {
            Sql sql;
            if (_joinSqlExpressions.Any())
                sql = _buildComplexSql.BuildJoin(_database, _sqlExpression, _joinSqlExpressions.Values.ToList(), null, false, false);
            else
                sql = new Sql(true, _sqlExpression.Context.ToSelectStatement(), _sqlExpression.Context.Params);
            return sql;
        }

        public IAsyncQueryProvider<T> IncludeMany(Expression<Func<T, IList>> expression, JoinType joinType = JoinType.Left)
        {
            _listExpression = expression;
            return QueryProviderWithIncludes(expression, null, joinType);
        }

        public IAsyncQueryProviderWithIncludes<T> Include<T2>(Expression<Func<T, T2>> expression, JoinType joinType = JoinType.Left) where T2 : class
        {
            return QueryProviderWithIncludes(expression, null, joinType);
        }

        public IAsyncQueryProviderWithIncludes<T> Include<T2>(Expression<Func<T, T2>> expression, string tableAlias, JoinType joinType = JoinType.Left) where T2 : class
        {
            return QueryProviderWithIncludes(expression, tableAlias, joinType);
        }

        public IAsyncQueryProviderWithIncludes<T> UsingAlias(string tableAlias)
        {
            if (!string.IsNullOrEmpty(tableAlias))
                _pocoData.TableInfo.AutoAlias = tableAlias;
            return this;
        }

        private IAsyncQueryProviderWithIncludes<T> QueryProviderWithIncludes(Expression expression, string tableAlias, JoinType joinType)
        {
            var joinExpressions = _buildComplexSql.GetJoinExpressions(expression, tableAlias, joinType);
            foreach (var joinExpression in joinExpressions)
            {
                _joinSqlExpressions[joinExpression.Key] = joinExpression.Value;
            }

            return this;
        }

#if !NET35 && !NET40
        public async Task<List<T>> ToList()
        {
            return (await ToEnumerable().ConfigureAwait(false)).ToList();
        }

        public async Task<T[]> ToArray()
        {
            return (await ToEnumerable().ConfigureAwait(false)).ToArray();
        }

        public Task<IEnumerable<T>> ToEnumerable()
        {
            return _database.QueryAsync(default(T), _listExpression, null, BuildSql());
        }

        public Task<T> FirstOrDefault()
        {
            return FirstOrDefault(null);
        }

        public async Task<T> FirstOrDefault(Expression<Func<T, bool>> whereExpression)
        {
            AddWhere(whereExpression);
            return (await ToEnumerable().ConfigureAwait(false)).FirstOrDefault();
        }

        public Task<T> First()
        {
            return First(null);
        }

        public async Task<T> First(Expression<Func<T, bool>> whereExpression)
        {
            AddWhere(null);
            return (await ToEnumerable().ConfigureAwait(false)).First();
        }

        public Task<T> SingleOrDefault()
        {
            return SingleOrDefault(null);
        }

        public async Task<T> SingleOrDefault(Expression<Func<T, bool>> whereExpression)
        {
            AddWhere(whereExpression);
            return (await ToEnumerable().ConfigureAwait(false)).SingleOrDefault();
        }

        public Task<T> Single()
        {
            return Single(null);
        }

        public async Task<T> Single(Expression<Func<T, bool>> whereExpression)
        {
            AddWhere(whereExpression);
            return (await ToEnumerable().ConfigureAwait(false)).Single();
        }

        public Task<int> Count()
        {
            return Count(null);
        }

        public async Task<int> Count(Expression<Func<T, bool>> whereExpression)
        {
            AddWhere(whereExpression);
            var sql = _buildComplexSql.BuildJoin(_database, _sqlExpression, _joinSqlExpressions.Values.ToList(), null, true, false);
            return await _database.ExecuteScalarAsync<int>(sql).ConfigureAwait(false);
        }

        public Task<bool> Any()
        {
            return Any(null);
        }

        public async Task<bool> Any(Expression<Func<T, bool>> whereExpression)
        {
            return (await Count(whereExpression).ConfigureAwait(false)) > 0;
        }

        public async Task<Page<T>> ToPage(int page, int pageSize)
        {
            int offset = (page - 1) * pageSize;

            // Save the one-time command time out and use it for both queries
            int saveTimeout = _database.OneTimeCommandTimeout;

            // Setup the paged result
            var result = new Page<T>();
            result.CurrentPage = page;
            result.ItemsPerPage = pageSize;
            result.TotalItems = await Count();
            result.TotalPages = result.TotalItems / pageSize;
            if ((result.TotalItems % pageSize) != 0)
                result.TotalPages++;

            _database.OneTimeCommandTimeout = saveTimeout;

            _sqlExpression = _sqlExpression.Limit(offset, pageSize);

            result.Items = await ToList();

            return result;
        }

        public async Task<List<T2>> ProjectTo<T2>(Expression<Func<T, T2>> projectionExpression)
        {
            var sql = _buildComplexSql.GetSqlForProjection(projectionExpression, false);
            return (await _database.QueryAsync<T>(sql).ConfigureAwait(false)).Select(projectionExpression.Compile()).ToList();
        }

        public async Task<List<T>> Distinct()
        {
            return (await _database.QueryAsync<T>(new Sql(_sqlExpression.Context.ToSelectStatement(true, true), _sqlExpression.Context.Params))).ToList();
        }

        public async Task<List<T2>> Distinct<T2>(Expression<Func<T, T2>> projectionExpression)
        {
            var sql = _buildComplexSql.GetSqlForProjection(projectionExpression, true);
            return (await _database.QueryAsync<T>(sql).ConfigureAwait(false)).Select(projectionExpression.Compile()).ToList();
        }
#endif

        public IAsyncQueryProvider<T> Where(Expression<Func<T, bool>> whereExpression)
        {
            _sqlExpression = _sqlExpression.Where(whereExpression);
            return this;
        }

        public IAsyncQueryProvider<T> WhereSql(string sql, params object[] args)
        {
            _sqlExpression = _sqlExpression.Where(sql, args);
            return this;
        }

        public IAsyncQueryProvider<T> WhereSql(Sql sql)
        {
            _sqlExpression = _sqlExpression.Where(sql.SQL, sql.Arguments);
            return this;
        }

        public IAsyncQueryProvider<T> WhereSql(Func<QueryContext<T>, Sql> queryBuilder)
        {
            var sql = queryBuilder(new QueryContext<T>(_database, _pocoData, _joinSqlExpressions));
            return WhereSql(sql);
        }

        public IAsyncQueryProvider<T> Limit(int rows)
        {
            ThrowIfOneToMany();
            _sqlExpression = _sqlExpression.Limit(rows);
            return this;
        }

        public IAsyncQueryProvider<T> Limit(int skip, int rows)
        {
            ThrowIfOneToMany();
            _sqlExpression = _sqlExpression.Limit(skip, rows);
            return this;
        }

        private void ThrowIfOneToMany()
        {
            if (_listExpression != null)
            {
                throw new NotImplementedException("One to many queries with paging is not implemented");
            }
        }

        public IAsyncQueryProvider<T> OrderBy(Expression<Func<T, object>> column)
        {
            _sqlExpression = _sqlExpression.OrderBy(column);
            return this;
        }

        public IAsyncQueryProvider<T> OrderByDescending(Expression<Func<T, object>> column)
        {
            _sqlExpression = _sqlExpression.OrderByDescending(column);
            return this;
        }

        public IAsyncQueryProvider<T> ThenBy(Expression<Func<T, object>> column)
        {
            _sqlExpression = _sqlExpression.ThenBy(column);
            return this;
        }

        public IAsyncQueryProvider<T> ThenByDescending(Expression<Func<T, object>> column)
        {
            _sqlExpression = _sqlExpression.ThenByDescending(column);
            return this;
        }

        public IAsyncQueryProvider<T> From(QueryBuilder<T> builder)
        {
            if (!builder.Data.Skip.HasValue && builder.Data.Rows.HasValue)
            {
                Limit(builder.Data.Rows.Value);
            }

            if (builder.Data.Skip.HasValue && builder.Data.Rows.HasValue)
            {
                Limit(builder.Data.Skip.Value, builder.Data.Rows.Value);
            }

            if (builder.Data.WhereExpression != null)
            {
                Where(builder.Data.WhereExpression);
            }

            if (builder.Data.OrderByExpression != null)
            {
                OrderBy(builder.Data.OrderByExpression);
            }

            if (builder.Data.OrderByDescendingExpression != null)
            {
                OrderByDescending(builder.Data.OrderByDescendingExpression);
            }

            if (builder.Data.ThenByExpression.Any())
            {
                foreach (var expression in builder.Data.ThenByExpression)
                {
                    ThenBy(expression);
                }
            }

            if (builder.Data.ThenByDescendingExpression.Any())
            {
                foreach (var expression in builder.Data.ThenByDescendingExpression)
                {
                    ThenByDescending(expression);
                }
            }

            return this;
        }

#if !NET35
        public List<dynamic> ToDynamicList()
        {
            return ToDynamicEnumerable().ToList();
        }

        public IEnumerable<dynamic> ToDynamicEnumerable()
        {
            var sql = BuildSql();
            return _database.QueryImp<dynamic>(null, null, null, sql);
        }
#endif
        IDatabase INeedDatabase.GetDatabase()
        {
            return _database;
        }
    }

    public interface INeedDatabase
    {
        IDatabase GetDatabase();
    }

    public class QueryProvider<T> : AsyncQueryProvider<T>, IQueryProviderWithIncludes<T>
    {

        public QueryProvider(Database database, Expression<Func<T, bool>> whereExpression) : base(database, whereExpression)
        {
        }

        public QueryProvider(Database database) : base(database, null)
        {
        }

#pragma warning disable CS0109
        public new T FirstOrDefault()
        {
            return FirstOrDefault(null);
        }

        public new T FirstOrDefault(Expression<Func<T, bool>> whereExpression)
        {
            AddWhere(whereExpression);
            return ToEnumerable().FirstOrDefault();
        }

        public new T First()
        {
            return First(null);
        }

        public new T First(Expression<Func<T, bool>> whereExpression)
        {
            AddWhere(whereExpression);
            return ToEnumerable().First();
        }

        public new T SingleOrDefault()
        {
            return SingleOrDefault(null);
        }

        public new T SingleOrDefault(Expression<Func<T, bool>> whereExpression)
        {
            AddWhere(whereExpression);
            return ToEnumerable().SingleOrDefault();
        }

        public new T Single()
        {
            return Single(null);
        }

        public new T Single(Expression<Func<T, bool>> whereExpression)
        {
            AddWhere(whereExpression);
            return ToEnumerable().Single();
        }

        public new int Count()
        {
            return Count(null);
        }

        public new int Count(Expression<Func<T, bool>> whereExpression)
        {
            AddWhere(whereExpression);
            var sql = _buildComplexSql.BuildJoin(_database, _sqlExpression, _joinSqlExpressions.Values.ToList(), null, true, false);

            return _database.ExecuteScalar<int>(sql);
        }

        public new bool Any()
        {
            return Count() > 0;
        }

        public new bool Any(Expression<Func<T, bool>> whereExpression)
        {
            return Count(whereExpression) > 0;
        }

        public new Page<T> ToPage(int page, int pageSize)
        {
            int offset = (page - 1) * pageSize;

            // Save the one-time command time out and use it for both queries
            int saveTimeout = _database.OneTimeCommandTimeout;

            // Setup the paged result
            var result = new Page<T>();
            result.CurrentPage = page;
            result.ItemsPerPage = pageSize;
            result.TotalItems = Count();
            result.TotalPages = result.TotalItems / pageSize;
            if ((result.TotalItems % pageSize) != 0)
                result.TotalPages++;

            _database.OneTimeCommandTimeout = saveTimeout;

            _sqlExpression = _sqlExpression.Limit(offset, pageSize);

            result.Items = ToList();

            return result;
        }

        public new List<T2> ProjectTo<T2>(Expression<Func<T, T2>> projectionExpression)
        {
            var sql = _buildComplexSql.GetSqlForProjection(projectionExpression, false);
            return ExecuteQuery(sql).Select(projectionExpression.Compile()).ToList();
        }

        public new List<T2> Distinct<T2>(Expression<Func<T, T2>> projectionExpression)
        {
            var sql = _buildComplexSql.GetSqlForProjection(projectionExpression, true);
            return ExecuteQuery(sql).Select(projectionExpression.Compile()).ToList();
        }

        public new List<T> Distinct()
        {
            return ExecuteQuery(new Sql(_sqlExpression.Context.ToSelectStatement(true, true), _sqlExpression.Context.Params)).ToList();
        }
        public new T[] ToArray()
        {
            return ToEnumerable().ToArray();
        }

        public new List<T> ToList()
        {
            return ToEnumerable().ToList();
        }

        public new IEnumerable<T> ToEnumerable()
        {
            var sql = BuildSql();
            return ExecuteQuery(sql);
        }
        
        private IEnumerable<T> ExecuteQuery(Sql sql)
        {
            return _database.QueryImp(default(T), _listExpression, null, sql);
        }
#pragma warning restore CS0109

#if !NET35 && !NET40
        public Task<List<T>> ToListAsync()
        {
            return base.ToList();
        }

        public Task<T[]> ToArrayAsync()
        {
            return base.ToArray();
        }

        public Task<IEnumerable<T>> ToEnumerableAsync()
        {
            return base.ToEnumerable();
        }

        public Task<T> FirstOrDefaultAsync()
        {
            return base.FirstOrDefault();
        }

        public Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> whereExpression)
        {
            return base.FirstOrDefault(whereExpression);
        }

        public Task<T> FirstAsync()
        {
            return base.First();
        }

        public Task<T> FirstAsync(Expression<Func<T, bool>> whereExpression)
        {
            return base.First(whereExpression);
        }

        public Task<T> SingleOrDefaultAsync()
        {
            return base.SingleOrDefault();
        }

        public Task<T> SingleOrDefaultAsync(Expression<Func<T, bool>> whereExpression)
        {
            return base.SingleOrDefault(whereExpression);
        }

        public Task<T> SingleAsync()
        {
            return base.Single();
        }

        public Task<T> SingleAsync(Expression<Func<T, bool>> whereExpression)
        {
            return base.Single(whereExpression);
        }

        public Task<int> CountAsync()
        {
            return base.Count();
        }

        public Task<int> CountAsync(Expression<Func<T, bool>> whereExpression)
        {
            return base.Count(whereExpression);
        }

        public Task<bool> AnyAsync()
        {
            return base.Any();
        }

        public Task<bool> AnyAsync(Expression<Func<T, bool>> whereExpression)
        {
            return base.Any(whereExpression);
        }

        public Task<Page<T>> ToPageAsync(int page, int pageSize)
        {
            return base.ToPage(page, pageSize);
        }

        public Task<List<T2>> ProjectToAsync<T2>(Expression<Func<T, T2>> projectionExpression)
        {
            return base.ProjectTo(projectionExpression);
        }

        public Task<List<T2>> DistinctAsync<T2>(Expression<Func<T, T2>> projectionExpression)
        {
            return base.Distinct(projectionExpression);
        }

        public Task<List<T>> DistinctAsync()
        {
            return base.Distinct();
        }
#endif
        
        public new IQueryProvider<T> IncludeMany(Expression<Func<T, IList>> expression, JoinType joinType = JoinType.Left)
        {
            return (IQueryProvider<T>)base.IncludeMany(expression, joinType);
        }

        public new IQueryProviderWithIncludes<T> Include<T2>(Expression<Func<T, T2>> expression, JoinType joinType = JoinType.Left) where T2 : class
        {
            return (IQueryProviderWithIncludes<T>)base.Include(expression, joinType);
        }

        public new IQueryProviderWithIncludes<T> Include<T2>(Expression<Func<T, T2>> expression, string tableAlias, JoinType joinType = JoinType.Left) where T2 : class
        {
            return (IQueryProviderWithIncludes<T>)base.Include(expression, tableAlias, joinType);
        }

        public new IQueryProviderWithIncludes<T> UsingAlias(string empty)
        {
            return (IQueryProviderWithIncludes<T>)base.UsingAlias(empty);
        }

        public new IQueryProvider<T> Where(Expression<Func<T, bool>> whereExpression)
        {
            return (IQueryProvider<T>)base.Where(whereExpression);
        }

        public new IQueryProvider<T> WhereSql(string sql, params object[] args)
        {
            return (IQueryProvider<T>)base.WhereSql(sql, args);
        }

        public new IQueryProvider<T> WhereSql(Sql sql)
        {
            return (IQueryProvider<T>)base.WhereSql(sql);
        }

        public new IQueryProvider<T> WhereSql(Func<QueryContext<T>, Sql> queryBuilder)
        {
            return (IQueryProvider<T>)base.WhereSql(queryBuilder);
        }

        public new IQueryProvider<T> OrderBy(Expression<Func<T, object>> column)
        {
            return (IQueryProvider<T>)base.OrderBy(column);
        }

        public new IQueryProvider<T> OrderByDescending(Expression<Func<T, object>> column)
        {
            return (IQueryProvider<T>)base.OrderByDescending(column);
        }

        public new IQueryProvider<T> ThenBy(Expression<Func<T, object>> column)
        {
            return (IQueryProvider<T>)base.ThenBy(column);
        }

        public new IQueryProvider<T> ThenByDescending(Expression<Func<T, object>> column)
        {
            return (IQueryProvider<T>)base.ThenByDescending(column);
        }

        public new IQueryProvider<T> Limit(int rows)
        {
            return (IQueryProvider<T>)base.Limit(rows);
        }

        public new IQueryProvider<T> Limit(int skip, int rows)
        {
            return (IQueryProvider<T>)base.Limit(skip, rows);
        }

        public new IQueryProvider<T> From(QueryBuilder<T> builder)
        {
            return (IQueryProvider<T>)base.From(builder);
        }
    }
}
