using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace NPoco
{
    public class AutoSelectHelper
    {
        private static Regex rxSelect = new Regex(@"\A\s*(SELECT|EXECUTE|CALL|EXEC)\s", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Multiline);
        private static Regex rxFrom = new Regex(@"\A\s*FROM\s", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Multiline);

        public static string AddSelectClause<T>(Database database, string sql)
        {
            if (sql.StartsWith(";"))
                return sql.Substring(1);

            if (!rxSelect.IsMatch(sql))
            {
                var pd = PocoData.ForType(typeof(T), database.PocoDataFactory);
                var tableName = database.DatabaseType.EscapeTableName(pd.TableInfo.TableName);
                string cols = String.Join(", ", (from c in pd.QueryColumns select database.DatabaseType.EscapeSqlIdentifier(c)).ToArray());
                if (!rxFrom.IsMatch(sql))
                    sql = String.Format("SELECT {0} FROM {1} {2}", cols, tableName, sql);
                else
                    sql = String.Format("SELECT {0} {1}", cols, sql);
            }
            return sql;
        }
    }
}
