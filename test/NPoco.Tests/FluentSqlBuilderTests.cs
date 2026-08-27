using System;
using System.Linq;
using Microsoft.Data.Sqlite;
using NPoco.FluentSqlBuilder;
using NUnit.Framework;

namespace NPoco.Tests
{
    [TestFixture]
    public class FluentSqlBuilderTests
    {
        private SqliteConnection _connection;
        private Database _database;

        [SetUp]
        public void SetUp()
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();
            _database = new Database(_connection, DatabaseType.SQLite);
            _database.Execute("CREATE TABLE BuilderUsers (Id INTEGER PRIMARY KEY, Name TEXT, IsActive INTEGER, Age INTEGER)");
            _database.Execute("CREATE TABLE BuilderOrders (Id INTEGER PRIMARY KEY, UserId INTEGER, Amount NUMERIC)");
        }

        [TearDown]
        public void TearDown()
        {
            _database.Dispose();
            _connection.Dispose();
        }

        [Test]
        public void BuildsSelectWhereAndOrderBy()
        {
            var sql = _database.FluentQuery()
                .From<BuilderUser>(out var users)
                .Where(users, x => x.IsActive && x.Age >= 18)
                .OrderByDescending(users, x => x.Name)
                .Select(() => new UserProjection { Id = users.Row.Id, Name = users.Row.Name })
                .ToSql();

            Assert.That(sql.SQL, Does.Contain("SELECT [bu].[Id] AS [Id], [bu].[Name] AS [Name]"));
            Assert.That(sql.SQL, Does.Contain("FROM [BuilderUsers] [bu]"));
            Assert.That(sql.SQL, Does.Contain("[bu].[IsActive] = @0"));
            Assert.That(sql.SQL, Does.Contain("[bu].[Age] >= @1"));
            Assert.That(sql.SQL, Does.Contain("ORDER BY [bu].[Name] DESC"));
            Assert.That(sql.Arguments, Is.EqualTo(new object[] { true, 18 }));
        }

        [Test]
        public void BuildsTypedJoinAndProjectionAlias()
        {
            var sql = _database.FluentQuery()
                .From<BuilderUser>(out var users)
                .InnerJoin<BuilderOrder>(out var orders, o => o.UserId == users.Row.Id)
                .Select(() => new BuilderSummary { Name = users.Row.Name, Total = orders.Row.Amount })
                .ToSql();

            Assert.That(sql.SQL, Does.Contain("INNER JOIN [BuilderOrders] [bo] ON ([bo].[UserId] = [bu].[Id])"));
            Assert.That(sql.SQL, Does.Contain("[bo].[Amount] AS [Total]"));
        }

        [Test]
        public void SupportsUnlimitedStyleJoinsAndMultiTableRowPredicate()
        {
            var sql = _database.FluentQuery()
                .From<BuilderUser>(out var user)
                .InnerJoin<BuilderOrder>(out var order, o => o.UserId == user.Row.Id)
                .LeftJoin<BuilderOrder>(out var previousOrder,
                    previous => previous.UserId == user.Row.Id && previous.Id < order.Row.Id)
                .Where(() => user.Row.IsActive && order.Row.Amount > previousOrder.Row.Amount)
                .Select(() => new NestedSummary { User = user.Row, Order = order.Row })
                .ToSql();

            Assert.That(sql.SQL, Does.Contain("INNER JOIN [BuilderOrders] [bo] ON ([bo].[UserId] = [bu].[Id])"));
            Assert.That(sql.SQL, Does.Contain("LEFT JOIN [BuilderOrders] [bo1]"));
            Assert.That(sql.SQL, Does.Contain("[bo1].[Id] < [bo].[Id]"));
            Assert.That(sql.SQL, Does.Contain("[bo].[Amount] > [bo1].[Amount]"));
        }

        [Test]
        public void BuildsAggregatesHavingAndGroupBy()
        {
            var sql = _database.FluentQuery()
                .From<BuilderUser>(out var users)
                .InnerJoin<BuilderOrder>(out var orders, o => o.UserId == users.Row.Id)
                .GroupBy(users, x => x.Name)
                .Having(orders, x => FluentSql.Count(x.Id) > 1)
                .Select(() => new BuilderSummary { Name = users.Row.Name, Total = FluentSql.Sum(orders.Row.Amount) })
                .ToSql();

            Assert.That(sql.SQL, Does.Contain("SUM([bo].[Amount]) AS [Total]"));
            Assert.That(sql.SQL, Does.Contain("GROUP BY [bu].[Name]"));
            Assert.That(sql.SQL, Does.Contain("HAVING ((COUNT([bo].[Id]) > @0))"));
        }

        [Test]
        public void BuildsInSubqueryWithContinuousParameters()
        {
            var subquery = _database.FluentQuery()
                .From<BuilderUser>(out var subUsers)
                .Where(subUsers, x => x.Age >= 21)
                .SelectScalar(subUsers, x => x.Id);

            var sql = _database.FluentQuery()
                .From<BuilderOrder>(out var orders)
                .Where(orders, x => x.UserId.In(subquery) && x.Amount > 10m)
                .Select(orders)
                .ToSql();

            Assert.That(sql.SQL, Does.Contain("[bo].[UserId] IN (SELECT [bu].[Id]"));
            Assert.That(sql.SQL, Does.Contain("[bu].[Age] >= @0"));
            Assert.That(sql.SQL, Does.Contain("[bo].[Amount] > @1"));
            Assert.That(sql.Arguments, Is.EqualTo(new object[] { 21, 10m }));
        }

