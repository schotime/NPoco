using System;
using System.Linq.Expressions;
using NPoco.Expressions;

namespace NPoco.Linq
{
    public interface IDeleteQueryProvider<T>
    {
        int Execute();
        IDeleteQueryProvider<T> Where(Expression<Func<T, bool>> whereExpression);
    }

    public class DeleteQueryProvider<T> : IDeleteQueryProvider<T>
    {
        private readonly IDatabase _database;
        private SqlExpression<T> _sqlExpression;

        public DeleteQueryProvider(IDatabase database)
        {
            _database = database;
            _sqlExpression = database.DatabaseType.ExpressionVisitor<T>(database, false);
        }

        public IDeleteQueryProvider<T> Where(Expression<Func<T, bool>> whereExpression)
        {
            _sqlExpression = _sqlExpression.Where(whereExpression);
            return this;
        }

        public int Execute()
        {
            return _database.Execute(_sqlExpression.Context.ToDeleteStatement(), _sqlExpression.Context.Params);
        }
    }
}