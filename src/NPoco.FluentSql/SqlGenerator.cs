using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using NPoco.Expressions;

namespace NPoco.FluentSql
{
    internal static class SqlGenerator
    {
        internal static Sql Generate(IAsyncQueryDatabase database, IList<CtePart> ctes, IList<UnionPart> unions, TableReference from, IList<SelectPart> selects, IList<JoinPart> joins, IList<ApplyPart> applies,
            IList<PredicatePart> predicates, IList<GroupPart> groups, IList<PredicatePart> having, IList<SortPart> sorts, bool distinct, int? skip, int? take)
        {
            var parameters = new List<object>();
            var text = GenerateText(database, ctes, unions, from, selects, joins, applies, predicates, groups, having, sorts, distinct, skip, take, parameters);
            return new Sql(true, text, parameters.ToArray());
        }

        internal static string GenerateText(IAsyncQueryDatabase database, IList<CtePart> ctes, IList<UnionPart> unions, TableReference from, IList<SelectPart> selects, IList<JoinPart> joins, IList<ApplyPart> applies,
            IList<PredicatePart> predicates, IList<GroupPart> groups, IList<PredicatePart> having, IList<SortPart> sorts, bool distinct, int? skip, int? take, IList<object> parameters)
        {
            var sql = new StringBuilder();
            if (selects.Count == 0) throw new InvalidOperationException("A result projection is required.");
            if (ctes.Count > 0)
            {
                // The leading semicolon also tells NPoco's AutoSelectHelper this is a complete statement.
                sql.Append(";WITH ");
                for (var i = 0; i < ctes.Count; i++)
                {
                    if (i > 0) sql.Append(",\n");
                    sql.Append(database.DatabaseType.EscapeSqlIdentifier(ctes[i].Name)).Append(" AS (\n")
                        .Append(Indent(ctes[i].Query.Build(parameters))).Append("\n)");
                }
                sql.Append('\n');
            }
            sql.Append("SELECT ");
            var dialect = SqlDialects.For(database.DatabaseType);
            // A leading row limit, for the databases that spell one; the rest page below.
            var takePrefix = take.HasValue && !skip.HasValue ? dialect.TakeOnlyPrefix(take.Value) : null;
            if (distinct) sql.Append("DISTINCT ");
            if (takePrefix != null) sql.Append(takePrefix);
            var translators = new TranslatorCache(database, parameters);
            sql.Append(string.Join(", ", selects.SelectMany(x => RenderSelect(database, x, parameters, translators))));

            sql.Append("\nFROM ").Append(from.EscapedTableName).Append(' ').Append(from.EscapedAlias);
            foreach (var join in joins)
            {
                sql.Append('\n').Append(JoinKeyword(join.Type)).Append(' ');
                if (join.Query == null)
                    sql.Append(join.Table.EscapedTableName);
                else
                    sql.Append("(\n").Append(Indent(join.Query.Build(parameters))).Append("\n)");
                sql.Append(' ').Append(join.Table.EscapedAlias).Append(" ON ");
                var translator = new SqlExpressionTranslator(database, parameters, join.Condition, join.Tables);
                sql.Append(translator.TranslatePredicate(join.Condition.Body));
            }
            foreach (var apply in applies)
            {
                var nested = apply.Query.Build(parameters);
                sql.Append('\n').Append(dialect.OuterApply(Indent(nested), apply.Table.EscapedAlias));
            }
            AppendPredicates(sql, "WHERE", database, predicates, parameters);

            if (groups.Count > 0)
            {
                var expressions = groups.SelectMany(x => TranslateList(database, parameters, x.Expression, x.Tables));
                sql.Append("\nGROUP BY ").Append(string.Join(", ", expressions));
            }
            AppendPredicates(sql, "HAVING", database, having, parameters);

            if (sorts.Count > 0)
            {
                var expressions = sorts.SelectMany(x => TranslateList(database, parameters, x.Expression, x.Tables)
                    .Select(y => y + (x.Descending ? " DESC" : " ASC")));
                sql.Append("\nORDER BY ").Append(string.Join(", ", expressions));
            }
            foreach (var union in unions)
                sql.Append(union.All ? "\nUNION ALL\n" : "\nUNION\n").Append(union.Query.Build(parameters));
            if (skip.HasValue || (take.HasValue && takePrefix == null))
                return ApplyDatabasePaging(database, sql.ToString(), skip ?? 0, take ?? long.MaxValue, parameters);
            return sql.ToString();
        }

        private static string ApplyDatabasePaging(IAsyncQueryDatabase database, string sql, long skip, long take, IList<object> parameters)
        {
            SQLParts parts;
            if (!PagingHelper.SplitSQL(sql, out parts)) throw new InvalidOperationException("Unable to parse SQL statement for paging.");
            var arguments = parameters.ToArray();
            var paged = database.DatabaseType.BuildPageQuery(skip, take, parts, ref arguments);
            parameters.Clear();
            foreach (var argument in arguments) parameters.Add(argument);
            return paged;
        }

