using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using NPoco.RowMappers;

namespace NPoco.FluentSql
{
    /// <summary>
    /// A projected query that can be rendered as SQL. Subqueries are passed to
    /// <see cref="FSql.Scalar{T}"/>, <see cref="FSql.Exists"/> and
    /// <see cref="FSql.In{T}(T, IFluentSqlQuery)"/> as this.
    /// </summary>
    public interface IFluentSqlQuery
    {
        /// <summary>Builds the SQL and the parameters it is executed with.</summary>
        /// <returns>The generated statement.</returns>
        Sql ToSql();
        /// <summary>
        /// Renders the statement with its parameter values inlined, for logging. The result is not
        /// meant to be executed.
        /// </summary>
        /// <returns>The statement as readable text.</returns>
        string ToDebugSql();
        /// <summary>Wraps the statement in the provider's execution-plan request.</summary>
        /// <returns>The plan statement, with the query's parameters.</returns>
        Sql Explain();
    }

    internal interface IFluentSqlQueryInternal
    {
        string Build(IList<object> parameters);

        /// <summary>Columns this query projects. A subquery used as a value must project exactly one.</summary>
        int ProjectedColumnCount { get; }
    }

    /// <summary>
    /// A query being built, before it has a FROM. Obtained from
    /// <see cref="DatabaseExtensions.FluentQuery"/>, from <see cref="FluentSqlQueryStage.Subquery"/>
    /// for a correlated subquery, or handed to a CTE, UNION or OUTER APPLY callback. Declare any CTEs
    /// with <c>With</c>, then call <c>From</c> to move on to the <see cref="FluentSqlQueryStage"/>
    /// that takes the rest of the query.
    /// </summary>
    public sealed class FluentSqlQuery
    {
        private readonly IDatabase _database;
        private readonly List<TableReference> _tables = new List<TableReference>();
        private readonly List<SelectPart> _selects = new List<SelectPart>();
        private readonly List<JoinPart> _joins = new List<JoinPart>();
        private readonly List<ApplyPart> _applies = new List<ApplyPart>();
        private readonly List<PredicatePart> _where = new List<PredicatePart>();
        private readonly List<PredicatePart> _having = new List<PredicatePart>();
        private readonly List<GroupPart> _groups = new List<GroupPart>();
        private readonly List<SortPart> _sorts = new List<SortPart>();
        private readonly List<CtePart> _ctes = new List<CtePart>();
        private readonly List<UnionPart> _unions = new List<UnionPart>();
        private readonly HashSet<TableReference> _cteTables = new HashSet<TableReference>();
        private readonly HashSet<string> _cteNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        // Shared with any correlated sub-builder so an inner table can never be handed an alias the
        // outer query is already using - the two scopes see each other's columns, so a collision
        // silently produces wrong SQL rather than an error. CTE names are reserved here too, so a
        // table can never be aliased as one.
        private readonly HashSet<string> _aliases;

        // The query this one is nested inside, if any. Held as a link rather than a copy of its
        // tables so that scope is resolved when it is read: a subquery sees whatever its parent
        // sees, including tables joined after the subquery was created.
        private readonly FluentSqlQuery _parent;

        private TableReference _from;
        private ProjectionPlan _projectionPlan;

        // The query a snapshot was taken from, so a CTE or UNION callback can still tell that the
        // result it was handed belongs to the query it was given.
        private FluentSqlQuery _origin;

        internal FluentSqlQuery(IDatabase database) : this(database, null, null) { }

        private FluentSqlQuery(IDatabase database, FluentSqlQuery parent, HashSet<string> aliases)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _parent = parent;
            _aliases = aliases ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>Every table this query may reference: its own, plus everything in scope outside it.</summary>
        private IEnumerable<TableReference> AvailableTables
            => _parent == null ? _tables : _parent.AvailableTables.Concat(_tables);

        /// <summary>
        /// Sets the table the query selects from, and hands back the reference that expressions use to
        /// reach its columns.
        /// </summary>
        /// <typeparam name="T">The POCO type mapped to the table.</typeparam>
        /// <param name="table">Receives the reference for the table, with a generated alias.</param>
        /// <returns>The stage that takes the rest of the query.</returns>
        /// <exception cref="InvalidOperationException">A FROM has already been set.</exception>
        public FluentSqlQueryStage From<T>(out TableReference<T> table)
        {
            if (_from != null) throw new InvalidOperationException("From can only be specified once.");
            table = CreateTable<T>();
            _from = table;
            _tables.Add(table);
            return new FluentSqlQueryStage(this);
        }

        /// <summary>
        /// Declares a CTE. Its name is generated: the query is written against the reference this
        /// hands back, so the name only has to be unique within the statement.
        /// </summary>
        public FluentSqlQuery With<T>(Func<FluentSqlQuery, FluentSqlResult<T>> query, out TableReference<T> table)
            => With(NextCteName(), query, out table);

        /// <inheritdoc cref="With{T}(Func{FluentSqlQuery, FluentSqlResult{T}}, out TableReference{T})"/>
        public FluentSqlQuery With<T>(FluentSqlResult<T> query, out TableReference<T> table)
            => With(NextCteName(), query, out table);

        private string NextCteName()
        {
            for (var i = 1; ; i++)
            {
                // The leading underscores keep a generated name clear of anything a caller would
                // pick, so mixing named and unnamed CTEs never has to renumber around a collision.
                var name = "__w" + i;
                if (!_cteNames.Contains(name)) return name;
            }
        }

        private FluentSqlQuery With<T>(string name, Func<FluentSqlQuery, FluentSqlResult<T>> query, out TableReference<T> table)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            var cteQuery = new FluentSqlQuery(_database);
            var definition = query(cteQuery);
            if (definition == null) throw new InvalidOperationException("The CTE callback must return a projected query.");
            if (!definition.InnerQuery.Projects(cteQuery)) throw new InvalidOperationException("The CTE callback must return a result created from the supplied query.");
            return With(name, definition, out table);
        }

