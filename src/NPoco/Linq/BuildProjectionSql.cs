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

        public Sql GetSqlForProjection<T2>(Expression<Func<T, T2>> projectionExpression, Type[] types)
        {
            var selectMembers = _sqlExpression.SelectProjection(projectionExpression);
            var newMembers = GetSelectMembers<T2>(types, selectMembers);

            if (!_joinSqlExpressions.Any())
            {
                var finalsql = ((ISqlExpression)_sqlExpression).ApplyPaging(_sqlExpression.Context.ToSelectStatement(false), string.Join(", ", newMembers.Select(x => x.PocoColumn.AutoAlias)));
                return new Sql(finalsql, _sqlExpression.Context.Params);
            }

            var sql = BuildJoin(_database, _sqlExpression, _joinSqlExpressions.Values.ToList(), false);
            var final = ((ISqlExpression)_sqlExpression).ApplyPaging(sql.SQL, string.Join(", ", newMembers.Select(x => x.PocoColumn.AutoAlias)));
            return new Sql(final, sql.Arguments);
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
                    newMembers.Add(new SelectMember() {EntityType = type, PocoColumn = pk.Value, SelectSql = pk.Value.AutoAlias});
                }
            }
            return newMembers;
        }

        public Sql BuildJoin<T>(IDatabase database, SqlExpression<T> sqlExpression, List<JoinData> joinSqlExpressions, bool count)
        {
            var modelDef = database.PocoDataFactory.ForType(typeof(T));
            var sqlTemplate = count
                ? "SELECT COUNT(*) FROM {1} {2} {3} {4}"
                : "SELECT {0} FROM {1} {2} {3} {4}";

            // build cols
            var cols = modelDef.QueryColumns.Select((x, j) =>
                database.DatabaseType.EscapeTableName(modelDef.TableInfo.AutoAlias) + "." +
                database.DatabaseType.EscapeSqlIdentifier(x.Value.ColumnName) + " as " + x.Value.AutoAlias);

            // build wheres
            var wheres = new Sql();
            var where = sqlExpression.Context.ToWhereStatement();
            wheres.Append(string.IsNullOrEmpty(where) ? string.Empty : "\n" + where, sqlExpression.Context.Params);

            // build joins and add cols
            var joins = BuildJoinSql<T>(database, joinSqlExpressions, ref cols);

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

                orderbys = "\nORDER BY " + string.Join(", ", orderMembers.Select(x => x.Column.AutoAlias + " " + x.AscDesc));
            }

            // replace templates
            var resultantSql = string.Format(sqlTemplate,
                string.Join(", ", cols),
                database.DatabaseType.EscapeTableName(modelDef.TableInfo.TableName) + " " + modelDef.TableInfo.AutoAlias,
                joins,
                wheres.SQL,
                orderbys);

            return new Sql(resultantSql, wheres.Arguments);
        }

        private static string BuildJoinSql<T>(IDatabase database, List<JoinData> joinSqlExpressions, ref IEnumerable<string> cols)
        {
            var joins = new List<string>();

            foreach (var joinSqlExpression in joinSqlExpressions)
            {
                var type = joinSqlExpression.Type;
                var joinModelDef = database.PocoDataFactory.ForType(type);
                var tableName = database.DatabaseType.EscapeTableName(joinModelDef.TableInfo.TableName);

                cols = cols.Concat(joinModelDef.QueryColumns.Select((x, j) => database.DatabaseType.EscapeTableName(joinModelDef.TableInfo.AutoAlias)
                    + "." + database.DatabaseType.EscapeSqlIdentifier(x.Value.ColumnName) + " as " + x.Value.AutoAlias));

                joins.Add("  LEFT JOIN " + tableName + " " + joinModelDef.TableInfo.AutoAlias + " ON " + joinSqlExpression.OnSql);
            }

            return joins.Any() ? " \n" + string.Join(" \n", joins) : string.Empty;
        }
    }
}