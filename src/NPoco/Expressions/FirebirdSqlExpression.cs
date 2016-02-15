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
            // Substring values don't work with parameters
            if (length >= 0)
                return string.Format("substring({0} FROM {1} FOR {2})", columnName, startIndex, length);
            else
                return string.Format("substring({0} FROM {1})", columnName, startIndex); 
        }

        protected override object GetTrueExpression()
        {
            return new PartialSqlString("(1=1)");
        }

        protected override object GetFalseExpression()
        {
            return new PartialSqlString("(0=1)");
        }

        protected override object GetQuotedTrueValue()
        {
            return CreateParam(1);
        }

        protected override object GetQuotedFalseValue()
        {
            return CreateParam(0);
        }
    }
}