using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using NPoco.Linq;

namespace NPoco
{
    public interface IDatabase : IDisposable, IDatabaseQuery, IDatabaseConfig
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
        /// Insert POCO into the table, primary key and autoincrement specified
        /// </summary>         
        object Insert<T>(string tableName, string primaryKeyName, bool autoIncrement, T poco);
        /// <summary>
        /// Insert POCO into the table, primary key specified
        /// </summary>    
        object Insert<T>(string tableName, string primaryKeyName, T poco);
        /// <summary>
        /// Insert POCO into the table by convention or configuration
        /// </summary>   
        object Insert<T>(T poco);
              
#if !NET35 && !NET40
        /// <summary>
        /// Insert POCO into the table by convention or configuration
        /// </summary>        
        System.Threading.Tasks.Task<object> InsertAsync<T>(T poco);
        /// <summary>
        /// Update POCO in the table by convention or configuration
        /// </summary>        
        System.Threading.Tasks.Task<int> UpdateAsync(object poco);
        /// <summary>
        /// Update POCO in the table by convention or configuration specifying which columns to update
        /// </summary>        
        System.Threading.Tasks.Task<int> UpdateAsync(object poco, IEnumerable<string> columns);
        /// <summary>
        /// Update POCO in the table by convention or configuration specifying which columns to update
        /// </summary>  
        System.Threading.Tasks.Task<int> UpdateAsync<T>(T poco, Expression<Func<T, object>> fields);
        /// <summary>
        /// Delete POCO from table by convention or configuration
        /// </summary>        
        System.Threading.Tasks.Task<int> DeleteAsync(object poco);
#endif
        /// <summary>
        /// Insert POCO's into database using SqlBulkCopy for SqlServer (other DB's currently fall back to looping each row)
        /// </summary>        
        void InsertBulk<T>(IEnumerable<T> pocos);
        /// <summary>
        /// Insert POCO's into database using by concatenating sql using the provided batch options
        /// </summary>        
        void InsertBatch<T>(IEnumerable<T> pocos, BatchOptions options = null);
        /// <summary>
        /// Update POCO in the specified table, primary key and primarkey value
        /// </summary>        
        int Update(string tableName, string primaryKeyName, object poco, object primaryKeyValue);
        /// <summary>
        /// Update POCO in the specified table, primary key
        /// </summary>        
        int Update(string tableName, string primaryKeyName, object poco);
        /// <summary>
        /// Update POCO in the specified table, primary key, primarkey value for only the columns specified
        /// </summary>        
        int Update(string tableName, string primaryKeyName, object poco, object primaryKeyValue, IEnumerable<string> columns);
        /// <summary>
        /// Update POCO in the specified table, primary key for only the columns specified
        /// </summary>        
        int Update(string tableName, string primaryKeyName, object poco, IEnumerable<string> columns);
        /// <summary>
        /// Update POCO by convention or configuration for only the columns specified
        /// </summary>        
        int Update(object poco, IEnumerable<string> columns);
        /// <summary>
        /// Update POCO by primary key for only the columns specified
        /// </summary>        
        int Update(object poco, object primaryKeyValue, IEnumerable<string> columns);
        /// <summary>
        /// Update POCO by convention or configuration
        /// </summary>        
        int Update(object poco);
        /// <summary>
        /// Update POCO by convention or configuration specifying the properties to update
        /// </summary>        
        int Update<T>(T poco, Expression<Func<T, object>> fields);
        /// <summary>
        /// Update POCO by primary key
        /// </summary>        
        int Update(object poco, object primaryKeyValue);
        /// <summary>
        /// Runs an update statement deriving the table name from T and appending the sql provided. 
        /// </summary>        
        /// <example>
        /// Update&lt;User&gt;("set name = @0 where id = @1", "John", 1);
        /// </example>        
        int Update<T>(string sql, params object[] args);
        /// <summary>
        /// Runs an update statement deriving the table name from T and appending the sql provided. 
        /// </summary>        
        /// <example>
        /// Update&lt;User&gt;("set name = @0 where id = @1", "John", 1);
        /// </example>
        int Update<T>(Sql sql);
        /// <summary>
        /// Generate an update statement using a Fluent syntax. Remember to call Execute.
        /// </summary>
        IUpdateQueryProvider<T> UpdateMany<T>();
        /// <summary>
        /// Delete POCO specifying the table name and primary key
        /// </summary>        
        int Delete(string tableName, string primaryKeyName, object poco);
        /// <summary>
        /// Delete POCO specifying the table name, primary key name and primary key value
        /// </summary>        
        int Delete(string tableName, string primaryKeyName, object poco, object primaryKeyValue);
        /// <summary>
        /// Delete POCO using convention or configuration
        /// </summary>        
        int Delete(object poco);
        /// <summary>
        /// Runs an delete statement deriving the table name from T and appending the sql provided. 
        /// </summary>        
        /// <example>
        /// Delete&lt;User&gt;("where id = @0", 1);
        /// </example>     
        int Delete<T>(string sql, params object[] args);
        /// <summary>
        /// Runs an delete statement deriving the table name from T and appending the sql provided. 
        /// </summary>        
        /// <example>
        /// Delete&lt;User&gt;("where id = @0", 1);
        /// </example>
        int Delete<T>(Sql sql);
        /// <summary>
        /// Delete POCO deriving the table name from T and generating sql using the primary key
        /// </summary>        
        int Delete<T>(object pocoOrPrimaryKey);
        /// <summary>
        /// Generate a delete statement using a Fluent syntax. Remember to call Execute.
        /// </summary>        
        IDeleteQueryProvider<T> DeleteMany<T>();
        /// <summary>
        /// Performs an insert or an update depending on whether the POCO already exists. (i.e. an upsert/merge)
        /// </summary>        
        void Save<T>(T poco);
        /// <summary>
        /// Determines whether the POCO already exists
        /// </summary>        
        bool IsNew<T>(T poco);
    }

    public interface IDatabaseConfig
    {
        /// <summary>
        /// A collection of mappers used for converting values on inserting or on mapping
        /// </summary>        
        MapperCollection Mappers { get; set; }
        /// <summary>
        /// The PocoData factory used to build the meta data used by NPoco
        /// </summary>        
        IPocoDataFactory PocoDataFactory { get; set; }
        /// <summary>
        /// The target database used to handle different oddities in the different database providers
        /// </summary>        
        DatabaseType DatabaseType { get; }
        /// <summary>
        /// A list of IInterceptor's which can run at different times in the CRUD lifecyle
        /// </summary>        
        List<IInterceptor> Interceptors { get; }
        /// <summary>
        /// Retrieves current connection string
        /// </summary>        
        string ConnectionString { get; }
    }
}