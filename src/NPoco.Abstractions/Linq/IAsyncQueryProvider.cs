using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NPoco.Linq
{
    public interface IAsyncQueryResultProvider<T>
    {
        Task<List<T>> ToList(CancellationToken cancellationToken = default);
        Task<T[]> ToArray(CancellationToken cancellationToken = default);
        IAsyncEnumerable<T> ToEnumerable(CancellationToken cancellationToken = default);
        Task<T> FirstOrDefault(CancellationToken cancellationToken = default);
        Task<T> FirstOrDefault(Expression<Func<T, bool>> whereExpression, CancellationToken cancellationToken = default);
        Task<T> First(CancellationToken cancellationToken = default);
        Task<T> First(Expression<Func<T, bool>> whereExpression, CancellationToken cancellationToken = default);
        Task<T> SingleOrDefault(CancellationToken cancellationToken = default);
        Task<T> SingleOrDefault(Expression<Func<T, bool>> whereExpression, CancellationToken cancellationToken = default);
        Task<T> Single(CancellationToken cancellationToken = default);
        Task<T> Single(Expression<Func<T, bool>> whereExpression, CancellationToken cancellationToken = default);
        Task<int> Count(CancellationToken cancellationToken = default);
        Task<int> Count(Expression<Func<T, bool>> whereExpression, CancellationToken cancellationToken = default);
        Task<bool> Any(CancellationToken cancellationToken = default);
        Task<bool> Any(Expression<Func<T, bool>> whereExpression, CancellationToken cancellationToken = default);
        Task<Page<T>> ToPage(int page, int pageSize, CancellationToken cancellationToken = default);
        Task<List<T2>> ProjectTo<T2>(Expression<Func<T, T2>> projectionExpression, CancellationToken cancellationToken = default);
        Task<List<T2>> Distinct<T2>(Expression<Func<T, T2>> projectionExpression, CancellationToken cancellationToken = default);
        Task<List<T>> Distinct(CancellationToken cancellationToken = default);
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
        List<dynamic> ToDynamicList();
        IEnumerable<dynamic> ToDynamicEnumerable();
        Page<T> ToPage(int page, int pageSize);
        List<T2> ProjectTo<T2>(Expression<Func<T, T2>> projectionExpression);
        List<T2> Distinct<T2>(Expression<Func<T, T2>> projectionExpression);
        List<T> Distinct();
        Task<List<T>> ToListAsync(CancellationToken cancellationToken = default);
        Task<T[]> ToArrayAsync(CancellationToken cancellationToken = default);
        IAsyncEnumerable<T> ToEnumerableAsync(CancellationToken cancellationToken = default);
        Task<T> FirstOrDefaultAsync(CancellationToken cancellationToken = default);
        Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> whereExpression, CancellationToken cancellationToken = default);
        Task<T> FirstAsync(CancellationToken cancellationToken = default);
        Task<T> FirstAsync(Expression<Func<T, bool>> whereExpression, CancellationToken cancellationToken = default);
        Task<T> SingleOrDefaultAsync(CancellationToken cancellationToken = default);
        Task<T> SingleOrDefaultAsync(Expression<Func<T, bool>> whereExpression, CancellationToken cancellationToken = default);
        Task<T> SingleAsync(CancellationToken cancellationToken = default);
        Task<T> SingleAsync(Expression<Func<T, bool>> whereExpression, CancellationToken cancellationToken = default);
        Task<int> CountAsync(CancellationToken cancellationToken = default);
        Task<int> CountAsync(Expression<Func<T, bool>> whereExpression, CancellationToken cancellationToken = default);
        Task<bool> AnyAsync(CancellationToken cancellationToken = default);
        Task<bool> AnyAsync(Expression<Func<T, bool>> whereExpression, CancellationToken cancellationToken = default);
        Task<Page<T>> ToPageAsync(int page, int pageSize, CancellationToken cancellationToken = default);
        Task<List<T2>> ProjectToAsync<T2>(Expression<Func<T, T2>> projectionExpression, CancellationToken cancellationToken = default);
        Task<List<T2>> DistinctAsync<T2>(Expression<Func<T, T2>> projectionExpression, CancellationToken cancellationToken = default);
        Task<List<T>> DistinctAsync(CancellationToken cancellationToken = default);
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
        IAsyncQueryProvider<T> IncludeMany(Expression<Func<T, IList>> expression, JoinType joinType = JoinType.Left, string joinTableHint = "");
        IAsyncQueryProviderWithIncludes<T> Include<T2>(JoinType joinType = JoinType.Left, string joinTableHint = "") where T2 : class;
        IAsyncQueryProviderWithIncludes<T> Include<T2>(Expression<Func<T, T2>> expression, JoinType joinType = JoinType.Left, string joinTableHint = "") where T2 : class;
        IAsyncQueryProviderWithIncludes<T> Include<T2>(Expression<Func<T, T2>> expression, string tableAlias, JoinType joinType = JoinType.Left, string joinTableHint = "") where T2 : class;
        IAsyncQueryProviderWithIncludes<T> UsingAlias(string tableAlias);
        IAsyncQueryProviderWithIncludes<T> Hint(string tableHint);
    }

    public interface IQueryProviderWithIncludes<T> : IQueryProvider<T>
    {
        IQueryProvider<T> IncludeMany(Expression<Func<T, IList>> expression, JoinType joinType = JoinType.Left, string joinTableHint = "");
        IQueryProviderWithIncludes<T> Include<T2>(JoinType joinType = JoinType.Left, string joinTableHint = "") where T2 : class;
        IQueryProviderWithIncludes<T> Include<T2>(Expression<Func<T, T2>> expression, JoinType joinType = JoinType.Left, string joinTableHint = "") where T2 : class;
        IQueryProviderWithIncludes<T> Include<T2>(Expression<Func<T, T2>> expression, string tableAlias, JoinType joinType = JoinType.Left, string joinTableHint = "") where T2 : class;
        IQueryProviderWithIncludes<T> UsingAlias(string tableAlias);
        IQueryProviderWithIncludes<T> Hint(string tableHint);
    }
}
