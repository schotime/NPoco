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

        protected override string SubstringStatement(MemberAccessString quotedColName, int startIndex, int length = -1)
        {
            if (length >= 0)
                return string.Format("substring({0} FROM {1} FOR {2})", quotedColName, startIndex, length);
            else
                return string.Format("substring({0} FROM {1})", quotedColName, startIndex);
        }
    }
}