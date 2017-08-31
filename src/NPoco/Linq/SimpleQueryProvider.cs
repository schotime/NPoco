using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using NPoco.Expressions;

namespace NPoco.Linq
{
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
        System.Threading.Tasks.Task<List<T>> ToListAsync();
        System.Threading.Tasks.Task<T[]> ToArrayAsync();
        System.Threading.Tasks.Task<IEnumerable<T>> ToEnumerableAsync();
        System.Threading.Tasks.Task<T> FirstOrDefaultAsync();
        System.Threading.Tasks.Task<T> FirstAsync();
        System.Threading.Tasks.Task<T> SingleOrDefaultAsync();
        System.Threading.Tasks.Task<T> SingleAsync();
        System.Threading.Tasks.Task<int> CountAsync();
        System.Threading.Tasks.Task<bool> AnyAsync();
        System.Threading.Tasks.Task<Page<T>> ToPageAsync(int page, int pageSize);
        System.Threading.Tasks.Task<List<T2>> ProjectToAsync<T2>(Expression<Func<T, T2>> projectionExpression);
        System.Threading.Tasks.Task<List<T2>> DistinctAsync<T2>(Expression<Func<T, T2>> projectionExpression);
        System.Threading.Tasks.Task<List<T>> DistinctAsync();
#endif
    }

    public interface IQueryProvider<T> : IQueryResultProvider<T>
    {
        IQueryProvider<T> Where(Expression<Func<T, bool>> whereExpression);
        IQueryProvider<T> Where(string sql, params object[] args);
        IQueryProvider<T> Where(Sql sql);
        IQueryProvider<T> Where(Func<QueryContext<T>, Sql> queryBuilder);
        IQueryProvider<T> OrderBy(Expression<Func<T, object>> column);
        IQueryProvider<T> OrderByDescending(Expression<Func<T, object>> column);
        IQueryProvider<T> ThenBy(Expression<Func<T, object>> column);
        IQueryProvider<T> ThenByDescending(Expression<Func<T, object>> column);
        IQueryProvider<T> Limit(int rows);
        IQueryProvider<T> Limit(int skip, int rows);
        IQueryProvider<T> From(QueryBuilder<T> builder);
    }

    public interface IQueryProviderWithIncludes<T> : IQueryProvider<T>
    {
        IQueryProvider<T> IncludeMany(Expression<Func<T, IList>> expression, JoinType joinType = JoinType.Left);
        IQueryProviderWithIncludes<T> Include<T2>(Expression<Func<T, T2>> expression, JoinType joinType = JoinType.Left) where T2 : class;
        IQueryProviderWithIncludes<T> Include<T2>(Expression<Func<T, T2>> expression, string tableAlias, JoinType joinType = JoinType.Left) where T2 : class;
        IQueryProviderWithIncludes<T> UsingAlias(string empty);
    }

    public class QueryProvider<T> : IQueryProviderWithIncludes<T>, ISimpleQueryProviderExpression<T>
    {
        private readonly Database _database;
        private SqlExpression<T> _sqlExpression;
        private Dictionary<string, JoinData> _joinSqlExpressions = new Dictionary<string, JoinData>();
        private readonly ComplexSqlBuilder<T> _buildComplexSql;
        private Expression<Func<T, IList>> _listExpression = null;
        private PocoData _pocoData;

        public QueryProvider(Database database, Expression<Func<T, bool>> whereExpression)
        {
            _database = database;
            _pocoData = database.PocoDataFactory.ForType(typeof (T));
            _sqlExpression = database.DatabaseType.ExpressionVisitor<T>(database, _pocoData, true);
            _buildComplexSql = new ComplexSqlBuilder<T>(database, _pocoData, _sqlExpression, _joinSqlExpressions);
            _sqlExpression = _sqlExpression.Where(whereExpression);
        }

        public QueryProvider(Database database)
            : this(database, null)
        {
        }

        SqlExpression<T> ISimpleQueryProviderExpression<T>.AtlasSqlExpression { get { return _sqlExpression; } }

        public IQueryProvider<T> IncludeMany(Expression<Func<T, IList>> expression, JoinType joinType = JoinType.Left)
        {
            _listExpression = expression;
            return QueryProviderWithIncludes(expression, null, joinType);
        }

        public IQueryProviderWithIncludes<T> Include<T2>(Expression<Func<T, T2>> expression, JoinType joinType = JoinType.Left) where T2 : class
        {
            return QueryProviderWithIncludes(expression, null, joinType);
        }

        public IQueryProviderWithIncludes<T> Include<T2>(Expression<Func<T, T2>> expression, string tableAlias, JoinType joinType = JoinType.Left) where T2 : class
        {
            return QueryProviderWithIncludes(expression, tableAlias, joinType);
        }

        public IQueryProviderWithIncludes<T> UsingAlias(string tableAlias)
        {
            if (!string.IsNullOrEmpty(tableAlias))
                _pocoData.TableInfo.AutoAlias = tableAlias;
            return this;
        }

        private IQueryProviderWithIncludes<T> QueryProviderWithIncludes(Expression expression, string tableAlias, JoinType joinType)
        {
            var joinExpressions = _buildComplexSql.GetJoinExpressions(expression, tableAlias, joinType);
            foreach (var joinExpression in joinExpressions)
            {
                _joinSqlExpressions[joinExpression.Key] = joinExpression.Value;
            }

            return this;
        }

        public IQueryProvider<T> From(QueryBuilder<T> builder)
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

        private void AddWhere(Expression<Func<T, bool>> whereExpression)
        {
            if (whereExpression != null)
                _sqlExpression = _sqlExpression.Where(whereExpression);
        }

        public T FirstOrDefault()
        {
            return FirstOrDefault(null);
        }

        public T FirstOrDefault(Expression<Func<T, bool>> whereExpression)
        {
            AddWhere(whereExpression);
            return ToEnumerable().FirstOrDefault();
        }

        public T First()
        {
            return First(null);
        }

        public T First(Expression<Func<T, bool>> whereExpression)
        {
            AddWhere(whereExpression);
            return ToEnumerable().First();
        }

        public T SingleOrDefault()
        {
            return SingleOrDefault(null);
        }

        public T SingleOrDefault(Expression<Func<T, bool>> whereExpression)
        {
            AddWhere(whereExpression);
            return ToEnumerable().SingleOrDefault();
        }

        public T Single()
        {
            return Single(null);
        }

        public T Single(Expression<Func<T, bool>> whereExpression)
        {
            AddWhere(whereExpression);
            return ToEnumerable().Single();
        }

        public int Count()
        {
            return Count(null);
        }

        public int Count(Expression<Func<T, bool>> whereExpression)
        {
            if (whereExpression != null)
                _sqlExpression = _sqlExpression.Where(whereExpression);

            var sql = _buildComplexSql.BuildJoin(_database, _sqlExpression, _joinSqlExpressions.Values.ToList(), null, true, false);

            return _database.ExecuteScalar<int>(sql);
        }

        public bool Any()
        {
            return Count() > 0;
        }

        public bool Any(Expression<Func<T, bool>> whereExpression)
        {
            return Count(whereExpression) > 0;
        }

        public Page<T> ToPage(int page, int pageSize)
        {
            return ToPage(page, pageSize, (paged, action) =>
            {
                var list = ToList();
                action(paged, list);
                return paged;
            });
        }

        private TRet ToPage<TRet>(int page, int pageSize, Func<Page<T>, Action<Page<T>, List<T>>, TRet> executeFunc)
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

            return executeFunc(result, (paged, list) =>
            {
                paged.Items = list;
            });
        }

        public List<T2> ProjectTo<T2>(Expression<Func<T, T2>> projectionExpression)
        {
            var sql = _buildComplexSql.GetSqlForProjection(projectionExpression, false);
            return ExecuteQuery(sql).Select(projectionExpression.Compile()).ToList();
        }

        public List<T2> Distinct<T2>(Expression<Func<T, T2>> projectionExpression)
        {
            var sql = _buildComplexSql.GetSqlForProjection(projectionExpression, true);
            return ExecuteQuery(sql).Select(projectionExpression.Compile()).ToList();
        }

        public List<T> Distinct()
        {
            return ExecuteQuery(new Sql(_sqlExpression.Context.ToSelectStatement(true, true), _sqlExpression.Context.Params)).ToList();
        }

        public T[] ToArray()
        {
            return ToEnumerable().ToArray();
        }

        public List<T> ToList()
        {
            return ToEnumerable().ToList();
        }

        public IEnumerable<T> ToEnumerable()
        {
            var sql = BuildSql();
            return ExecuteQuery(sql);
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

        private IEnumerable<T> ExecuteQuery(Sql sql)
        {
            return _database.QueryImp(default(T), _listExpression, null, sql);
        }

        private Sql BuildSql()
        {
            Sql sql;
            if (_joinSqlExpressions.Any())
                sql = _buildComplexSql.BuildJoin(_database, _sqlExpression, _joinSqlExpressions.Values.ToList(), null, false, false);
            else
                sql = new Sql(true, _sqlExpression.Context.ToSelectStatement(), _sqlExpression.Context.Params);
            return sql;
        }

#if !NET35 && !NET40
        public async System.Threading.Tasks.Task<List<T>> ToListAsync()
        {
            return (await ToEnumerableAsync().ConfigureAwait(false)).ToList();
        }

        public async System.Threading.Tasks.Task<T[]> ToArrayAsync()
        {
            return (await ToEnumerableAsync().ConfigureAwait(false)).ToArray();
        }

        public System.Threading.Tasks.Task<IEnumerable<T>> ToEnumerableAsync()
        {
            return _database.QueryAsync(default(T), _listExpression, null, BuildSql());
        }

        public async System.Threading.Tasks.Task<T> FirstOrDefaultAsync()
        {
            AddWhere(null);
            return (await ToEnumerableAsync().ConfigureAwait(false)).FirstOrDefault();
        }

        public async System.Threading.Tasks.Task<T> FirstAsync()
        {
            AddWhere(null);
            return (await ToEnumerableAsync().ConfigureAwait(false)).First();
        }

        public async System.Threading.Tasks.Task<T> SingleOrDefaultAsync()
        {
            AddWhere(null);
            return (await ToEnumerableAsync().ConfigureAwait(false)).SingleOrDefault();
        }

        public async System.Threading.Tasks.Task<T> SingleAsync()
        {
            AddWhere(null);
            return (await ToEnumerableAsync().ConfigureAwait(false)).Single();
        }

        public async System.Threading.Tasks.Task<int> CountAsync()
        {
            var sql = _buildComplexSql.BuildJoin(_database, _sqlExpression, _joinSqlExpressions.Values.ToList(), null, true, false);
            return await _database.ExecuteScalarAsync<int>(sql).ConfigureAwait(false);
        }

        public async System.Threading.Tasks.Task<bool> AnyAsync()
        {
            return (await CountAsync().ConfigureAwait(false)) > 0;
        }

        public System.Threading.Tasks.Task<Page<T>> ToPageAsync(int page, int pageSize)
        {
            return ToPage(page, pageSize, async (paged, action) =>
            {
                var list = await ToListAsync().ConfigureAwait(false);
                action(paged, list);
                return paged;
            });
        }

        public async System.Threading.Tasks.Task<List<T2>> ProjectToAsync<T2>(Expression<Func<T, T2>> projectionExpression)
        {
            var sql = _buildComplexSql.GetSqlForProjection(projectionExpression, false);
            return (await _database.QueryAsync<T>(sql).ConfigureAwait(false)).Select(projectionExpression.Compile()).ToList();
        }

        public async System.Threading.Tasks.Task<List<T>> DistinctAsync()
        {
            return (await _database.QueryAsync<T>(new Sql(_sqlExpression.Context.ToSelectStatement(true, true), _sqlExpression.Context.Params))).ToList();
        }

        public async System.Threading.Tasks.Task<List<T2>> DistinctAsync<T2>(Expression<Func<T, T2>> projectionExpression)
        {
            var sql = _buildComplexSql.GetSqlForProjection(projectionExpression, true);
            return (await _database.QueryAsync<T>(sql).ConfigureAwait(false)).Select(projectionExpression.Compile()).ToList();
        }
#endif

        public IQueryProvider<T> Where(Expression<Func<T, bool>> whereExpression)
        {
            _sqlExpression = _sqlExpression.Where(whereExpression);
            return this;
        }

        public IQueryProvider<T> Where(string sql, params object[] args)
        {
            _sqlExpression = _sqlExpression.Where(sql, args);
            return this;
        }

        public IQueryProvider<T> Where(Sql sql)
        {
            _sqlExpression = _sqlExpression.Where(sql.SQL, sql.Arguments);
            return this;
        }

        public IQueryProvider<T> Where(Func<QueryContext<T>, Sql> queryBuilder)
        {
            var sql = queryBuilder(new QueryContext<T>(_database, _pocoData, _joinSqlExpressions));
            return Where(sql);
        }

        public IQueryProvider<T> Limit(int rows)
        {
            ThrowIfOneToMany();
            _sqlExpression = _sqlExpression.Limit(rows);
            return this;
        }

        public IQueryProvider<T> Limit(int skip, int rows)
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

        public IQueryProvider<T> OrderBy(Expression<Func<T, object>> column)
        {
            _sqlExpression = _sqlExpression.OrderBy(column);
            return this;
        }

        public IQueryProvider<T> OrderByDescending(Expression<Func<T, object>> column)
        {
            _sqlExpression = _sqlExpression.OrderByDescending(column);
            return this;
        }

        public IQueryProvider<T> ThenBy(Expression<Func<T, object>> column)
        {
            _sqlExpression = _sqlExpression.ThenBy(column);
            return this;
        }

        public IQueryProvider<T> ThenByDescending(Expression<Func<T, object>> column)
        {
            _sqlExpression = _sqlExpression.ThenByDescending(column);
            return this;
        }
    }
}
