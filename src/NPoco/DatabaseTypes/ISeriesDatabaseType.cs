using System;
using System.Data;

namespace NPoco.DatabaseTypes
{
    public class ISeriesDatabaseType : DatabaseType
    {
        public override string GetParameterPrefix(string connectionString)
        {
            return "@";
        }

        public override void PreExecute(IDbCommand cmd)
        {
            cmd.CommandText = cmd.CommandText.Replace("/*poco_dual*/", "from sysibm.sysdummy1");
        }

        public override string BuildPageQuery(long skip, long take, PagingHelper.SQLParts parts, ref object[] args)
        {
            if (parts.sqlSelectRemoved.StartsWith("*"))
                throw new Exception("Query must alias '*' when performing a paged query.\neg. select t.* from table t order by t.id");

            // Same deal as SQL Server
            return Singleton<SqlServerDatabaseType>.Instance.BuildPageQuery(skip, take, parts, ref args);
        }

        public override string EscapeSqlIdentifier(string str)
        {
            return string.Format("{0}", str.ToUpperInvariant());
        }

        public override string GetAutoIncrementExpression(TableInfo ti)
        {
            if (!string.IsNullOrEmpty(ti.SequenceName))
                return string.Format("{0}.nextval", ti.SequenceName);

            return null;
        }

        public override object ExecuteInsert<T>(Database db, IDbCommand cmd, string primaryKeyName, T poco, object[] args)
        {
            if (primaryKeyName != null)
            {
                cmd.CommandText = string.Format("SELECT {0} FROM FINAL TABLE ({1})",
                    EscapeSqlIdentifier(primaryKeyName), cmd.CommandText);

                object retValue = db.ExecuteScalarHelper(cmd);
                Console.WriteLine("RETVALUE = " + retValue);
                return retValue;
            }

            db.ExecuteNonQueryHelper(cmd);
            return -1;
        }

        public override string GetProviderName()
        {
            return "IBM.Data.DB2.iSeries";
        }
    }
}
