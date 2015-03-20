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

        public Sql GetSqlForProjection<T2>(Expression<Func<T, T2>> projectionExpression, Type[] types, bool distinct)
        {
            var selectMembers = _database.DatabaseType.ExpressionVisitor<T>(_database).SelectProjection(projectionExpression);
            var newMembers = GetSelectMembers<T2>(types, selectMembers).ToList();

            ((ISqlExpression)_sqlExpression).SelectMembers.Clear();
            ((ISqlExpression)_sqlExpression).SelectMembers.AddRange(newMembers);

            if (!_joinSqlExpressions.Any())
            {
                var finalsql = ((ISqlExpression)_sqlExpression).ApplyPaging(_sqlExpression.Context.ToSelectStatement(false, distinct), newMembers.Select(x => x.PocoColumn));
                return new Sql(finalsql, _sqlExpression.Context.Params);
            }

            var sql = BuildJoin(_database, _sqlExpression, _joinSqlExpressions.Values.ToList(), newMembers, false, distinct);
            return sql;
        }

        private IEnumerable<SelectMember> GetSelectMembers<T2>(IEnumerable<Type> types, List<SelectMember> selectMembers)
        {
            var newMembers = new List<SelectMember>();
            foreach (var type in types)
            {
                var membersForType = selectMembers.Where(x => x.EntityType == type).ToList();
                if (membersForType.Any())
                {
                    newMembers.AddRange(membersForType);
                }
                else
                {
                    var pocoData = _database.PocoDataFactory.ForType(type);
                    var pk = pocoData.Columns.FirstOrDefault(x => x.Value.ColumnName == pocoData.TableInfo.PrimaryKey);
                    newMembers.Add(new SelectMember() {EntityType = type, PocoColumn = pk.Value});
                }
            }
            return newMembers;
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
                            database.DatabaseType.EscapeSqlIdentifier(x.Value.ColumnName) + " as " + database.DatabaseType.EscapeSqlIdentifier(x.Value.AutoAlias),
                PocoColumn = x.Value
            });

            // build wheres
            var wheres = new Sql();
            var where = sqlExpression.Context.ToWhereStatement();
            wheres.Append(string.IsNullOrEmpty(where) ? string.Empty : "\n" + where, sqlExpression.Context.Params);

            // build joins and add cols
            var joins = BuildJoinSql(database, joinSqlExpressions, ref cols);

            // build orderbys
            ISqlExpression exp = sqlExpression;
            var orderbys = string.Empty;
            if (!count && exp.OrderByMembers.Any())
            {
                var orderMembers = exp.OrderByMembers.Select(x => new
                {
                    Column = database.PocoDataFactory.ForType(x.EntityType).Columns.Values.Single(z => z.MemberInfo.Name == x.PocoColumn.MemberInfo.Name),
                    x.AscDesc
                }).ToList();

                orderbys = "\nORDER BY " + string.Join(", ", orderMembers.Select(x => database.DatabaseType.EscapeSqlIdentifier(x.Column.AutoAlias) + " " + x.AscDesc).ToArray());
            }

            // Override select columns with projected ones
            if (newMembers != null)
            {
                var selectMembers = ((ISqlExpression) _sqlExpression).OrderByMembers
                    .Select(x => new SelectMember() {PocoColumn = x.PocoColumn, EntityType = x.EntityType})
                    .Where(x => !newMembers.Any(y => y.EntityType == x.EntityType && y.PocoColumn.MemberInfo.Name == x.PocoColumn.MemberInfo.Name));

                cols = newMembers.Concat(selectMembers).Select(x =>
                {
                    var pocoData = database.PocoDataFactory.ForType(x.EntityType);
                    return new StringPocoCol
                    {
                        StringCol = database.DatabaseType.EscapeTableName(pocoData.TableInfo.AutoAlias) + "." +
                                    database.DatabaseType.EscapeSqlIdentifier(x.PocoColumn.ColumnName) + " as " + database.DatabaseType.EscapeSqlIdentifier(x.PocoColumn.AutoAlias),
                        PocoColumn = x.PocoColumn
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

            var newsql = ((ISqlExpression)_sqlExpression).ApplyPaging(resultantSql, cols.Select(x=>x.PocoColumn));

            return new Sql(newsql, wheres.Arguments);
        }

        private static string BuildJoinSql(IDatabase database, List<JoinData> joinSqlExpressions, ref IEnumerable<StringPocoCol> cols)
        {
            var joins = new List<string>();

            foreach (var joinSqlExpression in joinSqlExpressions)
            {
                var type = joinSqlExpression.Type;
                var joinModelDef = database.PocoDataFactory.ForType(type);
                var tableName = database.DatabaseType.EscapeTableName(joinModelDef.TableInfo.TableName);

                cols = cols.Concat(joinModelDef.QueryColumns.Select((x, j) => new StringPocoCol
                {
                    StringCol = database.DatabaseType.EscapeTableName(joinModelDef.TableInfo.AutoAlias)
                                + "." + database.DatabaseType.EscapeSqlIdentifier(x.Value.ColumnName) + " as " + database.DatabaseType.EscapeSqlIdentifier(x.Value.AutoAlias),
                    PocoColumn = x.Value
                }));

                joins.Add("  LEFT JOIN " + tableName + " " + database.DatabaseType.EscapeTableName(joinModelDef.TableInfo.AutoAlias) + " ON " + joinSqlExpression.OnSql);
            }

            return joins.Any() ? " \n" + string.Join(" \n", joins.ToArray()) : string.Empty;
        }
    }

    public class StringPocoCol
    {
        public string StringCol { get; set; }
        public PocoColumn PocoColumn { get; set; }
    }
}