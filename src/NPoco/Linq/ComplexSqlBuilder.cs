using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using NPoco.Expressions;

namespace NPoco.Linq
{
    public class ComplexSqlBuilder<T>
    {
        private readonly IDatabase _database;
        private readonly PocoData _pocoData;
        private readonly SqlExpression<T> _sqlExpression;
        private readonly Dictionary<string, JoinData> _joinSqlExpressions;

        public ComplexSqlBuilder(IDatabase database, PocoData pocoData, SqlExpression<T> sqlExpression, Dictionary<string, JoinData> joinSqlExpressions)
        {
            _database = database;
            _pocoData = pocoData;
            _sqlExpression = sqlExpression;
            _joinSqlExpressions = joinSqlExpressions;
        }

        public Sql GetSqlForProjection<T2>(Expression<Func<T, T2>> projectionExpression, bool distinct)
        {
            var selectMembers = _database.DatabaseType.ExpressionVisitor<T>(_database, _pocoData).SelectProjection(projectionExpression);

            ((ISqlExpression)_sqlExpression).SelectMembers.Clear();
            ((ISqlExpression)_sqlExpression).SelectMembers.AddRange(selectMembers);

            if (!_joinSqlExpressions.Any())
            {
                var finalsql = ((ISqlExpression)_sqlExpression).ApplyPaging(_sqlExpression.Context.ToSelectStatement(false, distinct), selectMembers.Select(x => x.PocoColumns), _joinSqlExpressions);
                return new Sql(finalsql, _sqlExpression.Context.Params);
            }

            var sql = BuildJoin(_database, _sqlExpression, _joinSqlExpressions.Values.ToList(), selectMembers, false, distinct);
            return sql;
        }

        public Sql BuildJoin(IDatabase database, SqlExpression<T> sqlExpression, List<JoinData> joinSqlExpressions, List<SelectMember> newMembers, bool count, bool distinct)
        {
            var modelDef = _pocoData;
            var sqlTemplate = count
                ? "SELECT COUNT(*) FROM {1} {2} {3} {4}"
                : "SELECT {0} FROM {1} {2} {3} {4}";

            // build cols
            var cols = modelDef.QueryColumns
                .Select(x => x.Value)
                .Select((x, j) =>
                {
                    var col = new StringPocoCol();
                    col.StringCol = database.DatabaseType.EscapeTableName(x.TableInfo.AutoAlias) + "." +
                                    database.DatabaseType.EscapeSqlIdentifier(x.ColumnName) + " as " + database.DatabaseType.EscapeSqlIdentifier(x.MemberInfoKey);
                    col.PocoColumn = new[] { x };
                    return col;
                }).ToList();

            // build wheres
            var where = sqlExpression.Context.ToWhereStatement();
            where = (string.IsNullOrEmpty(where) ? string.Empty : "\n" + where);

            // build joins and add cols
            var joins = BuildJoinSql(database, joinSqlExpressions, ref cols);

            // build orderbys
            ISqlExpression exp = sqlExpression;
            var orderbys = string.Empty;
            if (!count && exp.OrderByMembers.Any())
            {
                var orderMembers = exp.OrderByMembers.Select(x =>
                {
                    return new
                    {
                        Column = x.PocoColumns.Last().MemberInfoKey,
                        x.AscDesc
                    };
                }).ToList();

                orderbys = "\nORDER BY " + string.Join(", ", orderMembers.Select(x => database.DatabaseType.EscapeSqlIdentifier(x.Column) + " " + x.AscDesc).ToArray());
            }

            // Override select columns with projected ones
            if (newMembers != null)
            {
                var selectMembers = ((ISqlExpression) _sqlExpression).OrderByMembers
                    .Select(x => new SelectMember() {PocoColumn = x.PocoColumn, EntityType = x.EntityType, PocoColumns = x.PocoColumns})
                    .Where(x => !newMembers.Any(y => y.EntityType == x.EntityType && y.PocoColumns.SequenceEqual(x.PocoColumns)));

                cols = newMembers.Concat(selectMembers).Select(x =>
                {
                    return new StringPocoCol
                    {
                        StringCol = database.DatabaseType.EscapeTableName(x.PocoColumn.TableInfo.AutoAlias) + "." +
                                    database.DatabaseType.EscapeSqlIdentifier(x.PocoColumn.ColumnName) + " as " + database.DatabaseType.EscapeSqlIdentifier(x.PocoColumns.Last().MemberInfoKey),
                        PocoColumn = x.PocoColumns
                    };
                }).ToList();
            }

            // replace templates
            var resultantSql = string.Format(sqlTemplate,
                (distinct ? "DISTINCT " : "") + string.Join(", ", cols.Select(x=>x.StringCol).ToArray()),
                database.DatabaseType.EscapeTableName(modelDef.TableInfo.TableName) + " " + database.DatabaseType.EscapeTableName(modelDef.TableInfo.AutoAlias),
                joins,
                where,
                orderbys);

            var newsql = count ? resultantSql : ((ISqlExpression)_sqlExpression).ApplyPaging(resultantSql, cols.Select(x=>x.PocoColumn), _joinSqlExpressions);

            return new Sql(newsql, _sqlExpression.Context.Params);
        }

