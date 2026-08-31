using System.Collections.Generic;

namespace NPoco
{
    /// <summary>
    /// A dialect that starts from standard SQL and names only its differences. Every shipped
    /// dialect derives from this, and so should one written for a database NPoco does not ship.
    /// </summary>
    public abstract class SqlDialect : ISqlDialect
    {
        public virtual string Concat(IReadOnlyList<string> values) => "(" + string.Join(" || ", values) + ")";

        public virtual string StringLength(string value) => "LENGTH(" + value + ")";

        public virtual string Upper(string value) => "UPPER(" + value + ")";

        public virtual string Lower(string value) => "LOWER(" + value + ")";

        public virtual string Substring(string value, string startIndex, string length)
            => length == null
                ? "SUBSTR(" + value + ", " + startIndex + ")"
                : "SUBSTR(" + value + ", " + startIndex + ", " + length + ")";

        public virtual string Trim(string value, bool start, bool end)
        {
            if (start && end) return "TRIM(" + value + ")";
            return start ? "LTRIM(" + value + ")" : "RTRIM(" + value + ")";
        }

        public virtual string LikeEscapeCharacter => "!";

        /// <summary>
        /// Case-folded on both sides, so the comparison behaves the same whatever collation the
        /// column happens to have. A database whose collation is already case-insensitive can drop
        /// the fold - it keeps an index on the column usable - by overriding this.
        /// </summary>
        public virtual string Like(string value, string pattern)
            => Upper(value) + " LIKE " + pattern + " ESCAPE '" + LikeEscapeCharacter + "'";

        /// <summary>
        /// Not standard SQL, but what the builder emitted for any database it did not recognise
        /// before dialects existed, so an unrecognised one keeps behaving as it did.
        /// </summary>
        public virtual string DatePart(SqlDatePart part, string value)
            => "DATEPART(" + part.ToString().ToLowerInvariant() + ", " + value + ")";

        public virtual string DateAdd(SqlDatePart part, string value, string increment)
            => throw new System.NotSupportedException("Date addition is not supported by this database provider.");

        public virtual string Absolute(string value) => "ABS(" + value + ")";

        public virtual string Floor(string value) => "FLOOR(" + value + ")";

        public virtual string Ceiling(string value) => "CEIL(" + value + ")";

        public virtual string Round(string value, string digits)
            => digits == null ? "ROUND(" + value + ")" : "ROUND(" + value + ", " + digits + ")";

        public virtual string OuterApply(string nestedSql, string alias)
            => "OUTER APPLY (\n" + nestedSql + "\n) " + alias;

        /// <summary>Null: most databases page with a trailing LIMIT/OFFSET clause instead.</summary>
        public virtual string TakeOnlyPrefix(long take) => null;

        public virtual string ExplainStatement(string sql) => "EXPLAIN " + sql;
    }

    /// <summary>Standard SQL, used for any database with no dialect of its own.</summary>
    public sealed class AnsiSqlDialect : SqlDialect
    {
        public static readonly AnsiSqlDialect Instance = new AnsiSqlDialect();
    }

    public class SqlServerSqlDialect : SqlDialect
    {
        public static readonly SqlServerSqlDialect Instance = new SqlServerSqlDialect();

        public override string Concat(IReadOnlyList<string> values) => "(" + string.Join(" + ", values) + ")";

        public override string StringLength(string value) => "LEN(" + value + ")";

        // SQL Server has no two-argument form, so a substring with no length has to name one. 8000
        // rather than LEN(value): LEN does not count trailing spaces, which would cut a CHAR
        // column's padding off the result.
        public override string Substring(string value, string startIndex, string length)
            => "SUBSTRING(" + value + ", " + startIndex + ", " + (length ?? "8000") + ")";

        // TRIM arrived in SQL Server 2017, and NPoco still ships database types for 2005 onwards.
        public override string Trim(string value, bool start, bool end)
        {
            if (start && end) return "LTRIM(RTRIM(" + value + "))";
            return base.Trim(value, start, end);
        }

        public override string DatePart(SqlDatePart part, string value)
            => "DATEPART(" + part.ToString().ToLowerInvariant() + ", " + value + ")";

        public override string DateAdd(SqlDatePart part, string value, string increment)
            => "DATEADD(" + part.ToString().ToLowerInvariant() + ", " + increment + ", " + value + ")";

        public override string Ceiling(string value) => "CEILING(" + value + ")";

        public override string TakeOnlyPrefix(long take) => "TOP (" + take + ") ";

        public override string ExplainStatement(string sql) => "SET SHOWPLAN_ALL ON;\n" + sql;
    }

    /// <summary>
    /// SQL Server Compact: a T-SQL subset, but not one the builder ever treated as SQL Server - it
    /// has no APPLY, and the builder recognised it by a provider name that says SqlServerCe rather
    /// than SqlClient. So it starts from standard SQL and names only the T-SQL spellings it needs.
    /// </summary>
    public class SqlServerCeSqlDialect : SqlDialect
    {
        public static readonly SqlServerCeSqlDialect Instance = new SqlServerCeSqlDialect();

