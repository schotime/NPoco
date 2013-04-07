namespace NPoco.Expressions
{
    public class DefaultSqlExpression<T> : SqlExpression<T>
    {
        public DefaultSqlExpression(Database database, PocoData pocoData) : base(database, pocoData)
        {
        }
    }
}