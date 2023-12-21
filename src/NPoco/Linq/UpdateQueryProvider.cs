using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using NPoco.Expressions;

namespace NPoco.Linq
{
    public class UpdateQueryProvider<T> : AsyncUpdateQueryProvider<T>, IUpdateQueryProvider<T>
    {
        public UpdateQueryProvider(IDatabase database) : base(database)
        {
        }

        public new IUpdateQueryProvider<T> Where(Expression<Func<T, bool>> whereExpression)
        {
            return (IUpdateQueryProvider<T>) base.Where(whereExpression);
        }

        public new IUpdateQueryProvider<T> ExcludeDefaults()
        {
            return (IUpdateQueryProvider<T>)base.ExcludeDefaults();
        }

        public new IUpdateQueryProvider<T> OnlyFields(Expression<Func<T, object>> onlyFields)
        {
            return (IUpdateQueryProvider<T>)base.OnlyFields(onlyFields);
        }

#pragma warning disable CS0109
        public new int Execute(T obj)
        {
            var updateStatement = _sqlExpression.Context.ToUpdateStatement(obj, _excludeDefaults, _onlyFields);
            return _database.Execute(updateStatement, _sqlExpression.Context.Params);
        }
#pragma warning restore CS0109

        public Task<int> ExecuteAsync(T obj, CancellationToken cancellationToken = default)
        {
            return base.Execute(obj, cancellationToken);
        }
    }

    public class AsyncUpdateQueryProvider<T> : IAsyncUpdateQueryProvider<T>
    {
        protected readonly IDatabase _database;
        protected ISqlExpression<T> _sqlExpression;
        protected bool _excludeDefaults;
        protected bool _onlyFields;

        public AsyncUpdateQueryProvider(IDatabase database)
        {
            _database = database;
            _sqlExpression = database.DatabaseType.ExpressionVisitor<T>(database, database.PocoDataFactory.ForType(typeof(T)), false);
        }

        public IAsyncUpdateQueryProvider<T> Where(Expression<Func<T, bool>> whereExpression)
        {
            _sqlExpression = _sqlExpression.Where(whereExpression);
            return this;
        }

        public IAsyncUpdateQueryProvider<T> ExcludeDefaults()
        {
            _excludeDefaults = true;
            return this;
        }

        public IAsyncUpdateQueryProvider<T> OnlyFields(Expression<Func<T, object>> onlyFields)
        {
            _sqlExpression = _sqlExpression.Update(onlyFields);
            _onlyFields = true;
            return this;
        }

        public async Task<int> Execute(T obj, CancellationToken cancellationToken = default)
        {
            var updateStatement = _sqlExpression.Context.ToUpdateStatement(obj, _excludeDefaults, _onlyFields);
            return await _database.ExecuteAsync(updateStatement, _sqlExpression.Context.Params, cancellationToken).ConfigureAwait(false);
        }
    }
}
