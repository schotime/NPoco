using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using NPoco.RowMappers;

namespace NPoco.FluentSqlBuilder
{
    public interface IFluentSqlQuery
    {
        Sql ToSql();
        string ToDebugSql();
        Sql Explain();
    }

    internal interface IFluentSqlQueryInternal
    {
        string Build(IList<object> parameters);

        /// <summary>Columns this query projects. A subquery used as a value must project exactly one.</summary>
        int ProjectedColumnCount { get; }
    }

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
        // silently produces wrong SQL rather than an error.
        private readonly Dictionary<string, int> _aliasCounts;

        // The query this one is nested inside, if any. Held as a link rather than a copy of its
        // tables so that scope is resolved when it is read: a subquery sees whatever its parent
        // sees, including tables joined after the subquery was created.
        private readonly FluentSqlQuery _parent;

        private TableReference _from;
        private ProjectionPlan _projectionPlan;

        internal FluentSqlQuery(IDatabase database) : this(database, null, null) { }

        private FluentSqlQuery(IDatabase database, FluentSqlQuery parent, Dictionary<string, int> aliasCounts)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _parent = parent;
            _aliasCounts = aliasCounts ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>Every table this query may reference: its own, plus everything in scope outside it.</summary>
        private IEnumerable<TableReference> AvailableTables
            => _parent == null ? _tables : _parent.AvailableTables.Concat(_tables);

        public FluentSqlQueryStage From<T>(out TableReference<T> table)
        {
            if (_from != null) throw new InvalidOperationException("From can only be specified once.");
            table = CreateTable<T>();
            _from = table;
            _tables.Add(table);
            return new FluentSqlQueryStage(this);
        }

