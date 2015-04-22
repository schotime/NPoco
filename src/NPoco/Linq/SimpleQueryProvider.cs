using System;
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
        IEnumerable<T> ToEnumerable();
        Page<T> ToPage(int page, int pageSize);
        List<T2> ProjectTo<T2>(Expression<Func<T, T2>> projectionExpression);
        List<T2> Distinct<T2>(Expression<Func<T, T2>> projectionExpression);
        List<T> Distinct();
#if NET45
        System.Threading.Tasks.Task<List<T>> ToListAsync();
        System.Threading.Tasks.Task<IEnumerable<T>> ToEnumerableAsync();
        System.Threading.Tasks.Task<T> FirstOrDefaultAsync();
        System.Threading.Tasks.Task<T> FirstAsync();
        System.Threading.Tasks.Task<T> SingleOrDefaultAsync();
        System.Threading.Tasks.Task<T> SingleAsync();
        System.Threading.Tasks.Task<int> CountAsync();
        System.Threading.Tasks.Task<bool> AnyAsync();
        System.Threading.Tasks.Task<Page<T>> ToPageAsync(int page, int pageSize);
        System.Threading.Tasks.Task<List<T2>> ProjectToAsync<T2>(Expression<Func<T, T2>> projectionExpression);
#endif
    }

    public interface IQueryProvider<T> : IQueryResultProvider<T>
    {
        IQueryProvider<T> Where(Expression<Func<T, bool>> whereExpression);
        IQueryProvider<T> OrderBy(Expression<Func<T, object>> column);
        IQueryProvider<T> OrderByDescending(Expression<Func<T, object>> column);
        IQueryProvider<T> ThenBy(Expression<Func<T, object>> column);
        IQueryProvider<T> ThenByDescending(Expression<Func<T, object>> column);
        IQueryProvider<T> Limit(int rows);
        IQueryProvider<T> Limit(int skip, int rows);
    }

    public interface IQueryProviderWithIncludes<T> : IQueryProvider<T>
    {
        IQueryProviderWithIncludes<T> Include<T2>(Expression<Func<T, T2>> expression) where T2 : class;
        IQueryProviderWithIncludes<T> Include<T2>(Expression<Func<T, T2>> expression, Expression<Func<T, T2, bool>> onExpression) where T2 : class;
    }

    public class QueryProvider<T> : IQueryProviderWithIncludes<T>, ISimpleQueryProviderExpression<T>
    {
        private readonly IDatabase _database;
        private SqlExpression<T> _sqlExpression;
        private Dictionary<string, JoinData> _joinSqlExpressions = new Dictionary<string, JoinData>();
        private readonly ComplexSqlBuilder<T> _buildComplexSql;

        public QueryProvider(IDatabase database, Expression<Func<T, bool>> whereExpression)
        {
            _database = database;
            _sqlExpression = database.DatabaseType.ExpressionVisitor<T>(database, true);
            _buildComplexSql = new ComplexSqlBuilder<T>(database, _sqlExpression, _joinSqlExpressions);
            _sqlExpression = _sqlExpression.Where(whereExpression);
        }

        public QueryProvider(IDatabase database)
            : this(database, null)
        {
        }

        SqlExpression<T> ISimpleQueryProviderExpression<T>.AtlasSqlExpression { get { return _sqlExpression; } }

        public IQueryProviderWithIncludes<T> Include<T2>(Expression<Func<T, T2>> expression) where T2 : class
        {
            return Include(expression, null);
        }

        public IQueryProviderWithIncludes<T> Include<T2>(Expression<Func<T, T2>> expression, Expression<Func<T, T2, bool>> onExpression) where T2 : class
        {
            string onSql;
            if (onExpression == null)
            {
                var pocoDataT = _database.PocoDataFactory.ForType(typeof(T));
                var pocoDataT2 = _database.PocoDataFactory.ForType(typeof(T2));
                var colT2 = pocoDataT2.Columns.Values.Single(x => x.ColumnName == pocoDataT2.TableInfo.PrimaryKey);
                onSql = _database.DatabaseType.EscapeTableName(pocoDataT.TableInfo.AutoAlias)
                        + "." + _database.DatabaseType.EscapeSqlIdentifier(colT2.ColumnName)
                        + " = " + _database.DatabaseType.EscapeTableName(pocoDataT2.TableInfo.AutoAlias)
                        + "." + _database.DatabaseType.EscapeSqlIdentifier(colT2.ColumnName);
            }
            else
            {
                onSql = _database.DatabaseType.ExpressionVisitor<T>(_database, true).On(onExpression);
            }

            if (!_joinSqlExpressions.ContainsKey(onSql))
            {
                _joinSqlExpressions.Add(onSql, new JoinData()
                {
                    OnSql = onSql,
                    Type = typeof(T2)
                });
            }
            return this;
        }

        public IQueryProvider<T> Where(Expression<Func<T, bool>> whereExpression)
        {
            _sqlExpression = _sqlExpression.Where(whereExpression);
            return this;
        }

        private void AddLimitAndWhere(Expression<Func<T, bool>> whereExpression)
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
            AddLimitAndWhere(whereExpression);
            return ToEnumerable().FirstOrDefault();
        }

        public T First()
        {
            return First(null);
        }

        public T First(Expression<Func<T, bool>> whereExpression)
        {
            AddLimitAndWhere(whereExpression);
            return ToEnumerable().First();
        }

        public T SingleOrDefault()
        {
            return SingleOrDefault(null);
        }

        public T SingleOrDefault(Expression<Func<T, bool>> whereExpression)
        {
            AddLimitAndWhere(whereExpression);
            return ToEnumerable().SingleOrDefault();
        }

        public T Single()
        {
            return Single(null);
        }

        public T Single(Expression<Func<T, bool>> whereExpression)
        {
            AddLimitAndWhere(whereExpression);
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
            var types = new[] { typeof(T) }.Concat(_joinSqlExpressions.Values.Select(x => x.Type)).ToArray();
            var sql = _buildComplexSql.GetSqlForProjection(projectionExpression, types, false);
            return _database.Query<T>(types, null, sql).Select(projectionExpression.Compile()).ToList();
        }

        public List<T2> Distinct<T2>(Expression<Func<T, T2>> projectionExpression)
        {
            var types = new[] { typeof(T) }.Concat(_joinSqlExpressions.Values.Select(x => x.Type)).ToArray();
            var sql = _buildComplexSql.GetSqlForProjection(projectionExpression, types, true);
            return _database.Query<T>(types, null, sql).Select(projectionExpression.Compile()).ToList();
        }

        public List<T> Distinct()
        {
            return _database.Query<T>(_sqlExpression.Context.ToSelectStatement(true, true), _sqlExpression.Context.Params).ToList();
        }

        public List<T> ToList()
        {
            return ToEnumerable().ToList();
        }

        public IEnumerable<T> ToEnumerable()
        {
            if (!_joinSqlExpressions.Any())
                return _database.Query<T>(_sqlExpression.Context.ToSelectStatement(), _sqlExpression.Context.Params);

            var types = new[] { typeof(T) }.Concat(_joinSqlExpressions.Values.Select(x => x.Type)).ToArray();
            var sql = _buildComplexSql.BuildJoin(_database, _sqlExpression, _joinSqlExpressions.Values.ToList(), null, false, false);
            return _database.Query<T>(types, null, sql);
        }

