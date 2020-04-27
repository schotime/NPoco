using System;
using System.Linq.Expressions;
#if !NET35 && !NET40
using System.Threading.Tasks;
#endif
using NPoco.Expressions;

namespace NPoco.Linq
{
    public interface IAsyncDeleteQueryProvider<T>
    {
#if !NET35 && !NET40
        IAsyncDeleteQueryProvider<T> Where(Expression<Func<T, bool>> whereExpression);
        Task<int> Execute();
#endif
    }

    public interface IDeleteQueryProvider<T>
    {
        IDeleteQueryProvider<T> Where(Expression<Func<T, bool>> whereExpression);
        int Execute();
#if !NET35 && !NET40
        Task<int> ExecuteAsync();
#endif
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

#if !NET35 && !NET40
        public Task<int> ExecuteAsync()
        {
            return base.Execute();
        }
#endif
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
#if !NET35 && !NET40
        public Task<int> Execute()
        {
            return _database.ExecuteAsync(_sqlExpression.Context.ToDeleteStatement(), _sqlExpression.Context.Params);
        }
#endif
    }
}