using System;
using System.Collections.Generic;
using System.Text;
#if NET35
using System.Linq;
#endif

namespace NPoco
{
    public class Sql
    {
        public Sql()
        { }

        public Sql(string sql, params object[] args)
        {
            _sql = sql;
            _args = args;
        }

        public Sql(bool isBuilt, string sql, params object[] args)
        {
            _sql = sql;
            _args = args;

            if (!isBuilt) return;

            _sqlFinal = _sql;
            _argsFinal = _args;
        }

        public static Sql Builder => new Sql();

        string _sql;
        object[] _args;
        Sql _rhs;
        string _sqlFinal;
        object[] _argsFinal;

        private void Build()
        {
            // already built?
            if (_sqlFinal != null)
                return;

            // Build it
            var sb = new StringBuilder();
            var args = new List<object>();
            Build(sb, args, null);
            _sqlFinal = sb.ToString();
            _argsFinal = args.ToArray();
        }

        public string SQL
        {
            get
            {
                Build();
                return _sqlFinal;
            }
        }

        public object[] Arguments
        {
            get
            {
                Build();
                return _argsFinal;
            }
        }

        public Sql Append(Sql sql)
        {
            _sqlFinal = null;

            if (_rhs != null)
            {
                _rhs.Append(sql);
            }
            else if (_sql != null)
            {
                _rhs = sql;
            }
            else
            {
                _sql = sql._sql;
                _args = sql._args;
                _rhs = sql._rhs;
            }

            return this;
        }

        public Sql Append(string sql, params object[] args)
        {
            Append(new Sql(sql, args));
            return this;
        }

        static bool Is(Sql sql, string sqltype)
        {
            return sql?._sql != null && sql._sql.StartsWith(sqltype, StringComparison.OrdinalIgnoreCase);
        }

        private void Build(StringBuilder sb, List<object> args, Sql lhs)
        {
            if (!string.IsNullOrEmpty(_sql))
            {
                // add SQL to the string
                if (sb.Length > 0)
                    sb.Append("\n");

                var sql = ParameterHelper.ProcessParams(_sql, _args, args);

                if (Is(lhs, "WHERE ") && Is(this, "WHERE "))
                    sql = "AND " + sql.Substring(6);
                if (Is(lhs, "ORDER BY ") && Is(this, "ORDER BY "))
                    sql = ", " + sql.Substring(9);

                sb.Append(sql);
            }

            // now do rhs
            _rhs?.Build(sb, args, this);
        }

        public Sql Where(string sql, params object[] args)
        {
            Append("WHERE (" + sql + ")", args);
            return this;
        }

        public Sql OrderBy(params object[] columns)
        {
#if NET35
            Append("ORDER BY " + string.Join(",", columns.Select(x => x.ToString()).ToArray()));
#else
            Append("ORDER BY " + string.Join(", ", columns));
#endif
            return this;
        }

        public Sql Select(params object[] columns)
        {
#if NET35
            Append("SELECT " + string.Join(", ", columns.Select(x => x.ToString()).ToArray()));
#else
            Append("SELECT " + string.Join(", ", columns));
#endif
            return this;
        }

        public Sql From(params object[] tables)
        {
#if NET35
            Append("FROM " + string.Join(", ", tables.Select(x => x.ToString()).ToArray()));
#else
            Append("FROM " + string.Join(", ", tables));
#endif
            return this;
        }

        public Sql GroupBy(params object[] columns)
        {
#if NET35
            Append("GROUP BY " + string.Join(", ", columns.Select(x => x.ToString()).ToArray()));
#else
            Append("GROUP BY " + string.Join(", ", columns));
#endif
            return this;
        }

        private SqlJoinClause Join(string joinType, string table)
        {
            Append(joinType + table);
            return new SqlJoinClause(this);
        }

        public SqlJoinClause InnerJoin(string table) { return Join("INNER JOIN ", table); }
        public SqlJoinClause LeftJoin(string table) { return Join("LEFT JOIN ", table); }
        public SqlJoinClause RightJoin(string table) { return Join("RIGHT JOIN ", table); }

        public class SqlJoinClause
        {
            private readonly Sql _sql;

            public SqlJoinClause(Sql sql)
            {
                _sql = sql;
            }

            public Sql On(string onClause, params object[] args)
            {
                _sql.Append("ON " + onClause, args);
                return _sql;
            }
        }

        public static implicit operator Sql(SqlBuilder.Template template)
        {
            return new Sql(true, template.RawSql, template.Parameters);
        }

        public static Sql<TContext> BuilderFor<TContext>(TContext context)
        {
            return new Sql<TContext>(context);
        }
    }
}