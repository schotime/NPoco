namespace NPoco.FluentSql
{
    /// <summary>
    /// Entry point of the fluent SQL builder.
    /// </summary>
    public static class DatabaseExtensions
    {
        /// <summary>
        /// Starts a fluent query against this database. It is written from the
        /// <see cref="FluentSqlQuery.From{T}(out TableReference{T})"/> call that follows, and executed
        /// through the <see cref="FluentSqlResult{TResult}"/> one of the Select methods hands back.
        /// </summary>
        /// <param name="database">The database the query is built for and run against.</param>
        /// <returns>An empty query, awaiting its FROM.</returns>
        public static FluentSqlQuery FluentQuery(this IDatabaseQuery database) => new FluentSqlQuery(database);

        /// <summary>
        /// Starts a fluent query from an async-query database reference. Results built from this
        /// entry point expose async execution methods only.
        /// </summary>
        /// <param name="database">The database the query is built for and run against.</param>
        /// <returns>An empty query, awaiting its FROM.</returns>
        public static FluentSqlAsyncQuery FluentQuery(this IAsyncQueryDatabase database)
        {
            if (database == null) throw new System.ArgumentNullException(nameof(database));
            return new FluentSqlAsyncQuery(new FluentSqlQuery(database));
        }
    }
}