        [Test]
        public void ExecutesAgainstSqlite()
        {
            _database.Insert(new BuilderUser { Id = 1, Name = "Ada", IsActive = true, Age = 36 });
            _database.Insert(new BuilderUser { Id = 2, Name = "Bob", IsActive = false, Age = 40 });
            var result = _database.FluentQuery()
                .From<BuilderUser>(out var users)
                .Where(users, x => x.IsActive)
                .Select(users)
                .Fetch();

            Assert.That(result.Select(x => x.Name), Is.EqualTo(new[] { "Ada" }));
        }

        [Test]
        public void CreatesDebugAndExplainSql()
        {
            var query = _database.FluentQuery().From<BuilderUser>(out var users).Where(users, x => x.Id == 7).Select(users);
            Assert.That(query.ToDebugSql(), Does.Contain("7"));
            Assert.That(query.Explain().SQL, Does.StartWith("EXPLAIN SELECT"));
        }

        [Test]
        public void HonorsMappedColumnsComplexMembersEnumsAndDateParts()
        {
            var states = new[] { RecordState.Active };
            var cutoff = new DateTime(2020, 1, 1);
            var sql = _database.FluentQuery()
                .From<MappedRecord>(out var mapped)
                .Where(mapped, x => states.Contains(x.State) && x.Created.Year >= cutoff.Year)
                .Select(() => new MappedProjection { DisplayName = mapped.Row.DisplayName, City = mapped.Row.Address.City })
                .ToSql();

            Assert.That(sql.SQL, Does.Contain("[mr].[display_name]"));
            Assert.That(sql.SQL, Does.Contain("[mr].[Address__City]"));
            Assert.That(sql.SQL, Does.Contain("CAST(strftime('%Y', [mr].[Created]) AS INTEGER)"));
            Assert.That(sql.Arguments[0], Is.EqualTo("Active"));
            Assert.That(sql.Arguments[1], Is.EqualTo(2020));
        }

        [Test]
        public void HandlesNullableAndEmptyCollectionPredicates()
        {
            var ids = Array.Empty<int>();
            var sql = _database.FluentQuery()
                .From<MappedRecord>(out var mapped)
                .Where(mapped, x => x.OptionalId.HasValue && ids.Contains(x.OptionalId.Value))
                .Select(mapped)
                .ToSql();

            Assert.That(sql.SQL, Does.Contain("[mr].[OptionalId] IS NOT NULL"));
            Assert.That(sql.SQL, Does.Contain("1 = 0"));
            Assert.That(sql.Arguments, Is.Empty);
        }

        [Test]
        public void ExpandsEntitySelectWithNaturalNestedAliases()
        {
            var sql = _database.FluentQuery()
                .From<MappedRecord>(out var mapped)
                .Select(mapped)
                .ToSql();

            Assert.That(sql.SQL, Does.Contain("[mr].[display_name] AS [DisplayName]"));
            Assert.That(sql.SQL, Does.Contain("[mr].[Address__City] AS [Address__City]"));
            Assert.That(sql.SQL, Does.Not.Contain("[mr].*"));
        }

        [Test]
        public void ExpandsJoinedEntityUnderTypedDestinationPath()
        {
            var sql = _database.FluentQuery()
                .From<BuilderUser>(out var users)
                .InnerJoin<BuilderOrder>(out var orders, o => o.UserId == users.Row.Id)
                .Select(() => new NestedSummary { User = users.Row, Order = orders.Row })
                .ToSql();

            Assert.That(sql.SQL, Does.Contain("[bu].[Id] AS [User__Id]"));
            Assert.That(sql.SQL, Does.Contain("[bo].[Amount] AS [Order__Amount]"));
        }

        [Test]
        public void BuildsCorrelatedOuterApplyWithOrderingAndTake()
        {
            var sql = _database.FluentQuery()
                .From<BuilderUser>(out var user)
                .OuterApply(out var latestOrder, apply => apply
                    .From<BuilderOrder>(out var order)
                    .Where(() => order.Row.UserId == user.Row.Id)
                    .OrderByDescending(order, o => o.Id)
                    .Take(1)
                    .Select(order))
                .Select(() => new NestedSummary { User = user.Row, Order = latestOrder.Row })
                .ToSql();

            Assert.That(sql.SQL, Does.Contain("OUTER APPLY ("));
            Assert.That(sql.SQL, Does.Contain("WHERE (([bo].[UserId] = [bu].[Id]))"));
            Assert.That(sql.SQL, Does.Contain("ORDER BY [bo].[Id] DESC"));
            Assert.That(sql.SQL, Does.Contain("LIMIT @0 OFFSET @1"));
            Assert.That(sql.Arguments, Is.EqualTo(new object[] { 1, 0 }));
            // The derived table takes its own alias; it shares an alias counter with the inner
            // query so a correlated scope can never hand out a name already in use.
            Assert.That(sql.SQL, Does.Contain(") [bo1]"));
            Assert.That(sql.SQL, Does.Contain("[bo1].[Amount] AS [Order__Amount]"));
        }

