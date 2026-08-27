using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace NPoco.FluentSqlBuilder
{
    internal sealed class ProjectionLeaf
    {
        internal Expression Expression;
        internal string Alias;
        internal int Index;
    }

    internal abstract class ProjectionNode
    {
        internal abstract object Materialize(object[] values, IDatabase database);
        internal abstract bool HasData(object[] values);
    }

    internal sealed class ProjectionPlan
    {
        internal List<ProjectionLeaf> Leaves { get; } = new List<ProjectionLeaf>();
        internal ProjectionNode Root { get; set; }

        internal TResult Materialize<TResult>(object[] values, IDatabase database)
            => (TResult)Root.Materialize(values, database);
    }

    internal sealed class ScalarProjectionNode : ProjectionNode
    {
        internal string Alias;
        internal Type Type;
        internal MemberInfo Member;
        internal PocoColumn Column;
        internal int Index;

        internal override object Materialize(object[] values, IDatabase database)
        {
            var value = values[Index];
            if (value == null || value == DBNull.Value)
                return MappingHelper.GetDefault(Type);
            var converter = Column == null
                ? MappingHelper.GetConverter(database.Mappers, null, value.GetType(), Type)
                : MappingHelper.GetConverter(database.Mappers, Column, value.GetType(), Type);
            return converter == null ? value : converter(value);
        }

        internal override bool HasData(object[] values) => values[Index] != null && values[Index] != DBNull.Value;
    }

    internal sealed class ObjectProjectionNode : ProjectionNode
    {
        internal Type Type;
        internal ConstructorInfo Constructor;
        internal List<ProjectionNode> Children;
        internal List<MemberInfo> Members;
        internal bool NullWhenAllNull;

        internal override object Materialize(object[] values, IDatabase database)
        {
            if (NullWhenAllNull && !HasData(values)) return null;
            var childValues = Children.Select(x => x.Materialize(values, database)).ToArray();
            if (Constructor != null) return Constructor.Invoke(childValues);
            var instance = Activator.CreateInstance(Type, true);
            for (var i = 0; i < Members.Count; i++) SetMember(Members[i], instance, childValues[i]);
            return instance;
        }

        internal override bool HasData(object[] values) => Children.Any(x => x.HasData(values));

        private static void SetMember(MemberInfo member, object instance, object value)
        {
            var property = member as PropertyInfo;
            if (property != null) { property.SetValue(instance, value, null); return; }
            ((FieldInfo)member).SetValue(instance, value);
        }
    }

    internal sealed class EntityProjectionNode : ProjectionNode
    {
        internal TableReference Table;
        internal List<ScalarProjectionNode> Columns;

        internal override object Materialize(object[] values, IDatabase database)
        {
            if (Columns.All(x => values[x.Index] == null || values[x.Index] == DBNull.Value)) return null;
            var instance = Table.PocoData.CreateObject(null);
            foreach (var column in Columns)
            {
                var value = values[column.Index];
                if (value == null || value == DBNull.Value) continue;
                var converter = MappingHelper.GetConverter(database.Mappers, column.Column, value.GetType(), column.Column.MemberInfoData.MemberType);
                column.Column.SetValue(instance, converter == null ? value : converter(value));
            }
            return instance;
        }

        internal override bool HasData(object[] values) => Columns.Any(x => x.HasData(values));
    }

    internal static class ProjectionPlanBuilder
    {
        internal static ProjectionPlan Build<TResult>(Expression<Func<TResult>> projection, IEnumerable<TableReference> tables)
        {
            var plan = new ProjectionPlan();
            plan.Root = BuildNode(projection.Body, typeof(TResult), null, plan, tables.ToArray());
            return plan;
        }

        private static ProjectionNode BuildNode(Expression expression, Type resultType, string path, ProjectionPlan plan, TableReference[] tables)
        {
            expression = StripConvert(expression);
            var rowTable = ResolveRow(expression, tables);
            if (rowTable != null)
            {
                var columns = rowTable.PocoData.QueryColumns.Select(x =>
                {
                    var column = x.Value;
                    var name = string.IsNullOrWhiteSpace(column.ColumnAlias) ? column.MemberInfoKey : column.ColumnAlias;
                    var alias = Combine(path, name);
                    var columnExpression = BuildColumnExpression(expression, column.MemberInfoChain);
                    var index = plan.Leaves.Count;
                    plan.Leaves.Add(new ProjectionLeaf { Expression = columnExpression, Alias = alias, Index = index });
                    return new ScalarProjectionNode { Alias = alias, Index = index, Type = column.MemberInfoData.MemberType, Member = column.MemberInfoData.MemberInfo, Column = column };
                }).ToList();
                return new EntityProjectionNode { Table = rowTable, Columns = columns };
            }

            var created = expression as NewExpression;
            if (created != null)
            {
                var names = created.Members?.Select(x => x.Name).ToArray() ?? created.Constructor.GetParameters().Select(x => FindMemberName(resultType, x.Name)).ToArray();
                return new ObjectProjectionNode
                {
                    Type = resultType,
                    Constructor = created.Constructor,
                    Children = created.Arguments.Select((x, i) => BuildNode(x, GetMemberType(resultType, names[i]) ?? x.Type, Combine(path, names[i]), plan, tables)).ToList(),
                    Members = new List<MemberInfo>(),
                    NullWhenAllNull = !string.IsNullOrEmpty(path)
                };
            }

            var initialized = expression as MemberInitExpression;
            if (initialized != null)
            {
                var assignments = initialized.Bindings.Cast<MemberAssignment>().ToArray();
                return new ObjectProjectionNode
                {
                    Type = resultType,
                    Constructor = null,
                    Children = assignments.Select(x => BuildNode(x.Expression, GetMemberType(resultType, x.Member.Name) ?? x.Expression.Type, Combine(path, x.Member.Name), plan, tables)).ToList(),
                    Members = assignments.Select(x => x.Member).ToList(),
                    NullWhenAllNull = !string.IsNullOrEmpty(path)
                };
            }

            if (string.IsNullOrEmpty(path)) throw new ArgumentException("A projection must create a result object with named members.", nameof(expression));
            var leafIndex = plan.Leaves.Count;
            plan.Leaves.Add(new ProjectionLeaf { Expression = expression, Alias = path, Index = leafIndex });
            return new ScalarProjectionNode { Alias = path, Index = leafIndex, Type = resultType };
        }

        private static Expression BuildColumnExpression(Expression row, IEnumerable<MemberInfo> members)
        {
            Expression expression = row;
            foreach (var member in members) expression = Expression.MakeMemberAccess(expression, member);
            return expression;
        }

        private static TableReference ResolveRow(Expression expression, IEnumerable<TableReference> tables)
        {
            var member = expression as MemberExpression;
            if (member == null || member.Member.Name != "Row") return null;
            var value = Expression.Lambda<Func<object>>(Expression.Convert(member.Expression, typeof(object))).Compile()() as TableReference;
            return value != null && tables.Contains(value) ? value : null;
        }

        private static Expression StripConvert(Expression expression)
        {
            while (expression.NodeType == ExpressionType.Convert || expression.NodeType == ExpressionType.ConvertChecked)
                expression = ((UnaryExpression)expression).Operand;
            return expression;
        }

        private static string FindMemberName(Type type, string name)
            => type.GetMember(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase).FirstOrDefault()?.Name ?? name;

        private static Type GetMemberType(Type type, string name)
        {
            var member = type.GetMember(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase).FirstOrDefault();
            return member is PropertyInfo property ? property.PropertyType : (member as FieldInfo)?.FieldType;
        }

        private static string Combine(string prefix, string name) => string.IsNullOrEmpty(prefix) ? name : prefix + PocoData.Separator + name;
    }

    internal static class ProjectionExecutor
    {
        internal static List<TResult> Fetch<TResult>(IDatabase database, Sql sql, ProjectionPlan plan)
        {
            var rows = new List<TResult>();
            database.OpenSharedConnection();
            try
            {
                using (var command = database.CreateCommand(database.Connection, CommandType.Text, sql.SQL, sql.Arguments))
                using (var reader = ((IDatabaseHelpers)database).ExecuteReaderHelper(command))
                {
                    while (reader.Read())
                    {
                        var values = new object[reader.FieldCount];
                        reader.GetValues(values);
                        rows.Add(plan.Materialize<TResult>(values, database));
                    }
                }
            }
            finally
            {
                database.CloseSharedConnection();
            }
            return rows;
        }

        internal static async Task<List<TResult>> FetchAsync<TResult>(IDatabase database, Sql sql, ProjectionPlan plan, CancellationToken cancellationToken)
        {
            var rows = new List<TResult>();
            await database.OpenSharedConnectionAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                using (var command = database.CreateCommand(database.Connection, CommandType.Text, sql.SQL, sql.Arguments))
                using (var reader = await ((IDatabaseHelpers)database).ExecuteReaderHelperAsync(command, cancellationToken).ConfigureAwait(false))
                {
                    while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    {
                        var values = new object[reader.FieldCount];
                        reader.GetValues(values);
                        rows.Add(plan.Materialize<TResult>(values, database));
                    }
                }
            }
            finally
            {
                await database.CloseSharedConnectionAsync().ConfigureAwait(false);
            }
            return rows;
        }
    }
}
