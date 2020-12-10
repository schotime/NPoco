using System;
using Microsoft.Data.SqlClient;
using System.Linq;

namespace NPoco.DatabaseTypes
{
    public class SqlServer2012DatabaseType : SqlServer2008DatabaseType
    {
        public static Database Create(string connectionString)
        {
            return new Database(connectionString, SqlServer2012, SqlClientFactory.Instance);
        }

        public override string BuildPageQuery(long skip, long take, PagingHelper.SQLParts parts, ref object[] args)
        {
            var sqlPage = string.Format("{0}{1}\nOFFSET @{2} ROWS FETCH NEXT @{3} ROWS ONLY", 
                parts.sql,
                !HasTopLevelOrderBy(parts.sql) ? "\nORDER BY (SELECT NULL)" : string.Empty,
                args.Length, 
                args.Length + 1);
            args = args.Concat(new object[] {skip, take}).ToArray();
            return sqlPage;
        } 

        public static bool HasTopLevelOrderBy(string sql)
        {
            var indent = 0;
            for (int i = sql.Length - 1; i >= 0; i--)
            {
                if (i >= 7)
                {
                    if (indent == 0
                        && (sql[i - 7] == 'o' || sql[i - 7] == 'O')
                        && (sql[i - 6] == 'r' || sql[i - 6] == 'R')
                        && (sql[i - 5] == 'd' || sql[i - 5] == 'D')
                        && (sql[i - 4] == 'e' || sql[i - 4] == 'E')
                        && (sql[i - 3] == 'r' || sql[i - 3] == 'R')
                        && (sql[i - 2] == ' ')
                        && (sql[i - 1] == 'b' || sql[i - 1] == 'B')
                        && (sql[i]     == 'y' || sql[i]     == 'Y'))
                    {
                        return true;
                    }
                }

                if (sql[i] == ')')
                    indent++;
                else if (sql[i] == '(')
                    indent--;
            }

            return false;
        }

        public override string? GetAutoIncrementExpression(TableInfo ti)
        {
            if (!string.IsNullOrEmpty(ti.SequenceName))
                return $"NEXT VALUE FOR {ti.SequenceName}";

            return null;
        }
    }
}
