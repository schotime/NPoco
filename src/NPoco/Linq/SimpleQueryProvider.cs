using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using NPoco.Expressions;

namespace NPoco.Linq
{
    public class AsyncQueryProvider<T> : IAsyncQueryProviderWithIncludes<T>, ISimpleQueryProviderExpression<T>, INeedDatabase, INeedSql
    {
        protected readonly Database _database;
        protected ISqlExpression<T> _sqlExpression;
        protected Dictionary<string, JoinData> _joinSqlExpressions = new Dictionary<string, JoinData>();
        protected readonly ComplexSqlBuilder<T> _buildComplexSql;
        protected Expression<Func<T, IList>> _listExpression = null;
        protected PocoData _pocoData;

        public AsyncQueryProvider(Database database, Expression<Func<T, bool>> whereExpression)
        {
            _database = database;
            _pocoData = database.PocoDataFactory.ForType(typeof(T));
            _pocoData.IsQueryGenerated = true;
            _sqlExpression = database.DatabaseType.ExpressionVisitor<T>(database, _pocoData, true);
            _buildComplexSql = new ComplexSqlBuilder<T>(database, _pocoData, _sqlExpression, _joinSqlExpressions);
            _sqlExpression = _sqlExpression.Where(whereExpression);
        }

        ISqlExpression<T> ISimpleQueryProviderExpression<T>.AtlasSqlExpression { get { return _sqlExpression; } }

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

        public IAsyncQueryProvider<T> IncludeMany(Expression<Func<T, IList>> expression, JoinType joinType = JoinType.Left, string joinTableHint = "")
        {
            _listExpression = expression;
            return QueryProviderWithIncludes(expression, null, joinType, joinTableHint);
        }
        
        public IAsyncQueryProviderWithIncludes<T> Include<T2>(JoinType joinType = JoinType.Left, string joinTableHint = "") where T2 : class
        {
            var oneToOneMembers = _database.PocoDataFactory.ForType(typeof(T))
                .Members.Where(x => (x.ReferenceType == ReferenceType.OneToOne || x.ReferenceType == ReferenceType.Foreign)
                                    && x.MemberInfoData.MemberType == typeof(T2));

            foreach (var o2oMember in oneToOneMembers)
            {
                var entityParam = Expression.Parameter(typeof(T), "entity");
                var joinProperty = Expression.Lambda<Func<T, T2>>(Expression.PropertyOrField(entityParam, o2oMember.Name), entityParam);
                Include(joinProperty, joinType, joinTableHint);
            }

            return this;
        }

        public IAsyncQueryProviderWithIncludes<T> Include<T2>(Expression<Func<T, T2>> expression, JoinType joinType = JoinType.Left, string joinTableHint = "") where T2 : class
        {
            return QueryProviderWithIncludes(expression, null, joinType, joinTableHint);
        }

        public IAsyncQueryProviderWithIncludes<T> Include<T2>(Expression<Func<T, T2>> expression, string tableAlias, JoinType joinType = JoinType.Left, string joinTableHint = "") where T2 : class
        {
            return QueryProviderWithIncludes(expression, tableAlias, joinType, joinTableHint);
        }

        public IAsyncQueryProviderWithIncludes<T> UsingAlias(string tableAlias)
        {
            if (!string.IsNullOrEmpty(tableAlias))
                _pocoData.TableInfo.AutoAlias = tableAlias;
            return this;
        }

        public IAsyncQueryProviderWithIncludes<T> Hint(string tableHint)
        {
            _sqlExpression.Hint(tableHint);
            return this;
        }

        private IAsyncQueryProviderWithIncludes<T> QueryProviderWithIncludes(Expression expression, string tableAlias, JoinType joinType, string joinTableHint)
        {
            var joinExpressions = _buildComplexSql.GetJoinExpressions(expression, tableAlias, joinType, joinTableHint);
            foreach (var joinExpression in joinExpressions)
            {
                _joinSqlExpressions[joinExpression.Key] = joinExpression.Value;
            }

            return this;
        }

        public Task<List<T>> ToList(CancellationToken cancellationToken)
        {
            return ToEnumerable(cancellationToken).ToListAsync(cancellationToken).AsTask();
        }

        public Task<T[]> ToArray(CancellationToken cancellationToken)
        {
            return ToEnumerable(cancellationToken).ToArrayAsync(cancellationToken).AsTask();
        }

        public IAsyncEnumerable<T> ToEnumerable(CancellationToken cancellationToken)
        {
            return ExecuteQueryAsync(BuildSql(), cancellationToken);
        }

        private IAsyncEnumerable<T> ExecuteQueryAsync(Sql sql, CancellationToken cancellationToken)
        {
            return _database.QueryAsync<T>(default, _listExpression, null, sql, _pocoData, cancellationToken);
        }

