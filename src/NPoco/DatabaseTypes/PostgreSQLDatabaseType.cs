using System.Collections.Generic;
using System.Data;

namespace NPoco.DatabaseTypes
{
    public class PostgreSQLDatabaseType : DatabaseType
    {
        public override object MapParameterValue(object value)
        {
            // Don't map bools to ints in PostgreSQL
            if (value is bool) return value;

            return base.MapParameterValue(value);
        }

        public override string EscapeSqlIdentifier(string str)
        {
            return string.Format("\"{0}\"", str);
        }

        public override object ExecuteInsert<T>(Database db, IDbCommand cmd, string primaryKeyName, IEnumerable<string> outputColumns, T poco1, object[] args)
        {
            if (primaryKeyName != null)
            {
                cmd.CommandText += string.Format(" returning {0} as NewID", EscapeSqlIdentifier(primaryKeyName));
                return db.ExecuteScalarHelper(cmd);
            }

            db.ExecuteNonQueryHelper(cmd);
            return -1;
        }

        public override string GetProviderName()
        {
            return "Npgsql2";
        }
    }
}