        [Test]
        public void BuildsDistinctPagingAndSecondaryOrdering()
        {
            var sql = _database.FluentQuery()
                .From<BuilderUser>(out var user)
                .Distinct()
                .OrderBy(user, x => x.Name)
                .ThenByDescending(user, x => x.Id)
                .Skip(5)
                .Take(10)
                .SelectScalar(user, x => x.Name)
                .ToSql();

            Assert.That(sql.SQL, Does.StartWith("SELECT DISTINCT [bu].[Name]"));
            Assert.That(sql.SQL, Does.Contain("ORDER BY [bu].[Name] ASC, [bu].[Id] DESC"));
            Assert.That(sql.SQL, Does.EndWith("LIMIT @0 OFFSET @1"));
            Assert.That(sql.Arguments, Is.EqualTo(new object[] { 10, 5 }));
        }

        [Test]
        public void BuildsOrWhereAndGroupedPredicates()
        {
            var sql = _database.FluentQuery()
                .From<BuilderUser>(out var user)
                .Where(() => user.Row.IsActive)
                .WhereGroup(group => group
                    .And(() => user.Row.Age >= 18)
                    .Or(() => string.IsNullOrEmpty(user.Row.Name)))
                .OrWhere(() => user.Row.Id == 7)
                .Select(user)
                .ToSql();

            Assert.That(sql.SQL, Does.Contain("AND ((([bu].[Age] >= @1)) OR (([bu].[Name] IS NULL OR [bu].[Name] = @2)))"));
            Assert.That(sql.SQL, Does.Contain("OR (([bu].[Id] = @3))"));
            Assert.That(sql.Arguments, Is.EqualTo(new object[] { true, 18, string.Empty, 7 }));
        }

        [Test]
        public void WhereIfOnlyIncludesPredicateWhenConditionIsTrue()
        {
            var sql = _database.FluentQuery()
                .From<BuilderUser>(out var user)
                .WhereIf(false, user, x => x.Name == "ignored")
                .WhereIf(true, () => user.Row.Age >= 18)
                .Select(user)
                .ToSql();

            Assert.That(sql.SQL, Does.Contain("WHERE (([bu].[Age] >= @0))"));
            Assert.That(sql.SQL, Does.Not.Contain("[bu].[Name] ="));
            Assert.That(sql.Arguments, Is.EqualTo(new object[] { 18 }));
        }

        [Test]
        public void BuildsReusableTypedAndNestedPredicateGroups()
        {
            var query = _database.FluentQuery().From<BuilderUser>(out var user);
            var adultsOrNamed = query.CreatePredicate(group => group
                .And(user, x => x.Age >= 18)
                .OrGroup(nested => nested
                    .And(user, x => x.Name == "Ada")
                    .Or(user, x => x.Name == "Bob")));

            var sql = query
                .Where(adultsOrNamed)
                .OrWhereIf(false, user, x => x.Id == 99)
                .OrWhereIf(true, () => user.Row.Id == 7)
                .Select(user)
                .ToSql();

            Assert.That(sql.SQL, Does.Contain("(([bu].[Age] >= @0)) OR ((([bu].[Name] = @1)) OR (([bu].[Name] = @2)))"));
            Assert.That(sql.SQL, Does.Contain("OR (([bu].[Id] = @3))"));
            Assert.That(sql.Arguments, Is.EqualTo(new object[] { 18, "Ada", "Bob", 7 }));
        }

        [Test]
        public void RejectsReusablePredicateOnAnotherQuery()
        {
            var first = _database.FluentQuery().From<BuilderUser>(out var firstUser);
            var predicate = first.CreatePredicate(group => group.And(firstUser, x => x.IsActive));
            var second = _database.FluentQuery().From<BuilderUser>(out _);

            Assert.Throws<InvalidOperationException>(() => second.Where(predicate));
        }

        [Test]
        public void EscapesLikeWildcards()
        {
            var value = "A_%";
            var sql = _database.FluentQuery()
                .From<BuilderUser>(out var user)
                .Where(user, x => x.Name.Contains(value))
                .Select(user)
                .ToSql();

            Assert.That(sql.SQL, Does.Contain("UPPER([bu].[Name]) LIKE @0 ESCAPE '!'"));
            Assert.That(sql.Arguments, Is.EqualTo(new object[] { "%A!_!%%" }));
        }

        [Test]
        public void BuildsCalculatedCaseConcatenationAndDistinctAggregateProjections()
        {
            var sql = _database.FluentQuery()
                .From<BuilderUser>(out var user)
                .Select(() => new CalculatedProjection
                {
                    Label = user.Row.Name + "!",
                    Score = FluentSql.Case(user.Row.IsActive, user.Row.Age + 1, 0),
                    UniqueNames = FluentSql.CountDistinct(user.Row.Name)
                })
                .ToSql();

            Assert.That(sql.SQL, Does.Contain("([bu].[Name] || @0) AS [Label]"));
            Assert.That(sql.SQL, Does.Contain("CASE WHEN ([bu].[IsActive] = @1) THEN ([bu].[Age] + @2) ELSE @3 END"));
            Assert.That(sql.SQL, Does.Contain("COUNT(DISTINCT [bu].[Name]) AS [UniqueNames]"));
            Assert.That(sql.Arguments, Is.EqualTo(new object[] { "!", true, 1, 0 }));
        }

        [Test]
        public void BuildsConstructorProjection()
        {
            var sql = _database.FluentQuery()
                .From<BuilderUser>(out var user)
                .Select(() => new ConstructorProjection(user.Row.Id, user.Row.Name))
                .ToSql();

            Assert.That(sql.SQL, Does.Contain("[bu].[Id] AS [Id], [bu].[Name] AS [Name]"));
        }