        private static string Indent(string sql) => "    " + sql.Replace("\n", "\n    ");

        private static IEnumerable<string> RenderSelect(IAsyncQueryDatabase database, SelectPart part, IList<object> parameters, TranslatorCache translators)
        {
            // Which of the part's members are set is decided by the shape it was built in: every
            // column of one table, or an expression read against one table or against all of them.
            if (part.All)
            {
                var table = part.Table!;
                return table.PocoData.QueryColumns.Select(x =>
                {
                    var column = x.Value;
                    var alias = string.IsNullOrWhiteSpace(column.ColumnAlias) ? column.MemberInfoKey : column.ColumnAlias;
                    return table.EscapedAlias + "." + database.DatabaseType.EscapeSqlIdentifier(column.ColumnName) +
                           " AS " + database.DatabaseType.EscapeSqlIdentifier(alias);
                });
            }
            var expression = part.Expression!;
            var translated = part.Tables == null
                ? TranslateList(database, parameters, expression, part.Table!)
                : TranslateList(database, parameters, expression, part.Tables, translators);
            if (string.IsNullOrEmpty(part.Alias)) return translated;
            return new[] { translated[0] + " AS " + database.DatabaseType.EscapeSqlIdentifier(part.Alias) };
        }

        private static IList<string> TranslateList(IAsyncQueryDatabase database, IList<object> parameters, LambdaExpression expression, TableReference table)
        {
            var translator = new SqlExpressionTranslator(database, parameters, expression, new[] { table });
            return translator.TranslateList(expression.Body);
        }

        private static IList<string> TranslateList(IAsyncQueryDatabase database, IList<object> parameters, LambdaExpression expression, TableReference[] tables)
        {
            var translator = new SqlExpressionTranslator(database, parameters, expression, tables);
            return translator.TranslateList(expression.Body);
        }

        private static IList<string> TranslateList(IAsyncQueryDatabase database, IList<object> parameters, LambdaExpression expression, TableReference[] tables, TranslatorCache translators)
            => translators.For(expression, tables).TranslateList(expression.Body);

        /// <summary>
        /// A projection becomes one SELECT part per leaf, all over the same tables. A translator
        /// binds a lambda's parameters to tables, so any two parameterless expressions over the
        /// same tables can share one - which is every leaf of a Row-style projection.
        /// </summary>
        private sealed class TranslatorCache
        {
            private readonly IAsyncQueryDatabase _database;
            private readonly IList<object> _parameters;
            private TableReference[]? _tables;
            private SqlExpressionTranslator? _shared;

            internal TranslatorCache(IAsyncQueryDatabase database, IList<object> parameters)
            {
                _database = database;
                _parameters = parameters;
            }

            internal SqlExpressionTranslator For(LambdaExpression expression, TableReference[] tables)
            {
                if (expression.Parameters.Count > 0)
                    return new SqlExpressionTranslator(_database, _parameters, expression, tables, projection: true);
                if (_shared == null || !ReferenceEquals(_tables, tables))
                {
                    _tables = tables;
                    _shared = new SqlExpressionTranslator(_database, _parameters, expression, tables, projection: true);
                }
                return _shared;
            }
        }

        private static void AppendPredicates(StringBuilder sql, string keyword, IAsyncQueryDatabase database, IList<PredicatePart> predicates, IList<object> parameters)
        {
            if (predicates.Count == 0) return;
            sql.Append('\n').Append(keyword).Append(' ').Append(RenderPredicateList(database, predicates, parameters));
        }

        private static string RenderPredicateList(IAsyncQueryDatabase database, IList<PredicatePart> predicates, IList<object> parameters)
        {
            var sql = new StringBuilder();
            for (var i = 0; i < predicates.Count; i++)
            {
                var part = predicates[i];
                if (i > 0) sql.Append(' ').Append(part.Operator).Append(' ');
                if (part.Children != null)
                    sql.Append('(').Append(RenderPredicateList(database, part.Children, parameters)).Append(')');
                else
                {
                    // A part with no children is a predicate of its own, so it carries both.
                    var expression = part.Expression!;
                    var translator = new SqlExpressionTranslator(database, parameters, expression, part.Tables!);
                    sql.Append('(').Append(translator.TranslatePredicate(expression.Body)).Append(')');
                }
            }
            return sql.ToString();
        }

        private static string JoinKeyword(FluentJoinType type)
        {
            switch (type)
            {
                case FluentJoinType.Left: return "LEFT JOIN";
                case FluentJoinType.Right: return "RIGHT JOIN";
                case FluentJoinType.FullOuter: return "FULL OUTER JOIN";
                default: return "INNER JOIN";
            }
        }
    }
}
