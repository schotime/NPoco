#nullable enable
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace NPoco
{
    public interface IBaseCommonDatabase : IDatabaseConfig
    {
        /// <summary>
        /// The underlying connection object
        /// </summary>
        DbConnection Connection { get; }

        /// <summary>
        /// The underlying transaction object
        /// </summary>        
        DbTransaction? Transaction { get; }

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
        /// A data bag to store whatever you like per IDatabase instance
        /// </summary>        
        IDictionary<string, object> Data { get; }

        /// <summary>
        /// Sets command timeout for the lifetime of the Database instance
        /// </summary>
        public int CommandTimeout { get; set; }

        /// <summary>
        /// Sets command timeout only for the next command, is reverted after
        /// </summary>
        public int OneTimeCommandTimeout { get; set; }
    }
}