        private FluentSqlQuery With<T>(string name, FluentSqlResult<T> query, out TableReference<T> table)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (!ReferenceEquals(query.Database, _database)) throw new InvalidOperationException("The CTE query must use the same database instance as the containing query.");
            if (query.InnerQuery._ctes.Count > 0) throw new InvalidOperationException("A CTE definition cannot declare nested CTEs.");
            _cteNames.Add(name);
            _aliases.Add(name);
            table = CreateTable<T>(true, name);
            RequireReferenceableColumns(table, "A CTE");
            _cteTables.Add(table);
            _ctes.Add(new CtePart { Name = name, Query = query });
            return this;
        }

        /// <summary>Selects from a CTE declared on this query by <c>With</c>.</summary>
        /// <typeparam name="T">The type the CTE projects.</typeparam>
        /// <param name="table">The reference handed back by <c>With</c>.</param>
        /// <returns>The stage that takes the rest of the query.</returns>
        /// <exception cref="InvalidOperationException">A FROM has already been set, or the reference was not created by this query.</exception>
        public FluentSqlQueryStage From<T>(TableReference<T> table)
        {
            if (_from != null) throw new InvalidOperationException("From can only be specified once.");
            EnsureDatabase(table);
            if (!_cteTables.Contains(table)) throw new InvalidOperationException("From(table) requires a CTE reference created by this query.");
            _from = table;
            _tables.Add(table);
            return new FluentSqlQueryStage(this);
        }

        /// <summary>
        /// A new query that can reference this query's tables, for a correlated subquery passed to
        /// <see cref="FSql.Scalar{T}"/>, <see cref="FSql.Exists"/> or <see cref="FSql.In{T}(T, IFluentSqlQuery)"/>.
        /// </summary>
        internal FluentSqlQuery CreateSubquery()
        {
            RequireFrom();
            return new FluentSqlQuery(_database, this, _aliases);
        }

        /// <summary>Whether a projection has been taken from this query.</summary>
        internal bool IsProjected => _selects.Count > 0 || _projectionPlan != null;

        /// <summary>Whether this query is <paramref name="source"/> or was rebased from it.</summary>
        internal bool Projects(FluentSqlQuery source) => ReferenceEquals(this, source) || ReferenceEquals(_origin, source);

        // Projecting mutates this query and hands it to the result, which costs nothing extra for
        // the usual single projection. A stage that is used again after that - projected a second
        // time, or added to - rebases onto this copy of everything the projection did not touch,
        // so the result already handed out keeps the query it was built from.
        internal FluentSqlQuery Rebase()
        {
            var copy = new FluentSqlQuery(_database, _parent, _aliases);
            copy._origin = _origin ?? this;
            copy._from = _from;
            copy.TakeCount = TakeCount;
            copy.SkipCount = SkipCount;
            copy.IsDistinct = IsDistinct;
            copy._tables.AddRange(_tables);
            copy._joins.AddRange(_joins);
            copy._applies.AddRange(_applies);
            copy._where.AddRange(_where);
            copy._having.AddRange(_having);
            copy._groups.AddRange(_groups);
            copy._sorts.AddRange(_sorts);
            copy._ctes.AddRange(_ctes);
            foreach (var table in _cteTables) copy._cteTables.Add(table);
            foreach (var name in _cteNames) copy._cteNames.Add(name);
            // Selects, the projection plan and unions all belong to the projection being left
            // behind: a union can only be added through a result.
            return copy;
        }

        internal FluentSqlResult<T> Select<T>(TableReference<T> table)
        {
            EnsureAvailable(table);
            _selects.Add(new SelectPart { Table = table, All = true });
            return new FluentSqlResult<T>(this, _database);
        }

        internal FluentSqlResult<TValue> SelectScalar<T, TValue>(TableReference<T> table, Expression<Func<T, TValue>> selector)
        {
            EnsureAvailable(table);
            if (selector == null) throw new ArgumentNullException(nameof(selector));
            _selects.Add(new SelectPart { Table = table, Expression = selector });
            _projectionPlan = ProjectionPlanBuilder.BuildScalar(table, selector, _database.Mappers);
            return new FluentSqlResult<TValue>(this, _database);
        }

        internal FluentSqlResult<TValue> SelectScalar<TValue>(Expression<Func<TValue>> selector)
        {
            if (selector == null) throw new ArgumentNullException(nameof(selector));
            var tables = AvailableTables.Distinct().ToArray();
            _selects.Add(new SelectPart { Tables = tables, Expression = selector });
            _projectionPlan = ProjectionPlanBuilder.BuildScalar(selector, tables, _database.Mappers);
            return new FluentSqlResult<TValue>(this, _database);
        }

        internal FluentSqlResult<TResult> Select<TResult>(Expression<Func<TResult>> projection)
        {
            if (projection == null) throw new ArgumentNullException(nameof(projection));
            var tables = AvailableTables.Distinct().ToArray();
            if (ProjectionPlanBuilder.IsScalarProjection(projection, tables))
                return SelectScalar(projection);
            _projectionPlan = ProjectionPlanBuilder.Build(projection, tables, _database.Mappers);
            foreach (var leaf in _projectionPlan.Leaves)
            {
                // One array for every leaf: they all see the same tables, and the generator uses
                // that sameness to build a single translator for the whole projection.
                _selects.Add(new SelectPart
                {
                    Tables = tables,
                    Expression = Expression.Lambda(leaf.Expression),
                    Alias = leaf.Alias
                });
            }
            return new FluentSqlResult<TResult>(this, _database);
        }

        internal FluentSqlResult<TResult> Select<TResult>(Expression<Func<FSqlFunctions, TResult>> projection)
        {
            if (projection == null) throw new ArgumentNullException(nameof(projection));
            var body = new SqlFunctionsParameterReplacer(projection.Parameters[0]).Visit(projection.Body);
            return Select(Expression.Lambda<Func<TResult>>(body));
        }

        internal void AddUnion<TResult>(bool all, Func<FluentSqlQuery, FluentSqlResult<TResult>> query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (_sorts.Count > 0 || SkipCount.HasValue || TakeCount.HasValue)
                throw new InvalidOperationException("OrderBy, Skip, and Take cannot be applied to an individual UNION operand.");

            var unionQuery = new FluentSqlQuery(_database);
            var result = query(unionQuery);
            if (result == null) throw new InvalidOperationException("The UNION callback must return a projected query.");
            if (!result.InnerQuery.Projects(unionQuery)) throw new InvalidOperationException("The UNION callback must return a result created from the supplied query.");
            AddUnion(all, result);
        }

        internal void AddUnion<TResult>(bool all, FluentSqlResult<TResult> result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (_sorts.Count > 0 || SkipCount.HasValue || TakeCount.HasValue)
                throw new InvalidOperationException("OrderBy, Skip, and Take cannot be applied to an individual UNION operand.");
            if (!ReferenceEquals(result.Database, _database)) throw new InvalidOperationException("The UNION query must use the same database instance as the containing query.");
            if (ReferenceEquals(result.InnerQuery, this)) throw new InvalidOperationException("A query cannot be unioned with itself.");
            if (result.InnerQuery._ctes.Count > 0) throw new InvalidOperationException("A UNION operand cannot declare CTEs.");
            if (result.InnerQuery._sorts.Count > 0 || result.InnerQuery.SkipCount.HasValue || result.InnerQuery.TakeCount.HasValue)
                throw new InvalidOperationException("OrderBy, Skip, and Take cannot be applied to an individual UNION operand.");
            _unions.Add(new UnionPart { All = all, Query = result });
        }

        internal void AddJoin<TJoin>(FluentJoinType type, out TableReference<TJoin> table, LambdaExpression on)
        {
            RequireFrom();
            table = CreateTable<TJoin>();
            if (on == null) throw new ArgumentNullException(nameof(on));
            var available = new List<TableReference>(AvailableTables) { table };
            if (on.Parameters.Count != 1 || on.Parameters[0].Type != typeof(TJoin))
                throw new ArgumentException("The join expression must accept the joined row.", nameof(on));
            _tables.Add(table);
            _joins.Add(new JoinPart { Type = type, Table = table, Condition = on, Tables = available.ToArray() });
        }

        internal void OuterApply<TApply>(out TableReference<TApply> table, Func<FluentSqlQuery, FluentSqlResult<TApply>> subquery)
        {
            RequireFrom();
            if (subquery == null) throw new ArgumentNullException(nameof(subquery));
            var inner = new FluentSqlQuery(_database, this, _aliases);
            var result = subquery(inner);
            if (result == null) throw new InvalidOperationException("The OUTER APPLY callback must return a projected query.");
            table = CreateTable<TApply>(true);
            RequireReferenceableColumns(table, "An OUTER APPLY");
            _tables.Add(table);
            _applies.Add(new ApplyPart { Table = table, Query = result });
        }

        // A derived table is addressed through its TableReference, so its type has to have mapped
        // columns. A single value has none: the query would build, and then nothing could be read
        // back out of it - a failure that otherwise surfaces much later, and about something else.
        private static void RequireReferenceableColumns(TableReference table, string usage)
        {
            if (table.PocoData.QueryColumns.Length > 0) return;
            throw new InvalidOperationException(usage + " must project a type with mapped columns, but '"
                + table.EntityType.Name + "' has none. Project an entity or an object shape instead of a single value.");
        }

        private TableReference<T> CreateTable<T>(bool derived = false, string sourceName = null)
        {
            var root = TableAliasGenerator.Root(typeof(T));
            // A root the builder invented rather than took from a type name is always numbered, so
            // it reads like the CTE names it sits beside: __t1 alongside __w1.
            return new TableReference<T>(_database, Reserve(root, root == TableAliasGenerator.GeneratedRoot), derived, sourceName);
        }

        // How NPoco itself hands a PocoData its AutoAlias: the initials, then the first numbered
        // form of them nothing has taken. Probing beats counting per root, because a name can also
        // be taken by something the counter never saw - a CTE, or an alias a caller chose.
        private string Reserve(string root, bool alwaysNumbered)
        {
            for (var i = alwaysNumbered ? 1 : 0; ; i++)
            {
                var alias = i == 0 ? root : root + i;
                if (_aliases.Add(alias)) return alias;
            }
        }

        internal void Where<T>(TableReference<T> table, Expression<Func<T, bool>> predicate) => AddPredicate(_where, table, predicate);
        internal void Having<T>(TableReference<T> table, Expression<Func<T, bool>> predicate) => AddPredicate(_having, table, predicate);

        private void AddPredicate<T>(List<PredicatePart> target, TableReference<T> table, Expression<Func<T, bool>> predicate)
        {
            EnsureAvailable(table);
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            target.Add(new PredicatePart { Tables = new TableReference[] { table }, Expression = predicate });
        }

        internal void Where<T1, T2>(TableReference<T1> first, TableReference<T2> second, Expression<Func<T1, T2, bool>> predicate)
        {
            EnsureUsable(first);
            EnsureUsable(second);
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            _where.Add(new PredicatePart { Tables = new TableReference[] { first, second }, Expression = predicate });
        }

        internal void Where(Expression<Func<bool>> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            _where.Add(new PredicatePart { Tables = AvailableTables.Distinct().ToArray(), Expression = predicate });
        }

        internal void OrWhere(Expression<Func<bool>> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            _where.Add(new PredicatePart { Tables = AvailableTables.Distinct().ToArray(), Expression = predicate, Operator = "OR" });
        }

        internal void OrWhere<T>(TableReference<T> table, Expression<Func<T, bool>> predicate)
        {
            EnsureAvailable(table);
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            _where.Add(new PredicatePart { Tables = new TableReference[] { table }, Expression = predicate, Operator = "OR" });
        }

        internal void WhereGroup(Action<FluentSqlPredicateGroup> configure)
        {
            AddPredicateGroup(_where, "AND", CreatePredicate(configure));
        }

        internal FluentSqlPredicate CreatePredicate(Action<FluentSqlPredicateGroup> configure)
        {
            if (configure == null) throw new ArgumentNullException(nameof(configure));
            var group = new FluentSqlPredicateGroup(AvailableTables.Distinct().ToArray());
            configure(group);
            if (group.Parts.Count == 0) throw new InvalidOperationException("A predicate group must contain at least one predicate.");
            return new FluentSqlPredicate(group.Parts);
        }

        internal void Where(FluentSqlPredicate predicate) => AddPredicateGroup(_where, "AND", predicate);
        internal void OrWhere(FluentSqlPredicate predicate) => AddPredicateGroup(_where, "OR", predicate);

        private void AddPredicateGroup(List<PredicatePart> target, string operation, FluentSqlPredicate predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            foreach (var table in predicate.Tables) EnsureUsable(table);
            target.Add(new PredicatePart { Operator = operation, Children = predicate.Parts });
        }

        internal void Having(Expression<Func<bool>> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            _having.Add(new PredicatePart { Tables = AvailableTables.Distinct().ToArray(), Expression = predicate });
        }

        internal void Take(int count)
        {
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count), "Take must be greater than zero.");
            TakeCount = count;
        }

        internal int? TakeCount { get; private set; }
        internal int? SkipCount { get; private set; }
        internal bool IsDistinct { get; private set; }

        internal void Skip(int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "Skip cannot be negative.");
            SkipCount = count;
        }

        internal void Distinct() => IsDistinct = true;

        internal void GroupBy<T, TValue>(TableReference<T> table, Expression<Func<T, TValue>> selector)
        {
            EnsureAvailable(table);
            _groups.Add(new GroupPart { Tables = new TableReference[] { table }, Expression = selector ?? throw new ArgumentNullException(nameof(selector)) });
        }

        internal void GroupBy<TValue>(Expression<Func<TValue>> selector)
        {
            if (selector == null) throw new ArgumentNullException(nameof(selector));
            _groups.Add(new GroupPart { Tables = AvailableTables.Distinct().ToArray(), Expression = selector });
        }

        internal void OrderBy<T, TValue>(TableReference<T> table, Expression<Func<T, TValue>> selector, bool descending = false)
        {
            EnsureAvailable(table);
            _sorts.Add(new SortPart { Tables = new TableReference[] { table }, Expression = selector ?? throw new ArgumentNullException(nameof(selector)), Descending = descending });
        }

        internal void OrderBy<TValue>(Expression<Func<TValue>> selector, bool descending = false)
        {
            if (selector == null) throw new ArgumentNullException(nameof(selector));
            _sorts.Add(new SortPart { Tables = AvailableTables.Distinct().ToArray(), Expression = selector, Descending = descending });
        }

        internal int ProjectedColumnCount => _selects.Sum(x => x.All ? x.Table.PocoData.QueryColumns.Length : 1);
        internal ProjectionPlan ProjectionPlan => _projectionPlan;

        internal Sql BuildSql()
        {
            RequireProjection();
            return SqlGenerator.Generate(_database, _ctes, _unions, _from, _selects, _joins, _applies, _where, _groups, _having, _sorts, IsDistinct, SkipCount, TakeCount);
        }

        internal string Build(IList<object> parameters)
        {
            RequireProjection();
            return SqlGenerator.GenerateText(_database, _ctes, _unions, _from, _selects, _joins, _applies, _where, _groups, _having, _sorts, IsDistinct, SkipCount, TakeCount, parameters);
        }

        private sealed class SqlFunctionsParameterReplacer : ExpressionVisitor
        {
            private readonly ParameterExpression _parameter;
            internal SqlFunctionsParameterReplacer(ParameterExpression parameter) => _parameter = parameter;
            protected override Expression VisitParameter(ParameterExpression node)
                => node == _parameter ? Expression.Constant(new FSqlFunctions()) : base.VisitParameter(node);
        }

        private void RequireProjection()
        {
            RequireFrom();
            if (_selects.Count == 0) throw new InvalidOperationException("A query must end with Select or SelectScalar before SQL generation or execution.");
        }

        private void RequireFrom()
        {
            if (_from == null) throw new InvalidOperationException("From must be specified before building the query.");
        }

        private void EnsureDatabase(TableReference table)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
            if (!ReferenceEquals(table.Database, _database)) throw new InvalidOperationException("A table reference must be created by the query's database.");
        }

        private void EnsureAvailable(TableReference table)
        {
            EnsureDatabase(table);
            if (!_tables.Contains(table)) throw new InvalidOperationException("The table must be added with From or Join before it can be referenced.");
        }

        private void EnsureUsable(TableReference table)
        {
            EnsureDatabase(table);
            if (!AvailableTables.Contains(table))
                throw new InvalidOperationException("The table is not available to this query or correlated subquery.");
        }
    }

    /// <summary>
    /// A group of predicates built ahead of time by <see cref="FluentSqlQueryStage.CreatePredicate"/>,
    /// which can then be added to a query - or to several - as one parenthesised unit.
    /// </summary>
    public sealed class FluentSqlPredicate
    {
        internal FluentSqlPredicate(List<PredicatePart> parts) => Parts = parts;
        internal List<PredicatePart> Parts { get; }
        internal IEnumerable<TableReference> Tables => Parts.SelectMany(GetTables).Distinct();

        private static IEnumerable<TableReference> GetTables(PredicatePart part)
            => part.Children == null ? part.Tables : part.Children.SelectMany(GetTables);
    }

    /// <summary>
    /// Collects the predicates of a group while the callback that configures it runs. Each call
    /// appends one predicate joined by AND or OR; the operator on the first is ignored, and nested
    /// groups are parenthesised.
    /// </summary>
    public sealed class FluentSqlPredicateGroup
    {
        private readonly TableReference[] _tables;
        internal List<PredicatePart> Parts { get; } = new List<PredicatePart>();

        internal FluentSqlPredicateGroup(TableReference[] tables) => _tables = tables;

        /// <summary>Appends a predicate joined with AND, written against any table in scope.</summary>
        /// <param name="predicate">The predicate, reaching columns through <c>table.Row</c>.</param>
        /// <returns>This group, so calls chain.</returns>
        public FluentSqlPredicateGroup And(Expression<Func<bool>> predicate) => Add("AND", predicate);
        /// <summary>Appends a predicate joined with OR, written against any table in scope.</summary>
        /// <param name="predicate">The predicate, reaching columns through <c>table.Row</c>.</param>
        /// <returns>This group, so calls chain.</returns>
        public FluentSqlPredicateGroup Or(Expression<Func<bool>> predicate) => Add("OR", predicate);
        /// <summary>Appends a predicate over one table, joined with AND.</summary>
        /// <typeparam name="T">The POCO type mapped to the table.</typeparam>
        /// <param name="table">The table the predicate reads.</param>
        /// <param name="predicate">The predicate, taking a row of that table.</param>
        /// <returns>This group, so calls chain.</returns>
        public FluentSqlPredicateGroup And<T>(TableReference<T> table, Expression<Func<T, bool>> predicate) => Add("AND", table, predicate);
        /// <summary>Appends a predicate over one table, joined with OR.</summary>
        /// <typeparam name="T">The POCO type mapped to the table.</typeparam>
        /// <param name="table">The table the predicate reads.</param>
        /// <param name="predicate">The predicate, taking a row of that table.</param>
        /// <returns>This group, so calls chain.</returns>
        public FluentSqlPredicateGroup Or<T>(TableReference<T> table, Expression<Func<T, bool>> predicate) => Add("OR", table, predicate);
        /// <summary>Appends a nested, parenthesised group joined with AND.</summary>
        /// <param name="configure">Fills the nested group. It must add at least one predicate.</param>
        /// <returns>This group, so calls chain.</returns>
        /// <exception cref="InvalidOperationException">The nested group is left empty.</exception>
        public FluentSqlPredicateGroup AndGroup(Action<FluentSqlPredicateGroup> configure) => AddGroup("AND", configure);
        /// <summary>Appends a nested, parenthesised group joined with OR.</summary>
        /// <param name="configure">Fills the nested group. It must add at least one predicate.</param>
        /// <returns>This group, so calls chain.</returns>
        /// <exception cref="InvalidOperationException">The nested group is left empty.</exception>
        public FluentSqlPredicateGroup OrGroup(Action<FluentSqlPredicateGroup> configure) => AddGroup("OR", configure);

        private FluentSqlPredicateGroup Add(string operation, Expression<Func<bool>> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            Parts.Add(new PredicatePart { Tables = _tables, Expression = predicate, Operator = operation });
            return this;
        }

        private FluentSqlPredicateGroup Add<T>(string operation, TableReference<T> table, Expression<Func<T, bool>> predicate)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            Parts.Add(new PredicatePart { Tables = new TableReference[] { table }, Expression = predicate, Operator = operation });
            return this;
        }

        private FluentSqlPredicateGroup AddGroup(string operation, Action<FluentSqlPredicateGroup> configure)
        {
            if (configure == null) throw new ArgumentNullException(nameof(configure));
            var group = new FluentSqlPredicateGroup(_tables);
            configure(group);
            if (group.Parts.Count == 0) throw new InvalidOperationException("A predicate group must contain at least one predicate.");
            Parts.Add(new PredicatePart { Operator = operation, Children = group.Parts });
            return this;
        }
    }

    /// <summary>
    /// A query with its FROM in place, gathering clauses. Every clause method returns the same stage,
    /// so calls chain in any order; one of the Select methods finishes it and hands back a
    /// <see cref="FluentSqlResult{TResult}"/>. A stage stays usable after that - it rebases onto a
    /// copy, leaving the result already handed out as it was built.
    /// </summary>
    public sealed class FluentSqlQueryStage
    {
        private FluentSqlQuery _query;

        internal FluentSqlQueryStage(FluentSqlQuery query) => _query = query;

        // The query to build on. Everything a stage does mutates it, which is what makes a single
        // projection allocation-free; once a projection has been handed out, the next thing the
        // stage does moves it onto a copy so that result stays as it was built.
        private FluentSqlQuery Target()
        {
            if (_query.IsProjected) _query = _query.Rebase();
            return _query;
        }

        /// <summary>Adds a predicate over one table, ANDed onto the WHERE clause.</summary>
        /// <typeparam name="T">The POCO type mapped to the table.</typeparam>
        /// <param name="table">The table the predicate reads.</param>
        /// <param name="predicate">The predicate, taking a row of that table.</param>
        /// <returns>This stage, so calls chain.</returns>
        public FluentSqlQueryStage Where<T>(TableReference<T> table, Expression<Func<T, bool>> predicate)
        {
            Target().Where(table, predicate);
            return this;
        }

        /// <summary>Adds a predicate over one table only when <paramref name="condition"/> holds.</summary>
        /// <typeparam name="T">The POCO type mapped to the table.</typeparam>
        /// <param name="condition">When false, the query is left untouched.</param>
        /// <param name="table">The table the predicate reads.</param>
        /// <param name="predicate">The predicate, taking a row of that table.</param>
        /// <returns>This stage, so calls chain.</returns>
        public FluentSqlQueryStage WhereIf<T>(bool condition, TableReference<T> table, Expression<Func<T, bool>> predicate)
        {
            if (condition) Target().Where(table, predicate);
            return this;
        }

        /// <summary>Adds a predicate over any tables in scope, ANDed onto the WHERE clause.</summary>
        /// <param name="predicate">The predicate, reaching columns of any table in scope through <c>table.Row</c>.</param>
        /// <returns>This stage, so calls chain.</returns>
        public FluentSqlQueryStage Where(Expression<Func<bool>> predicate)
        {
            Target().Where(predicate);
            return this;
        }

        /// <summary>Adds a predicate only when <paramref name="condition"/> holds.</summary>
        /// <param name="condition">When false, the query is left untouched.</param>
        /// <param name="predicate">The predicate, reaching columns of any table in scope through <c>table.Row</c>.</param>
        /// <returns>This stage, so calls chain.</returns>
        public FluentSqlQueryStage WhereIf(bool condition, Expression<Func<bool>> predicate)
        {
            if (condition) Target().Where(predicate);
            return this;
        }

        /// <summary>Adds a predicate ORed onto the WHERE clause rather than ANDed.</summary>
        /// <param name="predicate">The predicate, reaching columns of any table in scope through <c>table.Row</c>.</param>
        /// <returns>This stage, so calls chain.</returns>
        public FluentSqlQueryStage OrWhere(Expression<Func<bool>> predicate)
        {
            Target().OrWhere(predicate);
            return this;
        }

        /// <summary>Adds a predicate over one table, ORed onto the WHERE clause.</summary>
        /// <typeparam name="T">The POCO type mapped to the table.</typeparam>
        /// <param name="table">The table the predicate reads.</param>
        /// <param name="predicate">The predicate, taking a row of that table.</param>
        /// <returns>This stage, so calls chain.</returns>
        public FluentSqlQueryStage OrWhere<T>(TableReference<T> table, Expression<Func<T, bool>> predicate)
        {
            Target().OrWhere(table, predicate);
            return this;
        }

        /// <summary>Adds an ORed predicate only when <paramref name="condition"/> holds.</summary>
        /// <param name="condition">When false, the query is left untouched.</param>
        /// <param name="predicate">The predicate, reaching columns of any table in scope through <c>table.Row</c>.</param>
        /// <returns>This stage, so calls chain.</returns>
        public FluentSqlQueryStage OrWhereIf(bool condition, Expression<Func<bool>> predicate)
        {
            if (condition) Target().OrWhere(predicate);
            return this;
        }

        /// <summary>Adds an ORed predicate over one table only when <paramref name="condition"/> holds.</summary>
        /// <typeparam name="T">The POCO type mapped to the table.</typeparam>
        /// <param name="condition">When false, the query is left untouched.</param>
        /// <param name="table">The table the predicate reads.</param>
        /// <param name="predicate">The predicate, taking a row of that table.</param>
        /// <returns>This stage, so calls chain.</returns>
        public FluentSqlQueryStage OrWhereIf<T>(bool condition, TableReference<T> table, Expression<Func<T, bool>> predicate)
        {
            if (condition) Target().OrWhere(table, predicate);
            return this;
        }

        /// <summary>
        /// Builds a reusable group of predicates without adding it to the query, for passing to
        /// <see cref="Where(FluentSqlPredicate)"/> or one of its siblings.
        /// </summary>
        /// <param name="configure">Fills the group. It must add at least one predicate.</param>
        /// <returns>The predicate group, which can be added to this query or another.</returns>
        /// <exception cref="InvalidOperationException">The group is left empty.</exception>
        public FluentSqlPredicate CreatePredicate(Action<FluentSqlPredicateGroup> configure)
            => _query.CreatePredicate(configure);

        /// <summary>Adds a prebuilt predicate group, ANDed onto the WHERE clause and parenthesised.</summary>
        /// <param name="predicate">A group from <see cref="CreatePredicate"/>.</param>
        /// <returns>This stage, so calls chain.</returns>
        public FluentSqlQueryStage Where(FluentSqlPredicate predicate)
        {
            Target().Where(predicate);
            return this;
        }

        /// <summary>Adds a prebuilt predicate group only when <paramref name="condition"/> holds.</summary>
        /// <param name="condition">When false, the query is left untouched.</param>
        /// <param name="predicate">A group from <see cref="CreatePredicate"/>.</param>
        /// <returns>This stage, so calls chain.</returns>
        public FluentSqlQueryStage WhereIf(bool condition, FluentSqlPredicate predicate)
        {
            if (condition) Target().Where(predicate);
            return this;
        }

        /// <summary>Adds a prebuilt predicate group, ORed onto the WHERE clause and parenthesised.</summary>
        /// <param name="predicate">A group from <see cref="CreatePredicate"/>.</param>
        /// <returns>This stage, so calls chain.</returns>
        public FluentSqlQueryStage OrWhere(FluentSqlPredicate predicate)
        {
            Target().OrWhere(predicate);
            return this;
        }

        /// <summary>Adds an ORed prebuilt predicate group only when <paramref name="condition"/> holds.</summary>
        /// <param name="condition">When false, the query is left untouched.</param>
        /// <param name="predicate">A group from <see cref="CreatePredicate"/>.</param>
        /// <returns>This stage, so calls chain.</returns>
        public FluentSqlQueryStage OrWhereIf(bool condition, FluentSqlPredicate predicate)
        {
            if (condition) Target().OrWhere(predicate);
            return this;
        }

        /// <summary>Adds a parenthesised group of predicates, ANDed onto the WHERE clause.</summary>
        /// <param name="group">Fills the group. It must add at least one predicate.</param>
        /// <returns>This stage, so calls chain.</returns>
        /// <exception cref="InvalidOperationException">The group is left empty.</exception>
        public FluentSqlQueryStage WhereGroup(Action<FluentSqlPredicateGroup> group)
        {
            Target().WhereGroup(group);
            return this;
        }

        /// <summary>Adds a predicate over one table to the HAVING clause, filtering grouped rows.</summary>
        /// <typeparam name="T">The POCO type mapped to the table.</typeparam>
        /// <param name="table">The table the predicate reads.</param>
        /// <param name="predicate">The predicate, taking a row of that table.</param>
        /// <returns>This stage, so calls chain.</returns>
        public FluentSqlQueryStage Having<T>(TableReference<T> table, Expression<Func<T, bool>> predicate)
        {
            Target().Having(table, predicate);
            return this;
        }

        /// <summary>Adds a HAVING predicate over one table only when <paramref name="condition"/> holds.</summary>
        /// <typeparam name="T">The POCO type mapped to the table.</typeparam>
        /// <param name="condition">When false, the query is left untouched.</param>
        /// <param name="table">The table the predicate reads.</param>
        /// <param name="predicate">The predicate, taking a row of that table.</param>
        /// <returns>This stage, so calls chain.</returns>
        public FluentSqlQueryStage HavingIf<T>(bool condition, TableReference<T> table, Expression<Func<T, bool>> predicate)
        {
            if (condition) Target().Having(table, predicate);
            return this;
        }

        /// <summary>Adds a HAVING predicate, typically over an aggregate from <see cref="FSql"/>.</summary>
        /// <param name="predicate">The predicate, reaching columns of any table in scope through <c>table.Row</c>.</param>
        /// <returns>This stage, so calls chain.</returns>
        public FluentSqlQueryStage Having(Expression<Func<bool>> predicate)
        {
            Target().Having(predicate);
            return this;
        }

        /// <summary>Adds a HAVING predicate only when <paramref name="condition"/> holds.</summary>
        /// <param name="condition">When false, the query is left untouched.</param>
        /// <param name="predicate">The predicate, reaching columns of any table in scope through <c>table.Row</c>.</param>
        /// <returns>This stage, so calls chain.</returns>
        public FluentSqlQueryStage HavingIf(bool condition, Expression<Func<bool>> predicate)
        {
            if (condition) Target().Having(predicate);
            return this;
        }

        /// <summary>Groups by an expression over one table.</summary>
        /// <typeparam name="T">The POCO type mapped to the table.</typeparam>
        /// <typeparam name="TValue">The type the grouping expression yields.</typeparam>
        /// <param name="table">The table the predicate reads.</param>
        /// <param name="selector">The grouping expression, taking a row of that table.</param>
        /// <returns>This stage, so calls chain.</returns>
        public FluentSqlQueryStage GroupBy<T, TValue>(TableReference<T> table, Expression<Func<T, TValue>> selector)
        {
            Target().GroupBy(table, selector);
            return this;
        }

        /// <summary>Groups by an expression over any tables in scope.</summary>
        /// <typeparam name="TValue">The type the grouping expression yields.</typeparam>
        /// <param name="selector">The grouping expression, reaching columns through <c>table.Row</c>.</param>
        /// <returns>This stage, so calls chain.</returns>
        public FluentSqlQueryStage GroupBy<TValue>(Expression<Func<TValue>> selector)
        {
            Target().GroupBy(selector);
            return this;
        }

        /// <summary>Adds a sort key over one table. Keys sort in the order they are added.</summary>
        /// <typeparam name="T">The POCO type mapped to the table.</typeparam>
        /// <typeparam name="TValue">The type the sort expression yields.</typeparam>
        /// <param name="table">The table the sort expression reads.</param>
        /// <param name="selector">The sort expression, taking a row of that table.</param>
        /// <param name="descending">Whether to sort this key descending.</param>
        /// <returns>This stage, so calls chain.</returns>
        public FluentSqlQueryStage OrderBy<T, TValue>(TableReference<T> table, Expression<Func<T, TValue>> selector, bool descending = false)
        {
            Target().OrderBy(table, selector, descending);
            return this;
        }

        /// <summary>Adds a descending sort key over one table.</summary>
        /// <typeparam name="T">The POCO type mapped to the table.</typeparam>
        /// <typeparam name="TValue">The type the sort expression yields.</typeparam>
        /// <param name="table">The table the sort expression reads.</param>
        /// <param name="selector">The sort expression, taking a row of that table.</param>
        /// <returns>This stage, so calls chain.</returns>
        public FluentSqlQueryStage OrderByDescending<T, TValue>(TableReference<T> table, Expression<Func<T, TValue>> selector)
            => OrderBy(table, selector, true);

        /// <summary>Adds a sort key over any tables in scope.</summary>
        /// <typeparam name="TValue">The type the sort expression yields.</typeparam>
        /// <param name="selector">The sort expression, reaching columns through <c>table.Row</c>.</param>
        /// <param name="descending">Whether to sort this key descending.</param>
        /// <returns>This stage, so calls chain.</returns>
        public FluentSqlQueryStage OrderBy<TValue>(Expression<Func<TValue>> selector, bool descending = false)
        {
            Target().OrderBy(selector, descending);
            return this;
        }

        /// <summary>Adds a descending sort key over any tables in scope.</summary>
        /// <typeparam name="TValue">The type the sort expression yields.</typeparam>
        /// <param name="selector">The sort expression, reaching columns through <c>table.Row</c>.</param>
        /// <returns>This stage, so calls chain.</returns>
        public FluentSqlQueryStage OrderByDescending<TValue>(Expression<Func<TValue>> selector)
            => OrderBy(selector, true);

        /// <summary>Limits the query to the first <paramref name="count"/> rows, using the provider's paging syntax.</summary>
        /// <param name="count">How many rows to return. Must be greater than zero.</param>
        /// <returns>This stage, so calls chain.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is not positive.</exception>
        public FluentSqlQueryStage Take(int count)
        {
            Target().Take(count);
            return this;
        }

        /// <summary>Skips <paramref name="count"/> rows before returning any.</summary>
        /// <param name="count">How many rows to skip. Zero or more.</param>
        /// <returns>This stage, so calls chain.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
        public FluentSqlQueryStage Skip(int count)
        {
            Target().Skip(count);
            return this;
        }

        /// <summary>Makes the projection a <c>SELECT DISTINCT</c>.</summary>
        /// <returns>This stage, so calls chain.</returns>
        public FluentSqlQueryStage Distinct()
        {
            Target().Distinct();
            return this;
        }

        /// <summary>Adds a further ascending sort key. Reads as a continuation of <see cref="OrderBy{T, TValue}(TableReference{T}, Expression{Func{T, TValue}}, bool)"/>; sort keys apply in the order added.</summary>
        /// <typeparam name="T">The POCO type mapped to the table.</typeparam>
        /// <typeparam name="TValue">The type the sort expression yields.</typeparam>
        /// <param name="table">The table the sort expression reads.</param>
        /// <param name="selector">The sort expression, taking a row of that table.</param>
        /// <returns>This stage, so calls chain.</returns>
        public FluentSqlQueryStage ThenBy<T, TValue>(TableReference<T> table, Expression<Func<T, TValue>> selector)
            => OrderBy(table, selector);

        /// <summary>Adds a further descending sort key.</summary>
        /// <typeparam name="T">The POCO type mapped to the table.</typeparam>
        /// <typeparam name="TValue">The type the sort expression yields.</typeparam>
        /// <param name="table">The table the sort expression reads.</param>
        /// <param name="selector">The sort expression, taking a row of that table.</param>
        /// <returns>This stage, so calls chain.</returns>
        public FluentSqlQueryStage ThenByDescending<T, TValue>(TableReference<T> table, Expression<Func<T, TValue>> selector)
            => OrderBy(table, selector, true);

        /// <summary>Adds a further ascending sort key over any tables in scope.</summary>
        /// <typeparam name="TValue">The type the sort expression yields.</typeparam>
        /// <param name="selector">The sort expression, reaching columns through <c>table.Row</c>.</param>
        /// <returns>This stage, so calls chain.</returns>
        public FluentSqlQueryStage ThenBy<TValue>(Expression<Func<TValue>> selector)
            => OrderBy(selector);

        /// <summary>Adds a further descending sort key over any tables in scope.</summary>
        /// <typeparam name="TValue">The type the sort expression yields.</typeparam>
        /// <param name="selector">The sort expression, reaching columns through <c>table.Row</c>.</param>
        /// <returns>This stage, so calls chain.</returns>
        public FluentSqlQueryStage ThenByDescending<TValue>(Expression<Func<TValue>> selector)
            => OrderBy(selector, true);

        /// <summary>
        /// Starts a subquery that can reference this query's tables. Pass the result to
        /// <see cref="FSql.Scalar{T}"/> to project it, or to <see cref="FSql.Exists"/> /
        /// <see cref="FSql.In{T}(T, IFluentSqlQuery)"/> to use it in a predicate. A subquery
        /// built from <see cref="DatabaseExtensions.FluentQuery"/> instead is uncorrelated and
        /// cannot see the outer tables.
        /// </summary>
        public FluentSqlQuery Subquery() => _query.CreateSubquery();

        /// <summary>Projects every mapped column of one table, materialising rows as <typeparamref name="T"/>.</summary>
        /// <typeparam name="T">The POCO type mapped to the table.</typeparam>
        /// <param name="table">The table to project.</param>
        /// <returns>The finished query, ready to render or execute.</returns>
        public FluentSqlResult<T> Select<T>(TableReference<T> table) => Target().Select(table);
        /// <summary>
        /// Projects an arbitrary shape: an anonymous type, a member initialiser, a whole entity through
        /// <c>table.Row</c>, or any nesting of those. A projection that is a single value is treated as
        /// <see cref="SelectScalar{TValue}(Expression{Func{TValue}})"/>.
        /// </summary>
        /// <typeparam name="TResult">The shape each row materialises as.</typeparam>
        /// <param name="projection">The projection expression.</param>
        /// <returns>The finished query, ready to render or execute.</returns>
        public FluentSqlResult<TResult> Select<TResult>(Expression<Func<TResult>> projection) => Target().Select(projection);
        /// <summary>
        /// Projects an arbitrary shape with the SQL functions in scope through the lambda's parameter -
        /// <c>Select(f =&gt; new { Total = f.Sum(order.Row.Amount) })</c>.
        /// </summary>
        /// <typeparam name="TResult">The shape each row materialises as.</typeparam>
        /// <param name="projection">The projection expression, taking the function set.</param>
        /// <returns>The finished query, ready to render or execute.</returns>
        public FluentSqlResult<TResult> Select<TResult>(Expression<Func<FSqlFunctions, TResult>> projection) => Target().Select(projection);
        /// <summary>Projects a single value per row from one table.</summary>
        /// <typeparam name="T">The POCO type mapped to the table.</typeparam>
        /// <typeparam name="TValue">The type the projected expression yields.</typeparam>
        /// <param name="table">The table to project from.</param>
        /// <param name="selector">The projected expression, taking a row of that table.</param>
        /// <returns>The finished query, ready to render or execute.</returns>
        public FluentSqlResult<TValue> SelectScalar<T, TValue>(TableReference<T> table, Expression<Func<T, TValue>> selector) => Target().SelectScalar(table, selector);
        /// <summary>
        /// Projects a single value per row over any tables in scope - an aggregate, or one column of a
        /// subquery used by <see cref="FSql.Scalar{T}"/> or <see cref="FSql.In{T}(T, IFluentSqlQuery)"/>.
        /// </summary>
        /// <typeparam name="TValue">The type the projected expression yields.</typeparam>
        /// <param name="selector">The projected expression, reaching columns through <c>table.Row</c>.</param>
        /// <returns>The finished query, ready to render or execute.</returns>
        public FluentSqlResult<TValue> SelectScalar<TValue>(Expression<Func<TValue>> selector) => Target().SelectScalar(selector);

        /// <summary>Adds an <c>INNER JOIN</c>.</summary>
        /// <typeparam name="TJoin">The POCO type mapped to the joined table.</typeparam>
        /// <param name="table">Receives the reference for the joined table, with a generated alias.</param>
        /// <param name="on">The join condition, taking a row of the joined table and reaching the other tables through <c>table.Row</c>.</param>
        /// <returns>This stage, so calls chain.</returns>
        public FluentSqlQueryStage InnerJoin<TJoin>(out TableReference<TJoin> table, Expression<Func<TJoin, bool>> on) => Join(FluentJoinType.Inner, out table, on);
        /// <summary>Adds a <c>LEFT JOIN</c>, so unmatched rows of the joined table come back null.</summary>
        /// <typeparam name="TJoin">The POCO type mapped to the joined table.</typeparam>
        /// <param name="table">Receives the reference for the joined table, with a generated alias.</param>
        /// <param name="on">The join condition, taking a row of the joined table and reaching the other tables through <c>table.Row</c>.</param>
        /// <returns>This stage, so calls chain.</returns>
        public FluentSqlQueryStage LeftJoin<TJoin>(out TableReference<TJoin> table, Expression<Func<TJoin, bool>> on) => Join(FluentJoinType.Left, out table, on);
        /// <summary>Adds a <c>RIGHT JOIN</c>.</summary>
        /// <typeparam name="TJoin">The POCO type mapped to the joined table.</typeparam>
        /// <param name="table">Receives the reference for the joined table, with a generated alias.</param>
        /// <param name="on">The join condition, taking a row of the joined table and reaching the other tables through <c>table.Row</c>.</param>
        /// <returns>This stage, so calls chain.</returns>
        public FluentSqlQueryStage RightJoin<TJoin>(out TableReference<TJoin> table, Expression<Func<TJoin, bool>> on) => Join(FluentJoinType.Right, out table, on);
        /// <summary>Adds a <c>FULL OUTER JOIN</c>.</summary>
        /// <typeparam name="TJoin">The POCO type mapped to the joined table.</typeparam>
        /// <param name="table">Receives the reference for the joined table, with a generated alias.</param>
        /// <param name="on">The join condition, taking a row of the joined table and reaching the other tables through <c>table.Row</c>.</param>
        /// <returns>This stage, so calls chain.</returns>
        public FluentSqlQueryStage FullOuterJoin<TJoin>(out TableReference<TJoin> table, Expression<Func<TJoin, bool>> on) => Join(FluentJoinType.FullOuter, out table, on);

        /// <summary>
        /// Adds an <c>OUTER APPLY</c> over a subquery that may correlate with this query's tables - the
        /// usual way to pick the latest, or top N, related rows. Not every database supports it.
        /// </summary>
        /// <typeparam name="TApply">The type the subquery projects, which must have mapped columns.</typeparam>
        /// <param name="table">Receives the reference for the applied derived table.</param>
        /// <param name="subquery">Builds the subquery from the query it is handed, which sees the outer tables.</param>
        /// <returns>This stage, so calls chain.</returns>
        /// <exception cref="InvalidOperationException">The callback returns no projection, or projects a type with no mapped columns.</exception>
        public FluentSqlQueryStage OuterApply<TApply>(out TableReference<TApply> table, Func<FluentSqlQuery, FluentSqlResult<TApply>> subquery)
        {
            Target().OuterApply(out table, subquery);
            return this;
        }

        private FluentSqlQueryStage Join<TJoin>(FluentJoinType type, out TableReference<TJoin> table, LambdaExpression on)
        {
            Target().AddJoin(type, out table, on);
            return this;
        }
    }

    /// <summary>
    /// A finished, projected query. It can be rendered as SQL, unioned with another projection of the
    /// same shape, passed to a CTE or subquery, or executed.
    /// </summary>
    /// <typeparam name="TResult">The shape each row materialises as.</typeparam>
    public sealed class FluentSqlResult<TResult> : IFluentSqlQuery, IFluentSqlQueryInternal
    {
        private readonly FluentSqlQuery _query;
        private readonly IDatabase _database;

        internal FluentSqlResult(FluentSqlQuery query, IDatabase database)
        {
            _query = query;
            _database = database;
        }

        internal FluentSqlQuery InnerQuery => _query;
        internal IDatabase Database => _database;

        /// <summary>
        /// Appends a <c>UNION</c> operand, which drops duplicate rows across the operands.
        /// </summary>
        /// <param name="query">Builds the operand from the query it is handed. It must project the same shape.</param>
        /// <returns>This result, so operands chain.</returns>
        /// <exception cref="InvalidOperationException">The callback returns no projection, the operand declares CTEs, or this operand carries OrderBy, Skip or Take.</exception>
        public FluentSqlResult<TResult> Union(Func<FluentSqlQuery, FluentSqlResult<TResult>> query)
        {
            _query.AddUnion(false, query);
            return this;
        }

        /// <summary>
        /// Appends a <c>UNION ALL</c> operand, which keeps duplicate rows.
        /// </summary>
        /// <param name="query">Builds the operand from the query it is handed. It must project the same shape.</param>
        /// <returns>This result, so operands chain.</returns>
        /// <exception cref="InvalidOperationException">The callback returns no projection, the operand declares CTEs, or this operand carries OrderBy, Skip or Take.</exception>
        public FluentSqlResult<TResult> UnionAll(Func<FluentSqlQuery, FluentSqlResult<TResult>> query)
        {
            _query.AddUnion(true, query);
            return this;
        }

        /// <summary>Appends an already-built query as a <c>UNION</c> operand.</summary>
        /// <param name="query">The operand, projecting the same shape and built against the same database.</param>
        /// <returns>This result, so operands chain.</returns>
        /// <exception cref="InvalidOperationException">The operand belongs to another database, is this query itself, declares CTEs, or either side carries OrderBy, Skip or Take.</exception>
        public FluentSqlResult<TResult> Union(FluentSqlResult<TResult> query)
        {
            _query.AddUnion(false, query);
            return this;
        }

        /// <summary>Appends an already-built query as a <c>UNION ALL</c> operand.</summary>
        /// <param name="query">The operand, projecting the same shape and built against the same database.</param>
        /// <returns>This result, so operands chain.</returns>
        /// <exception cref="InvalidOperationException">The operand belongs to another database, is this query itself, declares CTEs, or either side carries OrderBy, Skip or Take.</exception>
        public FluentSqlResult<TResult> UnionAll(FluentSqlResult<TResult> query)
        {
            _query.AddUnion(true, query);
            return this;
        }

        /// <summary>Builds the statement and the parameters it is executed with.</summary>
        /// <returns>The generated SQL.</returns>
        /// <exception cref="InvalidOperationException">The query has no FROM or no projection.</exception>
        public Sql ToSql() => _query.BuildSql();

        string IFluentSqlQueryInternal.Build(IList<object> parameters) => _query.Build(parameters);

        int IFluentSqlQueryInternal.ProjectedColumnCount => _query.ProjectedColumnCount;

        /// <summary>
        /// Renders the statement with its parameter values inlined, for logging. The result is not meant
        /// to be executed.
        /// </summary>
        /// <returns>The statement as readable text.</returns>
        public string ToDebugSql()
        {
            var sql = ToSql();
            return _database.DatabaseType.FormatCommand(sql.SQL, sql.Arguments);
        }

        /// <summary>
        /// Wraps the statement in the provider's execution-plan request: <c>SET SHOWPLAN_ALL</c> on SQL
        /// Server, <c>EXPLAIN</c> elsewhere.
        /// </summary>
        /// <returns>The plan statement, carrying this query's parameters.</returns>
        public Sql Explain()
        {
            var sql = ToSql();
            var provider = _database.DatabaseType.GetProviderName() ?? string.Empty;
            var prefix = provider.IndexOf("SqlClient", StringComparison.OrdinalIgnoreCase) >= 0 ? "SET SHOWPLAN_ALL ON;\n" : "EXPLAIN ";
            return new Sql(prefix + sql.SQL, sql.Arguments);
        }

        // A projection is materialized by a ProjectionPlan, which NPoco runs as an IRowMapper.
        // Both branches therefore go through the same query pipeline: the same connection and
        // transaction handling, the same interceptors, the same exception reporting.
        private IRowMapperDatabase RowMapperDatabase => (IRowMapperDatabase)_database;

        /// <summary>Executes the query and materialises every row.</summary>
        /// <returns>The rows, in the order the database returned them.</returns>
        public List<TResult> Fetch()
        {
            if (_query.ProjectionPlan == null) return _database.Fetch<TResult>(ToSql());
            return RowMapperDatabase.Fetch<TResult>(ToSql(), _query.ProjectionPlan);
        }

        /// <summary>Executes the query asynchronously and materialises every row.</summary>
        /// <param name="cancellationToken">Cancels the database call.</param>
        /// <returns>The rows, in the order the database returned them.</returns>
        public async Task<List<TResult>> FetchAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_query.ProjectionPlan == null) return await _database.FetchAsync<TResult>(ToSql(), cancellationToken).ConfigureAwait(false);
            return await RowMapperDatabase.FetchAsync<TResult>(ToSql(), _query.ProjectionPlan, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Streams rows instead of materializing them, mirroring <see cref="IDatabaseQuery.Query{T}(Sql)"/>.
        /// The connection is released when enumeration finishes or the enumerator is disposed, so a
        /// partially consumed result must be disposed - a foreach does this for you.
        /// </summary>
        public IEnumerable<TResult> Query()
        {
            if (_query.ProjectionPlan == null) return _database.Query<TResult>(ToSql());
            return RowMapperDatabase.Query<TResult>(ToSql(), _query.ProjectionPlan);
        }

        /// <summary>
        /// Streams rows asynchronously. Same connection lifetime caveat as <see cref="Query"/>.
        /// </summary>
        public IAsyncEnumerable<TResult> QueryAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_query.ProjectionPlan == null) return _database.QueryAsync<TResult>(ToSql(), cancellationToken);
            return RowMapperDatabase.QueryAsync<TResult>(ToSql(), _query.ProjectionPlan, cancellationToken);
        }

        // Streaming means Single/First stop at the rows they need rather than reading everything.
        /// <summary>Executes the query and returns its only row, reading no further than it needs to.</summary>
        /// <returns>The single row.</returns>
        /// <exception cref="InvalidOperationException">The query returned no rows, or more than one.</exception>
        public TResult Single() => _query.ProjectionPlan == null ? _database.Single<TResult>(ToSql()) : Query().Single();
        /// <summary>Executes the query and returns its first row, reading no further than it needs to.</summary>
        /// <returns>The first row.</returns>
        /// <exception cref="InvalidOperationException">The query returned no rows.</exception>
        public TResult First() => _query.ProjectionPlan == null ? _database.First<TResult>(ToSql()) : Query().First();
    }
}