        private static string BuildJoinSql(IDatabase database, List<JoinData> joinSqlExpressions, ref List<StringPocoCol> cols)
        {
            var joins = new List<string>();

            foreach (var joinSqlExpression in joinSqlExpressions)
            {
                var member = joinSqlExpression.PocoMemberJoin;

                cols = cols.Concat(joinSqlExpression.PocoMembers
                    .Where(x => x.ReferenceType == ReferenceType.None && x.PocoColumn != null && !x.PocoColumn.ResultColumn)
                    .Select(x => new StringPocoCol
                {
                    StringCol = database.DatabaseType.EscapeTableName(x.PocoColumn.TableInfo.AutoAlias)
                                + "." + database.DatabaseType.EscapeSqlIdentifier(x.PocoColumn.ColumnName) + " as " + database.DatabaseType.EscapeSqlIdentifier(x.PocoColumn.MemberInfoKey),
                    PocoColumn = new[] { x.PocoColumn }
                })).ToList(); 

                joins.Add(string.Format("  {0} JOIN " + database.DatabaseType.EscapeTableName(member.PocoColumn.TableInfo.TableName) + " " + database.DatabaseType.EscapeTableName(member.PocoColumn.TableInfo.AutoAlias) + " ON " + joinSqlExpression.OnSql, joinSqlExpression.JoinType == JoinType.Inner ? "INNER" : "LEFT"));
            }

            return joins.Any() ? " \n" + string.Join(" \n", joins.ToArray()) : string.Empty;
        }

        public Dictionary<string, JoinData> GetJoinExpressions(Expression expression, string tableAlias, JoinType joinType)
        {
            var memberInfos = MemberChainHelper.GetMembers(expression);
            var members = _pocoData.Members;
            var joinExpressions = new Dictionary<string, JoinData>();

            foreach (var memberInfo in memberInfos)
            {
                var pocoMember = members
                    .Where(x => x.ReferenceType != ReferenceType.None)
                    .Single(x => x.MemberInfoData.MemberInfo.Name == memberInfo.Name);

                var pocoColumn1 = pocoMember.PocoColumn;
                var pocoMember2 = pocoMember.PocoMemberChildren.Single(x => x.Name == pocoMember.ReferenceMemberName);
                var pocoColumn2 = pocoMember2.PocoColumn;

                pocoColumn2.TableInfo.AutoAlias = tableAlias ?? pocoColumn2.TableInfo.AutoAlias;

                var onSql = _database.DatabaseType.EscapeTableName(pocoColumn1.TableInfo.AutoAlias)
                            + "." + _database.DatabaseType.EscapeSqlIdentifier(pocoColumn1.ColumnName)
                            + " = " + _database.DatabaseType.EscapeTableName(pocoColumn2.TableInfo.AutoAlias)
                            + "." + _database.DatabaseType.EscapeSqlIdentifier(pocoColumn2.ColumnName);

                if (!joinExpressions.ContainsKey(onSql))
                {
                    joinExpressions.Add(onSql, new JoinData()
                    {
                        OnSql = onSql,
                        PocoMember = pocoMember,
                        PocoMemberJoin = pocoMember2,
                        PocoMembers = pocoMember.PocoMemberChildren,
                        JoinType = joinType
                    });
                }

                members = pocoMember.PocoMemberChildren;
            }

            return joinExpressions;
        }
    }

    public class StringPocoCol
    {
        public string StringCol { get; set; }
        public PocoColumn[] PocoColumn { get; set; }
    }
}