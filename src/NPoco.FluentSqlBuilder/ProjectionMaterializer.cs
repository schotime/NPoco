using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NPoco.RowMappers;

namespace NPoco.FluentSqlBuilder
{
    internal sealed class ProjectionLeaf
    {
        internal Expression Expression;
        internal string Alias;
        internal int Index;
    }

    /// <summary>
    /// Everything a node needs to bind itself to a particular reader shape. Resolved once per
    /// query in <see cref="ProjectionPlan.Init"/>, never per row.
    /// </summary>
    internal sealed class ProjectionInitContext
    {
        internal DbDataReader Reader;
        internal IMapperCollection Mappers;

        private readonly Dictionary<string, int> _exact = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _ignoringCase = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _ambiguous = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        internal void AddColumn(string name, int ordinal)
        {
            if (!_exact.ContainsKey(name)) _exact.Add(name, ordinal);
            if (_ignoringCase.ContainsKey(name)) _ambiguous.Add(name);
            else _ignoringCase.Add(name, ordinal);
        }

        internal int Ordinal(string alias)
        {
            int ordinal;

            // An exact hit is always right, and keeps aliases that differ only by case apart.
            if (_exact.TryGetValue(alias, out ordinal)) return ordinal;

            // Otherwise fall back to a case-insensitive match, for providers that fold identifiers.
            if (_ambiguous.Contains(alias))
                throw new InvalidOperationException("The projected column '" + alias
                    + "' matches more than one column returned by the query, differing only by case.");
            if (_ignoringCase.TryGetValue(alias, out ordinal)) return ordinal;

            throw new InvalidOperationException("The projected column '" + alias + "' was not returned by the query.");
        }
    }

    internal abstract class ProjectionNode
    {
        internal abstract void Init(ProjectionInitContext context);
        internal abstract object Materialize(object[] values, DbDataReader reader);
        internal abstract bool HasData(object[] values);

        protected static void NotifyLoaded(object instance)
        {
            var loaded = instance as IOnLoaded;
            if (loaded != null) loaded.OnLoaded();
        }
    }

    /// <summary>
    /// Materializes an arbitrarily shaped projection - anonymous types, constructor calls, member
    /// initialisers, whole entities, or any nesting of those - from a single row.
    ///
    /// It is an <see cref="IRowMapper"/> so that NPoco's own query pipeline runs it. That pipeline
    /// owns the connection, transaction, interceptors and exception handling; this type owns only
    /// the mapping of reader values onto the result graph.
    /// </summary>
    internal sealed class ProjectionPlan : IRowMapper
    {
        private readonly IMapperCollection _mappers;

        internal ProjectionPlan(IMapperCollection mappers)
        {
            _mappers = mappers;
        }

        internal List<ProjectionLeaf> Leaves { get; } = new List<ProjectionLeaf>();
        internal ProjectionNode Root { get; set; }

        public bool ShouldMap(PocoData pocoData) => true;

        public void Init(DbDataReader dataReader, PocoData pocoData)
        {
            var context = new ProjectionInitContext
            {
                Reader = dataReader,
                Mappers = _mappers ?? (pocoData == null ? null : pocoData.Mapper)
            };
            for (var i = 0; i < dataReader.FieldCount; i++) context.AddColumn(dataReader.GetName(i), i);
            Root.Init(context);
        }

        public object Map(DbDataReader dataReader, RowMapperContext context)
        {
            var values = new object[dataReader.FieldCount];
            dataReader.GetValues(values);
            return Root.Materialize(values, dataReader);
        }
    }

    internal sealed class ScalarProjectionNode : ProjectionNode
    {
        internal string Alias;
        internal Type Type;
        internal MemberInfo Member;
        internal PocoColumn Column;

        private int _ordinal;
        private Func<object, object> _converter;
        private object _default;

        internal int Ordinal => _ordinal;

        internal override void Init(ProjectionInitContext context)
        {
            _ordinal = context.Ordinal(Alias);
            _default = MappingHelper.GetDefault(Type);
            _converter = MappingHelper.GetConverter(context.Mappers, Column, context.Reader.GetFieldType(_ordinal), Type);
        }

        internal override object Materialize(object[] values, DbDataReader reader)
        {
            var value = values[_ordinal];
            if (value == null || value == DBNull.Value) return _default;
            return _converter == null ? value : _converter(value);
        }

        internal override bool HasData(object[] values)
        {
            var value = values[_ordinal];
            return value != null && value != DBNull.Value;
        }
    }

    internal sealed class ObjectProjectionNode : ProjectionNode
    {
        internal Type Type;
        internal ConstructorInfo Constructor;
        internal List<ProjectionNode> Children;
        internal List<MemberInfo> Members;
        internal bool NullWhenAllNull;

        private ProjectionNode[] _children;
        private Func<object[], object> _construct;
        private IFastCreate _create;
        private MemberAccessor[] _setters;
        private bool _notifyLoaded;