        [Test]
        public void BuildsModuloBitwiseStringAndDateExpressions()
        {
            var suffix = "!";
            var sql = _database.FluentQuery()
                .From<MappedRecord>(out var mapped)
                .Where(() => (mapped.Row.OptionalId.Value % 2) == 0 &&
                             (mapped.Row.OptionalId.Value & 1) == 0 &&
                             mapped.Row.Created.AddDays(1).Day > 1)
                .Select(() => new MappedProjection { DisplayName = string.Concat(mapped.Row.DisplayName, suffix) })
                .ToSql();

            Assert.That(sql.SQL, Does.Contain("([mr].[OptionalId] % @1)"));
            Assert.That(sql.SQL, Does.Contain("([mr].[OptionalId] & @3)"));
            Assert.That(sql.SQL, Does.Contain("strftime('%d', datetime([mr].[Created], @5 || ' day'))"));
            Assert.That(sql.SQL, Does.Contain("([mr].[display_name] || @0) AS [DisplayName]"));
            Assert.That(sql.Arguments, Is.EqualTo(new object[] { "!", 2, 0, 1, 0, 1d, 1 }));
        }

        [TestCaseSource(nameof(ProviderExpressionCases))]
        public void BuildsProviderSpecificStringLikeAndDateExpressions(
            DatabaseType databaseType, string concatenation, string dateAdd, string datePart)
        {
            using (var connection = new SqliteConnection("Data Source=:memory:"))
            using (var database = new Database(connection, databaseType))
            {
                var value = "A_%";
                var sql = database.FluentQuery()
                    .From<MappedRecord>(out var mapped)
                    .Where(mapped, x => x.DisplayName.Contains(value) && x.Created.AddMonths(2).Year > 2020)
                    .Select(() => new MappedProjection { DisplayName = mapped.Row.DisplayName + "!" })
                    .ToSql();

                Assert.That(sql.SQL, Does.Contain(concatenation));
                Assert.That(sql.SQL, Does.Contain(dateAdd));
                Assert.That(sql.SQL, Does.Contain(datePart));
                Assert.That(sql.SQL, Does.Contain("LIKE @1 ESCAPE '!'"));
                Assert.That(sql.Arguments, Is.EqualTo(new object[] { "!", "%A!_!%%", 2d, 2020 }));
            }
        }

        [Test]
        public void ExecutesComputedProjection()
        {
            _database.Insert(new BuilderUser { Id = 1, Name = "Ada", IsActive = true, Age = 36 });

            var row = _database.FluentQuery()
                .From<BuilderUser>(out var user)
                .Where(user, x => x.Id == 1)
                .Select(() => new CalculatedProjection
                {
                    Label = user.Row.Name + "!",
                    Score = FluentSql.Case(user.Row.IsActive, user.Row.Age + 1, 0)
                })
                .Single();

            Assert.That(row.Label, Is.EqualTo("Ada!"));
            Assert.That(row.Score, Is.EqualTo(37));
        }

        [Test]
        public void ExecutesConstructorProjection()
        {
            _database.Insert(new BuilderUser { Id = 1, Name = "Ada", IsActive = true, Age = 36 });

            var row = _database.FluentQuery()
                .From<BuilderUser>(out var user)
                .Where(user, x => x.Id == 1)
                .Select(() => new ConstructorProjection(user.Row.Id, user.Row.Name))
                .Single();

            Assert.That(row.Id, Is.EqualTo(1));
            Assert.That(row.Name, Is.EqualTo("Ada"));
        }

        [Test]
        public void BuildsAndExecutesNestedAnonymousProjectionWithWholeRow()
        {
            _database.Insert(new BuilderUser { Id = 1, Name = "Ada", IsActive = true, Age = 36 });
            _database.Insert(new BuilderOrder { Id = 10, UserId = 1, Amount = 25m });

            var query = _database.FluentQuery()
                .From<BuilderUser>(out var user)
                .InnerJoin<BuilderOrder>(out var order, x => x.UserId == user.Row.Id)
                .Select(() => new
                {
                    User = user.Row,
                    Order = new { order.Row.Id, order.Row.Amount }
                });

            var sql = query.ToSql();
            Assert.That(sql.SQL, Does.Contain("[bu].[Id] AS [User__Id]"));
            Assert.That(sql.SQL, Does.Contain("[bu].[Name] AS [User__Name]"));
            Assert.That(sql.SQL, Does.Contain("[bo].[Id] AS [Order__Id]"));
            Assert.That(sql.SQL, Does.Contain("[bo].[Amount] AS [Order__Amount]"));

            var row = query.Single();
            Assert.That(row.User.Id, Is.EqualTo(1));
            Assert.That(row.User.Name, Is.EqualTo("Ada"));
            Assert.That(row.User.IsActive, Is.True);
            Assert.That(row.User.Age, Is.EqualTo(36));
            Assert.That(row.Order.Id, Is.GreaterThan(0));
            Assert.That(row.Order.Amount, Is.EqualTo(25m));
        }

        [Test]
        public void BuildsAndExecutesNestedMemberInitProjection()
        {
            _database.Insert(new BuilderUser { Id = 1, Name = "Ada", IsActive = true, Age = 36 });

            var query = _database.FluentQuery()
                .From<BuilderUser>(out var user)
                .Select(() => new NestedProjectionDto
                {
                    User = new UserProjection { Id = user.Row.Id, Name = user.Row.Name }
                });

            var sql = query.ToSql();
            Assert.That(sql.SQL, Does.Contain("[bu].[Id] AS [User__Id]"));
            Assert.That(sql.SQL, Does.Contain("[bu].[Name] AS [User__Name]"));

            var row = query.Single();
            Assert.That(row.User.Id, Is.EqualTo(1));
            Assert.That(row.User.Name, Is.EqualTo("Ada"));
        }

