using System;
using System.Collections.Generic;
using System.Linq;

namespace NPoco.FluentSqlBuilder
{
    public sealed class FluentSqlFunctions
    {
        internal FluentSqlFunctions() { }

        public int Count<T>(T value) => throw Marker();
        public int Count() => throw Marker();
        public int CountDistinct<T>(T value) => throw Marker();
        public T Sum<T>(T value) => throw Marker();
        public double? Average<T>(T value) => throw Marker();
        public T Min<T>(T value) => throw Marker();
        public T Max<T>(T value) => throw Marker();
        public T Case<T>(bool condition, T whenTrue, T whenFalse) => throw Marker();

        private static InvalidOperationException Marker() => new InvalidOperationException("SQL functions can only be used in a fluent SQL expression.");
    }

    public static class FluentSql
    {
        public static int Count<T>(T value) => throw Marker();
        public static int Count() => throw Marker();
        public static int CountDistinct<T>(T value) => throw Marker();
        public static T Sum<T>(T value) => throw Marker();
        public static double? Average<T>(T value) => throw Marker();
        public static T Min<T>(T value) => throw Marker();
        public static T Max<T>(T value) => throw Marker();
        public static bool In<T>(this T value, IFluentSqlQuery subquery) => throw Marker();
        public static bool NotIn<T>(this T value, IFluentSqlQuery subquery) => throw Marker();
        public static bool In<T>(this T value, IEnumerable<T> values) => values.Contains(value);
        public static bool NotIn<T>(this T value, IEnumerable<T> values) => !values.Contains(value);
        public static bool Exists(IFluentSqlQuery subquery) => throw Marker();
        public static bool NotExists(IFluentSqlQuery subquery) => throw Marker();
        public static T Case<T>(bool condition, T whenTrue, T whenFalse) => throw Marker();

        private static InvalidOperationException Marker() => new InvalidOperationException("SQL subquery methods can only be used in a fluent SQL expression.");
    }
}
