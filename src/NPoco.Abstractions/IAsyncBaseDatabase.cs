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

        /// <summary>
        /// Begins a new transaction and returns ITransaction which can be used in a using statement
        /// </summary>        
        Task<IAsyncTransaction> GetTransactionAsync();

        /// <summary>
        /// Begins a new transaction and returns ITransaction which can be used in a using statement
        /// </summary>        
        Task<IAsyncTransaction> GetTransactionAsync(IsolationLevel isolationLevel);

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
    }
}