        [Test]
        public void NestedAnonymousProjectionMapsMissingLeftJoinToNull()
        {
            _database.Insert(new BuilderUser { Id = 1, Name = "Ada", IsActive = true, Age = 36 });

            var row = _database.FluentQuery()
                .From<BuilderUser>(out var user)
                .LeftJoin<BuilderOrder>(out var order, x => x.UserId == -1)
                .Select(() => new
                {
                    User = user.Row,
                    Order = new { order.Row.Id, order.Row.Amount }
                })
                .Single();

            Assert.That(row.User.Name, Is.EqualTo("Ada"));
            Assert.That(row.Order, Is.Null);
        }

        [Test]
        public async System.Threading.Tasks.Task FetchAsyncMaterializesNestedAnonymousProjection()
        {
            _database.Insert(new BuilderUser { Id = 1, Name = "Ada", IsActive = true, Age = 36 });

            var rows = await _database.FluentQuery()
                .From<BuilderUser>(out var user)
                .Select(() => new { User = user.Row })
                .FetchAsync();

            Assert.That(rows.Single().User.Name, Is.EqualTo("Ada"));
        }

        [Test]
        public void ExecutesNullableAggregatesForRowsAndEmptySets()
        {
            _database.Insert(new BuilderOrder { Id = 1, UserId = 1, Amount = 10m });
            _database.Insert(new BuilderOrder { Id = 2, UserId = 1, Amount = 20m });

            var aggregates = _database.FluentQuery()
                .From<BuilderOrder>(out var order)
                .Where(order, x => x.UserId == 1)
                .Select(() => new NullableAggregateProjection
                {
                    Sum = FluentSql.Sum((decimal?)order.Row.Amount),
                    Average = FluentSql.Average((decimal?)order.Row.Amount),
                    Min = FluentSql.Min((decimal?)order.Row.Amount),
                    Max = FluentSql.Max((decimal?)order.Row.Amount)
                })
                .Single();

            var empty = _database.FluentQuery()
                .From<BuilderOrder>(out var emptyOrder)
                .Where(emptyOrder, x => x.UserId == -1)
                .Select(() => new NullableAggregateProjection
                {
                    Sum = FluentSql.Sum((decimal?)emptyOrder.Row.Amount),
                    Average = FluentSql.Average((decimal?)emptyOrder.Row.Amount),
                    Min = FluentSql.Min((decimal?)emptyOrder.Row.Amount),
                    Max = FluentSql.Max((decimal?)emptyOrder.Row.Amount)
                })
                .Single();

            Assert.That(aggregates.Sum, Is.EqualTo(30m));
            Assert.That(aggregates.Average, Is.EqualTo(15m));
            Assert.That(aggregates.Min, Is.EqualTo(10m));
            Assert.That(aggregates.Max, Is.EqualTo(20m));
            Assert.That(empty.Sum, Is.Null);
            Assert.That(empty.Average, Is.Null);
            Assert.That(empty.Min, Is.Null);
            Assert.That(empty.Max, Is.Null);
        }

        [Test]
        public void SelectProjectsNullableAggregatesIntoDtoAndAnonymousType()
        {
            _database.Insert(new BuilderOrder { Id = 1, UserId = 1, Amount = 10m });
            _database.Insert(new BuilderOrder { Id = 2, UserId = 1, Amount = 20m });

            var dto = _database.FluentQuery()
                .From<BuilderOrder>(out var dtoOrder)
                .Where(dtoOrder, x => x.UserId == 1)
                .Select(() => new NullableAggregateProjection
                {
                    Sum = FluentSql.Sum((decimal?)dtoOrder.Row.Amount),
                    Average = FluentSql.Average((decimal?)dtoOrder.Row.Amount),
                    Min = FluentSql.Min((decimal?)dtoOrder.Row.Amount),
                    Max = FluentSql.Max((decimal?)dtoOrder.Row.Amount)
                })
                .Single();

            var anonymous = _database.FluentQuery()
                .From<BuilderOrder>(out var anonymousOrder)
                .Where(anonymousOrder, x => x.UserId == 1)
                .Select(() => new
                {
                    Sum = FluentSql.Sum((decimal?)anonymousOrder.Row.Amount),
                    Average = FluentSql.Average((decimal?)anonymousOrder.Row.Amount),
                    Min = FluentSql.Min((decimal?)anonymousOrder.Row.Amount),
                    Max = FluentSql.Max((decimal?)anonymousOrder.Row.Amount)
                })
                .Single();

            Assert.That(dto.Sum, Is.EqualTo(30m));
            Assert.That(dto.Average, Is.EqualTo(15m));
            Assert.That(dto.Min, Is.EqualTo(10m));
            Assert.That(dto.Max, Is.EqualTo(20m));
            Assert.That(anonymous.Sum, Is.EqualTo(30m));
            Assert.That(anonymous.Average, Is.EqualTo(15m));
            Assert.That(anonymous.Min, Is.EqualTo(10m));
            Assert.That(anonymous.Max, Is.EqualTo(20m));
        }

