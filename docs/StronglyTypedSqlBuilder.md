# Strongly-Typed SQL Builder for NPoco

`NPoco.FluentSql` builds SQL from C# expressions and NPoco's own mapping metadata, instead of
from SQL templates or interpolated strings. Tables, columns, aliases and parameters are all derived
from your POCOs, so a rename that breaks a query breaks the build. NPoco's existing `SqlBuilder` and
LINQ query APIs are untouched; this is an additional package.

```csharp
using NPoco.FluentSql;

var rows = db.FluentQuery()
    .From<User>(out var user)
    .InnerJoin<Order>(out var order, x => x.UserId == user.Row.Id)
    .Where(() => user.Row.IsActive && order.Row.Amount >= minimum)
    .OrderBy(() => user.Row.Name)
    .Select(() => new { user.Row.Name, order.Row.Amount })
    .Fetch();
```

```sql
SELECT [u].[Name] AS [Name], [o].[Amount] AS [Amount]
FROM [Users] [u]
INNER JOIN [Orders] [o] ON ([o].[UserId] = [u].[Id])
WHERE ((([u].[IsActive] = @0) AND ([o].[Amount] >= @1)))
ORDER BY [u].[Name] ASC
```

`FluentQuery()` is named so the package cannot collide with NPoco's existing `Query<T>()`.

## Tables and references