        public Task<T> FirstOrDefault(CancellationToken cancellationToken = default)
        {
            return FirstOrDefault(null, cancellationToken);
        }

        public Task<T> FirstOrDefault(Expression<Func<T, bool>> whereExpression, CancellationToken cancellationToken = default)
        {
            AddWhere(whereExpression);
            return ToEnumerable(cancellationToken).FirstOrDefaultAsync(cancellationToken).AsTask();
        }

        public Task<T> First(CancellationToken cancellationToken = default)
        {
            return First(null, cancellationToken);
        }

        public Task<T> First(Expression<Func<T, bool>> whereExpression, CancellationToken cancellationToken = default)
        {
            AddWhere(whereExpression);
            return ToEnumerable(cancellationToken).FirstAsync(cancellationToken).AsTask();
        }

        public Task<T> SingleOrDefault(CancellationToken cancellationToken = default)
        {
            return SingleOrDefault(null, cancellationToken);
        }

        public Task<T> SingleOrDefault(Expression<Func<T, bool>> whereExpression, CancellationToken cancellationToken = default)
        {
            AddWhere(whereExpression);
            return ToEnumerable(cancellationToken).SingleOrDefaultAsync(cancellationToken).AsTask();
        }

        public Task<T> Single(CancellationToken cancellationToken = default)
        {
            return Single(null, cancellationToken);
        }

        public Task<T> Single(Expression<Func<T, bool>> whereExpression, CancellationToken cancellationToken = default)
        {
            AddWhere(whereExpression);
            return ToEnumerable(cancellationToken).SingleAsync(cancellationToken).AsTask();
        }

        public Task<int> Count(CancellationToken cancellationToken = default)
        {
            return Count(null, cancellationToken);
        }

        public Task<int> Count(Expression<Func<T, bool>> whereExpression, CancellationToken cancellationToken = default)
        {
            AddWhere(whereExpression);
            var sql = _buildComplexSql.BuildJoin(_database, _sqlExpression, _joinSqlExpressions.Values.ToList(), null, true, false);
            return _database.ExecuteScalarAsync<int>(sql, cancellationToken);
        }

        public Task<bool> Any(CancellationToken cancellationToken = default)
        {
            return Any(null, cancellationToken);
        }

        public async Task<bool> Any(Expression<Func<T, bool>> whereExpression, CancellationToken cancellationToken = default)
        {
            return (await Count(whereExpression, cancellationToken).ConfigureAwait(false)) > 0;
        }

        public async Task<Page<T>> ToPage(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            int offset = (page - 1) * pageSize;

            // Save the one-time command time out and use it for both queries
            int saveTimeout = _database.OneTimeCommandTimeout;

            // Setup the paged result
            var result = new Page<T>();
            result.CurrentPage = page;
            result.ItemsPerPage = pageSize;
            result.TotalItems = await Count(cancellationToken).ConfigureAwait(false);
            result.TotalPages = result.TotalItems / pageSize;
            if ((result.TotalItems % pageSize) != 0)
                result.TotalPages++;

            _database.OneTimeCommandTimeout = saveTimeout;

            _sqlExpression = _sqlExpression.Limit(offset, pageSize);

            result.Items = await ToList(cancellationToken).ConfigureAwait(false);

            return result;
        }

        public Task<List<T2>> ProjectTo<T2>(Expression<Func<T, T2>> projectionExpression, CancellationToken cancellationToken = default)
        {
            var sql = _buildComplexSql.GetSqlForProjection(projectionExpression, false);
            return ExecuteQueryAsync(sql, cancellationToken).Select(projectionExpression.Compile()).ToListAsync(cancellationToken).AsTask();
        }

        public Task<List<T>> Distinct(CancellationToken cancellationToken = default)
        {
            return ExecuteQueryAsync(new Sql(_sqlExpression.Context.ToSelectStatement(true, true), _sqlExpression.Context.Params), cancellationToken).ToListAsync(cancellationToken).AsTask();
        }

        public Task<List<T2>> Distinct<T2>(Expression<Func<T, T2>> projectionExpression, CancellationToken cancellationToken = default)
        {
            var sql = _buildComplexSql.GetSqlForProjection(projectionExpression, true);
            return ExecuteQueryAsync(sql, cancellationToken).Select(projectionExpression.Compile()).ToListAsync(cancellationToken).AsTask();
        }

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

        public List<dynamic> ToDynamicList()
        {
            return ToDynamicEnumerable().ToList();
        }

        public IEnumerable<dynamic> ToDynamicEnumerable()
        {
            var sql = BuildSql();
            return _database.QueryImp<dynamic>(null, null, null, sql);
        }

        IDatabase INeedDatabase.GetDatabase()
        {
            return _database;
        }

        Sql INeedSql.GetSql()
        {
            return BuildSql();
        }
    }

    public interface INeedSql
    {
        Sql GetSql();
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
            return ExecuteQuery(BuildSql());
        }
        
