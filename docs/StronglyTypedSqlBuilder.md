# Strongly-Typed SQL Builder for NPoco

## Goal

Add a separate `NPoco.FluentSqlBuilder` API that builds SQL from expressions and NPoco metadata instead of SQL templates or interpolated SQL. The existing `SqlBuilder` and LINQ query APIs remain unchanged.

The builder must provide:

- strongly typed table and column references;
- generated, escaped table aliases;
- fluent `SELECT`, `FROM`, joins, `WHERE`, `GROUP BY`, `HAVING`, and `ORDER BY`;
- automatic parameterization;
- support for SQL Server, MySQL, PostgreSQL, SQLite, Oracle, and Firebird through `IDatabaseType`;
- aggregate functions, subqueries, right/full joins, and diagnostics;
- query execution through NPoco.

## Intended API

```csharp
var query = db.FluentQuery()
    .From<FieldDefinition>(out var fd)
    .InnerJoin<Client>(out var c,
        c => c.Id == fd.Row.ClientId)
    .WhereIf(clientId.HasValue, c, x => x.ClientId == clientId.Value)
    .WhereIf(fieldKeys?.Any() == true, fd,
        x => fieldKeys.Contains(x.FieldKey))
    .SelectInto<DefinitionView>(select => select
        .All(fd, x => x.Definition)
        .Column(c, x => x.ClientId, x => x.ClientNumericId));

var rows = query.Fetch();
```

`FluentQuery()` is intentionally named so the separate package does not collide with NPoco's existing `Query<T>()` API. A `Query()` compatibility extension is also supplied.

## Architecture

```text
FluentSqlQuery
    -> TableReference<T> and query parts
    -> SqlExpressionTranslator
    -> SqlGenerator
    -> PocoData / TableInfo / PocoColumn / IDatabaseType
    -> NPoco.Sql
```

`TableReference<T>` resolves mapped columns through `PocoData` and `MemberChainHelper`. It never accepts a raw table name. References and aliases are generated and owned by each query.

The expression translator maps lambda parameters to table references. It handles comparisons, boolean logic, null semantics, captured values, string operations, collection `Contains`, aggregates, and subqueries. Values are emitted as NPoco positional parameters.

Queries can also use `Distinct`, `Skip`, `Take`, `ThenBy`, `ThenByDescending`, `OrWhere`, `WhereIf`, and explicit grouped predicates. `WhereIf(condition, ...)` includes its predicate only when the condition is true. Paging is delegated to the configured `IDatabaseType`, preserving each provider's paging syntax and parameter handling.

`CreatePredicate` creates a reusable predicate group for the query's table references. Groups support typed `And`/`Or`, nested `AndGroup`/`OrGroup`, and can be added through `Where`, `WhereIf`, `OrWhere`, or `OrWhereIf`:

```csharp
var predicate = query.CreatePredicate(group => group
    .And(user, x => x.IsActive)
    .OrGroup(nested => nested
        .And(user, x => x.Name == "Ada")
        .Or(user, x => x.Name == "Bob")));

query.Where(predicate)
    .OrWhereIf(includeInactive, user, x => !x.IsActive);
```

Computed result members use `Expression`, while constructor/record-style projections use `Select`:

```csharp
var result = db.FluentQuery()
    .From<User>(out var user)
    .WhereGroup(group => group
        .And(() => user.Row.IsActive)
        .Or(() => string.IsNullOrEmpty(user.Row.Name)))
    .SelectInto<UserResult>(select => select
        .Expression(() => user.Row.Name + "!", x => x.Label)
        .Expression(() => FluentSql.Case(user.Row.IsActive, 1, 0), x => x.Score));

var immutable = db.FluentQuery()
    .From<User>(out var user)
    .Select(() => new UserRecord(user.Row.Id, user.Row.Name));
```

Anonymous and constructor projections may be nested. Nested expressions are flattened to NPoco's `__` aliases and a projection-aware materializer recursively constructs nested anonymous objects, records, DTOs, and complete entity rows:

```csharp
var rows = db.FluentQuery()
    .From<User>(out var user)
    .InnerJoin<Order>(out var order, x => x.UserId == user.Row.Id)
    .Select(() => new
    {
        User = user.Row,
        Order = new { order.Row.Id, order.Row.Amount }
    });
```

Computed expressions include conditional/`CASE` values, provider-specific string concatenation, arithmetic, modulo and bitwise operators, date-part extraction, and date addition. `FluentSql.CountDistinct` emits `COUNT(DISTINCT ...)`. String `Contains`, `StartsWith`, and `EndsWith` escape `%`, `_`, and the escape character using the portable `ESCAPE '!'` form.

