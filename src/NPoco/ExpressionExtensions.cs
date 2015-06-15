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
        public static int UpdateWhere<T>(this IDatabase database, T obj, string where, params object[] parameters)
        {
            var ev = database.DatabaseType.ExpressionVisitor<T>(database, database.PocoDataFactory.ForType(typeof(T)));
            ev.Where(where, parameters);
            var sql = ev.Context.ToUpdateStatement(obj);
            return database.Execute(sql, ev.Context.Params);
        }

        public static int DeleteWhere<T>(this IDatabase database, string where, params object[] parameters)
        {
            var ev = database.DatabaseType.ExpressionVisitor<T>(database, database.PocoDataFactory.ForType(typeof(T)));
            ev.Where(where, parameters);
            return database.Execute(ev.Context.ToDeleteStatement(), ev.Context.Params);
        }
    }
}
