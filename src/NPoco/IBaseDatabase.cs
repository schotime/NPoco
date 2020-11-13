using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace NPoco
{
    public interface IBaseDatabase : IDisposable
    {
        /// <summary>
        /// The underlying connection object
        /// </summary>
        DbConnection Connection { get; }

        /// <summary>
        /// The underlying transaction object
        /// </summary>        
        DbTransaction Transaction { get; }

        /// <summary>
        /// Creates a DbParameter for the specific database provider
        /// </summary>        
        DbParameter CreateParameter();

        /// <summary>
        /// Adds a parameter to the DbCommand specified
        /// </summary>        
        void AddParameter(DbCommand cmd, object value);

        /// <summary>
        /// Creates a command given a connection, command type and sql
        /// </summary>        
        DbCommand CreateCommand(DbConnection connection, CommandType commandType, string sql, params object[] args);

        /// <summary>
        /// Begins a new transaction and returns ITransaction which can be used in a using statement
        /// </summary>        
        ITransaction GetTransaction();

        /// <summary>
        /// Begins a new transaction and returns ITransaction which can be used in a using statement
        /// </summary>        
        ITransaction GetTransaction(IsolationLevel isolationLevel);

        /// <summary>
        /// A data bag to store whatever you like per IDatabase instance
        /// </summary>        
        IDictionary<string, object> Data { get; }

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
        IDatabase OpenSharedConnection();

        /// <summary>
        /// Closes the DBConnection manually
        /// </summary>
        void CloseSharedConnection();
    }
}