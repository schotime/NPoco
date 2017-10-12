using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using NPoco.Linq;
#if !NET35 && !NET40
using System.Threading.Tasks;
#endif

namespace NPoco
{
    public interface IDatabaseQuery
    {
        /// <summary>
        /// Opens the DbConnection manually
        /// </summary>
        IDatabase OpenSharedConnection();
        
        /// <summary>
        /// Closes the DBConnection manually
        /// </summary>
        void CloseSharedConnection();
        
        /// <summary>
        /// Builds a paged query from a non-paged query
        /// </summary>
        void BuildPageQueries<T>(long skip, long take, string sql, ref object[] args, out string sqlCount, out string sqlPage);
        
        /// <summary>
        /// Executes the provided sql and parameters
        /// </summary>
        int Execute(string sql, params object[] args);
        
        /// <summary>
        /// Executes the provided sql and parameters
        /// </summary>
        int Execute(Sql sql);

        /// <summary>
        /// Executes the provided sql and parameters with the specified command type
        /// </summary>
        int Execute(string sql, CommandType commandType, params object[] args);

        /// <summary>
        /// Executes the provided sql and parameters and casts the result to T
        /// </summary>
        T ExecuteScalar<T>(string sql, params object[] args);
        
        /// <summary>
        /// Executes the provided sql and parameters and casts the result to T
        /// </summary>
        T ExecuteScalar<T>(Sql sql);

        /// <summary>
        /// Executes the provided sql and parameters with the specified commandType and casts the result to T
        /// </summary>
        T ExecuteScalar<T>(string sql, CommandType commandType, params object[] args);

        /// <summary>
        /// Non generic Fetch which returns a list of objects of the given type provided
        /// </summary>
        List<object> Fetch(Type type, string sql, params object[] args);
        
        /// <summary>
        /// Non generic Fetch which returns a list of objects of the given type provided
        /// </summary>
        List<object> Fetch(Type type, Sql Sql);
        
        /// <summary>
        /// Non generic Query which returns a list of objects of the given type provided. 
        /// Caution: This query will only be executed once you start iterating the result
        /// </summary>
        IEnumerable<object> Query(Type type, string sql, params object[] args);
        
        /// <summary>
        /// Non generic Query which returns a list of objects of the given type provided. 
        /// Caution: This query will only be executed once you start iterating the result
        /// </summary>
        IEnumerable<object> Query(Type type, Sql Sql);

        /// <summary>
        /// Fetch all objects of type T from the database using the conventions or configuration on the type T. 
        /// Caution: This will retrieve ALL objects in the table
        /// </summary>
        List<T> Fetch<T>();
        
        /// <summary>
        /// Fetch objects of type T from the database using the sql and parameters specified. 
        /// </summary>
        List<T> Fetch<T>(string sql, params object[] args);
        
        /// <summary>
        /// Fetch objects of type T from the database using the sql and parameters specified
        /// </summary>
        List<T> Fetch<T>(Sql sql);
        
        /// <summary>
        /// Fetch objects of type T from the database using the sql and parameters specified. 
        /// The sql provided will be converted so that only the results for the page and itemsPerPage values specified will be returned.
        /// </summary>
        List<T> Fetch<T>(long page, long itemsPerPage, string sql, params object[] args);
        
        /// <summary>
        /// Fetch objects of type T from the database using the sql and parameters specified. 
        /// The sql provided will be converted so that only the results for the page and itemsPerPage values specified will be returned.
        /// </summary>
        List<T> Fetch<T>(long page, long itemsPerPage, Sql sql);
        
        /// <summary>
        /// Fetch objects of type T from the database using the sql and parameters specified. 
        /// The sql provided will be converted so that only the results for the page and itemsPerPage specified will be returned.
        /// Extra metadata in the Page class will also be returned.
        /// Note: This will perform two queries. One for the paged results and one for the count of all results.
        /// </summary>
        Page<T> Page<T>(long page, long itemsPerPage, string sql, params object[] args);
        
        /// <summary>
        /// Fetch objects of type T from the database using the sql and parameters specified. 
        /// The sql provided will be converted so that only the results for the page and itemsPerPage specified will be returned.
        /// Extra metadata in the Page class will also be returned.
        /// Note: This will perform two queries. One for the paged results and one for the count of all results.
        /// </summary>
        Page<T> Page<T>(long page, long itemsPerPage, Sql sql);

        /// <summary>
        /// Fetch objects of type T from the database using the sql and parameters specified. 
        /// The sql provided will be converted so that only the results for the skip and take values specified will be returned.
        /// </summary>
        List<T> SkipTake<T>(long skip, long take, string sql, params object[] args);
        
