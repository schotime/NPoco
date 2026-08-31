using System;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using NPoco.FluentSql;
using NUnit.Framework;

namespace NPoco.Tests.FluentSqlTests
{
    /// <summary>
    /// FSql.Raw and FSql.Scalar - the two escape hatches for SQL the expression
    /// translator has no node for. Executed against SQLite rather than asserted as text, so the
    /// fragments have to actually run and materialize.
    /// </summary>
    [TestFixture]
    public class FluentSqlRawAndScalarTests
    {
        private string _file;
        private string _connectionString;

        [SetUp]
        public void SetUp()
        {
            _file = Path.Combine(Path.GetTempPath(), "npoco-raw-" + Guid.NewGuid().ToString("N") + ".db");
            _connectionString = "Data Source=" + _file;
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText =
                    "create table rawsites(id integer primary key, name text);" +
                    "create table rawsystems(id integer primary key, site_id integer, name text, active integer);" +
                    "insert into rawsites values(1,'north'),(2,'south');" +
                    "insert into rawsystems values(1,1,'a',1),(2,1,'b',1),(3,1,'c',0),(4,2,'d',1);";
                command.ExecuteNonQuery();
            }
        }

        [TearDown]
        public void TearDown()
        {
            SqliteConnection.ClearAllPools();
            try { File.Delete(_file); } catch (IOException) { }
        }

        private Database CreateDatabase() => new Database(_connectionString, DatabaseType.SQLite, SqliteFactory.Instance);

        // ---- Raw ----------------------------------------------------------------

        [Test]
        public void RawProjectsAVendorFunctionOverColumns()
        {
            using (var database = CreateDatabase())
            {
                var rows = database.FluentQuery()
                    .From<RawSystem>(out var system)
                    .Where(system, x => x.SiteId == 1)
                    .OrderBy(system, x => x.Id)
                    .Select(() => new
                    {
                        system.Row.Name,
                        Padded = FSql.Raw<string>("substr({0} || '____', 1, 4)", system.Row.Name)
                    })
                    .Fetch();

                Assert.That(rows.Select(x => x.Padded), Is.EqualTo(new[] { "a___", "b___", "c___" }));
            }
        }

        [Test]
        public void RawReusesAPlaceholderAndParameterizesCapturedValues()
        {
            using (var database = CreateDatabase())
            {
                var separator = "|";
                var sql = database.FluentQuery()
                    .From<RawSystem>(out var system)
                    .Select(() => new
                    {
                        Doubled = FSql.Raw<string>("({0} || {1} || {0})", system.Row.Name, separator)
                    })
                    .ToSql();

                // The captured value became a parameter rather than inlined text.
                Assert.That(sql.SQL, Does.Contain("[rs].[name] || @0 || [rs].[name]"));
                Assert.That(sql.Arguments, Is.EqualTo(new object[] { "|" }));
            }
        }

        [Test]
        public void RawWorksAsAPredicate()
        {
            using (var database = CreateDatabase())
            {
                var names = database.FluentQuery()
                    .From<RawSystem>(out var system)
                    .Where(() => FSql.Raw<bool>("{0} in ('a','d')", system.Row.Name))
                    .OrderBy(system, x => x.Id)
                    .Select(() => new { system.Row.Name })
                    .Fetch();

                Assert.That(names.Select(x => x.Name), Is.EqualTo(new[] { "a", "d" }));
            }
        }

        [Test]
        public void RawComposesInsideALargerPredicate()
        {
            using (var database = CreateDatabase())
            {
                var names = database.FluentQuery()
                    .From<RawSystem>(out var system)
                    .Where(() => FSql.Raw<bool>("{0} = 1 or {0} = 4", system.Row.Id) && system.Row.Active)
                    .OrderBy(system, x => x.Id)
                    .Select(() => new { system.Row.Name })
                    .Fetch();

                // Without the parentheses Raw adds, the OR would swallow the AND.
                Assert.That(names.Select(x => x.Name), Is.EqualTo(new[] { "a", "d" }));
            }
        }

