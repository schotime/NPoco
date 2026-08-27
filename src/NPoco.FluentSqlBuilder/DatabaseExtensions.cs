namespace NPoco.FluentSqlBuilder
{
    public static class DatabaseExtensions
    {
        public static FluentSqlQuery FluentQuery(this IDatabase database) => new FluentSqlQuery(database);
    }
}