        /// <summary>
        /// Fetch objects of type T from the database using the sql and parameters specified. 
        /// The sql provided will be converted so that only the results for the skip and take values specified will be returned.
        /// </summary>
        List<T> SkipTake<T>(long skip, long take, Sql sql);

        /// <summary>
        /// Fetch objects of type T using the sql provided, but also retrieve the many property's data using the sql provided.
        /// The one columns should come first then the many columns. 
        /// eg. select one.*, many.* from one inner join many on one.id = many.oneid
        /// </summary>
        List<T> FetchOneToMany<T>(Expression<Func<T, IList>> many, string sql, params object[] args);
        
        /// <summary>
        /// Fetch objects of type T using the sql provided, but also retrieve the many property's data using the sql provided.
        /// The one columns should come first then the many columns. 
        /// eg. select one.*, many.* from one inner join many on one.id = many.oneid
        /// </summary>
        List<T> FetchOneToMany<T>(Expression<Func<T, IList>> many, Sql sql);
        
        /// <summary>
        /// Fetch objects of type T using the sql provided, but also retrieve the many property's data using the sql provided.
        /// The one columns should come first then the many columns. 
        /// eg. select one.*, many.* from one inner join many on one.id = many.oneid
        /// </summary>
        List<T> FetchOneToMany<T>(Expression<Func<T, IList>> many, Func<T, object> idFunc, string sql, params object[] args);
        
        /// <summary>
        /// Fetch objects of type T using the sql provided, but also retrieve the many property's data using the sql provided.
        /// The one columns should come first then the many columns. 
        /// eg. select one.*, many.* from one inner join many on one.id = many.oneid
        /// </summary>
        List<T> FetchOneToMany<T>(Expression<Func<T, IList>> many, Func<T, object> idFunc, Sql sql);

        /// <summary>
        /// Fetch objects of type T from the database using the sql and parameters specified. 
        /// Caution: This query will only be executed once you start iterating the result
        /// </summary>
        IEnumerable<T> Query<T>(string sql, params object[] args);
        
        /// <summary>
        /// Fetch objects of type T from the database using the sql and parameters specified. 
        /// Caution: This query will only be executed once you start iterating the result
        /// </summary>
        IEnumerable<T> Query<T>(Sql sql);
        
        /// <summary>
        /// Entry point for LINQ queries
        /// </summary>
        IQueryProviderWithIncludes<T> Query<T>();
        
        /// <summary>
        /// Get an object of type T by primary key value
        /// </summary>
        T SingleById<T>(object primaryKey);
        
        /// <summary>
        /// Fetch the only row of type T using the sql and parameters specified
        /// </summary>
        T Single<T>(string sql, params object[] args);
        
        /// <summary>
        /// Fetch the only row of type T using the sql and parameters specified into the T instance provided
        /// </summary>
        T SingleInto<T>(T instance, string sql, params object[] args);
        
        /// <summary>
        /// Get an object of type T by primary key value where the row may not be there
        /// </summary>
        T SingleOrDefaultById<T>(object primaryKey);
        
        /// <summary>
        /// Fetch the only row of type T using the sql and parameters specified
        /// </summary>
        T SingleOrDefault<T>(string sql, params object[] args);
        
        /// <summary>
        /// Fetch the only row of type T using the sql and parameters specified into the T instance provided
        /// </summary>
        T SingleOrDefaultInto<T>(T instance, string sql, params object[] args);

        /// <summary>
        /// Fetch the first row of type T using the sql and parameters specified
        /// </summary>
        T First<T>(string sql, params object[] args);
        
        /// <summary>
        /// Fetch the first row of type T using the sql and parameters specified into the T instance provided
        /// </summary>
        T FirstInto<T>(T instance, string sql, params object[] args);
        
        /// <summary>
        /// Fetch the first row of type T using the sql and parameters specified
        /// </summary>
        T FirstOrDefault<T>(string sql, params object[] args);
        
        /// <summary>
        /// Fetch the first row of type T using the sql and parameters specified into the T instance provided
        /// </summary>
        T FirstOrDefaultInto<T>(T instance, string sql, params object[] args);

        /// <summary>
        /// Fetch the only row of type T using the Sql specified
        /// </summary>
        T Single<T>(Sql sql);
        
        /// <summary>
        /// Fetch the only row of type T using the Sql specified into the T instance provided
        /// </summary>
        T SingleInto<T>(T instance, Sql sql);
        
        /// <summary>
        /// Fetch the only row of type T using the Sql specified
        /// </summary>
        T SingleOrDefault<T>(Sql sql);
        
        /// <summary>
        /// Fetch the only row of type T using the Sql specified
        /// </summary>
        T SingleOrDefaultInto<T>(T instance, Sql sql);
        
