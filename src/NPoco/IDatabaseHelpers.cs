using System.Data.Common;
using System.Threading.Tasks;

namespace NPoco
{
    public interface IDatabaseHelpers
    {
        int ExecuteNonQueryHelper(DbCommand cmd);
        object ExecuteScalarHelper(DbCommand cmd);
        DbDataReader ExecuteReaderHelper(DbCommand cmd);
        Task<int> ExecuteNonQueryHelperAsync(DbCommand cmd);
        Task<object> ExecuteScalarHelperAsync(DbCommand cmd);
        Task<DbDataReader> ExecuteReaderHelperAsync(DbCommand cmd);
    }
}