using System;
using System.Data;

namespace NPoco
{
    public class Transaction : IDisposable
    {
        public Transaction(Database db, IsolationLevel isolationLevel)
        {
            _db = db;
            _db.BeginTransaction(isolationLevel);
            if (_db.BaseTransaction == null)
            {
                _db.BaseTransaction = this;
            }
        }

        public virtual void Complete()
        {
            if (_db.BaseTransaction == this)
            {
                _db.BaseTransaction = null;
                _db.CompleteTransaction();
                _db = null;
            }
        }

        public void Dispose()
        {
            if (_db != null && _db.BaseTransaction == this)
            {
                _db.BaseTransaction = null;
                _db.AbortTransaction();
            }
        }

        Database _db;
    }
}