        [Test]
        public void RawWorksWithoutArguments()
        {
            using (var database = CreateDatabase())
            {
                var rows = database.FluentQuery()
                    .From<RawSystem>(out var system)
                    .Where(() => FSql.Raw<bool>("1 = 1"))
                    .Select(() => new { Answer = FSql.Raw<int>("40 + 2") })
                    .Fetch();

                Assert.That(rows.Select(x => x.Answer).Distinct().Single(), Is.EqualTo(42));
            }
        }

        [Test]
        public void RawWorksInAnAggregateProjection()
        {
            using (var database = CreateDatabase())
            {
                var rows = database.FluentQuery()
                    .From<RawSystem>(out var system)
                    .GroupBy(system, x => x.SiteId)
                    .OrderBy(system, x => x.SiteId)
                    .Select(() => new RawGrouped
                    {
                        SiteId = system.Row.SiteId,
                        Names = FSql.Raw<string>("group_concat({0}, '-')", system.Row.Name)
                    })
                    .Fetch();

                Assert.That(rows.Select(x => x.Names), Is.EqualTo(new[] { "a-b-c", "d" }));
            }
        }

        [Test]
        public void RawEscapesDoubledBraces()
        {
            using (var database = CreateDatabase())
            {
                var rows = database.FluentQuery()
                    .From<RawSystem>(out var system)
                    .Where(system, x => x.Id == 1)
                    .Select(() => new { Braced = FSql.Raw<string>("'{{' || {0} || '}}'", system.Row.Name) })
                    .Fetch();

                Assert.That(rows.Single().Braced, Is.EqualTo("{a}"));
            }
        }

        [Test]
        public void RawRejectsAPlaceholderWithNoMatchingArgument()
        {
            using (var database = CreateDatabase())
            {
                var query = database.FluentQuery()
                    .From<RawSystem>(out var system)
                    .Select(() => new { Bad = FSql.Raw<string>("upper({1})", system.Row.Name) });

                var exception = Assert.Throws<ArgumentException>(() => query.ToSql());
                Assert.That(exception.Message, Does.Contain("1 argument(s)"));
                Assert.That(exception.Message, Does.Contain("upper({1})"));
            }
        }

        // ---- Scalar -------------------------------------------------------------

        [Test]
        public void ScalarProjectsACorrelatedSubquery()
        {
            using (var database = CreateDatabase())
            {
                var query = database.FluentQuery().From<RawSite>(out var site);

                var activeSystems = query.Subquery()
                    .From<RawSystem>(out var system)
                    .Where(() => system.Row.SiteId == site.Row.Id && system.Row.Active)
                    .SelectScalar(system, x => FSql.Count());

                var rows = query
                    .OrderBy(site, x => x.Id)
                    .Select(() => new { site.Row.Name, ActiveSystems = FSql.Scalar<int>(activeSystems) })
                    .Fetch();

                Assert.That(rows.Select(x => x.Name), Is.EqualTo(new[] { "north", "south" }));
                Assert.That(rows.Select(x => x.ActiveSystems), Is.EqualTo(new[] { 2, 1 }));
            }
        }

        [Test]
        public void ScalarProjectsAnUncorrelatedSubquery()
        {
            using (var database = CreateDatabase())
            {
                var total = database.FluentQuery()
                    .From<RawSystem>(out var all)
                    .SelectScalar(all, x => FSql.Count());

                var rows = database.FluentQuery()
                    .From<RawSite>(out var site)
                    .OrderBy(site, x => x.Id)
                    .Select(() => new { site.Row.Name, Total = FSql.Scalar<int>(total) })
                    .Fetch();

                Assert.That(rows.Select(x => x.Total), Is.EqualTo(new[] { 4, 4 }));
            }
        }

        [Test]
        public void ScalarComposesWithOtherExpressions()
        {
            using (var database = CreateDatabase())
            {
                var query = database.FluentQuery().From<RawSite>(out var site);

                var systems = query.Subquery()
                    .From<RawSystem>(out var system)
                    .Where(() => system.Row.SiteId == site.Row.Id)
                    .SelectScalar(system, x => FSql.Count());

                var rows = query
                    .Where(() => FSql.Scalar<int>(systems) > 1)
                    .Select(() => new { site.Row.Name, Plus = FSql.Scalar<int>(systems) + 10 })
                    .Fetch();

                Assert.That(rows.Single().Name, Is.EqualTo("north"));
                Assert.That(rows.Single().Plus, Is.EqualTo(13));
            }
        }

