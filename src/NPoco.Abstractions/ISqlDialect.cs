using System.Collections.Generic;

namespace NPoco
{
    /// <summary>The parts of a date a query can ask for.</summary>
    public enum SqlDatePart
    {
        Year,
        Month,
        Day,
        Hour,
        Minute,
        Second
    }

    /// <summary>
    /// The SQL fragments that differ between databases. Every <see cref="DatabaseType"/> is one of
    /// these, so a query builder asks the database it is targeting rather than sniffing its
    /// provider name, and a new database only has to override what it actually spells differently.
    /// </summary>
    public interface ISqlDialect
    {
        /// <summary>Joins values into one string, e.g. <c>(a || b)</c>.</summary>
        string Concat(IReadOnlyList<string> values);

        /// <summary>The length of a string value.</summary>
        string StringLength(string value);

        /// <summary>Upper-cases a string value.</summary>
        string Upper(string value);

        /// <summary>Lower-cases a string value.</summary>
        string Lower(string value);

        /// <summary>
        /// Part of a string, one-based as SQL counts.
        /// </summary>
        /// <param name="value">The string to take from.</param>
        /// <param name="startIndex">One-based position to start at.</param>
        /// <param name="length">How much to take, or null for everything that is left.</param>
        string Substring(string value, string startIndex, string length);

        /// <summary>Trims whitespace from one or both ends.</summary>
        string Trim(string value, bool start, bool end);

        /// <summary>A case-insensitive LIKE, escaping with <see cref="LikeEscapeCharacter"/>.</summary>
        string Like(string value, string pattern);

        /// <summary>The character a LIKE pattern escapes wildcards with.</summary>
        string LikeEscapeCharacter { get; }

        /// <summary>Reads one part out of a date.</summary>
        string DatePart(SqlDatePart part, string value);

        /// <summary>Adds a number of <paramref name="part"/>s to a date.</summary>
        string DateAdd(SqlDatePart part, string value, string increment);

        string Absolute(string value);
        string Floor(string value);
        string Ceiling(string value);

        /// <summary>Rounds to <paramref name="digits"/> decimal places, or to a whole number when null.</summary>
        string Round(string value, string digits);

        /// <summary>
        /// A correlated join against a subquery: <c>OUTER APPLY</c>, or the <c>LEFT JOIN LATERAL</c>
        /// that databases without APPLY spell it as.
        /// </summary>
        string OuterApply(string nestedSql, string alias);

        /// <summary>
        /// The prefix that limits a query to the first rows when nothing is skipped - SQL Server's
        /// <c>TOP (n)</c> - or null for databases that page with a trailing clause instead.
        /// </summary>
        string TakeOnlyPrefix(long take);

        /// <summary>Turns a statement into a request for its execution plan.</summary>
        string ExplainStatement(string sql);
    }
}
