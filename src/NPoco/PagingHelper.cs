using System;
using System.Data;
using System.Linq;
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

        private static readonly Regex OrderByAlias = new Regex(@"[\""\[\]\w]+\.([\[\]\""\w]+)", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnoreCase);

        public static string BuildPaging(long skip, long take, SQLParts parts, ref object[] args)
        {
            parts.sqlOrderBy = string.IsNullOrEmpty(parts.sqlOrderBy) ? null : OrderByAlias.Replace(parts.sqlOrderBy, "$1");
            var sqlPage = string.Format("SELECT {4} FROM (SELECT poco_base.*, ROW_NUMBER() OVER ({0}) poco_rn \nFROM ( \n{1}) poco_base ) poco_paged \nWHERE poco_rn > @{2} AND poco_rn <= @{3} \nORDER BY poco_rn",
                parts.sqlOrderBy ?? "ORDER BY (SELECT NULL /*poco_dual*/)", parts.sqlUnordered, args.Length, args.Length + 1, parts.sqlColumns);
            args = args.Concat(new object[] { skip, skip + take }).ToArray();

            return sqlPage;
        }
    }
}