        // ---- Exists, which shares the marker path Scalar uses --------------------

        [Test]
        public void ExistsBuildsInsteadOfInvokingTheMarkerMethod()
        {
            using (var database = CreateDatabase())
            {
                var query = database.FluentQuery().From<RawSite>(out var site);

                var inactive = query.Subquery()
                    .From<RawSystem>(out var system)
                    .Where(() => system.Row.SiteId == site.Row.Id && !system.Row.Active)
                    .SelectScalar(system, x => x.Id);

                var rows = query
                    .Where(() => FSql.Exists(inactive))
                    .Select(() => new { site.Row.Name })
                    .Fetch();

                Assert.That(rows.Single().Name, Is.EqualTo("north"));
            }
        }

        [Test]
        public void NotExistsBuildsInsteadOfInvokingTheMarkerMethod()
        {
            using (var database = CreateDatabase())
            {
                var query = database.FluentQuery().From<RawSite>(out var site);

                var inactive = query.Subquery()
                    .From<RawSystem>(out var system)
                    .Where(() => system.Row.SiteId == site.Row.Id && !system.Row.Active)
                    .SelectScalar(system, x => x.Id);

                var rows = query
                    .Where(() => FSql.NotExists(inactive))
                    .Select(() => new { site.Row.Name })
                    .Fetch();

                Assert.That(rows.Single().Name, Is.EqualTo("south"));
            }
        }

        [Test]
        public void SubqueryRequiresAFromOnTheOuterQuery()
        {
            using (var database = CreateDatabase())
            {
                var query = database.FluentQuery().From<RawSite>(out var site);
                var uncorrelated = database.FluentQuery()
                    .From<RawSystem>(out var system)
                    .Where(() => system.Row.SiteId == site.Row.Id)
                    .SelectScalar(system, x => FSql.Count());

                // Built from FluentQuery(), not query.Subquery(), so the outer table is out of scope.
                Assert.Throws<InvalidOperationException>(() => uncorrelated.ToSql());
            }
        }

        [Test]
        public void CorrelatedSubqueryNeverReusesAnOuterAlias()
        {
            using (var database = CreateDatabase())
            {
                var query = database.FluentQuery().From<RawSite>(out var site);

                // RawSite and RawSystem both shorten to "rs"; the inner one has to be renamed or
                // the correlation predicate compares a table against itself.
                var systems = query.Subquery()
                    .From<RawSystem>(out var system)
                    .Where(() => system.Row.SiteId == site.Row.Id)
                    .SelectScalar(system, x => FSql.Count());

                Assert.That(site.Alias, Is.Not.EqualTo(system.Alias));

                var sql = query.Select(() => new { Count = FSql.Scalar<int>(systems) }).ToSql().SQL;
                Assert.That(sql, Does.Contain("[" + system.Alias + "].[site_id] = [" + site.Alias + "].[id]"));
            }
        }

        [Test]
        public void OuterApplyNeverReusesAnOuterAlias()
        {
            using (var database = CreateDatabase())
            {
                // SQLite has no OUTER APPLY, so this asserts the generated SQL rather than running it.
                var sql = database.FluentQuery()
                    .From<RawSite>(out var site)
                    .OuterApply(out var newest, apply => apply
                        .From<RawSystem>(out var system)
                        .Where(() => system.Row.SiteId == site.Row.Id)
                        .OrderByDescending(system, x => x.Id)
                        .Take(1)
                        .Select(system))
                    .Select(() => new { site.Row.Name, Newest = newest.Row.Name })
                    .ToSql().SQL;

                // OuterApply creates two references: the inner FROM and the derived table. Neither
                // may take the outer alias, and they may not take each other's.
                Assert.That(newest.Alias, Is.Not.EqualTo(site.Alias));
                Assert.That(sql, Does.Contain("FROM [rawsites] [" + site.Alias + "]"));
                Assert.That(sql, Does.Contain(") [" + newest.Alias + "]"));
                Assert.That(sql, Does.Contain("].[site_id] = [" + site.Alias + "].[id]"));
                Assert.That(sql, Does.Not.Contain("FROM [rawsystems] [" + site.Alias + "]"));
            }
        }

