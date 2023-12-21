using System;

namespace NPoco
{
    public class Sql<TContext> : Sql
    {
        public Sql(TContext sqlContext)
        {
            if (sqlContext == null) throw new ArgumentNullException(nameof(sqlContext));
            SqlContext = sqlContext;
        }

        public Sql(TContext sqlContext, string sql, params object[] args)
            : base(sql, args)
        {
            if (sqlContext == null) throw new ArgumentNullException(nameof(sqlContext));
            SqlContext = sqlContext;
        }

        public Sql(TContext sqlContext, bool isBuilt, string sql, params object[] args)
            : base(isBuilt, sql, args)
        {
            if (sqlContext == null) throw new ArgumentNullException(nameof(sqlContext));
            SqlContext = sqlContext;
        }

        public TContext SqlContext { get; }

        public new Sql<TContext> Append(Sql sql)
        {
            // not perfect as it will build the query that is appended - edge case, though
            base.Append(new Sql<TContext>(SqlContext, sql.SQL, sql.Arguments));
            return this;
        }

        public Sql<TContext> Append(Sql<TContext> sql)
        {
            // paranoia, and annoying
            //if (!ReferenceEquals(sql.SqlContext, SqlContext)) throw new ArgumentException("Cannot append Sql with a different context.");
            base.Append(sql);
            return this;
        }

        public new Sql<TContext> Append(string sql, params object[] args)
        {
            base.Append(sql, args);
            return this;
        }

        public new Sql<TContext> Where(string sql, params object[] args)
        {
            base.Where(sql, args);
            return this;
        }

        public new Sql<TContext> OrderBy(params object[] columns)
        {
            base.OrderBy(columns);
            return this;
        }

        public new Sql<TContext> Select(params object[] columns)
        {
            base.Select(columns);
            return this;
        }

        public new Sql<TContext> From(params object[] tables)
        {
            base.From(tables);
            return this;
        }

        public new Sql<TContext> GroupBy(params object[] columns)
        {
            base.GroupBy(columns);
            return this;
        }

        private SqlJoinClause<TContext> Join(string joinType, string table)
        {
            Append(joinType + table);
            return new SqlJoinClause<TContext>(this);
        }

        public new SqlJoinClause<TContext> InnerJoin(string table) { return Join("INNER JOIN ", table); }
        public new SqlJoinClause<TContext> LeftJoin(string table) { return Join("LEFT JOIN ", table); }
        public new SqlJoinClause<TContext> RightJoin(string table) { return Join("RIGHT JOIN ", table); }

        public class SqlJoinClause<T>
        {
            private readonly Sql<T> _sql;

            public SqlJoinClause(Sql<T> sql)
            {
                _sql = sql;
            }

            public T SqlContext => _sql.SqlContext;

            public Sql<T> On(string onClause, params object[] args)
            {
                _sql.Append("ON " + onClause, args);
                return _sql;
            }
        }

        // cannot do it because we don't have the context in the template
        //
        //public static implicit operator Sql<TContext>(SqlBuilder.Template template)
        //{
        //    return new Sql<TContext>(true, template.RawSql, template.Parameters);
        //}
    }
}
