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
                .Replace("\\", EscapeChar + EscapeChar)
                .Replace("_", EscapeChar + "_");
            return param;
        }

        protected override string GetDateTimeSql(string memberName, object m)
        {
            string sql = null;
            switch (memberName)
            {
                case "Year": sql = string.Format("YEAR({0})", m); break;
                case "Month": sql = string.Format("MONTH({0})", m); break;
                case "Day": sql = string.Format("DAY({0})", m); break;
                case "Hour": sql = string.Format("HOUR({0})", m); break;
                case "Minute": sql = string.Format("MINUTE({0})", m); break;
                case "Second": sql = string.Format("SECOND({0})", m); break;
                //case "HasValue": sql = m.ToString() + " IS NOT NULL "; break;
                default: throw new NotSupportedException("Not Supported " + memberName);
            }
            return sql;
        }


    }
}