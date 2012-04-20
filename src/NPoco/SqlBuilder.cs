using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace NPoco
{
    public class SqlBuilder
    {
        Dictionary<string, Clauses> data = new Dictionary<string, Clauses>();
        int seq;

        class Clause
        {
            public string Sql { get; set; }
            public List<object> Parameters { get; set; }
        }

        class Clauses : List<Clause>
        {
            string joiner;
            string prefix;
            string postfix;

            public Clauses(string joiner, string prefix, string postfix)
            {
                this.joiner = joiner;
                this.prefix = prefix;
                this.postfix = postfix;
            }

            public string ResolveClauses(List<object> finalParams)
            {
                foreach (var item in this)
                {
                    item.Sql = Database.ProcessParams(item.Sql, item.Parameters.ToArray(), finalParams);
                }
                return prefix + string.Join(joiner, this.Select(c => c.Sql).ToArray()) + postfix;
            }
        }

        public class Template
        {
            readonly string sql;
            readonly SqlBuilder builder;
            private List<object> finalParams = new List<object>();
            int dataSeq;

            public Template(SqlBuilder builder, string sql, params object[] parameters)
            {
                this.sql = Database.ProcessParams(sql, parameters, finalParams);
                this.builder = builder;
            }

            static Regex regex = new Regex(@"\/\*\*.+\*\*\/", RegexOptions.Compiled | RegexOptions.Multiline);

            void ResolveSql()
            {
                rawSql = sql;

                if (dataSeq != builder.seq)
                {
                    foreach (var pair in builder.data)
                    {
                        rawSql = rawSql.Replace("/**" + pair.Key + "**/", pair.Value.ResolveClauses(finalParams));
                    }

                    ReplaceDefaults();

                    dataSeq = builder.seq;
                }

                if (builder.seq == 0)
                {
                    ReplaceDefaults();
                }
            }

            private void ReplaceDefaults()
            {
                foreach (var pair in builder.defaultsIfEmpty)
                {
                    rawSql = rawSql.Replace("/**" + pair.Key + "**/", " " + pair.Value + " ");
                }

                // replace all that is left with empty
                rawSql = regex.Replace(rawSql, "");
            }

            string rawSql;

            public string RawSql { get { ResolveSql(); return rawSql; } }
            public object[] Parameters { get { ResolveSql(); return finalParams.ToArray(); } }
        }


        public SqlBuilder()
        {
        }

        public Template AddTemplate(string sql, params object[] parameters)
        {
            return new Template(this, sql, parameters);
        }

        void AddClause(string name, string sql, object[] parameters, string joiner, string prefix, string postfix)
        {
            Clauses clauses;
            if (!data.TryGetValue(name, out clauses))
            {
                clauses = new Clauses(joiner, prefix, postfix);
                data[name] = clauses;
            }
            clauses.Add(new Clause { Sql = sql, Parameters = new List<object>(parameters) });
            seq++;
        }

        readonly Dictionary<string, string> defaultsIfEmpty = new Dictionary<string, string>
        {
            { "where", "1=1" },
            { "select", "1" }
        };

        public SqlBuilder Select(params string[] columns)
        {
            AddClause("select", string.Join(", ", columns), new object[] { }, ", ", "", "");
            return this;
        }

        public SqlBuilder Join(string sql, params object[] parameters)
        {
            AddClause("join", sql, parameters, "\nINNER JOIN ", "\nINNER JOIN ", "\n");
            return this;
        }

        public SqlBuilder LeftJoin(string sql, params object[] parameters)
        {
            AddClause("leftjoin", sql, parameters, "\nLEFT JOIN ", "\nLEFT JOIN ", "\n");
            return this;
        }

        public SqlBuilder Where(string sql, params object[] parameters)
        {
            AddClause("where", sql, parameters, " AND ", " ( ", " )\n");
            return this;
        }

        public SqlBuilder OrderBy(string sql, params object[] parameters)
        {
            AddClause("orderby", sql, parameters, ", ", "ORDER BY ", "\n");
            return this;
        }

        public SqlBuilder OrderByCols(params string[] columns)
        {
            AddClause("orderbycols", string.Join(", ", columns), new object[] { }, ", ", ", ", "");
            return this;
        }

        public SqlBuilder GroupBy(string sql, params object[] parameters)
        {
            AddClause("groupby", sql, parameters, " , ", "\nGROUP BY ", "\n");
            return this;
        }

        public SqlBuilder Having(string sql, params object[] parameters)
        {
            AddClause("having", sql, parameters, "\nAND ", "HAVING ", "\n");
            return this;
        }
    }
}