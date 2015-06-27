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

        public static string AddSelectClause(Database database, Type type, string sql)
        {
            if (sql.StartsWith(";"))
                return sql.Substring(1);

            if (!rxSelect.IsMatch(sql))
            {
                var pd = database.PocoDataFactory.ForType(type);
                var tableName = database.DatabaseType.EscapeTableName(pd.TableInfo.TableName);
                var columns = pd.QueryColumns.Select(c =>
                {
                    return database.DatabaseType.EscapeSqlIdentifier(c.Value.ColumnName) +
                           (!string.IsNullOrEmpty(c.Value.ColumnAlias)
                                ? " AS " + database.DatabaseType.EscapeSqlIdentifier(c.Value.ColumnAlias)
                                : " AS " + database.DatabaseType.EscapeSqlIdentifier(c.Value.MemberInfoKey));
                });
                string cols = String.Join(", ", columns.ToArray());
                if (!rxFrom.IsMatch(sql))
                    sql = String.Format("SELECT {0} FROM {1} {2}", cols, tableName, sql);
                else
                    sql = String.Format("SELECT {0} {1}", cols, sql);
            }
            return sql;
        }
    }
}
