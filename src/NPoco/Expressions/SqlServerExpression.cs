namespace NPoco.Expressions
{
    public class SqlServerExpression<T> : SqlExpression<T>
    {
        public SqlServerExpression(IDatabase database, PocoData pocoData, bool prefixTableName)
            : base(database, pocoData, prefixTableName)
        { }

        protected override string TrimStatement(PartialSqlString expression)
        {
            return string.Format("ltrim(rtrim({0}))", expression);
        }
    }
}
