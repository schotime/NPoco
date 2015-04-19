using System;
using System.Linq.Expressions;
#if NET45
using System.Threading.Tasks;
#endif
using NPoco.Expressions;

namespace NPoco.Linq
{
    public interface IDeleteQueryProvider<T>
    {
        IDeleteQueryProvider<T> Where(Expression<Func<T, bool>> whereExpression);
        int Execute();
#if NET45
        Task<int> ExecuteAsync();
#endif
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

#if NET45
        public Task<int> ExecuteAsync()
        {
            return _database.ExecuteAsync(_sqlExpression.Context.ToDeleteStatement(), _sqlExpression.Context.Params);
        }
#endif
    }
}