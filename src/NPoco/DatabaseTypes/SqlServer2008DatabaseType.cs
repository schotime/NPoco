using System.Data;
using System.Linq;

namespace NPoco.DatabaseTypes
{
    public class SqlServer2008DatabaseType : SqlServerDatabaseType
    {
        public override object ExecuteInsert(Database db, IDbCommand cmd, string primaryKeyName)
        {
            // Ah this doesn't work on SQL 2012 so using the normal method for getting the identity back out instead
            //cmd.CommandText = "DECLARE @idt table(id bigint);" + cmd.CommandText + ";SELECT id FROM @idt";
            cmd.CommandText = cmd.CommandText + ";SELECT SCOPE_IDENTITY();";
            return db.ExecuteScalarHelper(cmd);
        }

        public override string BuildPageQuery(long skip, long take, PagingHelper.SQLParts parts, ref object[] args)
        {
            var sqlPage = string.Format("{0}\nOFFSET @{1} ROWS FETCH NEXT @{2} ROWS ONLY", parts.sql, args.Length, args.Length + 1);
            args = args.Concat(new object[] { skip, take }).ToArray();
            return sqlPage;
        }
    }
}