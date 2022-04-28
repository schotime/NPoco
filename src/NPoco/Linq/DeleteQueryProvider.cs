using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using NPoco.Expressions;

namespace NPoco.Linq
{
    public interface IAsyncDeleteQueryProvider<T>
    {
        IAsyncDeleteQueryProvider<T> Where(Expression<Func<T, bool>> whereExpression);
        Task<int> Execute();
        Task<int> Execute(CancellationToken cancellationToken);
    }

    public interface IDeleteQueryProvider<T>
    {
        IDeleteQueryProvider<T> Where(Expression<Func<T, bool>> whereExpression);
        int Execute();
        Task<int> ExecuteAsync();
        Task<int> ExecuteAsync(CancellationToken cancellationToken);
    }

    public class DeleteQueryProvider<T> : AsyncDeleteQueryProvider<T>, IDeleteQueryProvider<T>
    {
        public DeleteQueryProvider(IDatabase database) : base(database)
        {
        }

        public new IDeleteQueryProvider<T> Where(Expression<Func<T, bool>> whereExpression)
        {
            return (IDeleteQueryProvider<T>)base.Where(whereExpression);
        }
#pragma warning disable CS0109
        public new int Execute()
        {
            return _database.Execute(_sqlExpression.Context.ToDeleteStatement(), _sqlExpression.Context.Params);
        }
#pragma warning restore CS0109

        public Task<int> ExecuteAsync()
        {
            return ExecuteAsync(CancellationToken.None);
        }

        public Task<int> ExecuteAsync(CancellationToken cancellationToken)
        {
            return Execute(cancellationToken);
        }
    }

    public class AsyncDeleteQueryProvider<T> : IAsyncDeleteQueryProvider<T>
    {
        protected readonly IDatabase _database;
        protected SqlExpression<T> _sqlExpression;

        public AsyncDeleteQueryProvider(IDatabase database)
        {
            _database = database;
            _sqlExpression = database.DatabaseType.ExpressionVisitor<T>(database, database.PocoDataFactory.ForType(typeof(T)), false);
        }

        public IAsyncDeleteQueryProvider<T> Where(Expression<Func<T, bool>> whereExpression)
        {
            _sqlExpression = _sqlExpression.Where(whereExpression);
            return this;
        }

        public Task<int> Execute()
        {
            return Execute(CancellationToken.None);
        }

        public Task<int> Execute(CancellationToken cancellationToken)
        {
            return _database.ExecuteAsync(_sqlExpression.Context.ToDeleteStatement(), cancellationToken, _sqlExpression.Context.Params);
        }
    }
}