        /// <summary>
        /// Fetch the first row of type T using the Sql specified
        /// </summary>
        T First<T>(Sql sql);
        
        /// <summary>
        /// Fetch the first row of type T using the Sql specified into the T instance provided
        /// </summary>
        T FirstInto<T>(T instance, Sql sql);
        
        /// <summary>
        /// Fetch the first row of type T using the Sql specified
        /// </summary>
        T FirstOrDefault<T>(Sql sql);
        
        /// <summary>
        /// Fetch the first row of type T using the Sql specified
        /// </summary>
        T FirstOrDefaultInto<T>(T instance, Sql sql);

        /// <summary>
        /// Fetches the first two columns into a dictionary using the first value as the key and the second as the value
        /// </summary>
        Dictionary<TKey, TValue> Dictionary<TKey, TValue>(Sql Sql);
        
        /// <summary>
        /// Fetches the first two columns into a dictionary using the first value as the key and the second as the value
        /// </summary>
        Dictionary<TKey, TValue> Dictionary<TKey, TValue>(string sql, params object[] args);
        
        /// <summary>
        /// Checks if the POCO of type T exists by using the primary key value
        /// </summary>
        bool Exists<T>(object primaryKey);
        
        /// <summary>
        /// Specifies the command timeout to be used for the very next command
        /// </summary>
        int OneTimeCommandTimeout { get; set; }

        /// <summary>
        /// Fetches multiple result sets into the one object.
        /// In this method you must provide how you will take the results and combine them
        /// </summary>
        TRet FetchMultiple<T1, T2, TRet>(Func<List<T1>, List<T2>, TRet> cb, string sql, params object[] args);
        
        /// <summary>
        /// Fetches multiple result sets into the one object.
        /// In this method you must provide how you will take the results and combine them
        /// </summary>
        TRet FetchMultiple<T1, T2, T3, TRet>(Func<List<T1>, List<T2>, List<T3>, TRet> cb, string sql, params object[] args);
        
        /// <summary>
        /// Fetches multiple result sets into the one object.
        /// In this method you must provide how you will take the results and combine them
        /// </summary>
        TRet FetchMultiple<T1, T2, T3, T4, TRet>(Func<List<T1>, List<T2>, List<T3>, List<T4>, TRet> cb, string sql, params object[] args);
        
        /// <summary>
        /// Fetches multiple result sets into the one object.
        /// In this method you must provide how you will take the results and combine them
        /// </summary>
        TRet FetchMultiple<T1, T2, TRet>(Func<List<T1>, List<T2>, TRet> cb, Sql sql);
        
        /// <summary>
        /// Fetches multiple result sets into the one object.
        /// In this method you must provide how you will take the results and combine them
        /// </summary>
        TRet FetchMultiple<T1, T2, T3, TRet>(Func<List<T1>, List<T2>, List<T3>, TRet> cb, Sql sql);
        
        /// <summary>
        /// Fetches multiple result sets into the one object.
        /// In this method you must provide how you will take the results and combine them
        /// </summary>
        TRet FetchMultiple<T1, T2, T3, T4, TRet>(Func<List<T1>, List<T2>, List<T3>, List<T4>, TRet> cb, Sql sql);

        /// <summary>
        /// Fetches multiple result sets into the one Tuple.
        /// </summary>
        Tuple<List<T1>, List<T2>> FetchMultiple<T1, T2>(string sql, params object[] args);
        
        /// <summary>
        /// Fetches multiple result sets into the one Tuple.
        /// </summary>
        Tuple<List<T1>, List<T2>, List<T3>> FetchMultiple<T1, T2, T3>(string sql, params object[] args);
        
        /// <summary>
        /// Fetches multiple result sets into the one Tuple.
        /// </summary>
        Tuple<List<T1>, List<T2>, List<T3>, List<T4>> FetchMultiple<T1, T2, T3, T4>(string sql, params object[] args);
        
        /// <summary>
        /// Fetches multiple result sets into the one Tuple.
        /// </summary>
        Tuple<List<T1>, List<T2>> FetchMultiple<T1, T2>(Sql sql);
        
        /// <summary>
        /// Fetches multiple result sets into the one Tuple.
        /// </summary>
        Tuple<List<T1>, List<T2>, List<T3>> FetchMultiple<T1, T2, T3>(Sql sql);
        
        /// <summary>
        /// Fetches multiple result sets into the one Tuple.
        /// </summary>
        Tuple<List<T1>, List<T2>, List<T3>, List<T4>> FetchMultiple<T1, T2, T3, T4>(Sql sql);

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
#endif
    }
}