namespace NPoco.Expressions
{
    public class DefaultSqlExpression<T> : SqlExpression<T>
    {
        public DefaultSqlExpression(IDatabase database, PocoData pocoData) : base(database, pocoData)
        {
        }
    }
}