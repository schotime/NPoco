namespace NPoco.Expressions
{
    public class DefaultSqlExpressionVisitor<T> : SqlExpressionVisitor<T>
    {
        public DefaultSqlExpressionVisitor(Database database, PocoData pocoData) : base(database, pocoData)
        {
        }
    }
}