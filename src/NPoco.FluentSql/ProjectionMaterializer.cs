using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NPoco.RowMappers;

namespace NPoco.FluentSql
{
    internal sealed class ProjectionLeaf
    {
        internal Expression Expression = null!;
        internal string Alias = null!;
        internal int Index;
    }

    /// <summary>
    /// Everything a node needs to bind itself to a particular reader shape. Resolved once per
    /// query in <see cref="ProjectionPlan.Init"/>, never per row.
    /// </summary>
    internal sealed class ProjectionInitContext
    {
        internal DbDataReader Reader = null!;
        internal IMapperCollection? Mappers;

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
        internal abstract object? Materialize(object[] values, DbDataReader reader);
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
        private readonly IMapperCollection? _mappers;

        internal ProjectionPlan(IMapperCollection? mappers)
        {
            _mappers = mappers;
        }

        internal List<ProjectionLeaf> Leaves { get; } = new List<ProjectionLeaf>();
        internal ProjectionNode Root { get; set; } = null!;

        /// <summary>
        /// Always true: the plan was built for this query, so it maps whatever the query returns rather
        /// than deciding by poco type.
        /// </summary>
        /// <param name="pocoData">The mapping metadata NPoco would otherwise map by, unused here.</param>
        /// <returns>Always <see langword="true"/>.</returns>
        public bool ShouldMap(PocoData pocoData) => true;

        /// <summary>
        /// Binds the plan to the shape of this reader, resolving every projected alias to an ordinal
        /// once, before the first row.
        /// </summary>
        /// <param name="dataReader">The reader the query is about to be read from.</param>
        /// <param name="pocoData">Mapping metadata, used for its mapper collection when the plan was built without one.</param>
        /// <exception cref="InvalidOperationException">A projected column is missing from the reader, or matches more than one of its columns.</exception>
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

        /// <summary>Materialises the current row into the projected result graph.</summary>
        /// <param name="dataReader">The reader, positioned on the row to map.</param>
        /// <param name="context">NPoco's per-row mapping context, unused: the plan carries its own shape.</param>
        /// <returns>The projected row, or null when every column it reads came back null.</returns>
        public object? Map(DbDataReader dataReader, RowMapperContext context)
        {
            var values = new object[dataReader.FieldCount];
            dataReader.GetValues(values);
            return Root.Materialize(values, dataReader);
        }
    }

    internal sealed class ScalarProjectionNode : ProjectionNode
    {
        internal string? Alias;
        internal Type Type = null!;
        internal MemberInfo? Member;
        internal PocoColumn? Column;
        // Whether Column is the member being written to rather than the column being read. The two
        // are the same thing for a leaf that reads a column, and different for one that computes
        // a value the member merely receives.
        internal bool ColumnIsDestination;

        private int _ordinal;
        private Func<object, object>? _converter;
        private object? _default;

        internal int Ordinal => _ordinal;

        internal override void Init(ProjectionInitContext context)
        {
            // A scalar projection emits no alias - the SQL is the bare column - so the only column
            // the query returns is the one it asked for.
            _ordinal = Alias == null ? 0 : context.Ordinal(Alias);
            _default = MappingHelper.GetDefault(Type);

            var fieldType = context.Reader.GetFieldType(_ordinal);
            _converter = MappingHelper.GetConverter(context.Mappers, ColumnToReadBy(context, fieldType), fieldType, Type);
        }

        /// <summary>
        /// The column the value is read back through, which is usually the one the leaf reads. A
        /// leaf that reads no column borrows the member it is written onto instead, and there the
        /// column describes how that member is stored rather than what this expression produced: a
        /// serialized member says the value arrives as text to deserialize, which a value the
        /// provider already materialized as the member's own type - a Postgres array, a blob - did
        /// not. Deserializing it would read it as a stored form it never had, so it is read as it
        /// stands. A converter the member carries still applies, because it is chosen first and is
        /// about the member rather than its storage.
        /// </summary>
        private PocoColumn? ColumnToReadBy(ProjectionInitContext context, Type fieldType)
        {
            if (!ColumnIsDestination || Column == null) return Column;
            if (!Column.SerializedColumn || !Type.IsAssignableFrom(fieldType)) return Column;
            return context.Mappers?.FindFromDbConverter(Column.MemberInfoData.MemberInfo, fieldType) == null ? null : Column;
        }

