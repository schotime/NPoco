using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace NPoco.FluentSql
{
    internal enum FluentJoinType { Inner, Left, Right, FullOuter }

    internal sealed class CtePart
    {
        internal string Name = null!;
        internal IFluentSqlQueryInternal Query = null!;
    }

    internal sealed class UnionPart
    {
        internal bool All;
        internal IFluentSqlQueryInternal Query = null!;
    }

    internal sealed class ApplyPart
    {
        internal TableReference Table = null!;
        internal IFluentSqlQueryInternal Query = null!;
    }

    /// <summary>
    /// One entry of the SELECT list, in one of three shapes: every column of a table, an expression
    /// read against a single table, or an expression read against all the tables in scope. Which
    /// shape it is decides which of these are set.
    /// </summary>
    internal sealed class SelectPart
    {
        internal TableReference? Table;
        internal TableReference[]? Tables;
        internal LambdaExpression? Expression;
        internal string? Alias;
        internal bool All;
    }

    internal sealed class JoinPart
    {
        internal FluentJoinType Type;
        internal TableReference Table = null!;
        internal LambdaExpression Condition = null!;
        internal TableReference[] Tables = null!;
    }

    /// <summary>
    /// One predicate, or a parenthesised group of them: a group carries <see cref="Children"/> and
    /// no expression of its own, and a leaf carries the expression and the tables it reads.
    /// </summary>
    internal sealed class PredicatePart
    {
        internal TableReference[]? Tables;
        internal LambdaExpression? Expression;
        internal string Operator = "AND";
        internal List<PredicatePart>? Children;
    }

    internal sealed class SortPart
    {
        internal TableReference[] Tables = null!;
        internal LambdaExpression Expression = null!;
        internal bool Descending;
    }

    internal sealed class GroupPart
    {
        internal TableReference[] Tables = null!;
        internal LambdaExpression Expression = null!;
    }
}
