using Microsoft.Data.SqlClient;
using System;
using System.Threading.Tasks;
using NPoco.DatabaseTypes;
using System.Threading;

namespace NPoco.SqlServer
{
    public class SqlServerDatabase : Database
    {
        private readonly IPollyPolicy? _pollyPolicy;

        public SqlServerDatabase(string connectionString, IPollyPolicy? pollyPolicy = null) 
            : this(connectionString, Singleton<SqlServer2012DatabaseType>.Instance, pollyPolicy)
        {
        }

        public SqlServerDatabase(string connectionString, SqlServerDatabaseType databaseType, IPollyPolicy? pollyPolicy) 
            : base(connectionString, databaseType, SqlClientFactory.Instance)
        {
            _pollyPolicy = pollyPolicy;
        }

        protected override T ExecutionHook<T>(Func<T> action)
        {
            if (_pollyPolicy?.RetryPolicy != null)
            {
                return _pollyPolicy.RetryPolicy.Execute(action);
            }

            return base.ExecutionHook(action);
        }

        protected override async Task<T> ExecutionHookAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default)
        {
            if (_pollyPolicy?.AsyncRetryPolicy != null)
            {
                return await _pollyPolicy.AsyncRetryPolicy.ExecuteAsync(() => action(cancellationToken)).ConfigureAwait(false);
            }

            return await base.ExecutionHookAsync(action).ConfigureAwait(false);
        }        
    }
}
