using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

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
        public T Raw<T>(string sql, params Expression<Func<object>>[] arguments) => throw Marker();
        public T Scalar<T>(IFluentSqlQuery subquery) => throw Marker();

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

        /// <summary>
        /// Emits SQL the builder has no expression for - a vendor function, an operator, a cast.
        /// The text is used verbatim, so it is the caller's job to keep it valid for the database
        /// they are targeting; nothing here makes it portable.
        ///
        /// Placeholders are <c>string.Format</c> style: <c>{0}</c>, <c>{1}</c> and so on are
        /// replaced with the translated <paramref name="arguments"/>, and <c>{{</c> / <c>}}</c>
        /// produce a literal brace. Each argument is a lambda translated the same way any other
        /// builder expression is, so table aliases resolve correctly and captured values become
        /// query parameters rather than inlined text:
        ///
        /// <code>
        /// .Select(() => new
        /// {
        ///     Readings = FluentSql.Raw&lt;string&gt;(
        ///         "json_agg(json_build_object('value', {0}, 'at', {1}) ORDER BY {1})",
        ///         () => metric.Row.ValueNumber,
        ///         () => metric.Row.OccurredAt)
        /// })
        /// </code>
        ///
        /// The result is wrapped in parentheses so it composes safely inside a larger expression.
        /// </summary>
        /// <typeparam name="T">The type the fragment produces, used to map the value back.</typeparam>
        /// <param name="sql">SQL text with <c>{0}</c>-style placeholders. Must be a constant or captured string.</param>
        /// <param name="arguments">Expressions substituted into the placeholders.</param>
        public static T Raw<T>(string sql, params Expression<Func<object>>[] arguments) => throw Marker();

        /// <summary>
        /// Embeds a subquery that yields a single value, for use anywhere a column can appear -
        /// most usefully in a projection, which <see cref="Exists"/> and <see cref="In{T}(T, IFluentSqlQuery)"/>
        /// do not cover.
        ///
        /// To correlate the subquery with the outer query, build it from
        /// <see cref="FluentSqlQueryStage.Subquery"/> so the outer tables are in scope:
        ///
        /// <code>
        /// var query = db.FluentQuery().From&lt;EnergySystemSite&gt;(out var site);
        /// var integrations = query.Subquery()
        ///     .From&lt;Integration&gt;(out var integration)
        ///     .Where(() => integration.Row.EnergySystemId == site.Row.Id &amp;&amp; integration.Row.Active)
        ///     .SelectScalar(integration, x => FluentSql.Count());
        ///
        /// query.Select(() => new { site.Row.Name, Count = FluentSql.Scalar&lt;int&gt;(integrations) });
        /// </code>
        /// </summary>
        /// <typeparam name="T">The type the subquery yields.</typeparam>
        /// <param name="subquery">A query projecting exactly one column.</param>
        public static T Scalar<T>(IFluentSqlQuery subquery) => throw Marker();

        private static InvalidOperationException Marker() => new InvalidOperationException("SQL subquery methods can only be used in a fluent SQL expression.");
    }
}
