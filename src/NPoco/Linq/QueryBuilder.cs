using System;
using System.Linq.Expressions;
using NPoco.Expressions;

namespace NPoco.Linq
{
    public class QueryBuilder<T>
    {
        public QueryBuilderData<T> Data { get; private set; }

        public QueryBuilder()
        {
            Data = new QueryBuilderData<T>();
        }

        public virtual QueryBuilder<T> Limit(int rows)
        {
            Data.Rows = rows;
            return this;
        }

        public virtual QueryBuilder<T> Limit(int skip, int rows)
        {
            Data.Rows = rows;
            Data.Skip = skip;
            return this;
        }

        public virtual QueryBuilder<T> Where(Expression<Func<T, bool>> whereExpression)
        {
            Data.WhereExpression = Data.WhereExpression == null ? PredicateBuilder.Create(whereExpression) : Data.WhereExpression.And(whereExpression);
            return this;
        }

        public virtual QueryBuilder<T> OrderBy(Expression<Func<T, object>> orderByExpression)
        {
            Data.OrderByExpression = orderByExpression;
            return this;
        }

        public virtual QueryBuilder<T> OrderByDescending(Expression<Func<T, object>> orderByDescendingExpression)
        {
            Data.OrderByDescendingExpression = orderByDescendingExpression;
            return this;
        }

        public virtual QueryBuilder<T> ThenBy(Expression<Func<T, object>> thenByExpression)
        {
            Data.ThenByExpression.Add(thenByExpression);
            return this;
        }

        public virtual QueryBuilder<T> ThenByDescending(Expression<Func<T, object>> thenByDescendingExpression)
        {
            Data.ThenByDescendingExpression.Add(thenByDescendingExpression);
            return this;
        }
    }
}