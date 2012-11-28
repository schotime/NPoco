using System.Data;
using System.Linq;

namespace NPoco.DatabaseTypes
{
    public class SqlServerDatabaseType : DatabaseType
    {
        public override string BuildPageQuery(long skip, long take, PagingHelper.SQLParts parts, ref object[] args)
        {
            parts.sqlSelectRemoved = "peta_inner.* FROM (SELECT " + PagingHelper.rxOrderBy.Replace(parts.sqlSelectRemoved, "", 1) + ") peta_inner";
            var sqlPage = string.Format("SELECT * FROM (SELECT ROW_NUMBER() OVER ({0}) peta_rn, {1}) peta_paged WHERE peta_rn>@{2} AND peta_rn<=@{3}",
                                        parts.sqlOrderBy ?? "ORDER BY (SELECT NULL /*poco_dual*/)", parts.sqlSelectRemoved, args.Length, args.Length + 1);
            args = args.Concat(new object[] { skip, skip + take }).ToArray();

            return sqlPage;
        }

        public override object ExecuteInsert(Database db, IDbCommand cmd, string primaryKeyName)
        {
            // Ah this doesn't work on SQL 2012 so using the normal method for getting the identity back out instead
            //cmd.CommandText = "DECLARE @idt table(id bigint);" + cmd.CommandText + ";SELECT id FROM @idt";
            cmd.CommandText = cmd.CommandText + ";SELECT @@IDENTITY;";
            return db.ExecuteScalarHelper(cmd);
        }

        public override string GetExistsSql()
        {
            return "IF EXISTS (SELECT 1 FROM {0} WHERE {1}) SELECT 1 ELSE SELECT 0";
        }

        /* Like the @idt stuff in EsxecuteInsert I cannot get any SQL generated using this statement to execute in SQL 2005 or 
         * SQL 2012 and give back the identity so using the standard @@identity code
         * 
        public override string GetInsertOutputClause(string primaryKeyName)
        {
            return String.Format(" OUTPUT INSERTED.[{0}] into @idt", primaryKeyName);
        }
        */
    }
}