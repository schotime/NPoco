---
name: fluentsql
description: Write or review queries with NPoco.FluentSql - the strongly typed fluent SQL builder in src/NPoco.FluentSql. Use when a task involves FluentQuery(), TableReference, FSql.*, joins, CTEs, subqueries, derived tables, projections, or when translating hand-written SQL into the builder.
---

# NPoco.FluentSql

A strongly typed SQL builder. Queries are written against `TableReference<T>` values rather than
table names, and translated from expression trees. `docs/StronglyTypedSqlBuilder.md` is the reference;
this is the working knowledge needed to write a query correctly the first time.

## The one rule that shapes everything

**C# allows no `out` argument inside an expression tree.** So anything written *inside* a `Where`,
`Select` or `SelectScalar` lambda - a correlated subquery, a derived table, a union operand - cannot
declare its own references. Reserve them first with `Table<T>()`:

```csharp
var query = db.FluentQuery();
var order = query.Table<Order>();          // alias reserved, nothing added to the query yet

var rows = query
    .From<User>(out var user)              // statement level: out var is fine here
    .Select(() => new
    {
        user.Row.Name,
        Orders = FSql.Scalar(query.Subquery()
            .From(order)                   // inside an expression: declared reference
            .Where(() => order.Row.UserId == user.Row.Id)
            .SelectScalar(() => FSql.Count()))
    })
    .Fetch();
```

If you find yourself wanting `out var` inside a lambda, the answer is always `Table<T>()` above it.

## The reference model

A `TableReference<T>` is **one occurrence** of a source - it carries one alias. It is added to a
query exactly once, by a `From` or a join, and reading its columns is `reference.Row.Member`.

| need | how |
| --- | --- |
| a table, statement level | `From<T>(out var t)` / `InnerJoin<T>(out var t, x => …)` |
| a table, inside an expression | `var t = query.Table<T>();` then `From(t)` / `InnerJoin(t, () => …)` |
| the same table twice | two references - call `Table<T>()` twice, or two `out var` joins |
| a second occurrence of a CTE | `query.Table(cte)` |
| a CTE | `With(out var t, sub => …)` then `From(t)` or a join |
| a derived table | `From(out var t, sub => …)` / `From(t, sub => …)` / `LeftJoin(out var t, sub => …, x => …)` |
| a lateral subquery | `OuterApply(out var t, sub => …)` |

Adding one reference twice is an error - it would put the same alias in the statement twice. An
`OUTER APPLY` reference names nothing selectable, so it can only be read, never re-added.

**The out parameter always comes first**, matching SQL's own order (`JOIN orders o ON …`,
`WITH totals AS (…)`).

## Join conditions come in two shapes

```csharp
.InnerJoin<Order>(out var order, o => o.UserId == user.Row.Id)   // out var: lambda takes the row
.InnerJoin(order, () => order.Row.UserId == user.Row.Id)         // declared: everything via .Row
```

The declared form takes no parameter, which is what lets it live inside an expression tree. The
`out var` form must use its lambda parameter - C#'s definite-assignment rule rejects the reference
inside its own initializer.

## What can see what

| scope | reads |
| --- | --- |
| `Subquery()` | its own tables **and** the query it was built from - this is correlation |
| derived table (`From(sub)`, joined subquery) | its own tables, plus any query the containing one is *nested in* - not the containing query itself |
| UNION operand | same as a derived table |
| `OuterApply` body | its own tables and the query it sits beside (lateral) |
| CTE body | its own tables only |

That middle row is what lets a derived table inside a correlated subquery carry the correlation
down - `(SELECT agg(…) FROM (SELECT … WHERE x = outer.id UNION …) s)` - while still refusing to read
the query it is written into, which plain SQL would reject.

Reading a reference that is not in scope throws while the query is built - "The table reference is
not available to this query" - rather than producing SQL that silently means something else.

## Expressions

Comparisons, boolean logic, arithmetic, string concatenation with `+`, `Contains`/`StartsWith`/
`EndsWith`, date parts and date addition all translate. Aggregates and the rest are on `FSql`:

- `FSql.Count()`, `Count(x)`, `CountDistinct(x)`, `Sum`, `Average`, `Min`, `Max`
- `FSql.Case(condition, whenTrue, whenFalse)`
- `FSql.Scalar(subquery)` - type inferred from the subquery; name it (`Scalar<long>(…)`) only to read
  a value back as a wider type
