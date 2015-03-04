using System;
using System.Data;

namespace NPoco
{
    public class Transaction : ITransaction
    {
        IDatabase _db;

        public Transaction(IDatabase db, IsolationLevel isolationLevel)
        {
            _db = db;
            _db.BeginTransaction(isolationLevel);
        }

        public virtual void Complete()
        {
            _db.CompleteTransaction();
            _db = null;
        }

        public void Dispose()
        {
            if (_db != null)
            {
                _db.AbortTransaction();
            }
        }
    }

    public interface ITransaction : IDisposable
    {
        void Complete();
    }
}