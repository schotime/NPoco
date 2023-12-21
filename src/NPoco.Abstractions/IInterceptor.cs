using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace NPoco
{
    public interface IInterceptor
    {
    }

    public interface IExecutingInterceptor : IInterceptor
    {
        void OnExecutingCommand(IDatabase database, DbCommand cmd);
        void OnExecutedCommand(IDatabase database, DbCommand cmd);
    }

    public interface IConnectionInterceptor : IInterceptor
    {
        DbConnection OnConnectionOpened(IDatabase database, DbConnection conn);
        void OnConnectionClosing(IDatabase database, DbConnection conn);
    }

    public interface IExceptionInterceptor : IInterceptor
    {
        void OnException(IDatabase database, Exception exception);
    }

    public interface IDataInterceptor : IInterceptor
    {
        bool OnInserting(IDatabase database, InsertContext insertContext);
        bool OnUpdating(IDatabase database, UpdateContext updateContext);
        bool OnDeleting(IDatabase database, DeleteContext deleteContext);
    }

    public interface ITransactionInterceptor : IInterceptor
    {
        void OnBeginTransaction(IDatabase database);
        void OnAbortTransaction(IDatabase database);
        void OnCompleteTransaction(IDatabase database);
    }
}
