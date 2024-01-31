using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace NPoco.Linq
{
    public class QueryBuilderData<T>
    {
        public QueryBuilderData()
        {
            ThenByExpression = new List<Expression<Func<T, object>>>();
            ThenByDescendingExpression = new List<Expression<Func<T, object>>>();
        }

        public int? Skip { get; set; }
        public int? Rows { get; set; }
        public Expression<Func<T, bool>> WhereExpression { get; set; }
        public Expression<Func<T, object>> OrderByExpression { get; set; }
        public Expression<Func<T, object>> OrderByDescendingExpression { get; set; }
        public List<Expression<Func<T, object>>> ThenByExpression  { get; private set; }
        public List<Expression<Func<T, object>>> ThenByDescendingExpression  { get; private set; }
    }
}