`From<T>` and every join hand back a `TableReference<T>`. That reference is how the rest of the query
names the table - there is no way to pass a raw table name, and aliases are generated and owned by the
query (see [Generated names](#generated-names)).

`table.Row` is a compile-time stand-in for a row of that table. It only exists inside a builder
expression; calling it anywhere else throws.

```csharp
var query = db.FluentQuery().From<User>(out var user);   // FluentSqlQueryStage
user.Alias;                                              // "u"
user.GetColumn(x => x.Name);                             // "[u].[Name]"
```

## Predicates

Every predicate has two forms. Which you use is a matter of taste until an expression spans more than
one table, at which point only the `Row` form works:

```csharp
.Where(user, x => x.IsActive)                                  // one table, lambda parameter
.Where(() => user.Row.IsActive)                                // same thing, Row form
.Where(() => order.Row.UserId == user.Row.Id)                  // two tables - Row form only
```

`OrWhere` adds an `OR`-joined predicate. Both have `If` variants that include the predicate only when
the condition holds, which keeps optional filters out of `if` blocks:

```csharp
.WhereIf(clientId.HasValue, () => user.Row.ClientId == clientId.Value)
.OrWhereIf(includeInactive, user, x => !x.IsActive)
```

Grouped predicates come from `WhereGroup`, or from `CreatePredicate` when the same group is used more
than once. Groups nest through `AndGroup`/`OrGroup`:

```csharp
var active = query.CreatePredicate(group => group
    .And(() => user.Row.IsActive)
    .OrGroup(nested => nested
        .And(() => user.Row.Name == "Ada")
        .Or(() => user.Row.Name == "Bob")));

query.Where(active).WhereIf(recent, () => user.Row.Created > cutoff);
```

Captured values become parameters, never inlined text. `In`/`NotIn` accept a collection or a subquery,
and `string.Contains`/`StartsWith`/`EndsWith` emit `LIKE` with `%`, `_` and the escape character
escaped using the portable `ESCAPE '!'` form.

## Joins

```csharp
.InnerJoin<Order>(out var order, x => x.UserId == user.Row.Id)
.LeftJoin<Address>(out var address, x => x.UserId == user.Row.Id)
.RightJoin<Region>(out var region, x => x.Id == address.Row.RegionId)
.FullOuterJoin<Audit>(out var audit, x => x.UserId == user.Row.Id)
```

The join condition takes the row being joined as its parameter, and can reference any table already in
scope through `Row`. That parameter is the only way to name the new table - the `out var` is not
assigned yet while the condition is being written.

`OuterApply` runs a correlated subquery per row (`OUTER APPLY` on SQL Server, `LEFT JOIN LATERAL` on
PostgreSQL and MySQL):

```csharp
.OuterApply(out var latest, apply => apply
    .From<Order>(out var recent)
    .Where(() => recent.Row.UserId == user.Row.Id)
    .OrderByDescending(() => recent.Row.Created)
    .Take(1)
    .Select(recent))
```

## Grouping, ordering and paging

`GroupBy`, `Having`, `OrderBy` and `ThenBy` take the same two forms as predicates. `Having` has an
`If` variant, and `OrderBy`/`ThenBy` have `Descending` variants:

```csharp
.GroupBy(() => user.Row.Name)
.HavingIf(minimumOrders > 0, () => FSql.Count() > minimumOrders)
.OrderByDescending(() => FSql.Count())
.ThenBy(() => user.Row.Name)
.Distinct()
.Skip(20).Take(10)
```

Paging is delegated to the configured `IDatabaseType`, so each provider keeps its own syntax and
parameter handling. `Take` without `Skip` becomes `TOP` on SQL Server.

## Projections

A query ends in `Select` or `SelectScalar`, which returns the thing you execute: a
`FluentSqlResult<T>` from an `IDatabaseQuery`, or an async-only `FluentSqlAsyncResult<T>` from an
`IAsyncQueryDatabase`. Four shapes are available.

**A whole entity**, mapped exactly as NPoco would map it:

```csharp
.Select(user)
```

**An object shape** - anonymous types, records, constructor calls and member initialisers, nested
freely, with whole entities as members. Nested paths are flattened to NPoco's `__` aliases and rebuilt
by a projection-aware materializer:

```csharp
.Select(() => new
{
    User = user.Row,
    Order = new { order.Row.Id, order.Row.Amount },
    Label = user.Row.Name + "!"
})

.Select(() => new UserRecord(user.Row.Id, user.Row.Name))
.Select(() => new UserDto { Id = user.Row.Id, Name = user.Row.Name })
```

A member whose source row is absent - the null side of a `LEFT JOIN` - materializes as `null` rather
than an object full of defaults.

**A single value**, as `List<TValue>` rather than a wrapper object:

```csharp
.SelectScalar(user, x => x.Name)          // one table
.SelectScalar(() => user.Row.Name)        // any table in scope
.SelectScalar(() => user.Row.Name + "/" + order.Row.Id)
```

`Select` also accepts a single-value body and routes it the same way, so `Select(() => user.Row.Name)`
and `SelectScalar(() => user.Row.Name)` are the same query. A single-value projection skips the
projection plan entirely and maps through NPoco's ordinary single-column path.

> An aggregate over an empty set is SQL `NULL`. On the single-value path that needs a nullable result
> type - `FSql.Sum(user.Row.Score)` where `Score` is `int` will fail on no rows, where `int?`
> yields `null`.

**A discoverable functions parameter**, if you would rather not reach for the static `FSql`:

```csharp
.Select(sql => new
{
    Score = sql.Case(user.Row.IsActive, user.Row.Age + 1, 0),
    UniqueNames = sql.CountDistinct(user.Row.Name)
})
```

## Expressions

Comparisons, boolean logic, arithmetic, modulo, bitwise operators, coalescing, conditionals (`CASE`),
string concatenation, date-part extraction and date addition all translate, with provider-specific SQL
where it differs. Aggregates are `FSql.Count`, `CountDistinct`, `Sum`, `Average`, `Min`, `Max`.

Inside a projection, parameterless `ToString()` on a mapped column selects that column unchanged and
uses NPoco's normal materialization conversion. For a database-side conversion, use
`FSql.Cast<T>(value, dbType)`; `dbType` is provider-specific, for example
`FSql.Cast<string>(user.Row.Id, "text")` on PostgreSQL.

`FSql.Raw` emits SQL the builder has no expression for. Placeholders are `string.Format` style and
each argument is translated like any other expression rather than evaluated, so aliases resolve and
captured values become parameters:

```csharp
.Select(() => new
{
    Readings = FSql.Raw<string>(
        "json_agg(json_build_object('value', {0}, 'at', {1}) ORDER BY {1})",
        metric.Row.Value,
        metric.Row.OccurredAt)
})
```

## Subqueries

`Subquery()` starts a query that can see the outer query's tables, which is what makes it correlated.
Pass the result to `FSql.Scalar` to project it, or to `Exists`/`NotExists`/`In`/`NotIn` to use it
in a predicate:

```csharp
var outer = db.FluentQuery().From<User>(out var user);

var orderCount = outer.Subquery()
    .From<Order>(out var order)
    .Where(() => order.Row.UserId == user.Row.Id)
    .Select(() => FSql.Count());

var rows = outer
    .Where(() => FSql.Exists(orderCount))
    .Select(() => new { user.Row.Name, Orders = FSql.Scalar<int>(orderCount) })
    .Fetch();
```

A query built from `db.FluentQuery()` instead of `Subquery()` is uncorrelated and cannot see the outer
tables. Subqueries used as a value or an `IN` list must project exactly one column.

### Writing one inline

C# allows no `out` argument inside an expression tree, so `From<T>(out var t)` cannot appear in a
`Where` or a projection. `Table<T>()` reserves the alias up front instead, and the subquery is then
built where it is used:

```csharp
var query = db.FluentQuery();
var order = query.Table<Order>();

var rows = query
    .From<User>(out var user)
    .Select(() => new
    {
        user.Row.Name,
        Orders = FSql.Scalar<int>(query.Subquery().From(order)
            .Where(() => order.Row.UserId == user.Row.Id)
            .SelectScalar(() => FSql.Count()))
    })
    .Fetch();
```

An alias can be reserved before the query has a FROM, so the declarations sit above one chain rather
than splitting it in two. `Subquery()` reads the query as it stands when the subquery is built, which
is after the FROM further down the same chain has run.

An inline subquery joins the same way, because the join overloads that take a declared reference need
no `out` either. Their condition reaches every table through `table.Row`, the joined one included,
rather than taking the joined row as a parameter:

```csharp
var address = query.Table<Address>();
var region = query.Table<Region>();

FSql.Scalar<int>(query.Subquery().From(address)
    .InnerJoin(region, () => region.Row.Id == address.Row.RegionId)
    .Where(() => address.Row.UserId == user.Row.Id)
    .SelectScalar(() => FSql.Count()));
```

A reference stands for one occurrence of a table, alias included, so exactly one `From` or join takes
it - a table appearing twice needs a `Table<T>()` each. The reference is not in scope until then;
until it is added, an expression that reads its columns is an error, not a silent cross join. Declared
references work on the outer query too, if you would rather name the alias before the join reads.

## CTEs

CTEs are typed, query-owned and non-recursive. The name is generated - the reference is what the query
is written against:

```csharp
var query = db.FluentQuery()
    .With(cte => cte
        .From<User>(out var candidate)
        .Where(() => candidate.Row.IsActive)
        .Select(candidate), out var active)
    .From(active)
    .Where(() => active.Row.Age >= 18)
    .Select(active);
```

A definition can also be built separately and handed over:

```csharp
var activeUsers = db.FluentQuery()
    .From<User>(out var user)
    .Where(() => user.Row.IsActive)
    .Select(user);

var query = db.FluentQuery()
    .With(activeUsers, out var active)
    .From(active)
    .Select(active);
```

Multiple CTEs render in declaration order, and their parameters precede the main query's.

## Unions

```csharp
var names = db.FluentQuery()
    .From<User>(out var minor)
    .Where(() => minor.Row.Age < 18)
    .SelectScalar(() => minor.Row.Name)
    .UnionAll(query => query
        .From<User>(out var senior)
        .Where(() => senior.Row.Age >= 65)
        .SelectScalar(() => senior.Row.Name));
```

Operands share the result type, parameters stay continuous across the compound query, and an operand
built separately can be passed directly to `Union(other)` / `UnionAll(other)`. An operand cannot
declare its own CTEs or apply its own `OrderBy`/`Skip`/`Take`; wrap the compound query in a CTE when
you need to compose further.

## Reusing a query

Everything before the projection mutates the query; projecting takes a snapshot. So one partially built
query can be projected several ways, and each result keeps the query it was built from:

```csharp
var stage = db.FluentQuery()
    .From<User>(out var user)
    .Where(() => user.Row.IsActive)
    .OrderBy(() => user.Row.Name);

var names = stage.Select(() => user.Row.Name).Fetch();
var count = stage.Select(() => FSql.Count()).Single();
var page  = stage.Take(10).Select(user).Fetch();   // does not disturb the two above
```

The copy only happens when a stage is used again after being projected, so the ordinary
project-once query pays nothing for it.

## Executing

Both result types run through NPoco's own pipeline - the same connection and transaction handling,
interceptors and exception reporting as a plain query. `FluentSqlAsyncResult<T>`, returned when the
database is typed as `IAsyncQueryDatabase` or `IAsyncDatabase`, exposes only async execution:

```csharp
Task<List<T>>       FetchAsync(CancellationToken);
IAsyncEnumerable<T> QueryAsync(CancellationToken);
Task<T>             SingleAsync(CancellationToken);
Task<T?>            SingleOrDefaultAsync(CancellationToken);
Task<T>             FirstAsync(CancellationToken);
Task<T?>            FirstOrDefaultAsync(CancellationToken);
```

`FluentSqlResult<T>`, returned from `IDatabaseQuery` or `IDatabase`, adds the synchronous forms:

```csharp
List<T>        Fetch();
IEnumerable<T> Query();
T              Single();
T?             SingleOrDefault();
T              First();            
T?             FirstOrDefault();
```

`Query()` streams rather than materializing, and releases the connection when enumeration finishes or
the enumerator is disposed - a `foreach` does that for you.

For diagnostics: `ToSql()` returns the `Sql` with its parameters, `ToDebugSql()` formats the command as
the provider would see it, and `Explain()` prefixes the statement for the provider's plan output.

## Generated names

| what | form | example |
| --- | --- | --- |
| table alias | type initials, lowercase, letters only; numbered when taken | `u`, `o`, `u1` |
| table alias for an anonymous projection | `__t1`, `__t2`, … | `FROM [__w1] [__t1]` |
| CTE name | `__w1`, `__w2`, … | `;WITH [__w1] AS (…)` |
| projected column | member path, nested levels joined by `__` | `Name`, `User__Id` |

Aliases and CTE names share one reservation set per query - shared with correlated subqueries and
applies - so an inner scope can never reuse an outer alias, and a table can never be aliased as an
existing CTE. Anything prefixed `__` was named by the builder.

## What the builder refuses

These fail while the query is being built, rather than as a database error later:

- a subquery used as a value or an `IN` list that projects more than one column;
- a CTE or `OUTER APPLY` body that projects a single value, since its column could never be referenced;
- `OrderBy`, `Skip` or `Take` on a `UNION` operand, and CTEs declared inside one;
- a table reference, CTE reference or reusable predicate belonging to a different query;
- a reference from `Table<T>()` added twice, or read before a `From` or join has added it;
- `From` more than once per query, `Take(0)` or a negative `Skip`;
- a query executed or rendered without a projection.

## Keeping this document honest

Every example here is compiled by `test/NPoco.Tests/FluentSql/DocumentationSamples.cs`, so an API
change that would invalidate one breaks the build.

## Providers

SQL Server, MySQL, PostgreSQL, SQLite, Oracle and Firebird, through `IDatabaseType`: identifier
escaping, paging, string concatenation, date parts and date arithmetic all follow the configured
provider.
