using System;

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

        protected override string GetDateTimeSql(string memberName, object m)
        {
            //  http://www.firebirdsql.org/refdocs/langrefupd21.html
            string sql;
            switch (memberName)
            {
                case "Year": sql = $"EXTRACT(YEAR FROM {m})"; break;
                case "Month": sql = $"EXTRACT(MONTH FROM {m})"; break;
                case "Day": sql = $"EXTRACT(DAY FROM {m})"; break;
                case "Hour": sql = $"EXTRACT(HOUR FROM {m})"; break;
                case "Minute": sql = $"EXTRACT(MINUTE FROM {m})"; break;
                case "Second": sql = $"EXTRACT(SECOND FROM {m})"; break;
                default: throw new NotSupportedException("Not Supported " + memberName);
            }
            return sql;
        }
    }
}