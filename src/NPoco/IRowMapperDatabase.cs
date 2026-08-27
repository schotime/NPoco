using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NPoco.RowMappers;

namespace NPoco
{
    /// <summary>
    /// Runs a query using a caller supplied <see cref="IRowMapper"/> rather than the one
    /// <see cref="MappingFactory"/> would select for the result type.
    ///
    /// This exists for result shapes NPoco cannot describe with a single <see cref="PocoData"/>,
    /// such as a projection spanning several tables. Connection, transaction, interceptor,
    /// command timeout and exception semantics are identical to the standard query path, so
    /// callers never need to drive the reader themselves.
    ///
    /// The supplied SQL is used as given; no select clause is generated for it. A leading
    /// semicolon is stripped, which allows the statement to start with a CTE.
    ///
    /// No <see cref="PocoData"/> is built for the result type - the row mapper decides how to
    /// construct it - so the result may be a shape NPoco could not otherwise map, and the
    /// <c>pocoData</c> passed to <see cref="IRowMapper.Init"/> is null.
    /// </summary>
    public interface IRowMapperDatabase
    {
        IEnumerable<T> Query<T>(Sql sql, IRowMapper rowMapper);
        List<T> Fetch<T>(Sql sql, IRowMapper rowMapper);
        IAsyncEnumerable<T> QueryAsync<T>(Sql sql, IRowMapper rowMapper, CancellationToken cancellationToken = default);
        Task<List<T>> FetchAsync<T>(Sql sql, IRowMapper rowMapper, CancellationToken cancellationToken = default);
    }
}