- `x.IsIn(values)` / `x.IsIn(subquery)` / `x.IsNotIn(...)`, `FSql.Exists(subquery)`, `NotExists`.
  Named apart from `In` because NPoco's older API declares `NPoco.Expressions.S.In` with the same
  extension signature - a second `In` makes every `.In(...)` ambiguous wherever both namespaces are
  imported. `S.In` is still translated, so existing code using it keeps working
- `FSql.Cast<T>(value, "text")`, `FSql.Raw<T>(sql, args…)`
- `FSql.Aggregate<T>(function, args…)` for an aggregate the builder has no method for, with
  `FSql.Distinct(x)` wrapping the first argument and `FSql.OrderBy(keys…)` passed last -
  `FSql.Aggregate<string[]>("json_agg", FSql.Distinct(s.Row.Name), FSql.OrderBy(s.Row.Name))`.
  Keys take `FSql.Descending` / `FSql.Ascending`; `FSql.Distinct` works in the built-in aggregates too

`FSql.Raw` is the escape hatch and its arguments are **translated, not evaluated**, so aliases
resolve and captured values become parameters. `{0}` may repeat:

```csharp
FSql.Raw<bool>("({0} = {1} OR {0} LIKE {1} || '.%')",
    metric.Row.Entity,
    FSql.Case(i.Row.Name == "", i.Row.Type, i.Row.Type + "." + i.Row.Name))
```

Reach for it for `LIKE` against a computed pattern (the built-in `LIKE` requires a constant), and for
SQL that fits no call shape - an aggregate with `DISTINCT`/`ORDER BY` inside is `FSql.Aggregate`.

## Projections and execution

`Select(table)` (whole entity), `Select(() => new { … })`, `Select(() => new Dto { … })`,
`Select(() => new Dto(a, b))`, or `SelectScalar(() => expr)` for a single value.

```csharp
List<T> Fetch();   IEnumerable<T> Query();   T Single();   T First();
T SingleOrDefault();   T FirstOrDefault();          // + …Async(CancellationToken) for all of them
```

An `IAsyncQueryDatabase`/`IAsyncDatabase` yields `FluentSqlAsyncResult<T>`, which exposes only the
async forms. `Query()` streams. `ToSql()` renders, `ToDebugSql()` inlines parameters for logging.

## Reading values back

The conversion for a projected value is chosen from a **column**, not just a type - that is what
deserializes a `[SerializedColumn]`, re-kinds a UTC column, and applies a converter registered for a
member. A leaf that reads a column uses that column; a leaf that computes a value - `FSql.Raw`, an
aggregate - uses the member it is written onto, when the projected type is mapped.

Consequences worth knowing:

- Projecting a computed value onto a `[SerializedColumn]` member deserializes it, exactly as
  `Fetch<T>` would. `json_agg` into a `[SerializedColumn] string[]` works.
- If the provider already materialized the value as the member's type - Postgres `array_agg` giving
  `string[]`, a blob giving `byte[]` - it is passed through rather than deserialized.
- An **anonymous** projection has no mapped member, so a computed value is read by its type only.

## What the builder refuses, while building

- a subquery used as a value or `IN` list projecting more than one column;
- a CTE, derived table or `OUTER APPLY` projecting a type with no mapped columns;
- a reference belonging to another statement, added twice, or read before it is added;
- `From` more than once per query; `Subquery()` before any `From`;
- `OrderBy`/`Skip`/`Take` on a UNION operand; CTEs declared inside one;
- a query executed or rendered without a projection.

## Working on the library itself

- `src/NPoco.FluentSql` - `FluentSqlQuery.cs` (surface + query state), `SqlExpressionTranslator.cs`
  (expression → SQL), `SqlGenerator.cs` (clause assembly), `ProjectionMaterializer.cs` (plan that
  reads rows back), `TableReference.cs`, `QueryParts.cs`.
- Nullable is **enabled**; the project builds warning-free across five TFMs. `net462` and
  `netstandard2.0` have unannotated reference assemblies, so guards like `string.IsNullOrEmpty` do
  not narrow there, and `System.Diagnostics.CodeAnalysis` attributes are unavailable.
- Tests are in `test/NPoco.Tests/FluentSql`, SQLite-backed so generated SQL has to actually run.
  SQLite cannot execute `OUTER APPLY` - assert on `ToSql()` for those.
- After changing translation or materialization, verify a new test *fails without the fix* before
  trusting it. Several bugs here differ only in which converter is chosen.