        public FluentSqlQuery With<T>(string name, Func<FluentSqlQuery, FluentSqlResult<T>> query, out TableReference<T> table)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            var cteQuery = new FluentSqlQuery(_database);
            var definition = query(cteQuery);
            if (definition == null) throw new InvalidOperationException("The CTE callback must return a projected query.");
            if (!ReferenceEquals(definition.Query, cteQuery)) throw new InvalidOperationException("The CTE callback must return a result created from the supplied query.");
            return With(name, definition, out table);
        }

        public FluentSqlQuery With<T>(string name, FluentSqlResult<T> query, out TableReference<T> table)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A CTE name is required.", nameof(name));
            if (!IsValidIdentifier(name)) throw new ArgumentException("A CTE name may contain only letters, digits, and underscores, and cannot start with a digit.", nameof(name));
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (!ReferenceEquals(query.Database, _database)) throw new InvalidOperationException("The CTE query must use the same database instance as the containing query.");
            if (query.Query._ctes.Count > 0) throw new InvalidOperationException("A CTE definition cannot declare nested CTEs.");
            if (!_cteNames.Add(name)) throw new InvalidOperationException("A CTE named '" + name + "' has already been added.");
            table = CreateTable<T>(true, name);
            _cteTables.Add(table);
            _ctes.Add(new CtePart { Name = name, Query = query });
            return this;
        }

        private static bool IsValidIdentifier(string name)
        {
            if (!(char.IsLetter(name[0]) || name[0] == '_')) return false;
            return name.All(x => char.IsLetterOrDigit(x) || x == '_');
        }

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
        /// <see cref="FluentSql.Scalar{T}"/>, <see cref="FluentSql.Exists"/> or <see cref="FluentSql.In{T}(T, IFluentSqlQuery)"/>.
        /// </summary>
        internal FluentSqlQuery CreateSubquery()
        {
            RequireFrom();
            return new FluentSqlQuery(_database, this, _aliasCounts);
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
            return new FluentSqlResult<TValue>(this, _database);
        }

        internal FluentSqlResult<TResult> Select<TResult>(Expression<Func<TResult>> projection)
        {
            if (projection == null) throw new ArgumentNullException(nameof(projection));
            _projectionPlan = ProjectionPlanBuilder.Build(projection, AvailableTables.Distinct(), _database.Mappers);
            foreach (var leaf in _projectionPlan.Leaves)
            {
                _selects.Add(new SelectPart
                {
                    Tables = AvailableTables.Distinct().ToArray(),
                    Expression = Expression.Lambda(leaf.Expression),
                    Alias = leaf.Alias
                });
            }
            return new FluentSqlResult<TResult>(this, _database);
        }

        internal FluentSqlResult<TResult> Select<TResult>(Expression<Func<FluentSqlFunctions, TResult>> projection)
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
            if (!ReferenceEquals(result.Query, unionQuery)) throw new InvalidOperationException("The UNION callback must return a result created from the supplied query.");
            AddUnion(all, result);
        }

        internal void AddUnion<TResult>(bool all, FluentSqlResult<TResult> result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (_sorts.Count > 0 || SkipCount.HasValue || TakeCount.HasValue)
                throw new InvalidOperationException("OrderBy, Skip, and Take cannot be applied to an individual UNION operand.");
            if (!ReferenceEquals(result.Database, _database)) throw new InvalidOperationException("The UNION query must use the same database instance as the containing query.");
            if (ReferenceEquals(result.Query, this)) throw new InvalidOperationException("A query cannot be unioned with itself.");
            if (result.Query._ctes.Count > 0) throw new InvalidOperationException("A UNION operand cannot declare CTEs.");
            if (result.Query._sorts.Count > 0 || result.Query.SkipCount.HasValue || result.Query.TakeCount.HasValue)
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
            var inner = new FluentSqlQuery(_database, this, _aliasCounts);
            var result = subquery(inner);
            if (result == null) throw new InvalidOperationException("The OUTER APPLY callback must return a projected query.");
            table = CreateTable<TApply>(true);
            _tables.Add(table);
            _applies.Add(new ApplyPart { Table = table, Query = result });
        }

        private TableReference<T> CreateTable<T>(bool derived = false, string sourceName = null)
        {
            var root = TableAliasGenerator.Root(typeof(T));
            int count;
            _aliasCounts.TryGetValue(root, out count);
            _aliasCounts[root] = count + 1;
            return new TableReference<T>(_database, count == 0 ? root : root + count, derived, sourceName);
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
            _groups.Add(new GroupPart { Table = table, Expression = selector ?? throw new ArgumentNullException(nameof(selector)) });
        }

        internal void OrderBy<T, TValue>(TableReference<T> table, Expression<Func<T, TValue>> selector, bool descending = false)
        {
            EnsureAvailable(table);
            _sorts.Add(new SortPart { Table = table, Expression = selector ?? throw new ArgumentNullException(nameof(selector)), Descending = descending });
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
                => node == _parameter ? Expression.Constant(new FluentSqlFunctions()) : base.VisitParameter(node);
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

    public sealed class FluentSqlPredicate
    {
        internal FluentSqlPredicate(List<PredicatePart> parts) => Parts = parts;
        internal List<PredicatePart> Parts { get; }
        internal IEnumerable<TableReference> Tables => Parts.SelectMany(GetTables).Distinct();

        private static IEnumerable<TableReference> GetTables(PredicatePart part)
            => part.Children == null ? part.Tables : part.Children.SelectMany(GetTables);
    }

    public sealed class FluentSqlPredicateGroup
    {
        private readonly TableReference[] _tables;
        internal List<PredicatePart> Parts { get; } = new List<PredicatePart>();

        internal FluentSqlPredicateGroup(TableReference[] tables) => _tables = tables;

        public FluentSqlPredicateGroup And(Expression<Func<bool>> predicate) => Add("AND", predicate);
        public FluentSqlPredicateGroup Or(Expression<Func<bool>> predicate) => Add("OR", predicate);
        public FluentSqlPredicateGroup And<T>(TableReference<T> table, Expression<Func<T, bool>> predicate) => Add("AND", table, predicate);
        public FluentSqlPredicateGroup Or<T>(TableReference<T> table, Expression<Func<T, bool>> predicate) => Add("OR", table, predicate);
        public FluentSqlPredicateGroup AndGroup(Action<FluentSqlPredicateGroup> configure) => AddGroup("AND", configure);
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

    public sealed class FluentSqlQueryStage
    {
        private readonly FluentSqlQuery _query;

        internal FluentSqlQueryStage(FluentSqlQuery query) => _query = query;

        public FluentSqlQueryStage Where<T>(TableReference<T> table, Expression<Func<T, bool>> predicate)
        {
            _query.Where(table, predicate);
            return this;
        }

        public FluentSqlQueryStage WhereIf<T>(bool condition, TableReference<T> table, Expression<Func<T, bool>> predicate)
        {
            if (condition) _query.Where(table, predicate);
            return this;
        }

        public FluentSqlQueryStage Where(Expression<Func<bool>> predicate)
        {
            _query.Where(predicate);
            return this;
        }

        public FluentSqlQueryStage WhereIf(bool condition, Expression<Func<bool>> predicate)
        {
            if (condition) _query.Where(predicate);
            return this;
        }

        public FluentSqlQueryStage OrWhere(Expression<Func<bool>> predicate)
        {
            _query.OrWhere(predicate);
            return this;
        }

        public FluentSqlQueryStage OrWhere<T>(TableReference<T> table, Expression<Func<T, bool>> predicate)
        {
            _query.OrWhere(table, predicate);
            return this;
        }

        public FluentSqlQueryStage OrWhereIf(bool condition, Expression<Func<bool>> predicate)
        {
            if (condition) _query.OrWhere(predicate);
            return this;
        }

        public FluentSqlQueryStage OrWhereIf<T>(bool condition, TableReference<T> table, Expression<Func<T, bool>> predicate)
        {
            if (condition) _query.OrWhere(table, predicate);
            return this;
        }

        public FluentSqlPredicate CreatePredicate(Action<FluentSqlPredicateGroup> configure)
            => _query.CreatePredicate(configure);

        public FluentSqlQueryStage Where(FluentSqlPredicate predicate)
        {
            _query.Where(predicate);
            return this;
        }

        public FluentSqlQueryStage WhereIf(bool condition, FluentSqlPredicate predicate)
        {
            if (condition) _query.Where(predicate);
            return this;
        }

        public FluentSqlQueryStage OrWhere(FluentSqlPredicate predicate)
        {
            _query.OrWhere(predicate);
            return this;
        }

        public FluentSqlQueryStage OrWhereIf(bool condition, FluentSqlPredicate predicate)
        {
            if (condition) _query.OrWhere(predicate);
            return this;
        }

        public FluentSqlQueryStage WhereGroup(Action<FluentSqlPredicateGroup> group)
        {
            _query.WhereGroup(group);
            return this;
        }

        public FluentSqlQueryStage Having<T>(TableReference<T> table, Expression<Func<T, bool>> predicate)
        {
            _query.Having(table, predicate);
            return this;
        }

        public FluentSqlQueryStage Having<T>(bool condition, TableReference<T> table, Expression<Func<T, bool>> predicate)
        {
            if (condition) _query.Having(table, predicate);
            return this;
        }

        public FluentSqlQueryStage Having(Expression<Func<bool>> predicate)
        {
            _query.Having(predicate);
            return this;
        }

        public FluentSqlQueryStage GroupBy<T, TValue>(TableReference<T> table, Expression<Func<T, TValue>> selector)
        {
            _query.GroupBy(table, selector);
            return this;
        }

        public FluentSqlQueryStage OrderBy<T, TValue>(TableReference<T> table, Expression<Func<T, TValue>> selector, bool descending = false)
        {
            _query.OrderBy(table, selector, descending);
            return this;
        }

        public FluentSqlQueryStage OrderByDescending<T, TValue>(TableReference<T> table, Expression<Func<T, TValue>> selector)
            => OrderBy(table, selector, true);

        public FluentSqlQueryStage Take(int count)
        {
            _query.Take(count);
            return this;
        }

        public FluentSqlQueryStage Skip(int count)
        {
            _query.Skip(count);
            return this;
        }

        public FluentSqlQueryStage Distinct()
        {
            _query.Distinct();
            return this;
        }

        public FluentSqlQueryStage ThenBy<T, TValue>(TableReference<T> table, Expression<Func<T, TValue>> selector)
            => OrderBy(table, selector);

        public FluentSqlQueryStage ThenByDescending<T, TValue>(TableReference<T> table, Expression<Func<T, TValue>> selector)
            => OrderBy(table, selector, true);

        /// <summary>
        /// Starts a subquery that can reference this query's tables. Pass the result to
        /// <see cref="FluentSql.Scalar{T}"/> to project it, or to <see cref="FluentSql.Exists"/> /
        /// <see cref="FluentSql.In{T}(T, IFluentSqlQuery)"/> to use it in a predicate. A subquery
        /// built from <see cref="DatabaseExtensions.FluentQuery"/> instead is uncorrelated and
        /// cannot see the outer tables.
        /// </summary>
        public FluentSqlQuery Subquery() => _query.CreateSubquery();

        public FluentSqlResult<T> Select<T>(TableReference<T> table) => _query.Select(table);
        public FluentSqlResult<TResult> Select<TResult>(Expression<Func<TResult>> projection) => _query.Select(projection);
        public FluentSqlResult<TResult> Select<TResult>(Expression<Func<FluentSqlFunctions, TResult>> projection) => _query.Select(projection);
        public FluentSqlResult<TValue> SelectScalar<T, TValue>(TableReference<T> table, Expression<Func<T, TValue>> selector) => _query.SelectScalar(table, selector);

        public FluentSqlQueryStage InnerJoin<TJoin>(out TableReference<TJoin> table, Expression<Func<TJoin, bool>> on) => Join(FluentJoinType.Inner, out table, on);
        public FluentSqlQueryStage LeftJoin<TJoin>(out TableReference<TJoin> table, Expression<Func<TJoin, bool>> on) => Join(FluentJoinType.Left, out table, on);
        public FluentSqlQueryStage RightJoin<TJoin>(out TableReference<TJoin> table, Expression<Func<TJoin, bool>> on) => Join(FluentJoinType.Right, out table, on);
        public FluentSqlQueryStage FullOuterJoin<TJoin>(out TableReference<TJoin> table, Expression<Func<TJoin, bool>> on) => Join(FluentJoinType.FullOuter, out table, on);

        public FluentSqlQueryStage OuterApply<TApply>(out TableReference<TApply> table, Func<FluentSqlQuery, FluentSqlResult<TApply>> subquery)
        {
            _query.OuterApply(out table, subquery);
            return this;
        }

        private FluentSqlQueryStage Join<TJoin>(FluentJoinType type, out TableReference<TJoin> table, LambdaExpression on)
        {
            _query.AddJoin(type, out table, on);
            return this;
        }
    }

    public sealed class FluentSqlResult<TResult> : IFluentSqlQuery, IFluentSqlQueryInternal
    {
        private readonly FluentSqlQuery _query;
        private readonly IDatabase _database;

        internal FluentSqlResult(FluentSqlQuery query, IDatabase database)
        {
            _query = query;
            _database = database;
        }

        internal FluentSqlQuery Query => _query;
        internal IDatabase Database => _database;

        public FluentSqlResult<TResult> Union(Func<FluentSqlQuery, FluentSqlResult<TResult>> query)
        {
            _query.AddUnion(false, query);
            return this;
        }

        public FluentSqlResult<TResult> UnionAll(Func<FluentSqlQuery, FluentSqlResult<TResult>> query)
        {
            _query.AddUnion(true, query);
            return this;
        }

        public FluentSqlResult<TResult> Union(FluentSqlResult<TResult> query)
        {
            _query.AddUnion(false, query);
            return this;
        }

        public FluentSqlResult<TResult> UnionAll(FluentSqlResult<TResult> query)
        {
            _query.AddUnion(true, query);
            return this;
        }

        public Sql ToSql() => _query.BuildSql();

        string IFluentSqlQueryInternal.Build(IList<object> parameters) => _query.Build(parameters);

        int IFluentSqlQueryInternal.ProjectedColumnCount => _query.ProjectedColumnCount;

        public string ToDebugSql()
        {
            var sql = ToSql();
            return _database.DatabaseType.FormatCommand(sql.SQL, sql.Arguments);
        }

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

        public List<TResult> Fetch()
        {
            if (_query.ProjectionPlan == null) return _database.Fetch<TResult>(ToSql());
            return RowMapperDatabase.Fetch<TResult>(ToSql(), _query.ProjectionPlan);
        }

        public async Task<List<TResult>> FetchAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_query.ProjectionPlan == null) return await _database.FetchAsync<TResult>(ToSql(), cancellationToken).ConfigureAwait(false);
            return await RowMapperDatabase.FetchAsync<TResult>(ToSql(), _query.ProjectionPlan, cancellationToken).ConfigureAwait(false);
        }

        // Streams rather than materializing a list, so Single/First stop at the rows they need.
        private IEnumerable<TResult> Enumerate() => RowMapperDatabase.Query<TResult>(ToSql(), _query.ProjectionPlan);

        public TResult Single() => _query.ProjectionPlan == null ? _database.Single<TResult>(ToSql()) : Enumerate().Single();
        public TResult First() => _query.ProjectionPlan == null ? _database.First<TResult>(ToSql()) : Enumerate().First();
    }
}
