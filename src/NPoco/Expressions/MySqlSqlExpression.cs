using System;

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
                .Replace("\\", EscapeChar)
                .Replace("_", "\\_")
                .Replace("%", "\\%");
            return param;
        }

        protected override string GetDateTimeSql(string memberName, object m)
        {
            string sql;
            switch (memberName)
            {
                case "Year": sql = $"YEAR({m})"; break;
                case "Month": sql = $"MONTH({m})"; break;
                case "Day": sql = $"DAY({m})"; break;
                case "Hour": sql = $"HOUR({m})"; break;
                case "Minute": sql = $"MINUTE({m})"; break;
                case "Second": sql = $"SECOND({m})"; break;
                default: throw new NotSupportedException("Not Supported " + memberName);
            }
            return sql;
        }
    }
}