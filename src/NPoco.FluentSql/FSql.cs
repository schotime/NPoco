using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace NPoco.FluentSql
{
    /// <summary>
    /// The SQL functions in scope inside a projection that takes one of these as its lambda
    /// parameter - <c>Select(f =&gt; new { Total = f.Sum(order.Row.Amount) })</c>. The members exist
    /// only to be recognised and translated, so calling one outside a fluent SQL expression throws.
    /// <see cref="FSql"/> exposes the same set as static methods.
    /// </summary>
    public sealed class FSqlFunctions
    {
        internal FSqlFunctions() { }

        /// <summary>Translates to <c>COUNT(<paramref name="value"/>)</c>.</summary>
        /// <typeparam name="T">The type of the counted expression.</typeparam>
        /// <param name="value">The expression to count. Rows where it is null are not counted.</param>
        /// <returns>Never returns a value - the call is translated to SQL.</returns>
        public int Count<T>(T value) => throw Marker();
        /// <summary>Translates to <c>COUNT(*)</c>.</summary>
        /// <returns>Never returns a value - the call is translated to SQL.</returns>
        public int Count() => throw Marker();
        /// <summary>Translates to <c>COUNT(DISTINCT <paramref name="value"/>)</c>.</summary>
        /// <typeparam name="T">The type of the counted expression.</typeparam>
        /// <param name="value">The expression whose distinct values are counted.</param>
        /// <returns>Never returns a value - the call is translated to SQL.</returns>
        public int CountDistinct<T>(T value) => throw Marker();
        /// <summary>Translates to <c>SUM(<paramref name="value"/>)</c>.</summary>
        /// <typeparam name="T">The type of the summed expression.</typeparam>
        /// <param name="value">The expression to add up.</param>
        /// <returns>Never returns a value - the call is translated to SQL.</returns>
        public T Sum<T>(T value) => throw Marker();
        /// <summary>Translates to <c>AVG(<paramref name="value"/>)</c>.</summary>
        /// <typeparam name="T">The type of the averaged expression.</typeparam>
        /// <param name="value">The expression to average.</param>
        /// <returns>Never returns a value - the call is translated to SQL.</returns>
        public double? Average<T>(T value) => throw Marker();
        /// <summary>Translates to <c>MIN(<paramref name="value"/>)</c>.</summary>
        /// <typeparam name="T">The type of the compared expression.</typeparam>
        /// <param name="value">The expression to take the smallest value of.</param>
        /// <returns>Never returns a value - the call is translated to SQL.</returns>
        public T Min<T>(T value) => throw Marker();
        /// <summary>Translates to <c>MAX(<paramref name="value"/>)</c>.</summary>
        /// <typeparam name="T">The type of the compared expression.</typeparam>
        /// <param name="value">The expression to take the largest value of.</param>
        /// <returns>Never returns a value - the call is translated to SQL.</returns>
        public T Max<T>(T value) => throw Marker();
        /// <summary>Translates to <c>CASE WHEN ... THEN ... ELSE ... END</c>.</summary>
        /// <typeparam name="T">The type both branches produce.</typeparam>
        /// <param name="condition">The predicate tested by the WHEN clause.</param>
        /// <param name="whenTrue">The value produced when the condition holds.</param>
        /// <param name="whenFalse">The value produced otherwise.</param>
        /// <returns>Never returns a value - the call is translated to SQL.</returns>
        public T Case<T>(bool condition, T whenTrue, T whenFalse) => throw Marker();
        /// <inheritdoc cref="FSql.Raw{T}(string, object[])"/>
        public T Raw<T>(string sql, params object[] arguments) => throw Marker();
        /// <inheritdoc cref="FSql.Scalar{T}(IFluentSqlQuery)"/>
        public T Scalar<T>(IFluentSqlQuery subquery) => throw Marker();

        private static InvalidOperationException Marker() => new InvalidOperationException("SQL functions can only be used in a fluent SQL expression.");
    }

    /// <summary>
    /// The SQL functions and subquery operators the builder recognises inside an expression:
    /// aggregates, <c>IN</c>, <c>EXISTS</c>, <c>CASE</c>, scalar subqueries and raw fragments.
    /// Apart from the sequence overloads of <see cref="In{T}(T, IEnumerable{T})"/> and
    /// <see cref="NotIn{T}(T, IEnumerable{T})"/>, these exist only to be translated, so calling one
    /// outside a fluent SQL expression throws.
    /// </summary>
    public static class FSql
    {
        /// <summary>Translates to <c>COUNT(<paramref name="value"/>)</c>.</summary>
        /// <typeparam name="T">The type of the counted expression.</typeparam>
        /// <param name="value">The expression to count. Rows where it is null are not counted.</param>
        /// <returns>Never returns a value - the call is translated to SQL.</returns>
        public static int Count<T>(T value) => throw Marker();
        /// <summary>Translates to <c>COUNT(*)</c>.</summary>
        /// <returns>Never returns a value - the call is translated to SQL.</returns>
        public static int Count() => throw Marker();
        /// <summary>Translates to <c>COUNT(DISTINCT <paramref name="value"/>)</c>.</summary>
        /// <typeparam name="T">The type of the counted expression.</typeparam>
        /// <param name="value">The expression whose distinct values are counted.</param>
        /// <returns>Never returns a value - the call is translated to SQL.</returns>
        public static int CountDistinct<T>(T value) => throw Marker();
        /// <summary>Translates to <c>SUM(<paramref name="value"/>)</c>.</summary>
        /// <typeparam name="T">The type of the summed expression.</typeparam>
        /// <param name="value">The expression to add up.</param>
        /// <returns>Never returns a value - the call is translated to SQL.</returns>
        public static T Sum<T>(T value) => throw Marker();
        /// <summary>Translates to <c>AVG(<paramref name="value"/>)</c>.</summary>
        /// <typeparam name="T">The type of the averaged expression.</typeparam>
        /// <param name="value">The expression to average.</param>
        /// <returns>Never returns a value - the call is translated to SQL.</returns>
        public static double? Average<T>(T value) => throw Marker();
        /// <summary>Translates to <c>MIN(<paramref name="value"/>)</c>.</summary>
        /// <typeparam name="T">The type of the compared expression.</typeparam>
        /// <param name="value">The expression to take the smallest value of.</param>
        /// <returns>Never returns a value - the call is translated to SQL.</returns>
        public static T Min<T>(T value) => throw Marker();
        /// <summary>Translates to <c>MAX(<paramref name="value"/>)</c>.</summary>
        /// <typeparam name="T">The type of the compared expression.</typeparam>
        /// <param name="value">The expression to take the largest value of.</param>
        /// <returns>Never returns a value - the call is translated to SQL.</returns>
        public static T Max<T>(T value) => throw Marker();
        /// <summary>
        /// Translates to <c>value IN (subquery)</c>. Build <paramref name="subquery"/> from
        /// <see cref="FluentSqlQueryStage.Subquery"/> to correlate it with the outer query.
        /// </summary>
        /// <typeparam name="T">The type of the tested expression.</typeparam>
        /// <param name="value">The expression being tested for membership.</param>
        /// <param name="subquery">A query projecting exactly one column.</param>
        /// <returns>Never returns a value - the call is translated to SQL.</returns>
        public static bool In<T>(this T value, IFluentSqlQuery subquery) => throw Marker();
        /// <summary>Translates to <c>value NOT IN (subquery)</c>.</summary>
        /// <typeparam name="T">The type of the tested expression.</typeparam>
        /// <param name="value">The expression being tested for membership.</param>
        /// <param name="subquery">A query projecting exactly one column.</param>
        /// <returns>Never returns a value - the call is translated to SQL.</returns>
        public static bool NotIn<T>(this T value, IFluentSqlQuery subquery) => throw Marker();
        /// <summary>
        /// Inside a fluent SQL expression, translates to <c>value IN (...)</c> with one query parameter
        /// per element. Called anywhere else it behaves as written, testing membership in memory.
        /// </summary>
        /// <typeparam name="T">The type of the tested expression.</typeparam>
        /// <param name="value">The expression being tested for membership.</param>
        /// <param name="values">The values to test against.</param>
        /// <returns>Whether <paramref name="value"/> is one of <paramref name="values"/>, when evaluated in memory.</returns>
        public static bool In<T>(this T value, IEnumerable<T> values) => values.Contains(value);
        /// <summary>
        /// Inside a fluent SQL expression, translates to <c>value NOT IN (...)</c> with one query
        /// parameter per element. Called anywhere else it behaves as written, testing membership in memory.
        /// </summary>
        /// <typeparam name="T">The type of the tested expression.</typeparam>
        /// <param name="value">The expression being tested for membership.</param>
        /// <param name="values">The values to test against.</param>
        /// <returns>Whether <paramref name="value"/> is absent from <paramref name="values"/>, when evaluated in memory.</returns>
        public static bool NotIn<T>(this T value, IEnumerable<T> values) => !values.Contains(value);
        /// <summary>
        /// Translates to <c>EXISTS (subquery)</c>. Build <paramref name="subquery"/> from
        /// <see cref="FluentSqlQueryStage.Subquery"/> to correlate it with the outer query.
        /// </summary>
        /// <param name="subquery">The subquery whose rows are tested for.</param>
        /// <returns>Never returns a value - the call is translated to SQL.</returns>
        public static bool Exists(IFluentSqlQuery subquery) => throw Marker();
        /// <summary>Translates to <c>NOT EXISTS (subquery)</c>.</summary>
        /// <param name="subquery">The subquery whose absence of rows is tested for.</param>
        /// <returns>Never returns a value - the call is translated to SQL.</returns>
        public static bool NotExists(IFluentSqlQuery subquery) => throw Marker();
        /// <summary>Translates to <c>CASE WHEN ... THEN ... ELSE ... END</c>.</summary>
        /// <typeparam name="T">The type both branches produce.</typeparam>
        /// <param name="condition">The predicate tested by the WHEN clause.</param>
        /// <param name="whenTrue">The value produced when the condition holds.</param>
        /// <param name="whenFalse">The value produced otherwise.</param>
        /// <returns>Never returns a value - the call is translated to SQL.</returns>
        public static T Case<T>(bool condition, T whenTrue, T whenFalse) => throw Marker();

        /// <summary>
        /// Emits SQL the builder has no expression for - a vendor function, an operator, a cast.
        /// The text is used verbatim, so it is the caller's job to keep it valid for the database
        /// they are targeting; nothing here makes it portable.
        ///
        /// Placeholders are <c>string.Format</c> style: <c>{0}</c>, <c>{1}</c> and so on are
        /// replaced with the translated <paramref name="arguments"/>, and <c>{{</c> / <c>}}</c>
        /// produce a literal brace. Each argument is translated the same way any other builder
        /// expression is - they are read as expressions, never evaluated - so table aliases resolve
        /// correctly and captured values become query parameters rather than inlined text:
        ///
        /// <code>
        /// .Select(() => new
        /// {
        ///     Readings = FSql.Raw&lt;string&gt;(
        ///         "json_agg(json_build_object('value', {0}, 'at', {1}) ORDER BY {1})",
        ///         metric.Row.ValueNumber,
        ///         metric.Row.OccurredAt)
        /// })
        /// </code>
        ///
        /// The result is wrapped in parentheses so it composes safely inside a larger expression.
        /// </summary>
        /// <typeparam name="T">The type the fragment produces, used to map the value back.</typeparam>
        /// <param name="sql">SQL text with <c>{0}</c>-style placeholders. Must be a constant or captured string.</param>
        /// <param name="arguments">Expressions substituted into the placeholders. Write them
        /// inline: they are read as expressions rather than evaluated.</param>
        public static T Raw<T>(string sql, params object[] arguments) => throw Marker();

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
        ///     .SelectScalar(integration, x => FSql.Count());
        ///
        /// query.Select(() => new { site.Row.Name, Count = FSql.Scalar&lt;int&gt;(integrations) });
        /// </code>
        /// </summary>
        /// <typeparam name="T">The type the subquery yields.</typeparam>
        /// <param name="subquery">A query projecting exactly one column.</param>
        public static T Scalar<T>(IFluentSqlQuery subquery) => throw Marker();

        private static InvalidOperationException Marker() => new InvalidOperationException("SQL subquery methods can only be used in a fluent SQL expression.");
    }
}
