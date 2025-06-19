using System;
using System.Data;
using System.Threading.Tasks;

namespace NPoco
{
    public class AsyncTransaction : IAsyncTransaction
    {
        IDatabase _db;
        readonly IsolationLevel _isolationLevel;

        public AsyncTransaction(IDatabase db, IsolationLevel isolationLevel)
        {
            _db = db;
            _isolationLevel = isolationLevel;
        }

        public Task BeginAsync()
        {
            return _db.BeginTransactionAsync(_isolationLevel);
        }

        public async Task CompleteAsync()
        {
            await _db.CompleteTransactionAsync();
            _db = null;
        }

        public async ValueTask DisposeAsync()
        {
            if (_db != null)
            {
                await _db.AbortTransactionAsync();
            }
        }
    }

    public interface IAsyncTransaction : IAsyncDisposable
    {
        Task CompleteAsync();
    }
}