        public override string StringLength(string value) => "LEN(" + value + ")";

        public override string Substring(string value, string startIndex, string length)
            => "SUBSTRING(" + value + ", " + startIndex + ", " + (length ?? "8000") + ")";

        // No TRIM in Compact, the same as the SQL Server versions it was built alongside.
        public override string Trim(string value, bool start, bool end)
        {
            if (start && end) return "LTRIM(RTRIM(" + value + "))";
            return base.Trim(value, start, end);
        }

        public override string Ceiling(string value) => "CEILING(" + value + ")";
    }

    public class SqliteSqlDialect : SqlDialect
    {
        public static readonly SqliteSqlDialect Instance = new SqliteSqlDialect();

        public override string DatePart(SqlDatePart part, string value)
        {
            var format = part == SqlDatePart.Year ? "%Y"
                : part == SqlDatePart.Month ? "%m"
                : part == SqlDatePart.Day ? "%d"
                : part == SqlDatePart.Hour ? "%H"
                : part == SqlDatePart.Minute ? "%M"
                : "%S";
            return "CAST(strftime('" + format + "', " + value + ") AS INTEGER)";
        }

        public override string DateAdd(SqlDatePart part, string value, string increment)
            => "datetime(" + value + ", " + increment + " || ' " + part.ToString().ToLowerInvariant() + "')";
    }

    public class MySqlSqlDialect : SqlDialect
    {
        public static readonly MySqlSqlDialect Instance = new MySqlSqlDialect();

        public override string Concat(IReadOnlyList<string> values) => "CONCAT(" + string.Join(", ", values) + ")";

        public override string Substring(string value, string startIndex, string length)
            => length == null
                ? "SUBSTRING(" + value + ", " + startIndex + ")"
                : "SUBSTRING(" + value + ", " + startIndex + ", " + length + ")";

        public override string DatePart(SqlDatePart part, string value)
            => part.ToString().ToUpperInvariant() + "(" + value + ")";

        public override string DateAdd(SqlDatePart part, string value, string increment)
            => "DATE_ADD(" + value + ", INTERVAL " + increment + " " + part.ToString().ToUpperInvariant() + ")";

        // MySQL has no APPLY; a lateral join does the same job.
        public override string OuterApply(string nestedSql, string alias)
            => "LEFT JOIN LATERAL (\n" + nestedSql + "\n) " + alias + " ON TRUE";
    }

    public class PostgreSqlDialect : SqlDialect
    {
        public static readonly PostgreSqlDialect Instance = new PostgreSqlDialect();

        public override string DatePart(SqlDatePart part, string value)
            => "EXTRACT(" + part.ToString().ToUpperInvariant() + " FROM " + value + ")";

        public override string DateAdd(SqlDatePart part, string value, string increment)
            => "(" + value + " + (" + increment + " * INTERVAL '1 " + part.ToString().ToLowerInvariant() + "'))";

        // Postgres spells APPLY as a lateral join.
        public override string OuterApply(string nestedSql, string alias)
            => "LEFT JOIN LATERAL (\n" + nestedSql + "\n) " + alias + " ON TRUE";
    }

    public class OracleSqlDialect : SqlDialect
    {
        public static readonly OracleSqlDialect Instance = new OracleSqlDialect();

        public override string DatePart(SqlDatePart part, string value)
            => "EXTRACT(" + part.ToString().ToUpperInvariant() + " FROM CAST(" + value + " AS TIMESTAMP))";

        public override string DateAdd(SqlDatePart part, string value, string increment)
        {
            if (part == SqlDatePart.Month) return "ADD_MONTHS(" + value + ", " + increment + ")";
            if (part == SqlDatePart.Year) return "ADD_MONTHS(" + value + ", (" + increment + " * 12))";
            if (part == SqlDatePart.Day) return "(" + value + " + " + increment + ")";

            // What is left is a fraction of a day.
            var divisor = part == SqlDatePart.Hour ? "24" : part == SqlDatePart.Minute ? "1440" : "86400";
            return "(" + value + " + (" + increment + " / " + divisor + "))";
        }

        public override string ExplainStatement(string sql) => "EXPLAIN PLAN FOR " + sql;
    }

    public class FirebirdSqlDialect : SqlDialect
    {
        public static readonly FirebirdSqlDialect Instance = new FirebirdSqlDialect();

        public override string DatePart(SqlDatePart part, string value)
            => "EXTRACT(" + part.ToString().ToUpperInvariant() + " FROM " + value + ")";

        public override string DateAdd(SqlDatePart part, string value, string increment)
            => "DATEADD(" + part.ToString().ToUpperInvariant() + ", " + increment + ", " + value + ")";

        public override string Ceiling(string value) => "CEILING(" + value + ")";
    }
}
