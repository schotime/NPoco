using System;
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
            public string ResolvedSql { get; set; }
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
                    item.ResolvedSql = ParameterHelper.ProcessParams(item.Sql, item.Parameters.ToArray(), finalParams);
                }
                return prefix + string.Join(joiner, this.Select(c => c.ResolvedSql).ToArray()) + postfix;
            }
        }

        public class Template
        {
            public bool TokenReplacementRequired { get; set; }

            readonly string sql;
            readonly SqlBuilder builder;
            private List<object> finalParams = new List<object>();
            int dataSeq;

            public Template(SqlBuilder builder, string sql, params object[] parameters)
            {
                this.sql = ParameterHelper.ProcessParams(sql, parameters, finalParams);
                this.builder = builder;
            }

            static Regex regex = new Regex(@"(\/\*\*[^*/]+\*\*\/)", RegexOptions.Compiled | RegexOptions.Multiline);

            void ResolveSql()
            {
                if (dataSeq != builder.seq)
                {
                    rawSql = sql;
                    foreach (var pair in builder.data)
                    {
                        rawSql = rawSql.Replace("/**" + pair.Key + "**/", pair.Value.ResolveClauses(finalParams));
                    }

                    ReplaceDefaults();

                    dataSeq = builder.seq;
                }

                if (builder.seq == 0)
                {
                    rawSql = sql;
                    ReplaceDefaults();
                }
            }

            private void ReplaceDefaults()
            {
                if (TokenReplacementRequired)
                {
                    foreach (var pair in builder.defaultsIfEmpty)
                    {
                        var fullToken = GetFullTokenRegexPattern(pair.Key);
                        if (Regex.IsMatch(rawSql, fullToken))
                        {
                            throw new Exception(string.Format("Token '{0}' not used. All tokens must be replaced if TokenReplacementRequired switched on.",fullToken));
                        }
                    }
                }

                rawSql = regex.Replace(rawSql, x =>
                {
                    var token = x.Groups[1].Value;
                    var found = false;

                    foreach (var pair in builder.defaultsIfEmpty)
                    {
                        var fullToken = GetFullTokenRegexPattern(pair.Key);
                        if (Regex.IsMatch(token, fullToken))
                        {
                            if (pair.Value != null)
                            {
                                token = Regex.Replace(token, fullToken, " " + pair.Value + " ");
                            }
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        token = string.Empty;
                    }

                    return token;
                });
            }

            private static string GetFullTokenRegexPattern(string key)
            {
                return @"/\*\*" + key + @"\*\*/";
            }

            string rawSql;

            public string RawSql { get { ResolveSql(); return rawSql; } }
            public object[] Parameters { get { ResolveSql(); return finalParams.ToArray(); } }
        }

        /// <summary>
        /// Initialises the SqlBuilder
        /// </summary>
        public SqlBuilder()
        {
        }

        /// <summary>
        /// Initialises the SqlBuilder with default replacement overrides
        /// </summary>
        /// <param name="defaultOverrides">A dictionary of token overrides. A value null means the token will not be replaced.</param>
        /// <example>
        /// { "where", "1=1" }
        /// { "where(name)", "1!=1" }
        /// </example>
        public SqlBuilder(Dictionary<string, string> defaultOverrides)
        {
            defaultsIfEmpty.InsertRange(0, defaultOverrides.Select(x => new KeyValuePair<string, string>(Regex.Escape(x.Key), x.Value)));
        }

        public Template AddTemplate(string sql, params object[] parameters)
        {
            return new Template(this, sql, parameters);
        }

        public void AddClause(string name, string sql, object[] parameters, string joiner, string prefix, string postfix)
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

        readonly List<KeyValuePair<string, string>> defaultsIfEmpty = new List<KeyValuePair<string, string>>()
        {
            new KeyValuePair<string, string>(@"where\([\w]+\)", "1=1"),
            new KeyValuePair<string, string>("where", "1=1"),
            new KeyValuePair<string, string>("select", "1")
        };
        
        /// <summary>
        /// Replaces the Select columns. Uses /**select**/
        /// </summary>
        public SqlBuilder Select(params string[] columns)
        {
            AddClause("select", string.Join(", ", columns), new object[] { }, ", ", "", "");
            return this;
        }

        /// <summary>
        /// Adds an Inner Join. Uses /**join**/
        /// </summary>
        public SqlBuilder Join(string sql, params object[] parameters)
        {
            AddClause("join", sql, parameters, "\nINNER JOIN ", "\nINNER JOIN ", "\n");
            return this;
        }

        /// <summary>
        /// Adds a Left Join. Uses /**leftjoin**/
        /// </summary>
        public SqlBuilder LeftJoin(string sql, params object[] parameters)
        {
            AddClause("leftjoin", sql, parameters, "\nLEFT JOIN ", "\nLEFT JOIN ", "\n");
            return this;
        }

        /// <summary>
        /// Adds a filter. The Where keyword still needs to be specified. Uses /**where**/
        /// </summary>
        public SqlBuilder Where(string sql, params object[] parameters)
        {
            AddClause("where", "( " + sql + " )", parameters, " AND ", "", "\n");
            return this;
        }

        /// <summary>
        /// Adds a named filter. The Where keyword still needs to be specified. Uses /**where(name)**/
        /// </summary>
        public SqlBuilder WhereNamed(string name, string sql, params object[] parameters)
        {
            AddClause("where(" + name + ")", "( " + sql + " )", parameters, " AND ", "", "\n");
            return this;
        }

        /// <summary>
        /// Adds an Order By clause. Uses /**orderby**/
        /// </summary>
        public SqlBuilder OrderBy(string sql, params object[] parameters)
        {
            AddClause("orderby", sql, parameters, ", ", "ORDER BY ", "\n");
            return this;
        }

        /// <summary>
        /// Adds columns in the Order By clause. Uses /**orderbycols**/
        /// </summary>
        public SqlBuilder OrderByCols(params string[] columns)
        {
            AddClause("orderbycols", string.Join(", ", columns), new object[] { }, ", ", ", ", "");
            return this;
        }

        /// <summary>
        /// Adds a Group By clause. Uses /**groupby**/
        /// </summary>
        public SqlBuilder GroupBy(string sql, params object[] parameters)
        {
            AddClause("groupby", sql, parameters, " , ", "\nGROUP BY ", "\n");
            return this;
        }

        /// <summary>
        /// Adds a Having clause. Uses /**having**/
        /// </summary>
        public SqlBuilder Having(string sql, params object[] parameters)
        {
            AddClause("having", sql, parameters, "\nAND ", "HAVING ", "\n");
            return this;
        }
    }
}