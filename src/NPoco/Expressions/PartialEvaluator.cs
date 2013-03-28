using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace NPoco.Expressions
{
    /// <summary>
    /// Rewrites an expression tree so that locally isolatable sub-expressions are evaluated and converted into ConstantExpression nodes.
    /// </summary>
    internal static class PartialEvaluator
    {
        /// <summary>
        /// Performs evaluation & replacement of independent sub-trees
        /// </summary>
        /// <param retval="expression">The root of the expression tree.</param>
        /// <param retval="fnCanBeEvaluated">A function that decides whether a given expression node can be part of the local function.</param>
        /// <returns>A new tree with sub-trees evaluated and replaced.</returns>
        public static Expression Eval(Expression expression, Func<Expression, bool> fnCanBeEvaluated)
        {
            return SubtreeEvaluator.Eval(Nominator.Nominate(fnCanBeEvaluated, expression), expression);
        }

        /// <summary>
        /// Performs evaluation & replacement of independent sub-trees
        /// </summary>
        /// <param retval="expression">The root of the expression tree.</param>
        /// <returns>A new tree with sub-trees evaluated and replaced.</returns>
        public static Expression Eval(Expression expression)
        {
            return Eval(expression, PartialEvaluator.CanBeEvaluatedLocally);
        }

        private static bool CanBeEvaluatedLocally(Expression expression)
        {
            return expression.NodeType != ExpressionType.Parameter;
        }

        /// <summary>
        /// Evaluates & replaces sub-trees when first candidate is reached (top-down)
        /// </summary>
        class SubtreeEvaluator : ExpressionVisitor
        {
            HashSet<Expression> candidates;

            private SubtreeEvaluator(HashSet<Expression> candidates)
            {
                this.candidates = candidates;
            }

            internal static Expression Eval(HashSet<Expression> candidates, Expression exp)
            {
                return new SubtreeEvaluator(candidates).Visit(exp);
            }

            protected override Expression Visit(Expression exp)
            {
                if (exp == null)
                {
                    return null;
                }
                if (this.candidates.Contains(exp))
                {
                    return this.Evaluate(exp);
                }
                return base.Visit(exp);
            }

            private Expression Evaluate(Expression e)
            {
                if (e.NodeType == ExpressionType.Constant)
                {
                    return e;
                }
                Type type = e.Type;
                if (type.IsValueType)
                {
                    e = Expression.Convert(e, typeof(object));
                }
                Expression<Func<object>> lambda = Expression.Lambda<Func<object>>(e);
                Func<object> fn = lambda.Compile();
                return Expression.Constant(fn(), type);
            }
        }

        /// <summary>
        /// Performs bottom-up analysis to determine which nodes can possibly
        /// be part of an evaluated sub-tree.
        /// </summary>
        class Nominator : ExpressionVisitor
        {
            Func<Expression, bool> fnCanBeEvaluated;
            HashSet<Expression> candidates;
            bool cannotBeEvaluated;

            private Nominator(Func<Expression, bool> fnCanBeEvaluated)
            {
                this.candidates = new HashSet<Expression>();
                this.fnCanBeEvaluated = fnCanBeEvaluated;
            }

            internal static HashSet<Expression> Nominate(Func<Expression, bool> fnCanBeEvaluated, Expression expression)
            {
                Nominator nominator = new Nominator(fnCanBeEvaluated);
                nominator.Visit(expression);
                return nominator.candidates;
            }

            protected override Expression Visit(Expression expression)
            {
                if (expression != null)
                {
                    bool saveCannotBeEvaluated = this.cannotBeEvaluated;
                    this.cannotBeEvaluated = false;
                    base.Visit(expression);
                    if (!this.cannotBeEvaluated)
                    {
                        if (this.fnCanBeEvaluated(expression))
                        {
                            this.candidates.Add(expression);
                        }
                        else
                        {
                            this.cannotBeEvaluated = true;
                        }
                    }
                    this.cannotBeEvaluated |= saveCannotBeEvaluated;
                }
                return expression;
            }
        }
    }
}
