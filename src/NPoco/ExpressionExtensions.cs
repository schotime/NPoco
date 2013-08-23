using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using NPoco.Expressions;

namespace NPoco
{
    public static class ExpressionExtensions
    {
        public static int UpdateWhere<T, TKey>(this IDatabase database, T obj, Expression<Func<T, bool>> where)
        {
            return database.UpdateWhere<T, TKey>(obj, where, null);
        }

        public static int UpdateWhere<T, TKey>(this IDatabase database, T obj, Expression<Func<T, bool>> where, Expression<Func<T, TKey>> onlyFields)
        {
            var ev = database.DatabaseType.ExpressionVisitor<T>(database);
            ev.Update(onlyFields);
            ev.Where(where);
            var updateStatement = ev.Context.ToUpdateStatement(obj, false, onlyFields == null);
            return database.Execute(updateStatement, ev.Context.Params);
        }

        public static int UpdateWhere<T>(this IDatabase database, T obj, string where, params object[] parameters)
        {
            var ev = database.DatabaseType.ExpressionVisitor<T>(database);
            ev.Where(where, parameters);
            var sql = ev.Context.ToUpdateStatement(obj);
            return database.Execute(sql, ev.Context.Params);
        }

        public static int UpdateBy<T>(this IDatabase database, T obj, Func<SqlExpression<T>, SqlExpression<T>> sqlExpression)
        {
            var ev = database.DatabaseType.ExpressionVisitor<T>(database);
            return database.Execute(sqlExpression(ev).Context.ToUpdateStatement(obj), ev.Context.Params);
        }

        public static int DeleteWhere<T>(this IDatabase database, Expression<Func<T, bool>> where)
        {
            var ev = database.DatabaseType.ExpressionVisitor<T>(database);
            ev.Where(where);
            return database.Execute(ev.Context.ToDeleteStatement(), ev.Context.Params);
        }

        public static int DeleteWhere<T>(this IDatabase database, string where, params object[] parameters)
        {
            var ev = database.DatabaseType.ExpressionVisitor<T>(database);
            ev.Where(where, parameters);
            return database.Execute(ev.Context.ToDeleteStatement(), ev.Context.Params);
        }

        public static int DeleteBy<T>(this IDatabase database, Func<SqlExpression<T>, SqlExpression<T>> sqlExpression)
        {
            var ev = database.DatabaseType.ExpressionVisitor<T>(database);
            return database.Execute(sqlExpression(ev).Context.ToDeleteStatement(), ev.Context.Params);
        }
    }
}
