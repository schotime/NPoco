namespace NPoco.Expressions
{
    public class DefaultSqlExpression<T> : SqlExpression<T>
    {
        public DefaultSqlExpression(IDatabase database) : base(database)
        {
        }
    }
}