        internal override void Init(ProjectionInitContext context)
        {
            _children = Children.ToArray();
            foreach (var child in _children) child.Init(context);

            _notifyLoaded = typeof(IOnLoaded).IsAssignableFrom(Type);

            if (Constructor != null)
            {
                _construct = BuildConstructor(Constructor);
                return;
            }

            _create = new FastCreate(Type, context.Mappers);
            _setters = Members.Select(x => new MemberAccessor(Type, x.Name)).ToArray();
        }

        internal override object Materialize(object[] values, DbDataReader reader)
        {
            if (NullWhenAllNull && !HasData(values)) return null;

            object instance;
            if (_construct != null)
            {
                var arguments = new object[_children.Length];
                for (var i = 0; i < _children.Length; i++) arguments[i] = _children[i].Materialize(values, reader);
                instance = _construct(arguments);
            }
            else
            {
                instance = _create.Create(reader);
                for (var i = 0; i < _children.Length; i++)
                    _setters[i].Set(instance, _children[i].Materialize(values, reader));
            }

            if (_notifyLoaded) NotifyLoaded(instance);
            return instance;
        }

        internal override bool HasData(object[] values)
        {
            for (var i = 0; i < _children.Length; i++)
                if (_children[i].HasData(values)) return true;
            return false;
        }

        private static Func<object[], object> BuildConstructor(ConstructorInfo constructor)
        {
            try
            {
                var arguments = Expression.Parameter(typeof(object[]), "args");
                var parameters = constructor.GetParameters()
                    .Select((x, i) => (Expression)Expression.Convert(Expression.ArrayIndex(arguments, Expression.Constant(i)), x.ParameterType));
                var construct = Expression.New(constructor, parameters);
                return Expression.Lambda<Func<object[], object>>(Expression.Convert(construct, typeof(object)), arguments).Compile();
            }
            catch (Exception)
            {
                // A constructor the expression compiler will not emit a call to (an inaccessible
                // one, or a platform without dynamic code) still works through reflection.
                return constructor.Invoke;
            }
        }
    }

    internal sealed class EntityProjectionNode : ProjectionNode
    {
        internal TableReference Table;
        internal List<ScalarProjectionNode> Columns;

        private ScalarProjectionNode[] _columns;
        private PocoData _pocoData;
        private bool _notifyLoaded;

        internal override void Init(ProjectionInitContext context)
        {
            _columns = Columns.ToArray();
            foreach (var column in _columns) column.Init(context);
            _pocoData = Table.PocoData;
            _notifyLoaded = typeof(IOnLoaded).IsAssignableFrom(_pocoData.Type);
        }

        internal override object Materialize(object[] values, DbDataReader reader)
        {
            if (!HasData(values)) return null;

            var instance = _pocoData.CreateObject(reader);
            for (var i = 0; i < _columns.Length; i++)
            {
                var column = _columns[i];
                var value = values[column.Ordinal];
                if (value == null || value == DBNull.Value) continue;
                column.Column.SetValue(instance, column.Materialize(values, reader));
            }

            if (_notifyLoaded) NotifyLoaded(instance);
            return instance;
        }

        internal override bool HasData(object[] values)
        {
            for (var i = 0; i < _columns.Length; i++)
                if (_columns[i].HasData(values)) return true;
            return false;
        }
    }

    internal static class ProjectionPlanBuilder
    {
        internal static ProjectionPlan Build<TResult>(Expression<Func<TResult>> projection, IEnumerable<TableReference> tables, IMapperCollection mappers)
        {
            var plan = new ProjectionPlan(mappers);
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
                    return new ScalarProjectionNode { Alias = alias, Type = column.MemberInfoData.MemberType, Member = column.MemberInfoData.MemberInfo, Column = column };
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
            return new ScalarProjectionNode { Alias = path, Type = resultType };
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

        /// <summary>
        /// An exact match wins; the case-insensitive pass exists so a constructor parameter
        /// (<c>id</c>) finds its member (<c>Id</c>), and must not override a member that matches
        /// the name as written.
        /// </summary>
        private static MemberInfo FindMember(Type type, string name)
        {
            var exact = type.GetMember(name, BindingFlags.Public | BindingFlags.Instance);
            if (exact.Length > 0) return exact[0];
            return type.GetMember(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase).FirstOrDefault();
        }

        private static string FindMemberName(Type type, string name) => FindMember(type, name)?.Name ?? name;

        private static Type GetMemberType(Type type, string name)
        {
            var member = FindMember(type, name);
            return member is PropertyInfo property ? property.PropertyType : (member as FieldInfo)?.FieldType;
        }

        private static string Combine(string prefix, string name) => string.IsNullOrEmpty(prefix) ? name : prefix + PocoData.Separator + name;
    }
}
