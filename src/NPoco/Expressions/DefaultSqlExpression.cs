namespace NPoco.Expressions
{
    public class DefaultSqlExpression<T> : SqlExpression<T>
    {
        public DefaultSqlExpression(IDatabase database, bool prefixTableName) : base(database, prefixTableName)
        {
        }

        public DefaultSqlExpression(IDatabase database) : base(database, false)
        {
        }
    }
}