using System.Linq;

namespace NPoco.DatabaseTypes
{
    public class SqlServer2012DatabaseType : SqlServer2008DatabaseType
    {
        public override string BuildPageQuery(long skip, long take, PagingHelper.SQLParts parts, ref object[] args)
        {
            var sqlPage = string.Format("{0}\nOFFSET @{1} ROWS FETCH NEXT @{2} ROWS ONLY", parts.sql, args.Length, args.Length + 1);
            args = args.Concat(new object[] { skip, take }).ToArray();
            return sqlPage;
        }
    }
}
