namespace NPoco.Expressions
{
    public class DefaultSqlExpression<T> : SqlExpression<T>
    {
        public DefaultSqlExpression(IDatabase database, PocoData pocoData, bool prefixTableName) : base(database, pocoData, prefixTableName)
        {
        }

        public DefaultSqlExpression(IDatabase database, PocoData pocoData) : base(database, pocoData, false)
        {
        }

        public DefaultSqlExpression(IDatabase database, bool prefixTableName)
            : this(database, database.PocoDataFactory.ForType(typeof(T)), prefixTableName)
        {
        }

        public DefaultSqlExpression(IDatabase database) 
            : this(database, database.PocoDataFactory.ForType(typeof(T)))
        {
        }
    }
}