        private IEnumerable<T> ExecuteQuery(Sql sql)
        {
            return _database.QueryImp(default(T), _listExpression, null, sql, _pocoData);
        }
#pragma warning restore CS0109

        public Task<List<T>> ToListAsync(CancellationToken cancellationToken = default)
        {
            return base.ToList(cancellationToken);
        }

        public Task<T[]> ToArrayAsync(CancellationToken cancellationToken = default)
        {
            return base.ToArray(cancellationToken);
        }

        public IAsyncEnumerable<T> ToEnumerableAsync(CancellationToken cancellationToken = default)
        {
            return base.ToEnumerable(cancellationToken);
        }

        public Task<T> FirstOrDefaultAsync(CancellationToken cancellationToken = default)
        {
            return base.FirstOrDefault(cancellationToken);
        }

        public Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> whereExpression, CancellationToken cancellationToken = default)
        {
            return base.FirstOrDefault(whereExpression, cancellationToken);
        }

        public Task<T> FirstAsync(CancellationToken cancellationToken = default)
        {
            return base.First(cancellationToken);
        }

        public Task<T> FirstAsync(Expression<Func<T, bool>> whereExpression, CancellationToken cancellationToken = default)
        {
            return base.First(whereExpression, cancellationToken);
        }

        public Task<T> SingleOrDefaultAsync(CancellationToken cancellationToken = default)
        {
            return base.SingleOrDefault(cancellationToken);
        }

        public Task<T> SingleOrDefaultAsync(Expression<Func<T, bool>> whereExpression, CancellationToken cancellationToken = default)
        {
            return base.SingleOrDefault(whereExpression, cancellationToken);
        }

        public Task<T> SingleAsync(CancellationToken cancellationToken = default)
        {
            return base.Single(cancellationToken);
        }

        public Task<T> SingleAsync(Expression<Func<T, bool>> whereExpression, CancellationToken cancellationToken = default)
        {
            return base.Single(whereExpression, cancellationToken);
        }

        public Task<int> CountAsync(CancellationToken cancellationToken = default)
        {
            return base.Count(cancellationToken);
        }

        public Task<int> CountAsync(Expression<Func<T, bool>> whereExpression, CancellationToken cancellationToken = default)
        {
            return base.Count(whereExpression, cancellationToken);
        }

        public Task<bool> AnyAsync(CancellationToken cancellationToken = default)
        {
            return base.Any(cancellationToken);
        }

        public Task<bool> AnyAsync(Expression<Func<T, bool>> whereExpression, CancellationToken cancellationToken = default)
        {
            return base.Any(whereExpression, cancellationToken);
        }

        public Task<Page<T>> ToPageAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            return base.ToPage(page, pageSize, cancellationToken);
        }

        public Task<List<T2>> ProjectToAsync<T2>(Expression<Func<T, T2>> projectionExpression, CancellationToken cancellationToken = default)
        {
            return base.ProjectTo(projectionExpression, cancellationToken);
        }

        public Task<List<T2>> DistinctAsync<T2>(Expression<Func<T, T2>> projectionExpression, CancellationToken cancellationToken = default)
        {
            return base.Distinct(projectionExpression, cancellationToken);
        }

        public Task<List<T>> DistinctAsync(CancellationToken cancellationToken = default)
        {
            return base.Distinct(cancellationToken);
        }
        
        public new IQueryProvider<T> IncludeMany(Expression<Func<T, IList>> expression, JoinType joinType = JoinType.Left, string joinTableHint = "")
        {
            return (IQueryProvider<T>)base.IncludeMany(expression, joinType, joinTableHint);
        }

        public new IQueryProviderWithIncludes<T> Include<T2>(JoinType joinType = JoinType.Left, string joinTableHint = "") where T2 : class
        {
            return (IQueryProviderWithIncludes<T>)base.Include<T2>(joinType, joinTableHint);
        }

        public new IQueryProviderWithIncludes<T> Include<T2>(Expression<Func<T, T2>> expression, JoinType joinType = JoinType.Left, string joinTableHint = "") where T2 : class
        {
            return (IQueryProviderWithIncludes<T>)base.Include(expression, joinType, joinTableHint);
        }

        public new IQueryProviderWithIncludes<T> Include<T2>(Expression<Func<T, T2>> expression, string tableAlias, JoinType joinType = JoinType.Left, string joinTableHint = "") where T2 : class
        {
            return (IQueryProviderWithIncludes<T>)base.Include(expression, tableAlias, joinType, joinTableHint);
        }

        public new IQueryProviderWithIncludes<T> UsingAlias(string tableAlias)
        {
            return (IQueryProviderWithIncludes<T>)base.UsingAlias(tableAlias);
        }

        public new IQueryProviderWithIncludes<T> Hint(string tableHint)
        {
            return (IQueryProviderWithIncludes<T>)base.Hint(tableHint);
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
