#nullable enable
using System;
using System.Data;
using System.Data.Common;

namespace NPoco
{
    public interface IBaseDatabase : IAsyncBaseDatabase, IDisposable
    {
        /// <summary>
        /// Begins a new transaction and returns ITransaction which can be used in a using statement
        /// </summary>        
        ITransaction GetTransaction();

        /// <summary>
        /// Begins a new transaction and returns ITransaction which can be used in a using statement
        /// </summary>        
        ITransaction GetTransaction(IsolationLevel isolationLevel);

        /// <summary>
        /// A way to set the transaction from an existing DbTransaction
        /// </summary>        
        void SetTransaction(DbTransaction tran);

        /// <summary>
        /// Manually begin a transaction. Recommended to use GetTransaction
        /// </summary>        
        void BeginTransaction();

        /// <summary>
        /// Manually begin a transaction. Recommended to use GetTransaction
        /// </summary>        
        void BeginTransaction(IsolationLevel isolationLevel);

        /// <summary>
        /// Manually abort/rollback a transaction
        /// </summary>        
        void AbortTransaction();

        /// <summary>
        /// Manually commit a transaction
        /// </summary>        
        void CompleteTransaction();

        /// <summary>
        /// Opens the DbConnection manually
        /// </summary>
        IDatabase OpenSharedConnection(OpenConnectionOptions? options = null);

        /// <summary>
        /// Closes the DBConnection manually
        /// </summary>
        void CloseSharedConnection();
    }
}