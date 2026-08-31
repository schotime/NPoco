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
        public static FluentSqlQuery FluentQuery(this IDatabase database) => new FluentSqlQuery(database);
    }
}
