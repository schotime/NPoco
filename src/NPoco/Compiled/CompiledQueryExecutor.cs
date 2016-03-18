using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NPoco.Expressions;
using NPoco.Linq;

namespace NPoco.Compiled
{
    public class CompiledQueryExecutor
    {
        public static TReturn ExecuteCompiledQuery<T, TReturn>(ICompiledQuery<T, TReturn> compiledQueryExpression, Database db, Dictionary<Type, CompiledQuery> compiledQueries)
        {
            var compiledQueryType = compiledQueryExpression.GetType();
            var queryProvider = new QueryProvider<T>(db);
            
            if (!compiledQueries.ContainsKey(compiledQueryType))
            {
                var compileQuery = compiledQueryExpression.QueryIs().Compile();
                queryProvider.CompileOnly = true;
                var properties = GetCompiledDatas(compiledQueryType);
                compileQuery(queryProvider);
                var compiledQueryInfo = GetCompiledQueryInfo(queryProvider, properties, () => compileQuery);
                compiledQueries[compiledQueryType] = compiledQueryInfo;
            }

            var compiledInfo = PrepareCachedQuery(compiledQueryExpression, compiledQueries, compiledQueryType, queryProvider);
            var result = ((Func<IQueryProviderWithIncludes<T>,TReturn>)compiledInfo.CompiledExpression())(queryProvider);
            return result;
        }

#if !NET35 && !NET40
        public static async System.Threading.Tasks.Task<TReturn> ExecuteCompiledQueryAsync<T, TReturn>(ICompiledQueryAsync<T, TReturn> compiledQueryExpression, Database db, Dictionary<Type, CompiledQuery> compiledQueries)
        {
            var compiledQueryType = compiledQueryExpression.GetType();
            var queryProvider = new QueryProvider<T>(db);
            
            if (!compiledQueries.ContainsKey(compiledQueryType))
            {
                var compileQuery = compiledQueryExpression.QueryIs().Compile();
                queryProvider.CompileOnly = true;
                var properties = GetCompiledDatas(compiledQueryType);
                await compileQuery(queryProvider);
                var compiledQueryInfo = GetCompiledQueryInfo(queryProvider, properties, () => compileQuery);
                compiledQueries[compiledQueryType] = compiledQueryInfo;
            }

            var compiledInfo = PrepareCachedQuery(compiledQueryExpression, compiledQueries, compiledQueryType, queryProvider);
            var result = await ((Func<IQueryProviderWithIncludes<T>, System.Threading.Tasks.Task<TReturn>>)compiledInfo.CompiledExpression())(queryProvider);
            return result;
        }
#endif
        private static CompiledQuery PrepareCachedQuery<T>(object compiledQueryExpression, Dictionary<Type, CompiledQuery> compiledQueries, Type compiledQueryType, QueryProvider<T> queryProvider)
        {
            var compQuery = compiledQueries[compiledQueryType];
            var newargs = ReconstructArgs(compiledQueryExpression, compQuery);
            var newSql = new Sql(true, compQuery.Template.SQL, newargs);
            queryProvider.SqlForExecution = newSql;
            queryProvider.CompileOnly = false;
            return compQuery;
        }

        private static object[] ReconstructArgs(object compiledQueryExpression, CompiledQuery compQuery)
        {
            var newargs = new object[compQuery.Template.Arguments.Length];
            for (int index = 0; index < compQuery.Template.Arguments.Length; index++)
            {
                var arg = compQuery.Template.Arguments[index];
                var compData = compQuery.Data.SingleOrDefault(x => x.Index == index);
                newargs[index] = compData != null
                    ? GetCompiledValue(compiledQueryExpression, compData.MemberAccessor)
                    : arg;
            }
            return newargs;
        }

        private static CompiledQuery GetCompiledQueryInfo<T>(QueryProvider<T> queryProvider, List<CompiledData> properties, Func<object> compiled)
        {
            var templateArgs = queryProvider.SqlForExecution.Arguments;
            for (int index = 0; index < templateArgs.Length; index++)
            {
                var templateArg = templateArgs[index];
                var compiledValue = properties.SingleOrDefault(x => templateArg != null && templateArg.ToString().IndexOf((SqlExpression<T>.COMPILED_MEMBER_PREFIX + x.Name), StringComparison.OrdinalIgnoreCase) >= 0);
                if (compiledValue != null)
                {
                    compiledValue.Index = index;
                }
            }

            var compiledQueryInfo = new CompiledQuery
            {
                Template = queryProvider.SqlForExecution,
                Data = properties,
                CompiledExpression = compiled
            };
            return compiledQueryInfo;
        }

        private static List<CompiledData> GetCompiledDatas(Type compiledQueryType)
        {
            var properties = compiledQueryType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(x => new CompiledData
                {
                    Name = x.Name,
                    MemberAccessor = new MemberAccessor(compiledQueryType, x.Name)
                })
                .ToList();

            return properties;
        }

        private static object GetCompiledValue(object compiledQueryExpression, MemberAccessor memberAccessor)
        {
            return memberAccessor.Get(compiledQueryExpression);
        }
    }
}
