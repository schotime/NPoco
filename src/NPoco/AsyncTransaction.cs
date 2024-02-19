using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace NPoco
{
    public class AsyncTransaction : IAsyncTransaction
    {
        IAsyncDatabase _db;

        private AsyncTransaction(IAsyncDatabase db)
        {
            _db = db;
        }

#pragma warning disable CS1998
        public static async Task<IAsyncTransaction> Init(IAsyncDatabase db, IsolationLevel isolationLevel)
        {
#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
            await db.BeginTransactionAsync(isolationLevel);
#else
            ((IBaseDatabase)db).BeginTransaction();
#endif
      
            return new AsyncTransaction(db);
        }

        public async Task CompleteAsync(CancellationToken cancellationToken = default)
        {

#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
            await _db.CompleteTransactionAsync(cancellationToken);
#else
            ((IBaseDatabase)_db).CompleteTransaction();
#endif
            _db = null;
        }

        public async ValueTask DisposeAsync()
        {
            if (_db != null)
            {
#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
                await _db.AbortTransactionAsync();
#else
                ((IBaseDatabase)_db).AbortTransaction();
#endif
            }
        }
#pragma warning restore CS1998

        public void Dispose()
        {
            ((IBaseDatabase)_db)?.AbortTransaction();
        }
    }
}