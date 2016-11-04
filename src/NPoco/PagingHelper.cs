using System;
using System.Data;
using System.Text.RegularExpressions;

namespace NPoco
{
    public class PagingHelper
    {
        public static Regex rxColumns = new Regex(@"\A\s*SELECT\s+((?:\((?>\((?<depth>)|\)(?<-depth>)|.?)*(?(depth)(?!))\)|.)*?)(?<!,\s+)\bFROM\b", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.Compiled);
        public static Regex rxOrderBy = new Regex(@"(?!.*(?:\s+FROM[\s\(]+))ORDER\s+BY\s+([\w\.\[\]\(\)\s""`,]+)(?!.*\))", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.Compiled);

        public struct SQLParts
        {
            public string sql;
            public string sqlCount;
            public string sqlSelectRemoved;
            public string sqlOrderBy;
            public string sqlUnordered;
            public string sqlColumns;
        }

        public static bool SplitSQL(string sql, out SQLParts parts)
        {
            parts.sql = sql;
            parts.sqlSelectRemoved = null;
            parts.sqlCount = null;
            parts.sqlOrderBy = null;
            parts.sqlUnordered = sql.Trim().Trim(';');
            parts.sqlColumns = "*";

            // Extract the columns from "SELECT <whatever> FROM"
            var m = rxColumns.Match(sql);
            if (!m.Success) return false;

            // Save column list  [and replace with COUNT(*)]
            Group g = m.Groups[1];
            parts.sqlSelectRemoved = sql.Substring(g.Index);

            // Look for the last "ORDER BY <whatever>" clause not part of a ROW_NUMBER expression
            var matches = rxOrderBy.Matches(parts.sqlUnordered);
            if (matches.Count > 0)
            {
                m = matches[matches.Count - 1];
                g = m.Groups[0];
                parts.sqlOrderBy = g.ToString();
                parts.sqlUnordered = rxOrderBy.Replace(parts.sqlUnordered, "", 1, m.Index);
            }

            parts.sqlCount = string.Format(@"SELECT COUNT(*) FROM ({0}) npoco_tbl", parts.sqlUnordered);

            return true;
        }
    }
}
