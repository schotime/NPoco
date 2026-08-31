using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace NPoco.FluentSql
{
    internal enum FluentJoinType { Inner, Left, Right, FullOuter }

    internal sealed class CtePart
    {
        internal string Name;
        internal IFluentSqlQueryInternal Query;
    }

    internal sealed class UnionPart
    {
        internal bool All;
        internal IFluentSqlQueryInternal Query;
    }

    internal sealed class ApplyPart
    {
        internal TableReference Table;
        internal IFluentSqlQueryInternal Query;
    }

    internal sealed class SelectPart
    {
        internal TableReference Table;
        internal TableReference[] Tables;
        internal LambdaExpression Expression;
        internal string Alias;
        internal bool All;
    }

    internal sealed class JoinPart
    {
        internal FluentJoinType Type;
        internal TableReference Table;
        internal LambdaExpression Condition;
        internal TableReference[] Tables;
    }

    internal sealed class PredicatePart
    {
        internal TableReference[] Tables;
        internal LambdaExpression Expression;
        internal string Operator = "AND";
        internal List<PredicatePart> Children;
    }

    internal sealed class SortPart
    {
        internal TableReference[] Tables;
        internal LambdaExpression Expression;
        internal bool Descending;
    }

    internal sealed class GroupPart
    {
        internal TableReference[] Tables;
        internal LambdaExpression Expression;
    }
}
