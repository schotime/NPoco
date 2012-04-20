using System;
using System.Data;

namespace NPoco
{
    public class Transaction : IDisposable
    {
        public Transaction(Database db) : this(db, null) { }

        public Transaction(Database db, IsolationLevel? isolationLevel)
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
                _db.AbortTransaction();
        }

        Database _db;
    }
}