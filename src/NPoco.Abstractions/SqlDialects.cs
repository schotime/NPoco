using System;

namespace NPoco
{
    /// <summary>
    /// The one place a query builder asks which SQL dialect it is writing for, and the one place
    /// that answer can be replaced.
    ///
    /// A database supplies its own dialect by implementing <see cref="ISqlDialect"/> or - the usual
    /// way - by being a <see cref="IDatabaseType"/> that exposes one. Anything else falls back to
    /// <see cref="AnsiSqlDialect"/>. Set <see cref="Resolver"/> to override that for every query in
    /// the process: to teach the builder a database NPoco does not ship, or to change how an
    /// existing one spells a function without subclassing its database type.
    /// </summary>
    public static class SqlDialects
    {
        private static Func<IDatabaseType, ISqlDialect> _resolver;

        /// <summary>
        /// Resolves the dialect for a database. Returning null falls through to the default
        /// resolution, so an override only has to answer for the databases it cares about.
        /// </summary>
        public static Func<IDatabaseType, ISqlDialect> Resolver
        {
            get { return _resolver; }
            set { _resolver = value; }
        }

        /// <summary>The dialect to write SQL for this database in. Never null.</summary>
        public static ISqlDialect For(IDatabaseType databaseType)
        {
            if (databaseType == null) return AnsiSqlDialect.Instance;

            var resolver = _resolver;
            if (resolver != null)
            {
                var resolved = resolver(databaseType);
                if (resolved != null) return resolved;
            }

            var provider = databaseType as ISqlDialectProvider;
            if (provider != null && provider.SqlDialect != null) return provider.SqlDialect;

            var dialect = databaseType as ISqlDialect;
            if (dialect != null) return dialect;

            return ForProviderName(databaseType.GetProviderName());
        }

        /// <summary>
        /// The dialect a provider name names. How the builder picked its SQL before dialects
        /// existed, kept for a database type that supplies no dialect of its own - a third-party
        /// one written against an older NPoco - so it goes on being written the same SQL.
        /// </summary>
        public static ISqlDialect ForProviderName(string providerName)
        {
            var provider = (providerName ?? string.Empty).ToLowerInvariant();

            // MySQL first: its provider name has historically been read before SqlClient, since
            // MySql.Data.MySqlClient contains both.
            if (provider.Contains("mysql")) return MySqlSqlDialect.Instance;
            if (provider.Contains("npgsql")) return PostgreSqlDialect.Instance;
            if (provider.Contains("sqlite")) return SqliteSqlDialect.Instance;
            if (provider.Contains("oracle")) return OracleSqlDialect.Instance;
            if (provider.Contains("firebird")) return FirebirdSqlDialect.Instance;
            if (provider.Contains("sqlserverce")) return SqlServerCeSqlDialect.Instance;
            if (provider.Contains("sqlclient")) return SqlServerSqlDialect.Instance;

            return AnsiSqlDialect.Instance;
        }
    }

    /// <summary>
    /// Implemented by a database type that knows its own dialect - the single member a new
    /// database has to add, rather than an override per SQL function.
    /// </summary>
    public interface ISqlDialectProvider
    {
        /// <summary>The dialect this database writes SQL in.</summary>
        ISqlDialect SqlDialect { get; }
    }
}
