using System;

namespace NPoco.Expressions
{
    public class SQLiteExpression<T> : SqlExpression<T>
    {
        public SQLiteExpression(IDatabase database, PocoData pocoData, bool prefixTableName) : base(database, pocoData, prefixTableName)
        {
        }

        protected override string GetDateTimeSql(string memberName, object m)
        {
            // http://www.sqlite.org/lang_corefunc.html
            string sql = null;
            switch (memberName)
            {
                case "Year": sql = string.Format("CAST(STRFTIME('%Y',{0}) AS INT)", m); break;
                case "Month": sql = string.Format("CAST(STRFTIME('%m',{0}) AS INT)", m); break;
                case "Day": sql = string.Format("CAST(STRFTIME('%d',{0}) AS INT)", m); break;
                case "Hour": sql = string.Format("CAST(STRFTIME('%H',{0}) AS INT)", m); break;
                case "Minute": sql = string.Format("CAST(STRFTIME('%M',{0}) AS INT)", m); break;
                case "Second": sql = string.Format("CAST(STRFTIME('%S',{0}) AS INT)", m); break;
                //case "HasValue": sql = m.ToString() + " IS NOT NULL "; break;
                default: throw new NotSupportedException("Not Supported " + memberName);
            }
            return sql;
        }
    }

}