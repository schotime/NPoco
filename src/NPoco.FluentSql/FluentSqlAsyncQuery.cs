using System;
using System.Linq.Expressions;

namespace NPoco.FluentSql
{
    /// <summary>A fluent SQL query whose projected result exposes async execution only.</summary>
    public sealed class FluentSqlAsyncQuery
    {
        private readonly FluentSqlQuery _query;

        internal FluentSqlAsyncQuery(FluentSqlQuery query) => _query = query;

        public FluentSqlAsyncQueryStage From<T>(out TableReference<T> table)
            => new FluentSqlAsyncQueryStage(_query.FromCore(out table));

        public FluentSqlAsyncQuery With<T>(Func<FluentSqlAsyncQuery, FluentSqlAsyncResult<T>> query, out TableReference<T> table)
        {
            _query.WithAsync(query, out table);
            return this;
        }

        public FluentSqlAsyncQuery With<T>(FluentSqlAsyncResult<T> query, out TableReference<T> table)
        {
            _query.WithAsync(query, out table);
            return this;
        }

        public FluentSqlAsyncQueryStage From<T>(TableReference<T> table)
            => new FluentSqlAsyncQueryStage(_query.FromCore(table));

        /// <inheritdoc cref="FluentSqlQuery.Table{T}()"/>
        public TableReference<T> Table<T>() => _query.Table<T>();

        /// <inheritdoc cref="FluentSqlQuery.Subquery()"/>
        public FluentSqlAsyncQuery Subquery() => new FluentSqlAsyncQuery(_query.CreateSubquery());
    }

    /// <summary>A FROM-complete fluent SQL query whose projection exposes async execution only.</summary>
    public sealed class FluentSqlAsyncQueryStage
    {
        private FluentSqlQuery _query;

        internal FluentSqlAsyncQueryStage(FluentSqlQuery query) => _query = query;

        private FluentSqlQuery Target()
        {
            if (_query.IsProjected) _query = _query.Rebase();
            return _query;
        }

        public FluentSqlAsyncQueryStage Where<T>(TableReference<T> table, Expression<Func<T, bool>> predicate) { Target().Where(table, predicate); return this; }
        public FluentSqlAsyncQueryStage WhereIf<T>(bool condition, TableReference<T> table, Expression<Func<T, bool>> predicate) { if (condition) Target().Where(table, predicate); return this; }
        public FluentSqlAsyncQueryStage Where(Expression<Func<bool>> predicate) { Target().Where(predicate); return this; }
        public FluentSqlAsyncQueryStage WhereIf(bool condition, Expression<Func<bool>> predicate) { if (condition) Target().Where(predicate); return this; }
        public FluentSqlAsyncQueryStage OrWhere(Expression<Func<bool>> predicate) { Target().OrWhere(predicate); return this; }
        public FluentSqlAsyncQueryStage OrWhere<T>(TableReference<T> table, Expression<Func<T, bool>> predicate) { Target().OrWhere(table, predicate); return this; }
        public FluentSqlAsyncQueryStage OrWhereIf(bool condition, Expression<Func<bool>> predicate) { if (condition) Target().OrWhere(predicate); return this; }
        public FluentSqlAsyncQueryStage OrWhereIf<T>(bool condition, TableReference<T> table, Expression<Func<T, bool>> predicate) { if (condition) Target().OrWhere(table, predicate); return this; }

        public FluentSqlPredicate CreatePredicate(Action<FluentSqlPredicateGroup> configure) => _query.CreatePredicate(configure);
        public FluentSqlAsyncQueryStage Where(FluentSqlPredicate predicate) { Target().Where(predicate); return this; }
        public FluentSqlAsyncQueryStage WhereIf(bool condition, FluentSqlPredicate predicate) { if (condition) Target().Where(predicate); return this; }
        public FluentSqlAsyncQueryStage OrWhere(FluentSqlPredicate predicate) { Target().OrWhere(predicate); return this; }
        public FluentSqlAsyncQueryStage OrWhereIf(bool condition, FluentSqlPredicate predicate) { if (condition) Target().OrWhere(predicate); return this; }
        public FluentSqlAsyncQueryStage WhereGroup(Action<FluentSqlPredicateGroup> group) { Target().WhereGroup(group); return this; }

        public FluentSqlAsyncQueryStage Having<T>(TableReference<T> table, Expression<Func<T, bool>> predicate) { Target().Having(table, predicate); return this; }
        public FluentSqlAsyncQueryStage HavingIf<T>(bool condition, TableReference<T> table, Expression<Func<T, bool>> predicate) { if (condition) Target().Having(table, predicate); return this; }
        public FluentSqlAsyncQueryStage Having(Expression<Func<bool>> predicate) { Target().Having(predicate); return this; }
        public FluentSqlAsyncQueryStage HavingIf(bool condition, Expression<Func<bool>> predicate) { if (condition) Target().Having(predicate); return this; }

