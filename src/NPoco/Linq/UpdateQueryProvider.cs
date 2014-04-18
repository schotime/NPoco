using System;
using System.Linq.Expressions;
using NPoco.Expressions;

namespace NPoco.Linq
{
    public interface IUpdateQueryProvider<T>
    {
        IUpdateQueryProvider<T> Where(Expression<Func<T, bool>> whereExpression);
        IUpdateQueryProvider<T> ExcludeDefaults();
        IUpdateQueryProvider<T> OnlyFields(Expression<Func<T, object>> onlyFields);
        int Execute(T obj);
    }

    public class UpdateQueryProvider<T> : IUpdateQueryProvider<T>
    {
        private readonly IDatabase _database;
        private SqlExpression<T> _sqlExpression;
        private bool _excludeDefaults;
        private bool _onlyFields;

        public UpdateQueryProvider(IDatabase database)
        {
            _database = database;
            _sqlExpression = database.DatabaseType.ExpressionVisitor<T>(database, false);
        }

        public IUpdateQueryProvider<T> Where(Expression<Func<T, bool>> whereExpression)
        {
            _sqlExpression = _sqlExpression.Where(whereExpression);
            return this;
        }

        public IUpdateQueryProvider<T> ExcludeDefaults()
        {
            _excludeDefaults = true;
            return this;
        }

        public IUpdateQueryProvider<T> OnlyFields(Expression<Func<T, object>> onlyFields)
        {
            _sqlExpression = _sqlExpression.Update(onlyFields);
            _onlyFields = true;
            return this;
        }

        public int Execute(T obj)
        {
            var updateStatement = _sqlExpression.Context.ToUpdateStatement(obj, _excludeDefaults, _onlyFields);
            return _database.Execute(updateStatement, _sqlExpression.Context.Params);
        }
    }
}