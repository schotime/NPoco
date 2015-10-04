using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace NPoco
{
    public interface IInterceptor
    {
    }

    public interface IExecutingInterceptor : IInterceptor
    {
        void OnExecutingCommand(IDatabase database, IDbCommand cmd);
        void OnExecutedCommand(IDatabase database, IDbCommand cmd);
    }

    public interface IConnectionInterceptor : IInterceptor
    {
        IDbConnection OnConnectionOpened(IDatabase database, IDbConnection conn);
        void OnConnectionClosing(IDatabase database, IDbConnection conn);
    }

    public interface IExceptionInterceptor : IInterceptor
    {
        void OnException(IDatabase database, Exception x);
    }

    public interface IDataInterceptor : IInterceptor
    {
        bool OnInserting(IDatabase database, InsertContext insertContext);
        bool OnUpdating(IDatabase database, UpdateContext updateContext);
        bool OnDeleting(IDatabase database, DeleteContext deleteContext);
    }
}