        internal override object? Materialize(object[] values, DbDataReader reader)
        {
            var value = values[_ordinal];
            // The whole projection is this one value when it carries no alias, and a null one is
            // handed back as null - what NPoco's own single-column mapping does. Inside an object
            // the member takes its default instead, so a null number leaves the rest of the row
            // intact rather than reading as absent.
            if (value == null || value == DBNull.Value) return Alias == null ? null : _default;
            return _converter == null ? value : _converter(value);
        }

        internal override bool HasData(object[] values)
        {
            var value = values[_ordinal];
            return value != null && value != DBNull.Value;
        }
    }

    /// <summary>
    /// A single value-object column read on its own. The wrapper is only ever built by
    /// <see cref="PocoColumn.SetValue"/>, so the value goes onto a throwaway instance of the poco
    /// that declares it and comes back off the member - which is what a whole-entity read does too,
    /// and the only way to get the same object out of a one-column query.
    /// </summary>
    internal sealed class ValueObjectProjectionNode : ProjectionNode
    {
        internal PocoColumn Column = null!;
        internal string? Alias;

        private int _ordinal;
        private IFastCreate _create = null!;
        private MemberAccessor _accessor = null!;
        private Func<object, object>? _converter;

        internal override void Init(ProjectionInitContext context)
        {
            // Null alias: the query projects this column alone, so it is the only one returned.
            _ordinal = Alias == null ? 0 : context.Ordinal(Alias);
            var declaring = Column.MemberInfoData.DeclaringType;
            _create = new FastCreate(declaring, context.Mappers);
            _accessor = new MemberAccessor(declaring, Column.MemberInfoData.MemberInfo.Name);
            _converter = MappingHelper.GetConverter(context.Mappers, Column, context.Reader.GetFieldType(_ordinal), Column.ColumnType);
        }

        internal override object? Materialize(object[] values, DbDataReader reader)
        {
            var value = values[_ordinal];
            if (value == null || value == DBNull.Value) return null;

            var owner = _create.Create(reader);
            Column.SetValue(owner, _converter == null ? value : _converter(value));
            return _accessor.Get(owner);
        }

        internal override bool HasData(object[] values)
        {
            var value = values[_ordinal];
            return value != null && value != DBNull.Value;
        }
    }

    internal sealed class ObjectProjectionNode : ProjectionNode
    {
        internal Type Type = null!;
        internal ConstructorInfo? Constructor;
        internal List<ProjectionNode> Children = null!;
        internal List<MemberInfo> Members = null!;
        internal bool NullWhenAllNull;

        private ProjectionNode[] _children = null!;
        private Func<object[], object>? _construct;
        private IFastCreate _create = null!;
        private MemberAccessor[] _setters = null!;
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

