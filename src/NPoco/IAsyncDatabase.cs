using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Threading.Tasks;
using NPoco.Linq;

namespace NPoco
{
    public interface IAsyncDatabase : IAsyncQueryDatabase
    {
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
        /// Performs an SQL Insert using the table name, primary key and POCO
        /// </summary>
        /// <returns>The auto allocated primary key of the new record</returns>
        Task<object> InsertAsync(string tableName, string primaryKeyName, object poco);
        
        /// <summary>
        /// Insert POCO into the table by convention or configuration
        /// </summary>        
        Task<object> InsertAsync<T>(T poco);

        /// <summary>
        /// Insert POCO's into database using SqlBulkCopy for SqlServer (other DB's currently fall back to looping each row)
        /// </summary>  
        Task InsertBulkAsync<T>(IEnumerable<T> pocos, InsertBulkOptions options = null);

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
        
        /// <summary>
        /// Determines whether the POCO already exists
        /// </summary>
        Task<bool> IsNewAsync<T>(T poco);
       
        /// <summary>
        /// Performs an insert or an update depending on whether the POCO already exists. (i.e. an upsert/merge)
        /// </summary>      
        Task SaveAsync<T>(T poco);
    }

    public interface IAsyncQueryDatabase : IBaseDatabase
    {
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
        IAsyncEnumerable<T> QueryAsync<T>(string sql, params object[] args);

        /// <summary>
        /// Fetch objects of type T from the database using the sql and parameters specified. 
        /// Caution: This query will only be executed once you start iterating the result
        /// </summary>
        IAsyncEnumerable<T> QueryAsync<T>(Sql sql);

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
        /// Fetches multiple result sets into the one object.
        /// In this method you must provide how you will take the results and combine them
        /// </summary>
        Task<TRet> FetchMultipleAsync<T1, T2, TRet>(Func<List<T1>, List<T2>, TRet> cb, string sql, params object[] args);

        /// <summary>
        /// Fetches multiple result sets into the one object.
        /// In this method you must provide how you will take the results and combine them
        /// </summary>
        Task<TRet> FetchMultipleAsync<T1, T2, T3, TRet>(Func<List<T1>, List<T2>, List<T3>, TRet> cb, string sql, params object[] args);

        /// <summary>
        /// Fetches multiple result sets into the one object.
        /// In this method you must provide how you will take the results and combine them
        /// </summary>
        Task<TRet> FetchMultipleAsync<T1, T2, T3, T4, TRet>(Func<List<T1>, List<T2>, List<T3>, List<T4>, TRet> cb, string sql, params object[] args);

        /// <summary>
        /// Fetches multiple result sets into the one object.
        /// In this method you must provide how you will take the results and combine them
        /// </summary>
        Task<TRet> FetchMultipleAsync<T1, T2, TRet>(Func<List<T1>, List<T2>, TRet> cb, Sql sql);

        /// <summary>
        /// Fetches multiple result sets into the one object.
        /// In this method you must provide how you will take the results and combine them
        /// </summary>
        Task<TRet> FetchMultipleAsync<T1, T2, T3, TRet>(Func<List<T1>, List<T2>, List<T3>, TRet> cb, Sql sql);

        /// <summary>
        /// Fetches multiple result sets into the one object.
        /// In this method you must provide how you will take the results and combine them
        /// </summary>
        Task<TRet> FetchMultipleAsync<T1, T2, T3, T4, TRet>(Func<List<T1>, List<T2>, List<T3>, List<T4>, TRet> cb, Sql sql);

        /// <summary>
        /// Fetches multiple result sets into the one Tuple.
        /// </summary>
        Task<(List<T1>, List<T2>)> FetchMultipleAsync<T1, T2>(string sql, params object[] args);

        /// <summary>
        /// Fetches multiple result sets into the one Tuple.
        /// </summary>
        Task<(List<T1>, List<T2>, List<T3>)> FetchMultipleAsync<T1, T2, T3>(string sql, params object[] args);

        /// <summary>
        /// Fetches multiple result sets into the one Tuple.
        /// </summary>
        Task<(List<T1>, List<T2>, List<T3>, List<T4>)> FetchMultipleAsync<T1, T2, T3, T4>(string sql, params object[] args);

        /// <summary>
        /// Fetches multiple result sets into the one Tuple.
        /// </summary>
        Task<(List<T1>, List<T2>)> FetchMultipleAsync<T1, T2>(Sql sql);

        /// <summary>
        /// Fetches multiple result sets into the one Tuple.
        /// </summary>
        Task<(List<T1>, List<T2>, List<T3>)> FetchMultipleAsync<T1, T2, T3>(Sql sql);

        /// <summary>
        /// Fetches multiple result sets into the one Tuple.
        /// </summary>
        Task<(List<T1>, List<T2>, List<T3>, List<T4>)> FetchMultipleAsync<T1, T2, T3, T4>(Sql sql);
    }
}
