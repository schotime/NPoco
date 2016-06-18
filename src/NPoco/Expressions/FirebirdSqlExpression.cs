namespace NPoco.Expressions
{
    public class FirebirdSqlExpression<T> : SqlExpression<T>
    {
        public FirebirdSqlExpression(IDatabase database, PocoData pocoData, bool prefixTableName) : base(database, pocoData, prefixTableName)
        {
        }

        public FirebirdSqlExpression(IDatabase database, PocoData pocoData) : base(database, pocoData, false)
        {
        }

        protected override string SubstringStatement(PartialSqlString columnName, int startIndex, int length)
        {
            // Substring function doesn't work with parameters
            if (length >= 0)
                return string.Format("substring({0} FROM {1} FOR {2})", columnName, startIndex, length);
            else
                return string.Format("substring({0} FROM {1})", columnName, startIndex);
        }
    }
}