`Select` optionally supplies a discoverable SQL-functions parameter for aggregates and `CASE` expressions. Ordinary arithmetic, comparisons, concatenation, and coalescing continue to use C# operators:

```csharp
var result = db.FluentQuery()
    .From<User>(out var user)
    .Select(sql => new
    {
        Label = user.Row.Name + "!",
        Score = sql.Case(user.Row.IsActive, user.Row.Age + 1, 0),
        UniqueNames = sql.CountDistinct(user.Row.Name)
    });
```

The static `FluentSql` markers remain available for predicates, `Having`, and parameterless `Select` expressions.

Non-recursive CTEs are typed and query-owned:

```csharp
var query = db.FluentQuery()
    .With<User>("active_users", cte => cte
        .From<User>(out var user)
        .Where(user, x => x.IsActive)
        .Select(user), out var active)
    .From(active)
    .Where(active, x => x.Age >= 18)
    .Select(active);
```

Multiple CTE definitions are rendered in declaration order. Their parameters precede main-query parameters, and duplicate CTE names or references owned by another query are rejected.

CTE definitions can also be constructed separately:

```csharp
var activeUsers = db.FluentQuery()
    .From<User>(out var user)
    .Where(user, x => x.IsActive)
    .Select(user);

var query = db.FluentQuery()
    .With("active_users", activeUsers, out TableReference<User> active)
    .From(active)
    .Select(active);
```

Projected queries can be combined with typed `Union` and `UnionAll` callbacks. Every operand has the same result type, and parameters remain continuous across the compound query:

```csharp
var names = db.FluentQuery()
    .From<User>(out var first)
    .Where(first, x => x.Age < 18)
    .SelectScalar(first, x => x.Name)
    .UnionAll(query => query
        .From<User>(out var second)
        .Where(second, x => x.Age >= 65)
        .SelectScalar(second, x => x.Name));
```

Union operands cannot declare their own CTEs or apply operand-local ordering and paging. A union query can be used as a CTE definition when further composition is required.

Union operands can likewise be created independently and combined directly with `Union(otherQuery)` or `UnionAll(otherQuery)`.

## Phase 1

- `From`
- select all columns or typed columns
- typed projection-member aliases
- inner and left joins
- conditional and unconditional `Where`
- `GroupBy`
- ascending and descending `OrderBy`
- result-bound `ToSql`, `Fetch`, and `FetchAsync`
- explicit select columns using NPoco's natural `__` nested mapping aliases
- database-specific identifier escaping

Join lambda parameters correspond to table order: the `FROM` table first, previous joins next, and the current join last.

## Phase 2

- `Count`, `Sum`, `Average`, `Min`, and `Max`
- `In` and `Exists` subqueries
- `Having`
- right and full outer joins
- typed projection aliases
- `ToDebugSql` and `Explain`
- correlated SQL Server `OUTER APPLY` (`LEFT JOIN LATERAL` on PostgreSQL/MySQL)

```csharp
var query = db.FluentQuery()
    .From<User>(out var user)
    .OuterApply<Order>(out var latestOrder, apply => apply
        .From<Order>(out var order)
        .Where(() => order.Row.UserId == user.Row.Id)
        .OrderByDescending(order, o => o.Created)
        .Take(1)
        .Select(order))
    .SelectInto<UserOrderResult>(select => select
        .All(user, x => x.User)
        .All(latestOrder, x => x.Order));
```

Examples:

```csharp
var summary = db.FluentQuery()
    .From<User>(out var users)
    .InnerJoin<Order>(out var orders,
        order => order.UserId == users.Row.Id)
    .GroupBy(users, u => u.Name)
    .Having(orders, o => FluentSql.Count(o.Id) > 5)
    .SelectInto<UserSummary>(select => select
        .Column(users, u => u.Name, x => x.Name)
        .Column(orders, o => FluentSql.Count(o.Id), x => x.OrderCount));
```

```csharp
var activeClientIds = db.FluentQuery()
    .From<Client>(out var clients)
    .Where(clients, x => x.IsActive)
    .SelectScalar(clients, x => x.Id);

var definitions = db.FluentQuery()
    .From<FieldDefinition>(out var fieldDefinitions)
    .Where(fieldDefinitions,
        x => x.ClientId.In(activeClientIds))
    .Select(fieldDefinitions);
```

## Validation

Tests cover metadata lookup, aliases, parameter numbering, joins, where expressions, grouping, ordering, aggregates, having clauses, subqueries, diagnostics, database escaping, and SQLite execution. Existing NPoco tests must continue to pass.
