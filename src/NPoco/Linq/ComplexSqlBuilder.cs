using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using NPoco.Expressions;

namespace NPoco.Linq
{
    public class ComplexSqlBuilder<T>
    {
        private readonly IDatabase _database;
        private readonly SqlExpression<T> _sqlExpression;
        private readonly Dictionary<string, JoinData> _joinSqlExpressions;

        public ComplexSqlBuilder(IDatabase database, SqlExpression<T> sqlExpression, Dictionary<string, JoinData> joinSqlExpressions)
        {
            _database = database;
            _sqlExpression = sqlExpression;
            _joinSqlExpressions = joinSqlExpressions;
        }

        public Sql GetSqlForProjection<T2>(Expression<Func<T, T2>> projectionExpression, bool distinct)
        {
            var selectMembers = _database.DatabaseType.ExpressionVisitor<T>(_database).SelectProjection(projectionExpression);

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
            var modelDef = database.PocoDataFactory.ForType(typeof (T));
            var sqlTemplate = count
                ? "SELECT COUNT(*) FROM {1} {2} {3} {4}"
                : "SELECT {0} FROM {1} {2} {3} {4}";

            // build cols
            var cols = modelDef.QueryColumns.Select((x, j) => new StringPocoCol
            {
                StringCol = database.DatabaseType.EscapeTableName(modelDef.TableInfo.AutoAlias) + "." +
                            database.DatabaseType.EscapeSqlIdentifier(x.Value.ColumnName) + " as " + database.DatabaseType.EscapeSqlIdentifier(string.Join("__", x.Value.MemberInfoChain.Select(y=>y.Name))),
                PocoColumn = new[] { x.Value }
            });

            // build wheres
            var wheres = new Sql();
            var where = sqlExpression.Context.ToWhereStatement();
            wheres.Append(string.IsNullOrEmpty(where) ? string.Empty : "\n" + where, sqlExpression.Context.Params);

            // build joins and add cols
            var joins = BuildJoinSql(modelDef, database, joinSqlExpressions, ref cols);

            // build orderbys
            ISqlExpression exp = sqlExpression;
            var orderbys = string.Empty;
            if (!count && exp.OrderByMembers.Any())
            {
                var orderMembers = exp.OrderByMembers.Select(x =>
                {
                    return new
                    {
                        Column = string.Join("__", x.PocoColumns.Last().MemberInfoChain.Select(z => z.Name)),
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
                    var pocoData = database.PocoDataFactory.ForType(x.EntityType);
                    return new StringPocoCol
                    {
                        StringCol = database.DatabaseType.EscapeTableName(pocoData.TableInfo.AutoAlias) + "." +
                                    database.DatabaseType.EscapeSqlIdentifier(x.PocoColumn.ColumnName) + " as " + database.DatabaseType.EscapeSqlIdentifier(string.Join("__", x.PocoColumns.Last().MemberInfoChain.Select(z=>z.Name))),
                        PocoColumn = x.PocoColumns
                    };
                });
            }

            // replace templates
            var resultantSql = string.Format(sqlTemplate,
                (distinct ? "DISTINCT " : "") + string.Join(", ", cols.Select(x=>x.StringCol).ToArray()),
                database.DatabaseType.EscapeTableName(modelDef.TableInfo.TableName) + " " + database.DatabaseType.EscapeTableName(modelDef.TableInfo.AutoAlias),
                joins,
                wheres.SQL,
                orderbys);

            var newsql = ((ISqlExpression)_sqlExpression).ApplyPaging(resultantSql, cols.Select(x=>x.PocoColumn), _joinSqlExpressions);

            return new Sql(newsql, wheres.Arguments);
        }

        private static string BuildJoinSql(PocoData pocoData, IDatabase database, List<JoinData> joinSqlExpressions, ref IEnumerable<StringPocoCol> cols)
        {
            var joins = new List<string>();

            List<PocoMember> members = pocoData.Members;

            foreach (var joinSqlExpression in joinSqlExpressions)
            {
                var member = members.First(x => x.MemberInfo == joinSqlExpression.MemberInfo);

                cols = cols.Concat(member.PocoMemberChildren.Where(x=> x.ReferenceMappingType == ReferenceMappingType.None).Select(x => new StringPocoCol
                {
                    StringCol = database.DatabaseType.EscapeTableName(x.PocoColumn.TableInfo.AutoAlias)
                                + "." + database.DatabaseType.EscapeSqlIdentifier(x.PocoColumn.ColumnName) + " as " + database.DatabaseType.EscapeSqlIdentifier(string.Join("__", x.PocoColumn.MemberInfoChain.Select(z => z.Name))),
                    PocoColumn = new[] { x.PocoColumn }
                }));

                joins.Add("  LEFT JOIN " + member.ReferenceTableInfo.TableName + " " + database.DatabaseType.EscapeTableName(member.ReferenceTableInfo.AutoAlias) + " ON " + joinSqlExpression.OnSql);

                members = member.PocoMemberChildren;
            }

            return joins.Any() ? " \n" + string.Join(" \n", joins.ToArray()) : string.Empty;
        }
    }

    public class StringPocoCol
    {
        public string StringCol { get; set; }
        public PocoColumn[] PocoColumn { get; set; }
    }
}