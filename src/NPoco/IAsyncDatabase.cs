using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using NPoco.Linq;
#if !NET35 && !NET40
using System.Threading.Tasks;
#endif

namespace NPoco
{
    public interface IAsyncDatabase : IBaseDatabase
    {
#if !NET35 && !NET40
        /// <summary>
        /// Fetch the only row of type T using the sql and parameters specified
        /// Get an object of type T by primary key value
        /// </summary>
        Task<T> SingleAsync<T>(string sql, params object[] args);

        /// <summary>
        /// Fetch the only row of type T using the sql and parameters specified
        /// </summary>
        Task<T> SingleAsync<T>(Sql sql);

        /// <summary>
        /// Fetch the only row of type T using the sql and parameters specified
        /// </summary>
        Task<T> SingleOrDefaultAsync<T>(string sql, params object[] args);

        /// <summary>
        /// Fetch the only row of type T using the sql and parameters specified
        /// </summary>
        Task<T> SingleOrDefaultAsync<T>(Sql sql);

        /// <summary>
        /// Get an object of type T by primary key value
        /// </summary>
        Task<T> SingleByIdAsync<T>(object primaryKey);

        /// <summary>
        /// Get an object of type T by primary key value
        /// </summary>
        Task<T> SingleOrDefaultByIdAsync<T>(object primaryKey);

        /// <summary>
        /// Fetch the first row of type T using the sql and parameters specified
        /// </summary>
        Task<T> FirstAsync<T>(string sql, params object[] args);

        /// <summary>
        /// Fetch the first row of type T using the sql and parameters specified
        /// </summary>
        Task<T> FirstAsync<T>(Sql sql);

        /// <summary>
        /// Fetch the first row of type T using the sql and parameters specified
        /// </summary>
        Task<T> FirstOrDefaultAsync<T>(string sql, params object[] args);

        /// <summary>
        /// Fetch the first row of type T using the sql and parameters specified
        /// </summary>
        Task<T> FirstOrDefaultAsync<T>(Sql sql);

        /// <summary>
        /// Fetch objects of type T from the database using the sql and parameters specified. 
        /// Caution: This query will only be executed once you start iterating the result
        /// </summary>
        Task<IEnumerable<T>> QueryAsync<T>(string sql, params object[] args);

        /// <summary>
        /// Fetch objects of type T from the database using the sql and parameters specified. 
        /// Caution: This query will only be executed once you start iterating the result
        /// </summary>
        Task<IEnumerable<T>> QueryAsync<T>(Sql sql);

        /// <summary>
        /// Entry point for LINQ queries
        /// </summary>
        IAsyncQueryProviderWithIncludes<T> QueryAsync<T>();

        /// <summary>
        /// Fetch objects of type T from the database using the sql and parameters specified. 
        /// </summary>
        Task<List<T>> FetchAsync<T>(string sql, params object[] args);

        /// <summary>
        /// Fetch objects of type T from the database using the sql and parameters specified. 
        /// </summary>
        Task<List<T>> FetchAsync<T>(Sql sql);

        /// <summary>
        /// Fetch all objects of type T from the database. 
        /// </summary>
        Task<List<T>> FetchAsync<T>();

        /// <summary>
        /// Fetch objects of type T from the database using the sql and parameters specified. 
        /// The sql provided will be converted so that only the results for the page and itemsPerPage specified will be returned.
        /// Extra metadata in the Page class will also be returned.
        /// Note: This will perform two queries. One for the paged results and one for the count of all results.
        /// </summary>
        Task<Page<T>> PageAsync<T>(long page, long itemsPerPage, string sql, params object[] args);

        /// <summary>
        /// Fetch objects of type T from the database using the sql and parameters specified. 
        /// The sql provided will be converted so that only the results for the page and itemsPerPage specified will be returned.
        /// Extra metadata in the Page class will also be returned.
        /// Note: This will perform two queries. One for the paged results and one for the count of all results.
        /// </summary>
        Task<Page<T>> PageAsync<T>(long page, long itemsPerPage, Sql sql);

        /// <summary>
        /// Fetch objects of type T from the database using the sql and parameters specified. 
        /// The sql provided will be converted so that only the results for the page and itemsPerPage values specified will be returned.
        /// </summary>
        Task<List<T>> FetchAsync<T>(long page, long itemsPerPage, string sql, params object[] args);

        /// <summary>
        /// Fetch objects of type T from the database using the sql and parameters specified. 
        /// The sql provided will be converted so that only the results for the page and itemsPerPage values specified will be returned.
        /// </summary>
        Task<List<T>> FetchAsync<T>(long page, long itemsPerPage, Sql sql);

        /// <summary>
        /// Fetch objects of type T from the database using the sql and parameters specified. 
        /// The sql provided will be converted so that only the results for the skip and take values specified will be returned.
        /// </summary>
        Task<List<T>> SkipTakeAsync<T>(long skip, long take, string sql, params object[] args);

        /// <summary>
        /// Fetch objects of type T from the database using the sql and parameters specified. 
        /// The sql provided will be converted so that only the results for the skip and take values specified will be returned.
        /// </summary>
        Task<List<T>> SkipTakeAsync<T>(long skip, long take, Sql sql);

        /// <summary>
        /// Executes the provided sql and parameters and casts the result to T
        /// </summary>
        Task<T> ExecuteScalarAsync<T>(string sql, params object[] args);

        /// <summary>
        /// Executes the provided sql and parameters and casts the result to T
        /// </summary>
        Task<T> ExecuteScalarAsync<T>(Sql sql);

        /// <summary>
        /// Executes the provided sql and parameters
        /// </summary>
        Task<int> ExecuteAsync(string sql, params object[] args);

        /// <summary>
        /// Executes the provided sql and parameters
        /// </summary>
        Task<int> ExecuteAsync(Sql sql);

        /// <summary>
        /// Insert POCO into the table by convention or configuration
        /// </summary>        
        Task<object> InsertAsync<T>(T poco);

        /// <summary>
        /// Insert POCO's into database by concatenating sql using the provided batch options
        /// </summary>  
        Task<int> InsertBatchAsync<T>(IEnumerable<T> pocos, BatchOptions options = null);

        /// <summary>
        /// Update POCO in the table by convention or configuration
        /// </summary>        
        Task<int> UpdateAsync(object poco);

        /// <summary>
        /// Update POCO in the table by convention or configuration specifying which columns to update
        /// </summary>        
        Task<int> UpdateAsync(object poco, IEnumerable<string> columns);

        /// <summary>
        /// Update POCO in the table by convention or configuration specifying which columns to update
        /// </summary>  
        Task<int> UpdateAsync<T>(T poco, Expression<Func<T, object>> fields);

        /// <summary>
        /// Update POCO's into database by concatenating sql using the provided batch options
        /// </summary>  
        Task<int> UpdateBatchAsync<T>(IEnumerable<UpdateBatch<T>> pocos, BatchOptions options = null);

        /// <summary>
        /// Delete POCO from table by convention or configuration
        /// </summary>        
        Task<int> DeleteAsync(object poco);

        /// <summary>
        /// Generate an update statement using a Fluent syntax. Remember to call Execute.
        /// </summary>
        IAsyncUpdateQueryProvider<T> UpdateManyAsync<T>();

        /// <summary>
        /// Generate a delete statement using a Fluent syntax. Remember to call Execute.
        /// </summary>
        IAsyncDeleteQueryProvider<T> DeleteManyAsync<T>();
#endif
    }
}
