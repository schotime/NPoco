using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace NPoco
{
    public interface IDatabaseHelpers
    {
        int ExecuteNonQueryHelper(DbCommand cmd);
        object ExecuteScalarHelper(DbCommand cmd);
        DbDataReader ExecuteReaderHelper(DbCommand cmd);
        Task<int> ExecuteNonQueryHelperAsync(DbCommand cmd, CancellationToken cancellationToken = default);
        Task<object> ExecuteScalarHelperAsync(DbCommand cmd, CancellationToken cancellationToken = default);
        Task<DbDataReader> ExecuteReaderHelperAsync(DbCommand cmd, CancellationToken cancellationToken = default);
    }
}