using System;
using System.Linq;

namespace NPoco.DatabaseTypes
{
    public class SqlServer2012DatabaseType : SqlServer2008DatabaseType
    {
        public override string BuildPageQuery(long skip, long take, PagingHelper.SQLParts parts, ref object[] args)
        {
            if (!parts.sql.ToLower().Contains("order by")) throw new Exception("SQL Server 2012 Paging via OFFSET requires an ORDER BY statement.");

            var sqlPage = string.Format("{0}\nOFFSET @{1} ROWS FETCH NEXT @{2} ROWS ONLY", parts.sql, args.Length, args.Length + 1);
            args = args.Concat(new object[] { skip, take }).ToArray();
            return sqlPage;
        }
    }
}
