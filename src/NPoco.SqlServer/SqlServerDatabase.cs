using Microsoft.Data.SqlClient;
using NPoco.SqlServer;
using System;
using System.Threading.Tasks;
using System.Text;
using NPoco.DatabaseTypes;
using System.Data;

namespace NPoco.SqlServer
{
    public class SqlServerDatabase : Database
    {
        private readonly IPollyPolicy _pollyPolicy;

        public SqlServerDatabase(string connectionString, IPollyPolicy pollyPolicy = null)
            : this(connectionString, Singleton<SqlServer2012DatabaseType>.Instance, pollyPolicy)
        {
        }

        public SqlServerDatabase(string connectionString, SqlServerDatabaseType databaseType, IPollyPolicy pollyPolicy)
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

        protected override async Task<T> ExecutionHookAsync<T>(Func<Task<T>> action)
        {
            if (_pollyPolicy?.AsyncRetryPolicy != null)
            {
                return await _pollyPolicy.AsyncRetryPolicy.ExecuteAsync(action).ConfigureAwait(false);
            }

            return await base.ExecutionHookAsync(action).ConfigureAwait(false);
        }

        public override string FormatCommand(string sql, object[] args)
        {
            if (sql == null)
                return "";
            var sb = new StringBuilder();
            if (args != null && args.Length > 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    var value = args[i];
                    var formatted = args[i] as FormattedParameter;
                    if (formatted != null)
                    {
                        value = formatted.Value;
                    }

                    var p = new Microsoft.Data.SqlClient.SqlParameter();
                    SetParameterValue(p, args[i]);
                    if (p.Size == 0 || p.SqlDbType == SqlDbType.UniqueIdentifier)
                    {
                        if (value == null && (p.SqlDbType == SqlDbType.NVarChar || p.SqlDbType == SqlDbType.VarChar))
                        {
                            sb.AppendFormat("DECLARE {0}{1} {2} = null\n", _paramPrefix, i, p.SqlDbType);
                        }
                        else
                        {
                            sb.AppendFormat("DECLARE {0}{1} {2} = '{3}'\n", _paramPrefix, i, p.SqlDbType, value);
                        }
                    }
                    else
                    {
                        sb.AppendFormat("DECLARE {0}{1} {2}[{3}] = '{4}'\n", _paramPrefix, i, p.SqlDbType, p.Size, value);
                    }
                }
            }
            sb.AppendFormat("\n{0}", sql);
            return sb.ToString();
        }
    }
}
