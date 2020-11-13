using System;
using System.Collections.Generic;
using System.Text;

namespace NPoco.Expressions
{
    public class OracleExpression<T> : SqlExpression<T>
    {
        public OracleExpression(IDatabase database, PocoData pocoData, bool prefixTableName) : base(database, pocoData, prefixTableName)
        {
        }

        protected override string GetDateTimeSql(string memberName, object m)
        {
            //Oracle
            // http://blog.csdn.net/gccr/article/details/1802740
            string sql;
            switch (memberName)
            {
                case "Year": sql = $"EXTRACT(YEAR FROM TIMESTAMP {m})"; break;
                case "Month": sql = $"EXTRACT(MONTH FROM TIMESTAMP {m})"; break;
                case "Day": sql = $"EXTRACT(DAY FROM TIMESTAMP {m})"; break;
                case "Hour": sql = $"EXTRACT(HOUR FROM TIMESTAMP {m})"; break;
                case "Minute": sql = $"EXTRACT(MINUTE FROM TIMESTAMP {m})"; break;
                case "Second": sql = $"EXTRACT(SECOND FROM TIMESTAMP {m})"; break;
                default: throw new NotSupportedException("Not Supported " + memberName);
            }
            return sql;
        }
    }
}