        public FluentSqlAsyncQueryStage GroupBy<T, TValue>(TableReference<T> table, Expression<Func<T, TValue>> selector) { Target().GroupBy(table, selector); return this; }
        public FluentSqlAsyncQueryStage GroupBy<TValue>(Expression<Func<TValue>> selector) { Target().GroupBy(selector); return this; }
        public FluentSqlAsyncQueryStage OrderBy<T, TValue>(TableReference<T> table, Expression<Func<T, TValue>> selector, bool descending = false) { Target().OrderBy(table, selector, descending); return this; }
        public FluentSqlAsyncQueryStage OrderByDescending<T, TValue>(TableReference<T> table, Expression<Func<T, TValue>> selector) => OrderBy(table, selector, true);
        public FluentSqlAsyncQueryStage OrderBy<TValue>(Expression<Func<TValue>> selector, bool descending = false) { Target().OrderBy(selector, descending); return this; }
        public FluentSqlAsyncQueryStage OrderByDescending<TValue>(Expression<Func<TValue>> selector) => OrderBy(selector, true);
        public FluentSqlAsyncQueryStage ThenBy<T, TValue>(TableReference<T> table, Expression<Func<T, TValue>> selector) => OrderBy(table, selector);
        public FluentSqlAsyncQueryStage ThenByDescending<T, TValue>(TableReference<T> table, Expression<Func<T, TValue>> selector) => OrderBy(table, selector, true);
        public FluentSqlAsyncQueryStage ThenBy<TValue>(Expression<Func<TValue>> selector) => OrderBy(selector);
        public FluentSqlAsyncQueryStage ThenByDescending<TValue>(Expression<Func<TValue>> selector) => OrderBy(selector, true);
        public FluentSqlAsyncQueryStage Take(int count) { Target().Take(count); return this; }
        public FluentSqlAsyncQueryStage Skip(int count) { Target().Skip(count); return this; }
        public FluentSqlAsyncQueryStage Distinct() { Target().Distinct(); return this; }

        public FluentSqlAsyncQuery Subquery() => new FluentSqlAsyncQuery(_query.CreateSubquery());

        public FluentSqlAsyncResult<T> Select<T>(TableReference<T> table)
        {
            var query = Target();
            query.Project(table);
            return new FluentSqlAsyncResult<T>(query, query.Database);
        }

        public FluentSqlAsyncResult<TResult> Select<TResult>(Expression<Func<TResult>> projection)
        {
            var query = Target();
            query.Project(projection);
            return new FluentSqlAsyncResult<TResult>(query, query.Database);
        }

        public FluentSqlAsyncResult<TResult> Select<TResult>(Expression<Func<FSqlFunctions, TResult>> projection)
        {
            var query = Target();
            query.Project(projection);
            return new FluentSqlAsyncResult<TResult>(query, query.Database);
        }

        public FluentSqlAsyncResult<TValue> SelectScalar<T, TValue>(TableReference<T> table, Expression<Func<T, TValue>> selector)
        {
            var query = Target();
            query.ProjectScalar(table, selector);
            return new FluentSqlAsyncResult<TValue>(query, query.Database);
        }

        public FluentSqlAsyncResult<TValue> SelectScalar<TValue>(Expression<Func<TValue>> selector)
        {
            var query = Target();
            query.ProjectScalar(selector);
            return new FluentSqlAsyncResult<TValue>(query, query.Database);
        }

        public FluentSqlAsyncQueryStage InnerJoin<TJoin>(out TableReference<TJoin> table, Expression<Func<TJoin, bool>> on) => Join(FluentJoinType.Inner, out table, on);
        public FluentSqlAsyncQueryStage LeftJoin<TJoin>(out TableReference<TJoin> table, Expression<Func<TJoin, bool>> on) => Join(FluentJoinType.Left, out table, on);
        public FluentSqlAsyncQueryStage RightJoin<TJoin>(out TableReference<TJoin> table, Expression<Func<TJoin, bool>> on) => Join(FluentJoinType.Right, out table, on);
        public FluentSqlAsyncQueryStage FullOuterJoin<TJoin>(out TableReference<TJoin> table, Expression<Func<TJoin, bool>> on) => Join(FluentJoinType.FullOuter, out table, on);

        /// <inheritdoc cref="FluentSqlQuery.Table{T}()"/>
        public TableReference<T> Table<T>() => _query.Table<T>();

        public FluentSqlAsyncQueryStage InnerJoin<TJoin>(TableReference<TJoin> table, Expression<Func<bool>> on) => Join(FluentJoinType.Inner, table, on);
        public FluentSqlAsyncQueryStage LeftJoin<TJoin>(TableReference<TJoin> table, Expression<Func<bool>> on) => Join(FluentJoinType.Left, table, on);
        public FluentSqlAsyncQueryStage RightJoin<TJoin>(TableReference<TJoin> table, Expression<Func<bool>> on) => Join(FluentJoinType.Right, table, on);
        public FluentSqlAsyncQueryStage FullOuterJoin<TJoin>(TableReference<TJoin> table, Expression<Func<bool>> on) => Join(FluentJoinType.FullOuter, table, on);

        public FluentSqlAsyncQueryStage OuterApply<TApply>(out TableReference<TApply> table, Func<FluentSqlAsyncQuery, FluentSqlAsyncResult<TApply>> subquery)
        {
            Target().OuterApplyAsync(out table, subquery);
            return this;
        }

        private FluentSqlAsyncQueryStage Join<TJoin>(FluentJoinType type, out TableReference<TJoin> table, LambdaExpression on)
        {
            Target().AddJoin(type, out table, on);
            return this;
        }

        private FluentSqlAsyncQueryStage Join<TJoin>(FluentJoinType type, TableReference<TJoin> table, LambdaExpression on)
        {
            Target().AddJoin(type, table, on);
            return this;
        }
    }
}
