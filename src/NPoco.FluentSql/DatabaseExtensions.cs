namespace NPoco.FluentSql
{
    public static class DatabaseExtensions
    {
        public static FluentSqlQuery FluentQuery(this IDatabase database) => new FluentSqlQuery(database);
    }
}
