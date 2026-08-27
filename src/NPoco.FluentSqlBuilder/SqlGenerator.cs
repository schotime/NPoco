using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using NPoco.Expressions;

namespace NPoco.FluentSqlBuilder
{
    internal static class SqlGenerator
    {
        internal static Sql Generate(IDatabase database, IList<CtePart> ctes, IList<UnionPart> unions, TableReference from, IList<SelectPart> selects, IList<JoinPart> joins, IList<ApplyPart> applies,
            IList<PredicatePart> predicates, IList<GroupPart> groups, IList<PredicatePart> having, IList<SortPart> sorts, bool distinct, int? skip, int? take)
        {
            var parameters = new List<object>();
            var text = GenerateText(database, ctes, unions, from, selects, joins, applies, predicates, groups, having, sorts, distinct, skip, take, parameters);
            return new Sql(true, text, parameters.ToArray());
        }

        internal static string GenerateText(IDatabase database, IList<CtePart> ctes, IList<UnionPart> unions, TableReference from, IList<SelectPart> selects, IList<JoinPart> joins, IList<ApplyPart> applies,
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
            var provider = (database.DatabaseType.GetProviderName() ?? string.Empty).ToLowerInvariant();
            var sqlServer = provider.Contains("sqlclient") && !provider.Contains("mysql");
            if (distinct) sql.Append("DISTINCT ");
            if (take.HasValue && !skip.HasValue && sqlServer) sql.Append("TOP (").Append(take.Value).Append(") ");
            sql.Append(string.Join(", ", selects.SelectMany(x => RenderSelect(database, x, parameters))));

            sql.Append("\nFROM ").Append(from.EscapedTableName).Append(' ').Append(from.EscapedAlias);
            foreach (var join in joins)
            {
                sql.Append('\n').Append(JoinKeyword(join.Type)).Append(' ')
                    .Append(join.Table.EscapedTableName).Append(' ').Append(join.Table.EscapedAlias).Append(" ON ");
                var translator = new SqlExpressionTranslator(database, parameters, join.Condition, join.Tables);
                sql.Append(translator.TranslatePredicate(join.Condition.Body));
            }
            foreach (var apply in applies)
            {
                var nested = apply.Query.Build(parameters);
                if (provider.Contains("npgsql") || provider.Contains("mysql"))
                    sql.Append("\nLEFT JOIN LATERAL (\n").Append(Indent(nested)).Append("\n) ").Append(apply.Table.EscapedAlias).Append(" ON TRUE");
                else
                    sql.Append("\nOUTER APPLY (\n").Append(Indent(nested)).Append("\n) ").Append(apply.Table.EscapedAlias);
            }
            AppendPredicates(sql, "WHERE", database, predicates, parameters);

            if (groups.Count > 0)
            {
                var expressions = groups.SelectMany(x => TranslateList(database, parameters, x.Expression, x.Table));
                sql.Append("\nGROUP BY ").Append(string.Join(", ", expressions));
            }
            AppendPredicates(sql, "HAVING", database, having, parameters);

            if (sorts.Count > 0)
            {
                var expressions = sorts.SelectMany(x => TranslateList(database, parameters, x.Expression, x.Table).Select(y => y + (x.Descending ? " DESC" : " ASC")));
                sql.Append("\nORDER BY ").Append(string.Join(", ", expressions));
            }
            foreach (var union in unions)
                sql.Append(union.All ? "\nUNION ALL\n" : "\nUNION\n").Append(union.Query.Build(parameters));
            if (skip.HasValue || (take.HasValue && !sqlServer))
                return ApplyDatabasePaging(database, sql.ToString(), skip ?? 0, take ?? long.MaxValue, parameters);
            return sql.ToString();
        }

        private static string ApplyDatabasePaging(IDatabase database, string sql, long skip, long take, IList<object> parameters)
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

        private static IEnumerable<string> RenderSelect(IDatabase database, SelectPart part, IList<object> parameters)
        {
            if (part.All)
            {
                return part.Table.PocoData.QueryColumns.Select(x =>
                {
                    var column = x.Value;
                    var naturalAlias = string.IsNullOrWhiteSpace(column.ColumnAlias) ? column.MemberInfoKey : column.ColumnAlias;
                    var alias = string.IsNullOrEmpty(part.Prefix) ? naturalAlias : part.Prefix + PocoData.Separator + naturalAlias;
                    return part.Table.EscapedAlias + "." + database.DatabaseType.EscapeSqlIdentifier(column.ColumnName) +
                           " AS " + database.DatabaseType.EscapeSqlIdentifier(alias);
                });
            }
            var translated = part.Tables == null
                ? TranslateList(database, parameters, part.Expression, part.Table)
                : TranslateList(database, parameters, part.Expression, part.Tables);
            if (translated.Count != 1 && part.AliasExpression != null)
                throw new InvalidOperationException("An aliased select expression must select exactly one value.");
            if (!string.IsNullOrEmpty(part.Alias))
                return new[] { translated[0] + " AS " + database.DatabaseType.EscapeSqlIdentifier(part.Alias) };
            var alias = part.AliasExpression == null ? null : string.Join(PocoData.Separator, MemberChainHelper.GetMembers(part.AliasExpression).Select(x => x.Name));
            if (string.IsNullOrEmpty(alias)) return translated;
            return new[] { translated[0] + " AS " + database.DatabaseType.EscapeSqlIdentifier(alias) };
        }

        private static IList<string> TranslateList(IDatabase database, IList<object> parameters, LambdaExpression expression, TableReference table)
        {
            var translator = new SqlExpressionTranslator(database, parameters, expression, new[] { table });
            return translator.TranslateList(expression.Body);
        }

        private static IList<string> TranslateList(IDatabase database, IList<object> parameters, LambdaExpression expression, TableReference[] tables)
        {
            var translator = new SqlExpressionTranslator(database, parameters, expression, tables);
            return translator.TranslateList(expression.Body);
        }

        private static void AppendPredicates(StringBuilder sql, string keyword, IDatabase database, IList<PredicatePart> predicates, IList<object> parameters)
        {
            if (predicates.Count == 0) return;
            sql.Append('\n').Append(keyword).Append(' ').Append(RenderPredicateList(database, predicates, parameters));
        }

        private static string RenderPredicateList(IDatabase database, IList<PredicatePart> predicates, IList<object> parameters)
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
                    var translator = new SqlExpressionTranslator(database, parameters, part.Expression, part.Tables);
                    sql.Append('(').Append(translator.TranslatePredicate(part.Expression.Body)).Append(')');
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