        [Test]
        public void SelectSqlFunctionsParameterProjectsExpressionsIntoDtoAndAnonymousType()
        {
            _database.Insert(new BuilderUser { Id = 1, Name = "Ada", IsActive = true, Age = 36 });
            _database.Insert(new BuilderUser { Id = 2, Name = "Bob", IsActive = false, Age = 40 });

            var dto = _database.FluentQuery()
                .From<BuilderUser>(out var dtoUser)
                .Where(dtoUser, x => x.Id == 1)
                .Select(sql => new CalculatedProjection
                {
                    Label = dtoUser.Row.Name + "!",
                    Score = sql.Case(dtoUser.Row.IsActive, dtoUser.Row.Age + 1, 0),
                    UniqueNames = sql.CountDistinct(dtoUser.Row.Name)
                })
                .Single();

            var anonymous = _database.FluentQuery()
                .From<BuilderUser>(out var anonymousUser)
                .Select(sql => new
                {
                    Total = sql.Count(anonymousUser.Row.Id),
                    Oldest = sql.Max(anonymousUser.Row.Age),
                    Label = sql.Case(sql.Count(anonymousUser.Row.Id) > 1, "Many", "One")
                })
                .Single();

            Assert.That(dto.Label, Is.EqualTo("Ada!"));
            Assert.That(dto.Score, Is.EqualTo(37));
            Assert.That(dto.UniqueNames, Is.EqualTo(1));
            Assert.That(anonymous.Total, Is.EqualTo(2));
            Assert.That(anonymous.Oldest, Is.EqualTo(40));
            Assert.That(anonymous.Label, Is.EqualTo("Many"));
        }

        private static object[] ProviderExpressionCases =
        {
            new object[] { DatabaseType.SQLite, "[mr].[display_name] || @0", "datetime([mr].[Created], @2 || ' month')", "strftime('%Y'" },
            new object[] { DatabaseType.SqlServer2012, "[mr].[display_name] + @0", "DATEADD(month, @2, [mr].[Created])", "DATEPART(year" },
            new object[] { DatabaseType.MySQL, "CONCAT(`mr`.`display_name`, @0)", "DATE_ADD(`mr`.`Created`, INTERVAL @2 MONTH)", "YEAR(" },
            new object[] { DatabaseType.PostgreSQL, "\"mr\".\"display_name\" || @0", "@2 * INTERVAL '1 month'", "EXTRACT(YEAR" },
            new object[] { DatabaseType.Oracle, "\"MR\".\"DISPLAY_NAME\" || @0", "ADD_MONTHS(\"MR\".\"CREATED\", @2)", "EXTRACT(YEAR" },
            new object[] { DatabaseType.Firebird, "\"mr\".\"display_name\" || @0", "DATEADD(MONTH, @2, \"mr\".\"Created\")", "EXTRACT(YEAR" }
        };

        [TestCaseSource(nameof(ProviderPagingCases))]
        public void BuildsProviderSpecificPaging(DatabaseType databaseType, string expectedSql, int? firstArgument, int? secondArgument)
        {
            using (var connection = new SqliteConnection("Data Source=:memory:"))
            using (var database = new Database(connection, databaseType))
            {
                var sql = database.FluentQuery()
                    .From<BuilderUser>(out var user)
                    .OrderBy(user, x => x.Id)
                    .Skip(5)
                    .Take(10)
                    .SelectScalar(user, x => x.Name)
                    .ToSql();

                Assert.That(sql.SQL, Does.Contain(expectedSql));
                Assert.That(sql.Arguments, firstArgument.HasValue
                    ? Is.EqualTo(new object[] { firstArgument.Value, secondArgument.Value })
                    : Is.Empty);
            }
        }

        [Test]
        public void PagingParametersContinueAfterPredicateParameters()
        {
            var sql = _database.FluentQuery()
                .From<BuilderUser>(out var user)
                .Where(user, x => x.Age >= 18)
                .OrderBy(user, x => x.Id)
                .Skip(5)
                .Take(10)
                .Select(user)
                .ToSql();

            Assert.That(sql.SQL, Does.Contain("[bu].[Age] >= @0"));
            Assert.That(sql.SQL, Does.EndWith("LIMIT @1 OFFSET @2"));
            Assert.That(sql.Arguments, Is.EqualTo(new object[] { 18, 10, 5 }));
        }

        [Test]
        public void BuildsTypedCteWithContinuousParameters()
        {
            var query = _database.FluentQuery()
                .With<BuilderUser>("active_users", cte => cte
                    .From<BuilderUser>(out var user)
                    .Where(user, x => x.IsActive)
                    .Select(user), out var active)
                .From(active)
                .Where(active, x => x.Age >= 18)
                .Select(active)
                .ToSql();

            Assert.That(query.SQL, Does.StartWith(";WITH [active_users] AS (\n"));
            Assert.That(query.SQL, Does.Contain("FROM [active_users] [bu]"));
            Assert.That(query.SQL, Does.Contain("[bu].[IsActive] = @0"));
            Assert.That(query.SQL, Does.Contain("[bu].[Age] >= @1"));
            Assert.That(query.Arguments, Is.EqualTo(new object[] { true, 18 }));
        }

