using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace NPoco
{
    public interface IAsyncBaseDatabase : IBaseDatabase
    {
        /// <summary>
        /// Opens the DbConnection manually
        /// </summary>
        Task<IAsyncDatabase> OpenSharedConnectionAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Closes the DBConnection manually
        /// </summary>
        Task CloseSharedConnectionAsync();

#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
        /// <summary>
        /// Manually begin a transaction. Recommended to use GetTransaction
        /// </summary>        
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Manually begin a transaction. Recommended to use GetTransaction
        /// </summary>        
        Task BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default);

        /// <summary>
        /// Manually abort/rollback a transaction
        /// </summary>        
        Task AbortTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Manually commit a transaction
        /// </summary>        
        Task CompleteTransactionAsync(CancellationToken cancellationToken = default);
#endif
    }
}