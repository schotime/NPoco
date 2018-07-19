using System.Data.Common;
#if !NET35 
using System.Threading.Tasks;
#endif

namespace NPoco
{
    public interface IDatabaseHelpers
    {
        int ExecuteNonQueryHelper(DbCommand cmd);
        object ExecuteScalarHelper(DbCommand cmd);
        DbDataReader ExecuteReaderHelper(DbCommand cmd);
#if !NET35 && !NET40
        Task<int> ExecuteNonQueryHelperAsync(DbCommand cmd);
        Task<object> ExecuteScalarHelperAsync(DbCommand cmd);
        Task<DbDataReader> ExecuteReaderHelperAsync(DbCommand cmd);
#endif
    }
}