        internal override object? Materialize(object[] values, DbDataReader reader)
        {
            if (NullWhenAllNull && !HasData(values)) return null;

            object instance;
            if (_construct != null)
            {
                var arguments = new object[_children.Length];
                for (var i = 0; i < _children.Length; i++) arguments[i] = _children[i].Materialize(values, reader)!;
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
        internal TableReference Table = null!;
        internal List<ScalarProjectionNode> Columns = null!;

        /// <summary>
        /// The members leading from the row to the object being built, when the projection picked
        /// out a complex-mapped member - <c>user.Row.Address</c> - rather than the row itself.
        /// Empty or null for a whole row.
        /// </summary>
        internal List<MemberInfo>? Prefix;

        private ScalarProjectionNode[] _columns = null!;
        private PocoMember[]?[] _owners = null!;
        private PocoData _pocoData = null!;
        private PocoMember? _root;
        private bool _notifyLoaded;

        internal override void Init(ProjectionInitContext context)
        {
            _columns = Columns.ToArray();
            foreach (var column in _columns) column.Init(context);
            _pocoData = Table.PocoData;

            var members = _pocoData.Members;
            if (Prefix != null)
            {
                foreach (var member in Prefix)
                {
                    _root = Find(members, member);
                    members = _root.PocoMemberChildren;
                }
            }

            _notifyLoaded = typeof(IOnLoaded).IsAssignableFrom(_root == null ? _pocoData.Type : _root.MemberInfoData.MemberType);
            _owners = _columns.Select(x => ResolveOwners(x.Column!, members)).ToArray();
        }

        private static PocoMember Find(List<PocoMember> members, MemberInfo member)
        {
            var found = members.FirstOrDefault(x => Equals(x.MemberInfoData.MemberInfo, member));
            if (found == null)
                throw new InvalidOperationException("The member '" + member.Name + "' is not mapped and cannot be projected.");
            return found;
        }

        // A complex-mapped column sets its value on the nested object that declares it, not on the
        // object this node builds, so walk the chain that leads to it - below whatever prefix this
        // node is rooted at - and remember the members along the way.
        private PocoMember[]? ResolveOwners(PocoColumn column, List<PocoMember> members)
        {
            var chain = column.MemberInfoChain;
            var depth = Prefix == null ? 0 : Prefix.Count;
            if (chain == null || chain.Count - depth < 2) return null;

            var owners = new PocoMember[chain.Count - depth - 1];
            for (var i = 0; i < owners.Length; i++)
            {
                var member = members.FirstOrDefault(x => Equals(x.MemberInfoData.MemberInfo, chain[depth + i]));
                if (member == null || member.IsList) return null;
                owners[i] = member;
                members = member.PocoMemberChildren;
            }
            return owners;
        }

        internal override object? Materialize(object[] values, DbDataReader reader)
        {
            if (!HasData(values)) return null;

            var instance = _root == null ? _pocoData.CreateObject(reader) : _root.Create(reader);
            for (var i = 0; i < _columns.Length; i++)
            {
                var column = _columns[i];
                var value = values[column.Ordinal];
                if (value == null || value == DBNull.Value) continue;
                column.Column!.SetValue(Owner(instance, _owners[i], reader), column.Materialize(values, reader));
            }

            if (_notifyLoaded) NotifyLoaded(instance);
            return instance;
        }

        // The nested objects are created only when a column actually has a value to set, so a
        // complex member whose columns all came back null stays null, as it does elsewhere.
        private static object Owner(object instance, PocoMember[]? owners, DbDataReader reader)
        {
            if (owners == null) return instance;

            var target = instance;
            for (var i = 0; i < owners.Length; i++)
            {
                var member = owners[i];
                var child = member.GetValue(target);
                if (child == null)
                {
                    child = member.Create(reader);
                    member.SetValue(target, child);
                }
                target = child;
            }
            return target;
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
        internal static ProjectionPlan Build<TResult>(Expression<Func<TResult>> projection, IEnumerable<TableReference> tables, IMapperCollection? mappers)
        {
            var plan = new ProjectionPlan(mappers);
            plan.Root = BuildNode(projection.Body, typeof(TResult), null, plan, tables.ToArray(), null);
            return plan;
        }

        // A body that is not an entity, an object construction or a member initialiser projects a
        // single value. That needs no plan at all - NPoco's ordinary single-column mapping handles
        // it - so the caller routes it away from here rather than building a one-leaf plan whose
        // root would have no member name to bind itself by.
        internal static bool IsScalarProjection(LambdaExpression projection, IEnumerable<TableReference> tables)
        {
            var body = StripConvert(projection.Body);
            if (body is NewExpression || body is MemberInitExpression) return false;
            if (ResolveRow(body, tables) != null) return false;

            // A complex-mapped member is several columns wrapped in an object, so it needs a plan
            // even though the expression reads like a single member access.
            var table = ResolveRowMember(body, tables, out _, out var prefix);
            return table == null || !IsComplexMember(table, prefix!);
        }

        /// <summary>
        /// The plan for a scalar projection that needs one, or null when NPoco's own single-column
        /// mapping already reads the value correctly.
        /// </summary>
        internal static ProjectionPlan? BuildScalar(LambdaExpression projection, IEnumerable<TableReference> tables, IMapperCollection? mappers)
        {
            var table = ResolveRowMember(StripConvert(projection.Body), tables, out _, out var prefix);
            var column = table == null ? null : table.TryResolveColumn(prefix!);
            return ScalarPlan(column, projection.ReturnType, mappers);
        }

        /// <summary>
        /// The plan for a scalar selected from a table reference - <c>SelectScalar(user, x =&gt; x.Name)</c> -
        /// whose selector is written against the row rather than against <c>table.Row</c>.
        /// </summary>
        internal static ProjectionPlan? BuildScalar(TableReference table, LambdaExpression selector, IMapperCollection? mappers)
        {
            var members = new List<MemberInfo>();
            var current = StripConvert(selector.Body);
            var member = current as MemberExpression;
            while (member != null)
            {
                members.Insert(0, member.Member);
                current = member.Expression == null ? null : StripConvert(member.Expression);
                member = current as MemberExpression;
            }

            if (members.Count == 0 || !(current is ParameterExpression)) return null;
            var column = table.TryResolveColumn(members.ToArray());
            return ScalarPlan(column, selector.ReturnType, mappers);
        }

        // Every conversion that reads a value back is chosen from the column and not only from its
        // type: a value object wraps the value, a serialized column deserializes into it, a UTC
        // column re-kinds it, and a member can carry a converter of its own. NPoco's plain
        // single-column mapping is handed no column and so can do none of it, which is why a scalar
        // that names a column gets a one-leaf plan. A scalar that is any other expression - an
        // aggregate, a concatenation - names no column, and a null plan leaves it where it was.
        private static ProjectionPlan? ScalarPlan(PocoColumn? column, Type type, IMapperCollection? mappers)
        {
            if (column == null) return null;

            var plan = new ProjectionPlan(mappers);
            plan.Root = column.ValueObjectColumn
                ? new ValueObjectProjectionNode { Column = column }
                : new ScalarProjectionNode { Type = type, Column = column };
            return plan;
        }

        private static ProjectionNode BuildNode(Expression expression, Type resultType, string? path, ProjectionPlan plan, TableReference[] tables, MemberInfo? target)
        {
            expression = StripConvert(expression);
            var rowTable = ResolveRow(expression, tables);
            if (rowTable != null) return BuildEntityNode(rowTable, expression, null, path, plan);

            // The projection picked out a complex-mapped member rather than a whole row: build the
            // same node from the columns that sit underneath that member.
            Expression? rowExpression;
            MemberInfo[]? memberPrefix;
            var memberTable = ResolveRowMember(expression, tables, out rowExpression, out memberPrefix);
            if (memberTable != null && IsComplexMember(memberTable, memberPrefix!))
                return BuildEntityNode(memberTable, rowExpression!, memberPrefix, path, plan);

            var created = expression as NewExpression;
            if (created != null)
            {
                var names = created.Members?.Select(x => x.Name).ToArray() ?? created.Constructor!.GetParameters().Select(x => FindMemberName(resultType, x.Name!)).ToArray();
                return new ObjectProjectionNode
                {
                    Type = resultType,
                    Constructor = created.Constructor,
                    Children = created.Arguments.Select((x, i) => BuildNode(x, GetMemberType(resultType, names[i]) ?? x.Type, Combine(path, names[i]), plan, tables, FindMember(resultType, names[i]))).ToList(),
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
                    Children = assignments.Select(x => BuildNode(x.Expression, GetMemberType(resultType, x.Member.Name) ?? x.Expression.Type, Combine(path, x.Member.Name), plan, tables, x.Member)).ToList(),
                    Members = assignments.Select(x => x.Member).ToList(),
                    NullWhenAllNull = !string.IsNullOrEmpty(path)
                };
            }

            if (string.IsNullOrEmpty(path)) throw new ArgumentException("A projection must create a result object with named members.", nameof(expression));
            var leafIndex = plan.Leaves.Count;
            plan.Leaves.Add(new ProjectionLeaf { Expression = expression, Alias = path!, Index = leafIndex });

            // A leaf keeps hold of its column, because the converter that reads the value back is
            // chosen from the column and not only from its type: that is what deserializes a
            // serialized column, re-kinds a UTC one, and finds a converter the member itself
            // carries. The column the leaf reads says how, and where the leaf reads no column - a
            // raw fragment, a computed value - the member it is written onto says how instead, so
            // the projected poco is filled the same way NPoco fills it reading the row directly.
            var leafColumn = memberTable?.TryResolveColumn(memberPrefix!);
            var fromDestination = leafColumn == null;
            leafColumn ??= DestinationColumn(target, tables);

            // A value object is a column like any other in the SQL, but the value read back has to
            // be wrapped in it rather than handed over raw.
            if (leafColumn != null && leafColumn.ValueObjectColumn)
                return new ValueObjectProjectionNode { Alias = path, Column = leafColumn };

            return new ScalarProjectionNode { Alias = path, Type = resultType, Column = leafColumn, ColumnIsDestination = fromDestination };
        }

        // The column the projected member maps to on its own type, for a leaf whose expression maps
        // to none. A type with no mapping of its own - an anonymous projection - has no column for
        // the member either, and the value is read by its type as it was before.
        private static PocoColumn? DestinationColumn(MemberInfo? target, TableReference[] tables)
        {
            if (target?.DeclaringType == null || tables.Length == 0) return null;

            var pocoData = tables[0].Database.PocoDataFactory.ForType(target.DeclaringType);
            foreach (var column in pocoData.Columns.Values)
            {
                // Only a member of the type itself: a column reached through another member is
                // named by that member, not by this one.
                if (column.MemberInfoChain.Count != 1) continue;
                if (column.MemberInfoData.MemberInfo.Equals(target)) return column;
            }
            return null;
        }

        // One node builds both a whole row and a complex-mapped member of one: the only difference
        // is which columns it takes and what it creates to hang them on.
        private static ProjectionNode BuildEntityNode(TableReference table, Expression row, MemberInfo[]? prefix, string? path, ProjectionPlan plan)
        {
            var depth = prefix == null ? 0 : prefix.Length;
            var columns = table.PocoData.QueryColumns
                .Select(x => x.Value)
                .Where(x => StartsWith(x.MemberInfoChain, prefix ?? new MemberInfo[0]) && x.MemberInfoChain.Count > depth)
                .Select(column =>
                {
                    // Below a prefix the path already names the member this node was reached by,
                    // so the column contributes only the part of its chain under that.
                    var key = depth == 0 ? column.MemberInfoKey : PocoColumn.GenerateKey(column.MemberInfoChain.Skip(depth));
                    var name = string.IsNullOrWhiteSpace(column.ColumnAlias) ? key : column.ColumnAlias;
                    var alias = Combine(path, name);
                    var columnExpression = BuildColumnExpression(row, column.MemberInfoChain);
                    var index = plan.Leaves.Count;
                    plan.Leaves.Add(new ProjectionLeaf { Expression = columnExpression, Alias = alias, Index = index });
                    return new ScalarProjectionNode { Alias = alias, Type = column.MemberInfoData.MemberType, Member = column.MemberInfoData.MemberInfo, Column = column };
                }).ToList();

            return new EntityProjectionNode { Table = table, Columns = columns, Prefix = prefix == null ? null : prefix.ToList() };
        }

        private static bool StartsWith(List<MemberInfo> chain, MemberInfo[] prefix)
        {
            if (prefix == null || prefix.Length == 0) return true;
            if (chain == null || chain.Count < prefix.Length) return false;
            for (var i = 0; i < prefix.Length; i++)
                if (!Equals(chain[i], prefix[i])) return false;
            return true;
        }

        /// <summary>
        /// A member reached through <c>table.Row</c>, with the members walked to get there. It may
        /// be a single column, a complex-mapped object, or nothing the table maps at all - the
        /// caller decides which of those it wants.
        /// </summary>
        private static TableReference? ResolveRowMember(Expression expression, IEnumerable<TableReference> tables, out Expression? row, out MemberInfo[]? prefix)
        {
            row = null;
            prefix = null;

            var members = new List<MemberInfo>();
            Expression? current = StripConvert(expression);
            var member = current as MemberExpression;
            while (member != null && member.Member.Name != "Row")
            {
                members.Insert(0, member.Member);
                current = member.Expression == null ? null : StripConvert(member.Expression);
                member = current as MemberExpression;
            }

            if (members.Count == 0) return null;
            var table = ResolveRow(current, tables);
            if (table == null) return null;

            row = current;
            prefix = members.ToArray();
            return table;
        }

        // Complex-mapped, rather than a column of its own: the table maps columns underneath it.
        private static bool IsComplexMember(TableReference table, MemberInfo[] prefix)
        {
            return table.PocoData.QueryColumns
                .Any(x => x.Value.MemberInfoChain != null
                          && x.Value.MemberInfoChain.Count > prefix.Length
                          && StartsWith(x.Value.MemberInfoChain, prefix));
        }

        private static Expression BuildColumnExpression(Expression row, IEnumerable<MemberInfo> members)
        {
            Expression expression = row;
            foreach (var member in members) expression = Expression.MakeMemberAccess(expression, member);
            return expression;
        }

        private static TableReference? ResolveRow(Expression? expression, IEnumerable<TableReference> tables)
        {
            var member = expression as MemberExpression;
            if (member == null || member.Member.Name != "Row") return null;
            var value = SqlExpressionTranslator.Evaluate(member.Expression!) as TableReference;
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
        private static MemberInfo? FindMember(Type type, string name)
        {
            var exact = type.GetMember(name, BindingFlags.Public | BindingFlags.Instance);
            if (exact.Length > 0) return exact[0];
            return type.GetMember(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase).FirstOrDefault();
        }

        private static string FindMemberName(Type type, string name) => FindMember(type, name)?.Name ?? name;

        private static Type? GetMemberType(Type type, string name)
        {
            var member = FindMember(type, name);
            return member is PropertyInfo property ? property.PropertyType : (member as FieldInfo)?.FieldType;
        }

        private static string Combine(string? prefix, string name) => string.IsNullOrEmpty(prefix) ? name : prefix + PocoData.Separator + name;
    }
}
