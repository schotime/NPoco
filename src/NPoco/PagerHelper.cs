using System;
using System.Data;
using System.Text.RegularExpressions;

namespace NPoco
{
    public class PagingHelper
    {
        public static Regex rxColumns = new Regex(@"\A\s*SELECT\s+((?:\((?>\((?<depth>)|\)(?<-depth>)|.?)*(?(depth)(?!))\)|.)*?)(?<!,\s+)\bFROM\b", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.Compiled);
        public static Regex rxOrderBy = new Regex(@"\bORDER\s+BY\s+(?:\((?>\((?<depth>)|\)(?<-depth>)|.?)*(?(depth)(?!))\)|[\w\(\)\.\[\]""`])+(?:\s+(?:ASC|DESC))?(?:\s*,\s*(?:\((?>\((?<depth>)|\)(?<-depth>)|.?)*(?(depth)(?!))\)|[\w\(\)\.\[\]""`])+(?:\s+(?:ASC|DESC))?)*", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.Compiled);

        public struct SQLParts
        {
            public string sql;
            public string sqlCount;
            public string sqlSelectRemoved;
            public string sqlOrderBy;
            public string sqlUnordered;
        }

        public static bool SplitSQL(string sql, out SQLParts parts)
        {
            parts.sql = sql;
            parts.sqlSelectRemoved = null;
            parts.sqlCount = null;
            parts.sqlOrderBy = null;
            parts.sqlUnordered = sql.Trim().Trim(';');

            // Extract the columns from "SELECT <whatever> FROM"
            var m = rxColumns.Match(sql);
            if (!m.Success) return false;

            // Save column list  [and replace with COUNT(*)]
            Group g = m.Groups[1];
            parts.sqlSelectRemoved = sql.Substring(g.Index);

            // Look for the last "ORDER BY <whatever>" clause not part of a ROW_NUMBER expression
            m = rxOrderBy.Match(parts.sql);
            if (m.Success)
            {
                g = m.Groups[0];
                parts.sqlOrderBy = g.ToString();
                parts.sqlUnordered = parts.sqlUnordered.Replace(parts.sqlOrderBy, "");
            }

            parts.sqlCount = string.Format(@"SELECT COUNT(*) FROM ({0}) peta_tbl", parts.sqlUnordered);

            return true;
        }
    }
}
