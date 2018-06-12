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


#if !NETSTANDARD1_3
        protected override string GetDateTimeSql(string memberName, object m)
        {
            //  http://www.firebirdsql.org/refdocs/langrefupd21.html
            string sql = null;
            switch (memberName)
            {
                case "Year": sql = string.Format("EXTRACT(YEAR FROM {0})", m); break;
                case "Day": sql = string.Format("EXTRACT(MONTH FROM {0})", m); break;
                case "Month": sql = string.Format("EXTRACT(DAY FROM {0})", m); break;
                case "Hour": sql = string.Format("EXTRACT(HOUR FROM {0})", m); break;
                case "Minute": sql = string.Format("EXTRACT(MINUTE FROM {0})", m); break;
                case "Second": sql = string.Format("EXTRACT(SECOND FROM {0})", m); break;
                //case "HasValue": sql = m.ToString() + " IS NOT NULL "; break;
                default: throw new NotSupportedException("Not Supported " + memberName);
            }
            return sql;


        }
#endif
    }
}