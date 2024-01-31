using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using NPoco.Linq;

namespace NPoco.Expressions
{
    public interface ISqlExpression
    {
        List<OrderByMember> OrderByMembers { get; }
        int? Rows { get; }
        int? Skip { get; }
        string WhereSql { get; }
        object[] Params { get; }
        Type Type { get; }
        List<SelectMember> SelectMembers { get; }
        List<GeneralMember> GeneralMembers { get; }
        string ApplyPaging(string sql, IEnumerable<PocoColumn[]> columns, Dictionary<string, JoinData> joinSqlExpressions);
        string TableHint { get; }
    }

    public interface ISqlExpression<T> : ISqlExpression
    {
        ISqlExpressionContext Context { get; }

        ISqlExpression<T> GroupBy<TKey>(Expression<Func<T, TKey>> keySelector);
        ISqlExpression<T> Limit(int rows);
        ISqlExpression<T> Limit(int skip, int rows);
        string On<T2>(Expression<Func<T, T2, bool>> predicate);
        ISqlExpression<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector);
        ISqlExpression<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector);
        ISqlExpression<T> Select<TKey>(Expression<Func<T, TKey>> fields);
        List<SelectMember> SelectDistinct<TKey>(Expression<Func<T, TKey>> fields);
        List<SelectMember> SelectProjection<TKey>(Expression<Func<T, TKey>> fields);
        void Hint(string hint);
        ISqlExpression<T> ThenBy<TKey>(Expression<Func<T, TKey>> keySelector);
        ISqlExpression<T> ThenByDescending<TKey>(Expression<Func<T, TKey>> keySelector);
        ISqlExpression<T> Update<TKey>(Expression<Func<T, TKey>> fields);
        ISqlExpression<T> Where(Expression<Func<T, bool>> predicate);
        ISqlExpression<T> Where(string sqlFilter, params object[] filterParams);

        public interface ISqlExpressionContext
        {
            object[] Params { get; }
            List<string> UpdateFields { get; set; }

            string ToDeleteStatement();
            string ToSelectStatement();
            string ToSelectStatement(bool applyPaging, bool distinct);
            string ToUpdateStatement(T item);
            string ToUpdateStatement(T item, bool excludeDefaults);
            string ToUpdateStatement(T item, bool excludeDefaults, bool allFields);
            string ToWhereStatement();
        }
    }
}