#if NET45
        public async System.Threading.Tasks.Task<List<T>> ToListAsync()
        {
            return (await ToEnumerableAsync().ConfigureAwait(false)).ToList();
        }

        public System.Threading.Tasks.Task<IEnumerable<T>> ToEnumerableAsync()
        {
            if (!_joinSqlExpressions.Any())
                return _database.QueryAsync<T>(_sqlExpression.Context.ToSelectStatement(), _sqlExpression.Context.Params);

            var types = new[] { typeof(T) }.Concat(_joinSqlExpressions.Values.Select(x => x.Type)).ToArray();
            var sql = _buildComplexSql.BuildJoin(_database, _sqlExpression, _joinSqlExpressions.Values.ToList(), null, false, false);
            return _database.QueryAsync<T>(types, null, sql);
        }

        public async System.Threading.Tasks.Task<T> FirstOrDefaultAsync()
        {
            AddLimitAndWhere(null);
            return (await ToEnumerableAsync().ConfigureAwait(false)).FirstOrDefault();
        }

        public async System.Threading.Tasks.Task<T> FirstAsync()
        {
            AddLimitAndWhere(null);
            return (await ToEnumerableAsync().ConfigureAwait(false)).First();
        }

        public async System.Threading.Tasks.Task<T> SingleOrDefaultAsync()
        {
            AddLimitAndWhere(null);
            return (await ToEnumerableAsync().ConfigureAwait(false)).SingleOrDefault();
        }

        public async System.Threading.Tasks.Task<T> SingleAsync()
        {
            AddLimitAndWhere(null);
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
            var types = new[] { typeof(T) }.Concat(_joinSqlExpressions.Values.Select(x => x.Type)).ToArray();
            var sql = _buildComplexSql.GetSqlForProjection(projectionExpression, types, false);
            return (await _database.QueryAsync<T>(types, null, sql).ConfigureAwait(false)).Select(projectionExpression.Compile()).ToList();
        }
#endif

        public IQueryProvider<T> Limit(int rows)
        {
            _sqlExpression = _sqlExpression.Limit(rows);
            return this;
        }

        public IQueryProvider<T> Limit(int skip, int rows)
        {
            _sqlExpression = _sqlExpression.Limit(skip, rows);
            return this;
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