        [Test]
        public void ReproducesTheCorrelatedCountAndExistsShapeFromUcbWeb()
        {
            using (var database = CreateDatabase())
            {
                // select id, name,
                //   (select count(*) from rawsystems s where s.site_id = ess.id and s.active) as systemCount
                // from rawsites ess
                // where exists (select 1 from rawsystems s where s.site_id = ess.id)
                var query = database.FluentQuery().From<RawSite>(out var site);

                var activeCount = query.Subquery()
                    .From<RawSystem>(out var counted)
                    .Where(() => counted.Row.SiteId == site.Row.Id && counted.Row.Active)
                    .SelectScalar(counted, x => FSql.Count());

                var any = query.Subquery()
                    .From<RawSystem>(out var probe)
                    .Where(() => probe.Row.SiteId == site.Row.Id)
                    .SelectScalar(probe, x => x.Id);

                var rows = query
                    .Where(() => FSql.Exists(any))
                    .OrderBy(site, x => x.Name)
                    .Select(() => new { site.Row.Id, site.Row.Name, SystemCount = FSql.Scalar<int>(activeCount) })
                    .Fetch();

                Assert.That(rows.Select(x => x.Name), Is.EqualTo(new[] { "north", "south" }));
                Assert.That(rows.Select(x => x.SystemCount), Is.EqualTo(new[] { 2, 1 }));
            }
        }

        [Test]
        public void SubquerySeesTablesJoinedAfterItWasCreated()
        {
            using (var database = CreateDatabase())
            {
                var query = database.FluentQuery().From<RawSite>(out var site);

                // Created before the join below. Scope is resolved when the subquery is read, not
                // when it was created, so the joined table is still in scope for it.
                var sub = query.Subquery().From<RawSystem>(out var counted);

                var staged = query.InnerJoin<RawSystem>(out var flagged, x => x.SiteId == site.Row.Id && x.Active);

                var atOrAfter = sub
                    .Where(() => counted.Row.SiteId == site.Row.Id && counted.Row.Id >= flagged.Row.Id)
                    .SelectScalar(counted, x => FSql.Count());

                var rows = staged
                    .OrderBy(flagged, x => x.Id)
                    .Select(() => new { site.Row.Name, From = flagged.Row.Id, N = FSql.Scalar<int>(atOrAfter) })
                    .Fetch();

                Assert.That(rows.Select(x => x.From), Is.EqualTo(new[] { 1, 2, 4 }));
                Assert.That(rows.Select(x => x.N), Is.EqualTo(new[] { 3, 2, 1 }));
            }
        }

        [Test]
        public void NestedOuterApplyCorrelatesToTheOutermostQuery()
        {
            using (var database = CreateDatabase())
            {
                // SQLite has no OUTER APPLY, so this asserts the generated SQL.
                var sql = database.FluentQuery()
                    .From<RawSite>(out var site)
                    .OuterApply(out var outerApply, first => first
                        .From<RawSystem>(out var s1)
                        .OuterApply(out var innerApply, second => second
                            .From<RawSystem>(out var s2)
                            .Where(() => s2.Row.SiteId == site.Row.Id && s2.Row.Id > s1.Row.Id)
                            .Take(1)
                            .Select(s2))
                        .Where(() => s1.Row.SiteId == site.Row.Id)
                        .Select(s1))
                    .Select(() => new { site.Row.Name, Next = outerApply.Row.Name })
                    .ToSql().SQL;

                // The innermost apply reaches two levels out to the original FROM.
                Assert.That(sql, Does.Contain("].[site_id] = [" + site.Alias + "].[id]"));
                Assert.That(sql, Does.Contain("[" + site.Alias + "].[id])"));
            }
        }

        [TableName("rawsites")]
        public class RawSite
        {
            [Column("id")] public int Id { get; set; }
            [Column("name")] public string Name { get; set; }
        }

        [TableName("rawsystems")]
        public class RawSystem
        {
            [Column("id")] public int Id { get; set; }
            [Column("site_id")] public int SiteId { get; set; }
            [Column("name")] public string Name { get; set; }
            [Column("active")] public bool Active { get; set; }
        }

        public class RawGrouped
        {
            public int SiteId { get; set; }
            public string Names { get; set; }
        }
    }
}
