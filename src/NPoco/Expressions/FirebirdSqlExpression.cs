namespace NPoco.Expressions
{
    public class FirebirdSqlExpression<T> : SqlExpression<T>
    {
        public FirebirdSqlExpression(IDatabase database, bool prefixTableName) : base(database, prefixTableName)
        {
        }

        public FirebirdSqlExpression(IDatabase database) : base(database, false)
        {
        }

        protected override string SubstringStatement(PartialSqlString columnName, int startIndex, int length)
        {
            if (length >= 0)
                return string.Format("substring({0} FROM {1} FOR {2})", columnName, CreateParam(startIndex), CreateParam(length));
            else
                return string.Format("substring({0} FROM {1})", columnName, CreateParam(startIndex));
        }
    }
}