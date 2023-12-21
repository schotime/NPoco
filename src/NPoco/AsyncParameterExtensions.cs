using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NPoco
{
    public static class AsyncParameterExtensions
    {
        // ExecuteScalarAsync
        public static Task<T> ExecuteScalarAsync<T>(this IAsyncDatabase db, string sql, object arg0, CancellationToken cancellationToken = default) => db.ExecuteScalarAsync<T>(new Sql(sql, arg0), cancellationToken);
        public static Task<T> ExecuteScalarAsync<T>(this IAsyncDatabase db, string sql, object arg0, object arg1, CancellationToken cancellationToken = default) => db.ExecuteScalarAsync<T>(new Sql(sql, arg0, arg1), cancellationToken);
        public static Task<T> ExecuteScalarAsync<T>(this IAsyncDatabase db, string sql, object arg0, object arg1, object arg2, CancellationToken cancellationToken = default) => db.ExecuteScalarAsync<T>(new Sql(sql, arg0, arg1, arg2), cancellationToken);
        public static Task<T> ExecuteScalarAsync<T>(this IAsyncDatabase db, string sql, object arg0, object arg1, object arg2, object arg3, CancellationToken cancellationToken = default) => db.ExecuteScalarAsync<T>(new Sql(sql, arg0, arg1, arg2, arg3), cancellationToken);
        public static Task<T> ExecuteScalarAsync<T>(this IAsyncDatabase db, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, CancellationToken cancellationToken = default) => db.ExecuteScalarAsync<T>(new Sql(sql, arg0, arg1, arg2, arg3, arg4), cancellationToken);
        public static Task<T> ExecuteScalarAsync<T>(this IAsyncDatabase db, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, CancellationToken cancellationToken = default) => db.ExecuteScalarAsync<T>(new Sql(sql, arg0, arg1, arg2, arg3, arg4, arg5), cancellationToken);
        public static Task<T> ExecuteScalarAsync<T>(this IAsyncDatabase db, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, CancellationToken cancellationToken = default) => db.ExecuteScalarAsync<T>(new Sql(sql, arg0, arg1, arg2, arg3, arg4, arg5, arg6), cancellationToken);
        public static Task<T> ExecuteScalarAsync<T>(this IAsyncDatabase db, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, CancellationToken cancellationToken = default) => db.ExecuteScalarAsync<T>(new Sql(sql, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7), cancellationToken);

        // ExecuteAsync
        public static Task<int> ExecuteAsync(this IAsyncDatabase db, string sql, object arg0, CancellationToken cancellationToken = default) => db.ExecuteAsync(new Sql(sql, arg0), cancellationToken);
        public static Task<int> ExecuteAsync(this IAsyncDatabase db, string sql, object arg0, object arg1, CancellationToken cancellationToken = default) => db.ExecuteAsync(new Sql(sql, arg0, arg1), cancellationToken);
        public static Task<int> ExecuteAsync(this IAsyncDatabase db, string sql, object arg0, object arg1, object arg2, CancellationToken cancellationToken = default) => db.ExecuteAsync(new Sql(sql, arg0, arg1, arg2), cancellationToken);
        public static Task<int> ExecuteAsync(this IAsyncDatabase db, string sql, object arg0, object arg1, object arg2, object arg3, CancellationToken cancellationToken = default) => db.ExecuteAsync(new Sql(sql, arg0, arg1, arg2, arg3), cancellationToken);
        public static Task<int> ExecuteAsync(this IAsyncDatabase db, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, CancellationToken cancellationToken = default) => db.ExecuteAsync(new Sql(sql, arg0, arg1, arg2, arg3, arg4), cancellationToken);
        public static Task<int> ExecuteAsync(this IAsyncDatabase db, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, CancellationToken cancellationToken = default) => db.ExecuteAsync(new Sql(sql, arg0, arg1, arg2, arg3, arg4, arg5), cancellationToken);
        public static Task<int> ExecuteAsync(this IAsyncDatabase db, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, CancellationToken cancellationToken = default) => db.ExecuteAsync(new Sql(sql, arg0, arg1, arg2, arg3, arg4, arg5, arg6), cancellationToken);
        public static Task<int> ExecuteAsync(this IAsyncDatabase db, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, CancellationToken cancellationToken = default) => db.ExecuteAsync(new Sql(sql, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7), cancellationToken);

        // SingleAsync
        public static Task<T> SingleAsync<T>(this IAsyncQueryDatabase db, string sql, object arg0, CancellationToken cancellationToken = default) => db.SingleAsync<T>(new Sql(sql, arg0), cancellationToken);
        public static Task<T> SingleAsync<T>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, CancellationToken cancellationToken = default) => db.SingleAsync<T>(new Sql(sql, arg0, arg1), cancellationToken);
        public static Task<T> SingleAsync<T>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, object arg2, CancellationToken cancellationToken = default) => db.SingleAsync<T>(new Sql(sql, arg0, arg1, arg2), cancellationToken);
        public static Task<T> SingleAsync<T>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, object arg2, object arg3, CancellationToken cancellationToken = default) => db.SingleAsync<T>(new Sql(sql, arg0, arg1, arg2, arg3), cancellationToken);
        public static Task<T> SingleAsync<T>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, CancellationToken cancellationToken = default) => db.SingleAsync<T>(new Sql(sql, arg0, arg1, arg2, arg3, arg4), cancellationToken);
        public static Task<T> SingleAsync<T>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, CancellationToken cancellationToken = default) => db.SingleAsync<T>(new Sql(sql, arg0, arg1, arg2, arg3, arg4, arg5), cancellationToken);
        public static Task<T> SingleAsync<T>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, CancellationToken cancellationToken = default) => db.SingleAsync<T>(new Sql(sql, arg0, arg1, arg2, arg3, arg4, arg5, arg6), cancellationToken);
        public static Task<T> SingleAsync<T>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, CancellationToken cancellationToken = default) => db.SingleAsync<T>(new Sql(sql, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7), cancellationToken);

        // SingleOrDefaultAsync
        public static Task<T> SingleOrDefaultAsync<T>(this IAsyncQueryDatabase db, string sql, object arg0, CancellationToken cancellationToken = default) => db.SingleOrDefaultAsync<T>(new Sql(sql, arg0), cancellationToken);
        public static Task<T> SingleOrDefaultAsync<T>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, CancellationToken cancellationToken = default) => db.SingleOrDefaultAsync<T>(new Sql(sql, arg0, arg1), cancellationToken);
        public static Task<T> SingleOrDefaultAsync<T>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, object arg2, CancellationToken cancellationToken = default) => db.SingleOrDefaultAsync<T>(new Sql(sql, arg0, arg1, arg2), cancellationToken);
        public static Task<T> SingleOrDefaultAsync<T>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, object arg2, object arg3, CancellationToken cancellationToken = default) => db.SingleOrDefaultAsync<T>(new Sql(sql, arg0, arg1, arg2, arg3), cancellationToken);
        public static Task<T> SingleOrDefaultAsync<T>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, CancellationToken cancellationToken = default) => db.SingleOrDefaultAsync<T>(new Sql(sql, arg0, arg1, arg2, arg3, arg4), cancellationToken);
        public static Task<T> SingleOrDefaultAsync<T>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, CancellationToken cancellationToken = default) => db.SingleOrDefaultAsync<T>(new Sql(sql, arg0, arg1, arg2, arg3, arg4, arg5), cancellationToken);
        public static Task<T> SingleOrDefaultAsync<T>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, CancellationToken cancellationToken = default) => db.SingleOrDefaultAsync<T>(new Sql(sql, arg0, arg1, arg2, arg3, arg4, arg5, arg6), cancellationToken);
        public static Task<T> SingleOrDefaultAsync<T>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, CancellationToken cancellationToken = default) => db.SingleOrDefaultAsync<T>(new Sql(sql, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7), cancellationToken);

        // FirstAsync
        public static Task<T> FirstAsync<T>(this IAsyncQueryDatabase db, string sql, object arg0, CancellationToken cancellationToken = default) => db.FirstAsync<T>(new Sql(sql, arg0), cancellationToken);
        public static Task<T> FirstAsync<T>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, CancellationToken cancellationToken = default) => db.FirstAsync<T>(new Sql(sql, arg0, arg1), cancellationToken);
        public static Task<T> FirstAsync<T>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, object arg2, CancellationToken cancellationToken = default) => db.FirstAsync<T>(new Sql(sql, arg0, arg1, arg2), cancellationToken);
        public static Task<T> FirstAsync<T>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, object arg2, object arg3, CancellationToken cancellationToken = default) => db.FirstAsync<T>(new Sql(sql, arg0, arg1, arg2, arg3), cancellationToken);
        public static Task<T> FirstAsync<T>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, CancellationToken cancellationToken = default) => db.FirstAsync<T>(new Sql(sql, arg0, arg1, arg2, arg3, arg4), cancellationToken);
        public static Task<T> FirstAsync<T>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, CancellationToken cancellationToken = default) => db.FirstAsync<T>(new Sql(sql, arg0, arg1, arg2, arg3, arg4, arg5), cancellationToken);
        public static Task<T> FirstAsync<T>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, CancellationToken cancellationToken = default) => db.FirstAsync<T>(new Sql(sql, arg0, arg1, arg2, arg3, arg4, arg5, arg6), cancellationToken);
        public static Task<T> FirstAsync<T>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, CancellationToken cancellationToken = default) => db.FirstAsync<T>(new Sql(sql, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7), cancellationToken);

        // FirstOrDefaultAsync
        public static Task<T> FirstOrDefaultAsync<T>(this IAsyncQueryDatabase db, string sql, object arg0, CancellationToken cancellationToken = default) => db.FirstOrDefaultAsync<T>(new Sql(sql, arg0), cancellationToken);
        public static Task<T> FirstOrDefaultAsync<T>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, CancellationToken cancellationToken = default) => db.FirstOrDefaultAsync<T>(new Sql(sql, arg0, arg1), cancellationToken);
        public static Task<T> FirstOrDefaultAsync<T>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, object arg2, CancellationToken cancellationToken = default) => db.FirstOrDefaultAsync<T>(new Sql(sql, arg0, arg1, arg2), cancellationToken);
        public static Task<T> FirstOrDefaultAsync<T>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, object arg2, object arg3, CancellationToken cancellationToken = default) => db.FirstOrDefaultAsync<T>(new Sql(sql, arg0, arg1, arg2, arg3), cancellationToken);
        public static Task<T> FirstOrDefaultAsync<T>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, CancellationToken cancellationToken = default) => db.FirstOrDefaultAsync<T>(new Sql(sql, arg0, arg1, arg2, arg3, arg4), cancellationToken);
        public static Task<T> FirstOrDefaultAsync<T>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, CancellationToken cancellationToken = default) => db.FirstOrDefaultAsync<T>(new Sql(sql, arg0, arg1, arg2, arg3, arg4, arg5), cancellationToken);
        public static Task<T> FirstOrDefaultAsync<T>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, CancellationToken cancellationToken = default) => db.FirstOrDefaultAsync<T>(new Sql(sql, arg0, arg1, arg2, arg3, arg4, arg5, arg6), cancellationToken);
        public static Task<T> FirstOrDefaultAsync<T>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, CancellationToken cancellationToken = default) => db.FirstOrDefaultAsync<T>(new Sql(sql, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7), cancellationToken);

        // FetchAsync
        public static Task<List<T>> FetchAsync<T>(this IAsyncQueryDatabase db, string sql, object arg0, CancellationToken cancellationToken = default) => db.FetchAsync<T>(new Sql(sql, arg0), cancellationToken);
        public static Task<List<T>> FetchAsync<T>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, CancellationToken cancellationToken = default) => db.FetchAsync<T>(new Sql(sql, arg0, arg1), cancellationToken);
        public static Task<List<T>> FetchAsync<T>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, object arg2, CancellationToken cancellationToken = default) => db.FetchAsync<T>(new Sql(sql, arg0, arg1, arg2), cancellationToken);
        public static Task<List<T>> FetchAsync<T>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, object arg2, object arg3, CancellationToken cancellationToken = default) => db.FetchAsync<T>(new Sql(sql, arg0, arg1, arg2, arg3), cancellationToken);
        public static Task<List<T>> FetchAsync<T>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, CancellationToken cancellationToken = default) => db.FetchAsync<T>(new Sql(sql, arg0, arg1, arg2, arg3, arg4), cancellationToken);
        public static Task<List<T>> FetchAsync<T>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, CancellationToken cancellationToken = default) => db.FetchAsync<T>(new Sql(sql, arg0, arg1, arg2, arg3, arg4, arg5), cancellationToken);
        public static Task<List<T>> FetchAsync<T>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, CancellationToken cancellationToken = default) => db.FetchAsync<T>(new Sql(sql, arg0, arg1, arg2, arg3, arg4, arg5, arg6), cancellationToken);
        public static Task<List<T>> FetchAsync<T>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, CancellationToken cancellationToken = default) => db.FetchAsync<T>(new Sql(sql, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7), cancellationToken);

        // QueryAsync
        public static IAsyncEnumerable<T> QueryAsync<T>(this IAsyncQueryDatabase db, string sql, object arg0, CancellationToken cancellationToken = default) => db.QueryAsync<T>(new Sql(sql, arg0), cancellationToken);
        public static IAsyncEnumerable<T> QueryAsync<T>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, CancellationToken cancellationToken = default) => db.QueryAsync<T>(new Sql(sql, arg0, arg1), cancellationToken);
        public static IAsyncEnumerable<T> QueryAsync<T>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, object arg2, CancellationToken cancellationToken = default) => db.QueryAsync<T>(new Sql(sql, arg0, arg1, arg2), cancellationToken);
        public static IAsyncEnumerable<T> QueryAsync<T>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, object arg2, object arg3, CancellationToken cancellationToken = default) => db.QueryAsync<T>(new Sql(sql, arg0, arg1, arg2, arg3), cancellationToken);
        public static IAsyncEnumerable<T> QueryAsync<T>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, CancellationToken cancellationToken = default) => db.QueryAsync<T>(new Sql(sql, arg0, arg1, arg2, arg3, arg4), cancellationToken);
        public static IAsyncEnumerable<T> QueryAsync<T>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, CancellationToken cancellationToken = default) => db.QueryAsync<T>(new Sql(sql, arg0, arg1, arg2, arg3, arg4, arg5), cancellationToken);
        public static IAsyncEnumerable<T> QueryAsync<T>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, CancellationToken cancellationToken = default) => db.QueryAsync<T>(new Sql(sql, arg0, arg1, arg2, arg3, arg4, arg5, arg6), cancellationToken);
        public static IAsyncEnumerable<T> QueryAsync<T>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, CancellationToken cancellationToken = default) => db.QueryAsync<T>(new Sql(sql, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7), cancellationToken);

        // PageAsync
        public static Task<Page<T>> PageAsync<T>(this IAsyncQueryDatabase db, long page, long itemsPerPage, string sql, object arg0, CancellationToken cancellationToken = default) 
            => db.PageAsync<T>(page, itemsPerPage, new Sql(sql, arg0), cancellationToken);
        public static Task<Page<T>> PageAsync<T>(this IAsyncQueryDatabase db, long page, long itemsPerPage, string sql, object arg0, object arg1, CancellationToken cancellationToken = default)
            => db.PageAsync<T>(page, itemsPerPage, new Sql(sql, arg0, arg1), cancellationToken);
        public static Task<Page<T>> PageAsync<T>(this IAsyncQueryDatabase db, long page, long itemsPerPage, string sql, object arg0, object arg1, object arg2, CancellationToken cancellationToken = default)
            => db.PageAsync<T>(page, itemsPerPage, new Sql(sql, arg0, arg1, arg2), cancellationToken);
        public static Task<Page<T>> PageAsync<T>(this IAsyncQueryDatabase db, long page, long itemsPerPage, string sql, object arg0, object arg1, object arg2, object arg3, CancellationToken cancellationToken = default)
            => db.PageAsync<T>(page, itemsPerPage, new Sql(sql, arg0, arg1, arg2, arg3), cancellationToken);
        public static Task<Page<T>> PageAsync<T>(this IAsyncQueryDatabase db, long page, long itemsPerPage, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, CancellationToken cancellationToken = default)
            => db.PageAsync<T>(page, itemsPerPage, new Sql(sql, arg0, arg1, arg2, arg3, arg4), cancellationToken);
        public static Task<Page<T>> PageAsync<T>(this IAsyncQueryDatabase db, long page, long itemsPerPage, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, CancellationToken cancellationToken = default)
            => db.PageAsync<T>(page, itemsPerPage, new Sql(sql, arg0, arg1, arg2, arg3, arg4, arg5), cancellationToken);
        public static Task<Page<T>> PageAsync<T>(this IAsyncQueryDatabase db, long page, long itemsPerPage, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, CancellationToken cancellationToken = default)
            => db.PageAsync<T>(page, itemsPerPage, new Sql(sql, arg0, arg1, arg2, arg3, arg4, arg5, arg6), cancellationToken);
        public static Task<Page<T>> PageAsync<T>(this IAsyncQueryDatabase db, long page, long itemsPerPage, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, CancellationToken cancellationToken = default)
            => db.PageAsync<T>(page, itemsPerPage, new Sql(sql, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7), cancellationToken);

        // FetchAsyncPaged
        public static Task<List<T>> FetchAsync<T>(this IAsyncQueryDatabase db, long page, long itemsPerPage, string sql, object arg0, CancellationToken cancellationToken = default)
            => db.FetchAsync<T>(page, itemsPerPage, new Sql(sql, arg0), cancellationToken);
        public static Task<List<T>> FetchAsync<T>(this IAsyncQueryDatabase db, long page, long itemsPerPage, string sql, object arg0, object arg1, CancellationToken cancellationToken = default)
            => db.FetchAsync<T>(page, itemsPerPage, new Sql(sql, arg0, arg1), cancellationToken);
        public static Task<List<T>> FetchAsync<T>(this IAsyncQueryDatabase db, long page, long itemsPerPage, string sql, object arg0, object arg1, object arg2, CancellationToken cancellationToken = default)
            => db.FetchAsync<T>(page, itemsPerPage, new Sql(sql, arg0, arg1, arg2), cancellationToken);
        public static Task<List<T>> FetchAsync<T>(this IAsyncQueryDatabase db, long page, long itemsPerPage, string sql, object arg0, object arg1, object arg2, object arg3, CancellationToken cancellationToken = default)
            => db.FetchAsync<T>(page, itemsPerPage, new Sql(sql, arg0, arg1, arg2, arg3), cancellationToken);
        public static Task<List<T>> FetchAsync<T>(this IAsyncQueryDatabase db, long page, long itemsPerPage, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, CancellationToken cancellationToken = default)
            => db.FetchAsync<T>(page, itemsPerPage, new Sql(sql, arg0, arg1, arg2, arg3, arg4), cancellationToken);
        public static Task<List<T>> FetchAsync<T>(this IAsyncQueryDatabase db, long page, long itemsPerPage, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, CancellationToken cancellationToken = default)
            => db.FetchAsync<T>(page, itemsPerPage, new Sql(sql, arg0, arg1, arg2, arg3, arg4, arg5), cancellationToken);
        public static Task<List<T>> FetchAsync<T>(this IAsyncQueryDatabase db, long page, long itemsPerPage, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, CancellationToken cancellationToken = default)
            => db.FetchAsync<T>(page, itemsPerPage, new Sql(sql, arg0, arg1, arg2, arg3, arg4, arg5, arg6), cancellationToken);
        public static Task<List<T>> FetchAsync<T>(this IAsyncQueryDatabase db, long page, long itemsPerPage, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, CancellationToken cancellationToken = default)
            => db.FetchAsync<T>(page, itemsPerPage, new Sql(sql, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7), cancellationToken);

        // SkipTakeAsync
        public static Task<List<T>> SkipTakeAsync<T>(this IAsyncQueryDatabase db, long skip, long take, string sql, object arg0, CancellationToken cancellationToken = default)
            => db.SkipTakeAsync<T>(skip, take, new Sql(sql, arg0), cancellationToken);
        public static Task<List<T>> SkipTakeAsync<T>(this IAsyncQueryDatabase db, long skip, long take, string sql, object arg0, object arg1, CancellationToken cancellationToken = default)
            => db.SkipTakeAsync<T>(skip, take, new Sql(sql, arg0, arg1), cancellationToken);
        public static Task<List<T>> SkipTakeAsync<T>(this IAsyncQueryDatabase db, long skip, long take, string sql, object arg0, object arg1, object arg2, CancellationToken cancellationToken = default)
            => db.SkipTakeAsync<T>(skip, take, new Sql(sql, arg0, arg1, arg2), cancellationToken);
        public static Task<List<T>> SkipTakeAsync<T>(this IAsyncQueryDatabase db, long skip, long take, string sql, object arg0, object arg1, object arg2, object arg3, CancellationToken cancellationToken = default)
            => db.SkipTakeAsync<T>(skip, take, new Sql(sql, arg0, arg1, arg2, arg3), cancellationToken);
        public static Task<List<T>> SkipTakeAsync<T>(this IAsyncQueryDatabase db, long skip, long take, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, CancellationToken cancellationToken = default)
            => db.SkipTakeAsync<T>(skip, take, new Sql(sql, arg0, arg1, arg2, arg3, arg4), cancellationToken);
        public static Task<List<T>> SkipTakeAsync<T>(this IAsyncQueryDatabase db, long skip, long take, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, CancellationToken cancellationToken = default)
            => db.SkipTakeAsync<T>(skip, take, new Sql(sql, arg0, arg1, arg2, arg3, arg4, arg5), cancellationToken);
        public static Task<List<T>> SkipTakeAsync<T>(this IAsyncQueryDatabase db, long skip, long take, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, CancellationToken cancellationToken = default)
            => db.SkipTakeAsync<T>(skip, take, new Sql(sql, arg0, arg1, arg2, arg3, arg4, arg5, arg6), cancellationToken);
        public static Task<List<T>> SkipTakeAsync<T>(this IAsyncQueryDatabase db, long skip, long take, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, CancellationToken cancellationToken = default)
            => db.SkipTakeAsync<T>(skip, take, new Sql(sql, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7), cancellationToken);

        // FetchMultipleAsync<T1, T2, TRet>
        public static Task<TRet> FetchMultipleAsync<T1, T2, TRet>(this IAsyncQueryDatabase db, Func<List<T1>, List<T2>, TRet> cb, string sql, object arg0, CancellationToken cancellationToken = default)
            => db.FetchMultipleAsync<T1, T2, TRet>(cb, new Sql(sql, arg0), cancellationToken);
        public static Task<TRet> FetchMultipleAsync<T1, T2, TRet>(this IAsyncQueryDatabase db, Func<List<T1>, List<T2>, TRet> cb, string sql, object arg0, object arg1, CancellationToken cancellationToken = default)
            => db.FetchMultipleAsync<T1, T2, TRet>(cb, new Sql(sql, arg0, arg1), cancellationToken);
        public static Task<TRet> FetchMultipleAsync<T1, T2, TRet>(this IAsyncQueryDatabase db, Func<List<T1>, List<T2>, TRet> cb, string sql, object arg0, object arg1, object arg2, CancellationToken cancellationToken = default)
            => db.FetchMultipleAsync<T1, T2, TRet>(cb, new Sql(sql, arg0, arg1, arg2), cancellationToken);
        public static Task<TRet> FetchMultipleAsync<T1, T2, TRet>(this IAsyncQueryDatabase db, Func<List<T1>, List<T2>, TRet> cb, string sql, object arg0, object arg1, object arg2, object arg3, CancellationToken cancellationToken = default)
            => db.FetchMultipleAsync<T1, T2, TRet>(cb, new Sql(sql, arg0, arg1, arg2, arg3), cancellationToken);
        public static Task<TRet> FetchMultipleAsync<T1, T2, TRet>(this IAsyncQueryDatabase db, Func<List<T1>, List<T2>, TRet> cb, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, CancellationToken cancellationToken = default)
            => db.FetchMultipleAsync<T1, T2, TRet>(cb, new Sql(sql, arg0, arg1, arg2, arg3, arg4), cancellationToken);
        public static Task<TRet> FetchMultipleAsync<T1, T2, TRet>(this IAsyncQueryDatabase db, Func<List<T1>, List<T2>, TRet> cb, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, CancellationToken cancellationToken = default)
            => db.FetchMultipleAsync<T1, T2, TRet>(cb, new Sql(sql, arg0, arg1, arg2, arg3, arg4, arg5), cancellationToken);
        public static Task<TRet> FetchMultipleAsync<T1, T2, TRet>(this IAsyncQueryDatabase db, Func<List<T1>, List<T2>, TRet> cb, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, CancellationToken cancellationToken = default)
            => db.FetchMultipleAsync<T1, T2, TRet>(cb, new Sql(sql, arg0, arg1, arg2, arg3, arg4, arg5, arg6), cancellationToken);
        public static Task<TRet> FetchMultipleAsync<T1, T2, TRet>(this IAsyncQueryDatabase db, Func<List<T1>, List<T2>, TRet> cb, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, CancellationToken cancellationToken = default)
            => db.FetchMultipleAsync<T1, T2, TRet>(cb, new Sql(sql, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7), cancellationToken);

        // FetchMultipleAsync<T1, T2, T3, TRet>
        public static Task<TRet> FetchMultipleAsync<T1, T2, T3, TRet>(this IAsyncQueryDatabase db, Func<List<T1>, List<T2>, List<T3>, TRet> cb, string sql, object arg0, CancellationToken cancellationToken = default)
            => db.FetchMultipleAsync<T1, T2, T3, TRet>(cb, new Sql(sql, arg0), cancellationToken);
        public static Task<TRet> FetchMultipleAsync<T1, T2, T3, TRet>(this IAsyncQueryDatabase db, Func<List<T1>, List<T2>, List<T3>, TRet> cb, string sql, object arg0, object arg1, CancellationToken cancellationToken = default)
            => db.FetchMultipleAsync<T1, T2, T3, TRet>(cb, new Sql(sql, arg0, arg1), cancellationToken);
        public static Task<TRet> FetchMultipleAsync<T1, T2, T3, TRet>(this IAsyncQueryDatabase db, Func<List<T1>, List<T2>, List<T3>, TRet> cb, string sql, object arg0, object arg1, object arg2, CancellationToken cancellationToken = default)
            => db.FetchMultipleAsync<T1, T2, T3, TRet>(cb, new Sql(sql, arg0, arg1, arg2), cancellationToken);
        public static Task<TRet> FetchMultipleAsync<T1, T2, T3, TRet>(this IAsyncQueryDatabase db, Func<List<T1>, List<T2>, List<T3>, TRet> cb, string sql, object arg0, object arg1, object arg2, object arg3, CancellationToken cancellationToken = default)
            => db.FetchMultipleAsync<T1, T2, T3, TRet>(cb, new Sql(sql, arg0, arg1, arg2, arg3), cancellationToken);
        public static Task<TRet> FetchMultipleAsync<T1, T2, T3, TRet>(this IAsyncQueryDatabase db, Func<List<T1>, List<T2>, List<T3>, TRet> cb, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, CancellationToken cancellationToken = default)
            => db.FetchMultipleAsync<T1, T2, T3, TRet>(cb, new Sql(sql, arg0, arg1, arg2, arg3, arg4), cancellationToken);
        public static Task<TRet> FetchMultipleAsync<T1, T2, T3, TRet>(this IAsyncQueryDatabase db, Func<List<T1>, List<T2>, List<T3>, TRet> cb, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, CancellationToken cancellationToken = default)
            => db.FetchMultipleAsync<T1, T2, T3, TRet>(cb, new Sql(sql, arg0, arg1, arg2, arg3, arg4, arg5), cancellationToken);
        public static Task<TRet> FetchMultipleAsync<T1, T2, T3, TRet>(this IAsyncQueryDatabase db, Func<List<T1>, List<T2>, List<T3>, TRet> cb, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, CancellationToken cancellationToken = default)
            => db.FetchMultipleAsync<T1, T2, T3, TRet>(cb, new Sql(sql, arg0, arg1, arg2, arg3, arg4, arg5, arg6), cancellationToken);
        public static Task<TRet> FetchMultipleAsync<T1, T2, T3, TRet>(this IAsyncQueryDatabase db, Func<List<T1>, List<T2>, List<T3>, TRet> cb, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, CancellationToken cancellationToken = default)
            => db.FetchMultipleAsync<T1, T2, T3, TRet>(cb, new Sql(sql, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7), cancellationToken);

        // FetchMultipleAsync<T1, T2, T3, T4, TRet>
        public static Task<TRet> FetchMultipleAsync<T1, T2, T3, T4, TRet>(this IAsyncQueryDatabase db, Func<List<T1>, List<T2>, List<T3>, List<T4>, TRet> cb, string sql, object arg0, CancellationToken cancellationToken = default)
            => db.FetchMultipleAsync<T1, T2, T3, T4, TRet>(cb, new Sql(sql, arg0), cancellationToken);
        public static Task<TRet> FetchMultipleAsync<T1, T2, T3, T4, TRet>(this IAsyncQueryDatabase db, Func<List<T1>, List<T2>, List<T3>, List<T4>, TRet> cb, string sql, object arg0, object arg1, CancellationToken cancellationToken = default)
            => db.FetchMultipleAsync<T1, T2, T3, T4, TRet>(cb, new Sql(sql, arg0, arg1), cancellationToken);
        public static Task<TRet> FetchMultipleAsync<T1, T2, T3, T4, TRet>(this IAsyncQueryDatabase db, Func<List<T1>, List<T2>, List<T3>, List<T4>, TRet> cb, string sql, object arg0, object arg1, object arg2, CancellationToken cancellationToken = default)
            => db.FetchMultipleAsync<T1, T2, T3, T4, TRet>(cb, new Sql(sql, arg0, arg1, arg2), cancellationToken);
        public static Task<TRet> FetchMultipleAsync<T1, T2, T3, T4, TRet>(this IAsyncQueryDatabase db, Func<List<T1>, List<T2>, List<T3>, List<T4>, TRet> cb, string sql, object arg0, object arg1, object arg2, object arg3, CancellationToken cancellationToken = default)
            => db.FetchMultipleAsync<T1, T2, T3, T4, TRet>(cb, new Sql(sql, arg0, arg1, arg2, arg3), cancellationToken);
        public static Task<TRet> FetchMultipleAsync<T1, T2, T3, T4, TRet>(this IAsyncQueryDatabase db, Func<List<T1>, List<T2>, List<T3>, List<T4>, TRet> cb, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, CancellationToken cancellationToken = default)
            => db.FetchMultipleAsync<T1, T2, T3, T4, TRet>(cb, new Sql(sql, arg0, arg1, arg2, arg3, arg4), cancellationToken);
        public static Task<TRet> FetchMultipleAsync<T1, T2, T3, T4, TRet>(this IAsyncQueryDatabase db, Func<List<T1>, List<T2>, List<T3>, List<T4>, TRet> cb, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, CancellationToken cancellationToken = default)
            => db.FetchMultipleAsync<T1, T2, T3, T4, TRet>(cb, new Sql(sql, arg0, arg1, arg2, arg3, arg4, arg5), cancellationToken);
        public static Task<TRet> FetchMultipleAsync<T1, T2, T3, T4, TRet>(this IAsyncQueryDatabase db, Func<List<T1>, List<T2>, List<T3>, List<T4>, TRet> cb, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, CancellationToken cancellationToken = default)
            => db.FetchMultipleAsync<T1, T2, T3, T4, TRet>(cb, new Sql(sql, arg0, arg1, arg2, arg3, arg4, arg5, arg6), cancellationToken);
        public static Task<TRet> FetchMultipleAsync<T1, T2, T3, T4, TRet>(this IAsyncQueryDatabase db, Func<List<T1>, List<T2>, List<T3>, List<T4>, TRet> cb, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, CancellationToken cancellationToken = default)
            => db.FetchMultipleAsync<T1, T2, T3, T4, TRet>(cb, new Sql(sql, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7), cancellationToken);


        // FetchMultipleAsync<T1, T2>
        public static Task<(List<T1>, List<T2>)> FetchMultipleAsync<T1, T2>(this IAsyncQueryDatabase db, string sql, object arg0, CancellationToken cancellationToken = default)
            => db.FetchMultipleAsync<T1, T2>(new Sql(sql, arg0), cancellationToken);
        public static Task<(List<T1>, List<T2>)> FetchMultipleAsync<T1, T2>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, CancellationToken cancellationToken = default)
            => db.FetchMultipleAsync<T1, T2>(new Sql(sql, arg0, arg1), cancellationToken);
        public static Task<(List<T1>, List<T2>)> FetchMultipleAsync<T1, T2>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, object arg2, CancellationToken cancellationToken = default)
            => db.FetchMultipleAsync<T1, T2>(new Sql(sql, arg0, arg1, arg2), cancellationToken);
        public static Task<(List<T1>, List<T2>)> FetchMultipleAsync<T1, T2>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, object arg2, object arg3, CancellationToken cancellationToken = default)
            => db.FetchMultipleAsync<T1, T2>(new Sql(sql, arg0, arg1, arg2, arg3), cancellationToken);
        public static Task<(List<T1>, List<T2>)> FetchMultipleAsync<T1, T2>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, CancellationToken cancellationToken = default)
            => db.FetchMultipleAsync<T1, T2>(new Sql(sql, arg0, arg1, arg2, arg3, arg4), cancellationToken);
        public static Task<(List<T1>, List<T2>)> FetchMultipleAsync<T1, T2>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, CancellationToken cancellationToken = default)
            => db.FetchMultipleAsync<T1, T2>(new Sql(sql, arg0, arg1, arg2, arg3, arg4, arg5), cancellationToken);
        public static Task<(List<T1>, List<T2>)> FetchMultipleAsync<T1, T2>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, CancellationToken cancellationToken = default)
            => db.FetchMultipleAsync<T1, T2>(new Sql(sql, arg0, arg1, arg2, arg3, arg4, arg5, arg6), cancellationToken);
        public static Task<(List<T1>, List<T2>)> FetchMultipleAsync<T1, T2>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, CancellationToken cancellationToken = default)
            => db.FetchMultipleAsync<T1, T2>(new Sql(sql, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7), cancellationToken);

        // FetchMultipleAsync<T1, T2, T3>
        public static Task<(List<T1>, List<T2>, List<T3>)> FetchMultipleAsync<T1, T2, T3>(this IAsyncQueryDatabase db, string sql, object arg0, CancellationToken cancellationToken = default)
            => db.FetchMultipleAsync<T1, T2, T3>(new Sql(sql, arg0), cancellationToken);
        public static Task<(List<T1>, List<T2>, List<T3>)> FetchMultipleAsync<T1, T2, T3>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, CancellationToken cancellationToken = default)
            => db.FetchMultipleAsync<T1, T2, T3>(new Sql(sql, arg0, arg1), cancellationToken);
        public static Task<(List<T1>, List<T2>, List<T3>)> FetchMultipleAsync<T1, T2, T3>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, object arg2, CancellationToken cancellationToken = default)
            => db.FetchMultipleAsync<T1, T2, T3>(new Sql(sql, arg0, arg1, arg2), cancellationToken);
        public static Task<(List<T1>, List<T2>, List<T3>)> FetchMultipleAsync<T1, T2, T3>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, object arg2, object arg3, CancellationToken cancellationToken = default)
            => db.FetchMultipleAsync<T1, T2, T3>(new Sql(sql, arg0, arg1, arg2, arg3), cancellationToken);
        public static Task<(List<T1>, List<T2>, List<T3>)> FetchMultipleAsync<T1, T2, T3>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, CancellationToken cancellationToken = default)
            => db.FetchMultipleAsync<T1, T2, T3>(new Sql(sql, arg0, arg1, arg2, arg3, arg4), cancellationToken);
        public static Task<(List<T1>, List<T2>, List<T3>)> FetchMultipleAsync<T1, T2, T3>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, CancellationToken cancellationToken = default)
            => db.FetchMultipleAsync<T1, T2, T3>(new Sql(sql, arg0, arg1, arg2, arg3, arg4, arg5), cancellationToken);
        public static Task<(List<T1>, List<T2>, List<T3>)> FetchMultipleAsync<T1, T2, T3>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, CancellationToken cancellationToken = default)
            => db.FetchMultipleAsync<T1, T2, T3>(new Sql(sql, arg0, arg1, arg2, arg3, arg4, arg5, arg6), cancellationToken);
        public static Task<(List<T1>, List<T2>, List<T3>)> FetchMultipleAsync<T1, T2, T3>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, CancellationToken cancellationToken = default)
            => db.FetchMultipleAsync<T1, T2, T3>(new Sql(sql, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7), cancellationToken);

        // FetchMultipleAsync<T1, T2, T3, T4>
        public static Task<(List<T1>, List<T2>, List<T3>, List<T4>)> FetchMultipleAsync<T1, T2, T3, T4>(this IAsyncQueryDatabase db, string sql, object arg0, CancellationToken cancellationToken = default)
            => db.FetchMultipleAsync<T1, T2, T3, T4>(new Sql(sql, arg0), cancellationToken);
        public static Task<(List<T1>, List<T2>, List<T3>, List<T4>)> FetchMultipleAsync<T1, T2, T3, T4>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, CancellationToken cancellationToken = default)
            => db.FetchMultipleAsync<T1, T2, T3, T4>(new Sql(sql, arg0, arg1), cancellationToken);
        public static Task<(List<T1>, List<T2>, List<T3>, List<T4>)> FetchMultipleAsync<T1, T2, T3, T4>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, object arg2, CancellationToken cancellationToken = default)
            => db.FetchMultipleAsync<T1, T2, T3, T4>(new Sql(sql, arg0, arg1, arg2), cancellationToken);
        public static Task<(List<T1>, List<T2>, List<T3>, List<T4>)> FetchMultipleAsync<T1, T2, T3, T4>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, object arg2, object arg3, CancellationToken cancellationToken = default)
            => db.FetchMultipleAsync<T1, T2, T3, T4>(new Sql(sql, arg0, arg1, arg2, arg3), cancellationToken);
        public static Task<(List<T1>, List<T2>, List<T3>, List<T4>)> FetchMultipleAsync<T1, T2, T3, T4>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, CancellationToken cancellationToken = default)
            => db.FetchMultipleAsync<T1, T2, T3, T4>(new Sql(sql, arg0, arg1, arg2, arg3, arg4), cancellationToken);
        public static Task<(List<T1>, List<T2>, List<T3>, List<T4>)> FetchMultipleAsync<T1, T2, T3, T4>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, CancellationToken cancellationToken = default)
            => db.FetchMultipleAsync<T1, T2, T3, T4>(new Sql(sql, arg0, arg1, arg2, arg3, arg4, arg5), cancellationToken);
        public static Task<(List<T1>, List<T2>, List<T3>, List<T4>)> FetchMultipleAsync<T1, T2, T3, T4>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, CancellationToken cancellationToken = default)
            => db.FetchMultipleAsync<T1, T2, T3, T4>(new Sql(sql, arg0, arg1, arg2, arg3, arg4, arg5, arg6), cancellationToken);
        public static Task<(List<T1>, List<T2>, List<T3>, List<T4>)> FetchMultipleAsync<T1, T2, T3, T4>(this IAsyncQueryDatabase db, string sql, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, CancellationToken cancellationToken = default)
            => db.FetchMultipleAsync<T1, T2, T3, T4>(new Sql(sql, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7), cancellationToken);


    }
}