        [Test]
        public void ExecutesTypedCte()
        {
            _database.Insert(new BuilderUser { Id = 1, Name = "Ada", IsActive = true, Age = 36 });
            _database.Insert(new BuilderUser { Id = 2, Name = "Bob", IsActive = false, Age = 40 });

            var users = _database.FluentQuery()
                .With<BuilderUser>("active_users", cte => cte
                    .From<BuilderUser>(out var user)
                    .Where(user, x => x.IsActive)
                    .Select(user), out var active)
                .From(active)
                .Select(active)
                .Fetch();

            Assert.That(users.Select(x => x.Name), Is.EqualTo(new[] { "Ada" }));
        }

        [Test]
        public void RejectsDuplicateCteNamesAndForeignCteReferences()
        {
            var query = _database.FluentQuery()
                .With<BuilderUser>("users", cte => cte.From<BuilderUser>(out var user).Select(user), out _);

            Assert.Throws<InvalidOperationException>(() => query
                .With<BuilderUser>("USERS", cte => cte.From<BuilderUser>(out var user).Select(user), out _));

            var owner = _database.FluentQuery()
                .With<BuilderUser>("other_users", cte => cte.From<BuilderUser>(out var user).Select(user), out var foreign);
            Assert.Throws<InvalidOperationException>(() => query.From(foreign));
        }

        [Test]
        public void BuildsMultipleCtesInDeclarationOrderWithContinuousParameters()
        {
            var sql = _database.FluentQuery()
                .With<BuilderUser>("adult_users", cte => cte
                    .From<BuilderUser>(out var user)
                    .Where(user, x => x.Age >= 18)
                    .Select(user), out _)
                .With<BuilderOrder>("large_orders", cte => cte
                    .From<BuilderOrder>(out var order)
                    .Where(order, x => x.Amount >= 100m)
                    .Select(order), out var orders)
                .From(orders)
                .Where(orders, x => x.UserId > 5)
                .Select(orders)
                .ToSql();

            Assert.That(sql.SQL.IndexOf("[adult_users] AS", StringComparison.Ordinal),
                Is.LessThan(sql.SQL.IndexOf("[large_orders] AS", StringComparison.Ordinal)));
            Assert.That(sql.SQL, Does.Contain("[bu].[Age] >= @0"));
            Assert.That(sql.SQL, Does.Contain("[bo].[Amount] >= @1"));
            Assert.That(sql.SQL, Does.Contain("[bo].[UserId] > @2"));
            Assert.That(sql.Arguments, Is.EqualTo(new object[] { 18, 100m, 5 }));
        }

        [Test]
        public void RejectsInvalidCteNameAndForeignCallbackResult()
        {
            var external = _database.FluentQuery().From<BuilderUser>(out var user).Select(user);
            Assert.Throws<ArgumentException>(() => _database.FluentQuery()
                .With<BuilderUser>("invalid name", cte => cte.From<BuilderUser>(out var row).Select(row), out _));
            Assert.Throws<InvalidOperationException>(() => _database.FluentQuery()
                .With<BuilderUser>("users", _ => external, out _));
        }

        [Test]
        public void BuildsUnionAndUnionAllWithContinuousParameters()
        {
            var sql = _database.FluentQuery()
                .From<BuilderUser>(out var first)
                .Where(first, x => x.Age < 18)
                .SelectScalar(first, x => x.Name)
                .Union(query => query
                    .From<BuilderUser>(out var second)
                    .Where(second, x => x.Age >= 65)
                    .SelectScalar(second, x => x.Name))
                .UnionAll(query => query
                    .From<BuilderUser>(out var third)
                    .Where(third, x => x.Name == "Ada")
                    .SelectScalar(third, x => x.Name))
                .ToSql();

            Assert.That(sql.SQL, Does.Contain("\nUNION\nSELECT"));
            Assert.That(sql.SQL, Does.Contain("\nUNION ALL\nSELECT"));
            Assert.That(sql.SQL, Does.Contain("[bu].[Age] < @0"));
            Assert.That(sql.SQL, Does.Contain("[bu].[Age] >= @1"));
            Assert.That(sql.SQL, Does.Contain("[bu].[Name] = @2"));
            Assert.That(sql.Arguments, Is.EqualTo(new object[] { 18, 65, "Ada" }));
        }

        [Test]
        public void ExecutesUnionAndUnionAll()
        {
            _database.Insert(new BuilderUser { Id = 1, Name = "Ada", IsActive = true, Age = 36 });
            _database.Insert(new BuilderUser { Id = 2, Name = "Bob", IsActive = false, Age = 40 });

            var union = _database.FluentQuery()
                .From<BuilderUser>(out var first)
                .Where(first, x => x.Id == 1)
                .SelectScalar(first, x => x.Name)
                .Union(query => query.From<BuilderUser>(out var second).Where(second, x => x.Id == 1).SelectScalar(second, x => x.Name))
                .Fetch();

            var unionAll = _database.FluentQuery()
                .From<BuilderUser>(out var allFirst)
                .Where(allFirst, x => x.Id == 1)
                .SelectScalar(allFirst, x => x.Name)
                .UnionAll(query => query.From<BuilderUser>(out var allSecond).Where(allSecond, x => x.Id == 1).SelectScalar(allSecond, x => x.Name))
                .Fetch();

            Assert.That(union, Is.EqualTo(new[] { "Ada" }));
            Assert.That(unionAll, Is.EqualTo(new[] { "Ada", "Ada" }));
        }

