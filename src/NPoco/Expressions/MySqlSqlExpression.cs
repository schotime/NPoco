namespace NPoco.Expressions
{
    public class MySqlSqlExpression<T> : SqlExpression<T>
    {
        public MySqlSqlExpression(IDatabase database, PocoData pocoData) : this(database, pocoData, false)
        {

        }

        public MySqlSqlExpression(IDatabase database, PocoData pocoData, bool prefixTableName) : base(database, pocoData, prefixTableName)
        {
            EscapeChar = "\\\\";
        }

        protected override string EscapeParam(object par)
        {
            var param = par.ToString().ToUpper();
            param = param
                .Replace("\\", EscapeChar + EscapeChar)
                .Replace("_", EscapeChar + "_");
            return param;
        }
    }
}