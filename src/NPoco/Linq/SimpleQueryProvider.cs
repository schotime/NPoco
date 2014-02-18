using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using NPoco.Expressions;

namespace NPoco.Linq
{
    public interface ISimpleJoinQueryProvider<T>
    {
        ISimpleQueryProvider<T> Where(Expression<Func<T, bool>> whereExpression);
        ISimpleQueryProvider<T> OrderBy(Expression<Func<T, object>> column);
        ISimpleQueryProvider<T> OrderByDescending(Expression<Func<T, object>> column);
        ISimpleQueryProvider<T> ThenBy(Expression<Func<T, object>> column);
        ISimpleQueryProvider<T> ThenByDescending(Expression<Func<T, object>> column);
    }

    public interface ISimpleQueryProvider<T> : ISimpleJoinQueryProvider<T>
    {
        T FirstOrDefault(Expression<Func<T, bool>> whereExpression = null);
        T First(Expression<Func<T, bool>> whereExpression = null);
        T SingleOrDefault(Expression<Func<T, bool>> whereExpression = null);
        T Single(Expression<Func<T, bool>> whereExpression = null);
        ISimpleQueryProvider<T> Limit(int rows);
        ISimpleQueryProvider<T> Limit(int skip, int rows);
        ISimpleQueryProvider<T> Join<T2>(Expression<Func<T, T2, bool>> onExpression, Func<ISimpleJoinQueryProvider<T2>, ISimpleJoinQueryProvider<T2>> queryProvider = null);
        int Count(Expression<Func<T, bool>> whereExpression = null);
        List<T> ToList();
    }

    public class SimpleQueryProvider<T> : ISimpleQueryProvider<T>, ISimpleQueryProviderExpression<T>
    {
        private readonly IDatabase _database;
        private SqlExpression<T> _sqlExpression;
        private List<JoinData> _joinSqlExpressions = new List<JoinData>();

        public SimpleQueryProvider(IDatabase database, Expression<Func<T, bool>> whereExpression)
        {
            _database = database;
            _sqlExpression = database.DatabaseType.ExpressionVisitor<T>(database, true);
            _sqlExpression = _sqlExpression.Where(whereExpression);
        }

        SqlExpression<T> ISimpleQueryProviderExpression<T>.AtlasSqlExpression { get { return _sqlExpression; } }

        public ISimpleQueryProvider<T> Join<T2>(Expression<Func<T, T2, bool>> onExpression, Func<ISimpleJoinQueryProvider<T2>, ISimpleJoinQueryProvider<T2>> queryProvider = null)
        {
            ISimpleJoinQueryProvider<T2> simpleExpression = new SimpleQueryProvider<T2>(_database, null);
            if (queryProvider != null)
                simpleExpression = queryProvider(simpleExpression);

            _joinSqlExpressions.Add(new JoinData()
            {
                OnSql = _database.DatabaseType.ExpressionVisitor<T>(_database, true).On(onExpression),
                SqlExpression = ((ISimpleQueryProviderExpression<T2>)simpleExpression).AtlasSqlExpression
            });
            return this;
        }

        public ISimpleQueryProvider<T> Where(Expression<Func<T, bool>> whereExpression)
        {
            _sqlExpression = _sqlExpression.Where(whereExpression);
            return this;
        }

        private void AddLimitAndWhere(Expression<Func<T, bool>> whereExpression)
        {
            if (whereExpression != null)
                _sqlExpression = _sqlExpression.Where(whereExpression);
            _sqlExpression = _sqlExpression.Limit(1);
        }

        public T FirstOrDefault(Expression<Func<T, bool>> whereExpression = null)
        {
            AddLimitAndWhere(whereExpression);
            return ToList().FirstOrDefault();
        }

        public T First(Expression<Func<T, bool>> whereExpression = null)
        {
            AddLimitAndWhere(whereExpression);
            return ToList().First();
        }

        public T SingleOrDefault(Expression<Func<T, bool>> whereExpression = null)
        {
            AddLimitAndWhere(whereExpression);
            return ToList().SingleOrDefault();
        }

        public T Single(Expression<Func<T, bool>> whereExpression = null)
        {
            AddLimitAndWhere(whereExpression);
            return ToList().Single();
        }

        public int Count(Expression<Func<T, bool>> whereExpression = null)
        {
            if (whereExpression != null)
                _sqlExpression = _sqlExpression.Where(whereExpression);

            var wheresql = _sqlExpression.Context.ToWhereStatement();
            var sql = string.Format("SELECT COUNT(*) FROM {0} {1}",
                                    _database.DatabaseType.EscapeTableName(_database.PocoDataFactory.ForType(typeof(T)).TableInfo.TableName),
                                    wheresql);
            var parameters = _sqlExpression.Context.Params;
            return _database.ExecuteScalar<int>(sql, parameters);
        }

        public List<T> ToList()
        {
            if (!_joinSqlExpressions.Any())
                return _database.Fetch<T>(_sqlExpression.Context.ToSelectStatement(), _sqlExpression.Context.Params);

            var types = new[] { typeof(T) }.Concat(_joinSqlExpressions.Select(x => x.SqlExpression.Type)).ToArray();
            return _database.Query<T>(types, null, BuildJoinSql()).ToList();
        }

        private Sql BuildJoinSql()
        {
            var sql = _database.DatabaseType.BuildJoin<T>(_database, _sqlExpression, _joinSqlExpressions);
            return sql;
        }

        public ISimpleQueryProvider<T> Limit(int rows)
        {
            _sqlExpression = _sqlExpression.Limit(rows);
            return this;
        }

        public ISimpleQueryProvider<T> Limit(int skip, int rows)
        {
            _sqlExpression = _sqlExpression.Limit(skip, rows);
            return this;
        }

        public ISimpleQueryProvider<T> OrderBy(Expression<Func<T, object>> column)
        {
            _sqlExpression = _sqlExpression.OrderBy(column);
            return this;
        }

        public ISimpleQueryProvider<T> OrderByDescending(Expression<Func<T, object>> column)
        {
            _sqlExpression = _sqlExpression.OrderByDescending(column);
            return this;
        }

        public ISimpleQueryProvider<T> ThenBy(Expression<Func<T, object>> column)
        {
            _sqlExpression = _sqlExpression.ThenBy(column);
            return this;
        }

        public ISimpleQueryProvider<T> ThenByDescending(Expression<Func<T, object>> column)
        {
            _sqlExpression = _sqlExpression.ThenByDescending(column);
            return this;
        }
    }

    public class JoinData
    {
        public string OnSql { get; set; }
        public ISqlExpression SqlExpression { get; set; }
    }

    public interface ISimpleQueryProviderExpression<TModel>
    {
        SqlExpression<TModel> AtlasSqlExpression { get; }
    }
}