        [Test]
        public void UsesUnionInsideCte()
        {
            var sql = _database.FluentQuery()
                .With<BuilderUser>("selected_users", cte => cte
                    .From<BuilderUser>(out var first)
                    .Where(first, x => x.Id == 1)
                    .Select(first)
                    .UnionAll(query => query
                        .From<BuilderUser>(out var second)
                        .Where(second, x => x.Id == 2)
                        .Select(second)), out var selected)
                .From(selected)
                .Select(selected)
                .ToSql();

            Assert.That(sql.SQL, Does.Contain("[selected_users] AS ("));
            Assert.That(sql.SQL, Does.Contain("\n    UNION ALL\n    SELECT"));
            Assert.That(sql.Arguments, Is.EqualTo(new object[] { 1, 2 }));
        }

        [Test]
        public void RejectsForeignUnionCallbackResult()
        {
            var external = _database.FluentQuery().From<BuilderUser>(out var externalUser).Select(externalUser);
            var result = _database.FluentQuery().From<BuilderUser>(out var user).Select(user);

            Assert.Throws<InvalidOperationException>(() => result.Union(_ => external));
        }

        [Test]
        public void CombinesSeparatelyConstructedCteAndUnionQueries()
        {
            var cteDefinition = _database.FluentQuery()
                .From<BuilderUser>(out var cteUser)
                .Where(cteUser, x => x.IsActive)
                .Select(cteUser);
            var secondSet = _database.FluentQuery()
                .From<BuilderUser>(out var secondUser)
                .Where(secondUser, x => x.Age >= 65)
                .Select(secondUser);

            var sql = _database.FluentQuery()
                .With("active_users", cteDefinition, out TableReference<BuilderUser> active)
                .From(active)
                .Where(active, x => x.Age >= 18)
                .Select(active)
                .UnionAll(secondSet)
                .ToSql();

            Assert.That(sql.SQL, Does.Contain("[active_users] AS ("));
            Assert.That(sql.SQL, Does.Contain("\nUNION ALL\nSELECT"));
            Assert.That(sql.Arguments, Is.EqualTo(new object[] { true, 18, 65 }));
        }

        private static object[] ProviderPagingCases =
        {
            new object[] { DatabaseType.SQLite, "LIMIT @0 OFFSET @1", 10, 5 },
            new object[] { DatabaseType.MySQL, "LIMIT @0 OFFSET @1", 10, 5 },
            new object[] { DatabaseType.PostgreSQL, "LIMIT @0 OFFSET @1", 10, 5 },
            new object[] { DatabaseType.SqlServer2012, "OFFSET @0 ROWS FETCH NEXT @1 ROWS ONLY", 5, 10 },
            new object[] { DatabaseType.Firebird, "SELECT FIRST 10 SKIP 5", null, null },
            new object[] { DatabaseType.Oracle, "ROW_NUMBER() OVER", 5, 15 }
        };

        [TestCaseSource(nameof(EscapingCases))]
        public void UsesDatabaseSpecificIdentifierEscaping(DatabaseType databaseType, string expected)
        {
            using (var connection = new SqliteConnection("Data Source=:memory:"))
            using (var database = new Database(connection, databaseType))
            {
                var sql = database.FluentQuery().From<BuilderUser>(out var users).SelectScalar(users, x => x.Name).ToSql();
                Assert.That(sql.SQL, Does.Contain(expected));
            }
        }

        private static object[] EscapingCases =
        {
            new object[] { DatabaseType.SQLite, "[bu].[Name]" },
            new object[] { DatabaseType.MySQL, "`bu`.`Name`" },
            new object[] { DatabaseType.PostgreSQL, "\"bu\".\"Name\"" }
        };

        [TableName("BuilderUsers")]
        private class BuilderUser
        {
            [Column("Id")]
            public int Id { get; set; }
            public string Name { get; set; }
            public bool IsActive { get; set; }
            public int Age { get; set; }
        }

        [TableName("BuilderOrders")]
        private class BuilderOrder
        {
            [Column("Id")]
            public int Id { get; set; }
            public int UserId { get; set; }
            public decimal Amount { get; set; }
        }

        private class BuilderSummary
        {
            public string Name { get; set; }
            public decimal Total { get; set; }
        }

        private class UserProjection
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        private class CalculatedProjection
        {
            public string Label { get; set; }
            public int Score { get; set; }
            public int UniqueNames { get; set; }
        }

        private sealed class ConstructorProjection
        {
            public ConstructorProjection(int id, string name)
            {
                Id = id;
                Name = name;
            }

            public int Id { get; }
            public string Name { get; }
        }

        private sealed class NullableAggregateProjection
        {
            public decimal? Sum { get; set; }
            public double? Average { get; set; }
            public decimal? Min { get; set; }
            public decimal? Max { get; set; }
        }

        private sealed class NestedProjectionDto
        {
            public UserProjection User { get; set; }
        }

        private class MappedProjection
        {
            public string DisplayName { get; set; }
            public string City { get; set; }
        }

        private class NestedSummary
        {
            public BuilderUser User { get; set; }
            public BuilderOrder Order { get; set; }
        }

        private enum RecordState { Inactive, Active }

        private class MappedAddress
        {
            public string City { get; set; }
        }

        [TableName("MappedRecords")]
        private class MappedRecord
        {
            public int Id { get; set; }
            [Column("display_name")]
            public string DisplayName { get; set; }
            [ColumnType(typeof(string))]
            public RecordState State { get; set; }
            public DateTime Created { get; set; }
            public int? OptionalId { get; set; }
            [ComplexMapping]
            public MappedAddress Address { get; set; }
        }
    }
}
