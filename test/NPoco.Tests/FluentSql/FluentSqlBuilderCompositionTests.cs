using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using NPoco.FluentSqlBuilder;
using NUnit.Framework;

namespace NPoco.Tests.FluentSqlTests
{
    /// <summary>
    /// How FluentSql.Raw and FluentSql.Scalar behave once combined with the rest of the builder -
    /// joins, grouping, CTEs, unions, paging, async - and the projection shapes the row mapper has
    /// to materialize. Run against SQLite so the SQL has to be valid, not merely well formed.
    /// </summary>
    [TestFixture]
    public class FluentSqlBuilderCompositionTests
    {
        private string _file;
        private string _connectionString;

        [SetUp]
        public void SetUp()
        {
            _file = Path.Combine(Path.GetTempPath(), "npoco-composition-" + Guid.NewGuid().ToString("N") + ".db");
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

        private Database Db() => new Database(_connectionString, DatabaseType.SQLite, SqliteFactory.Instance);

        [Test] public void RawInOrderBy()
        {
            using var db = Db();
            var r = db.FluentQuery().From<RawSystem>(out var s)
                .OrderBy(s, x => FluentSql.Raw<string>("upper({0})", s.Row.Name), descending: true)
                .Select(() => new { s.Row.Name }).Fetch();
            Assert.That(r.Select(x => x.Name), Is.EqualTo(new[] { "d", "c", "b", "a" }));
        }

        [Test] public void OrderByRowExpressionAcrossJoinedTables()
        {
            using var db = Db();
            var r = db.FluentQuery().From<RawSite>(out var site)
                .InnerJoin<RawSystem>(out var s, x => x.SiteId == site.Row.Id)
                .OrderByDescending(() => site.Row.Name)
                .ThenBy(() => FluentSql.Raw<string>("upper({0})", s.Row.Name))
                .Select(() => new { Site = site.Row.Name, System = s.Row.Name }).Fetch();
            Assert.That(r.Select(x => x.System), Is.EqualTo(new[] { "d", "a", "b", "c" }));
            Assert.That(r.Select(x => x.Site), Is.EqualTo(new[] { "south", "north", "north", "north" }));
        }

        [Test] public void GroupByRowExpressionAcrossJoinedTables()
        {
            using var db = Db();
            var r = db.FluentQuery().From<RawSite>(out var site)
                .InnerJoin<RawSystem>(out var s, x => x.SiteId == site.Row.Id)
                .GroupBy(() => site.Row.Name)
                .Having(() => FluentSql.Count() > 1)
                .OrderBy(() => site.Row.Name)
                .Select(() => new { Site = site.Row.Name, N = FluentSql.Count() }).Fetch();
            Assert.That(r.Select(x => x.Site), Is.EqualTo(new[] { "north" }));
            Assert.That(r.Select(x => x.N), Is.EqualTo(new[] { 3 }));
        }

        [Test] public void SelectWithScalarBodySkipsTheProjectionPlan()
        {
            using var db = Db();
            var names = db.FluentQuery().From<RawSystem>(out var s)
                .OrderBy(() => s.Row.Id)
                .Select(() => s.Row.Name).Fetch();
            Assert.That(names, Is.EqualTo(new[] { "a", "b", "c", "d" }));

            var counted = db.FluentQuery().From<RawSystem>(out var s2)
                .Select(x => x.Count()).Fetch();
            Assert.That(counted, Is.EqualTo(new[] { 4 }));

            var joined = db.FluentQuery().From<RawSite>(out var site)
                .InnerJoin<RawSystem>(out var s3, x => x.SiteId == site.Row.Id)
                .OrderBy(() => s3.Row.Id)
                .Select(() => site.Row.Name + "/" + s3.Row.Name).Fetch();
            Assert.That(joined, Is.EqualTo(new[] { "north/a", "north/b", "north/c", "south/d" }));

            // An entity body still goes through the plan.
            var entities = db.FluentQuery().From<RawSystem>(out var s4)
                .OrderBy(() => s4.Row.Id)
                .Select(() => s4.Row).Fetch();
            Assert.That(entities.Select(x => x.Name), Is.EqualTo(new[] { "a", "b", "c", "d" }));
        }

        [Test] public void SelectScalarRowExpressionAcrossJoinedTables()
        {
            using var db = Db();
            var labels = db.FluentQuery().From<RawSite>(out var site)
                .InnerJoin<RawSystem>(out var s, x => x.SiteId == site.Row.Id)
                .OrderBy(() => s.Row.Id)
                .SelectScalar(() => site.Row.Name + "/" + s.Row.Name)
                .Fetch();
            Assert.That(labels, Is.EqualTo(new[] { "north/a", "north/b", "north/c", "south/d" }));
        }

        [Test] public void SelectScalarRowExpressionAsCorrelatedScalar()
        {
            using var db = Db();
            var q = db.FluentQuery().From<RawSite>(out var site);
            var weighted = q.Subquery()
                .From<RawSystem>(out var s)
                .Where(() => s.Row.SiteId == site.Row.Id)
                .SelectScalar(() => FluentSql.Sum(s.Row.Id * site.Row.Id));
            var rows = q.OrderBy(() => site.Row.Id)
                .Select(() => new { site.Row.Name, Weighted = FluentSql.Scalar<int>(weighted) })
                .Fetch();
            Assert.That(rows.Select(x => x.Name), Is.EqualTo(new[] { "north", "south" }));
            Assert.That(rows.Select(x => x.Weighted), Is.EqualTo(new[] { 6, 8 }));
        }

        [Test] public void CtesCanBeDeclaredWithoutNamingThem()
        {
            using var db = Db();
            var query = db.FluentQuery();
            var result = query
                .With(sub => sub.From<RawSystem>(out var s).Where(() => s.Row.Active).Select(s), out var active)
                .With(sub => sub.From<RawSite>(out var site).Select(site), out var sites)
                .From(active)
                .InnerJoin<RawSite>(out var joined, x => x.Id == active.Row.SiteId)
                .OrderBy(() => active.Row.Id)
                .Select(() => new { System = active.Row.Name, Site = joined.Row.Name });

            var sql = result.ToSql().SQL;
            Assert.That(sql, Does.Contain("[__w1] AS"));
            Assert.That(sql, Does.Contain("[__w2] AS"));
            Assert.That(result.Fetch().Select(x => x.System), Is.EqualTo(new[] { "a", "b", "d" }));
        }

        [Test] public void AnAnonymousProjectionInACteGetsAReadableAlias()
        {
            using var db = Db();
            var query = db.FluentQuery();
            var rows = query
                .With(sub => sub.From<RawSystem>(out var s)
                                .Select(() => new { s.Row.Id, s.Row.Name }), out var summary)
                .From(summary)
                .OrderBy(() => summary.Row.Id)
                .Select(() => new { summary.Row.Name });

            Assert.That(summary.Alias, Is.EqualTo("__t1"));
            Assert.That(rows.ToSql().SQL, Does.Not.Contain("<"));
            Assert.That(rows.Fetch().Select(x => x.Name), Is.EqualTo(new[] { "a", "b", "c", "d" }));
        }

        [Test] public void AStageCanBeProjectedMoreThanOnce()
        {
            using var db = Db();
            var stage = db.FluentQuery().From<RawSystem>(out var s)
                .Where(() => s.Row.SiteId == 1)
                .OrderBy(() => s.Row.Id);

            // Each projection is a snapshot of the stage, not a mutation of it, so the same base
            // query can be read in several shapes.
            var names = stage.Select(() => s.Row.Name).Fetch();
            var count = stage.Select(() => FluentSql.Count()).Single();
            var rows = stage.Select(() => new { s.Row.Id, s.Row.Name }).Fetch();
            var entities = stage.Select(s).Fetch();

            Assert.That(names, Is.EqualTo(new[] { "a", "b", "c" }));
            Assert.That(count, Is.EqualTo(3));
            Assert.That(rows.Select(x => x.Name), Is.EqualTo(new[] { "a", "b", "c" }));
            Assert.That(entities.Select(x => x.Name), Is.EqualTo(new[] { "a", "b", "c" }));
        }

        [Test] public void AProjectionIgnoresWhatIsAddedToTheStageAfterwards()
        {
            using var db = Db();
            var stage = db.FluentQuery().From<RawSystem>(out var s).OrderBy(() => s.Row.Id);

            var all = stage.Select(() => s.Row.Name);
            stage.Where(() => s.Row.SiteId == 2).Take(1);

            Assert.That(all.Fetch(), Is.EqualTo(new[] { "a", "b", "c", "d" }));
            Assert.That(all.ToSql().SQL, Does.Not.Contain("site_id"));
            Assert.That(stage.Select(() => s.Row.Name).Fetch(), Is.EqualTo(new[] { "d" }));
        }

        [Test] public void ProjectionsFromOneStageCanBeUnionedAndCorrelated()
        {
            using var db = Db();
            var stage = db.FluentQuery().From<RawSystem>(out var s);

            var first = stage.Where(() => s.Row.Id == 1).Select(() => s.Row.Name);
            var second = db.FluentQuery().From<RawSystem>(out var s2)
                .Where(() => s2.Row.Id == 4).Select(() => s2.Row.Name);
            Assert.That(first.UnionAll(second).Fetch().OrderBy(x => x), Is.EqualTo(new[] { "a", "d" }));

            // A subquery taken from the stage still correlates after the stage has been projected.
            var outer = db.FluentQuery().From<RawSite>(out var site);
            var systems = outer.Subquery().From<RawSystem>(out var s3)
                .Where(() => s3.Row.SiteId == site.Row.Id)
                .Select(() => FluentSql.Count());
            outer.OrderBy(() => site.Row.Id);
            var names = outer.Select(() => site.Row.Name).Fetch();
            var counted = outer.Select(() => new { site.Row.Name, N = FluentSql.Scalar<int>(systems) }).Fetch();

            Assert.That(names, Is.EqualTo(new[] { "north", "south" }));
            Assert.That(counted.Select(x => x.N), Is.EqualTo(new[] { 3, 1 }));
        }

        [Test] public void CapturedValuesInOrderByStayAlignedWithPagingParameters()
        {
            using var db = Db();
            var min = 1;
            var factor = 10;
            var bump = 100;

            // Paging rewrites the statement and rebuilds the parameter list, so a captured value
            // that reached the SQL through ORDER BY has to survive that rewrite in place.
            var query = db.FluentQuery().From<RawSystem>(out var s)
                .Where(() => s.Row.Id >= min)
                .OrderBy(() => s.Row.Id * factor + bump)
                .Skip(1).Take(2)
                .Select(() => s.Row.Name);

            var sql = query.ToSql();
            Assert.That(sql.Arguments.Take(3), Is.EqualTo(new object[] { 1, 10, 100 }));
            Assert.That(query.Fetch(), Is.EqualTo(new[] { "b", "c" }));
        }

        [Test] public void CapturedValuesInGroupByAndHavingKeepTheirOrder()
        {
            using var db = Db();
            var multiplier = 1;
            var minCount = 1;

            var rows = db.FluentQuery().From<RawSystem>(out var s)
                .GroupBy(() => s.Row.SiteId * multiplier)
                .Having(() => FluentSql.Count() > minCount)
                .OrderBy(() => s.Row.SiteId * multiplier)
                .Select(() => new { Site = s.Row.SiteId, N = FluentSql.Count() })
                .Fetch();

            Assert.That(rows.Select(x => x.Site), Is.EqualTo(new[] { 1 }));
            Assert.That(rows.Select(x => x.N), Is.EqualTo(new[] { 3 }));
        }

        [Test] public void HavingIfHonoursCondition()
        {
            using var db = Db();
            int[] Sites(bool apply) => db.FluentQuery().From<RawSystem>(out var s)
                .GroupBy(() => s.Row.SiteId)
                .HavingIf(apply, () => FluentSql.Count() > 1)
                .OrderBy(() => s.Row.SiteId)
                .Select(() => new { Site = s.Row.SiteId }).Fetch().Select(x => x.Site).ToArray();
            Assert.That(Sites(true), Is.EqualTo(new[] { 1 }));
            Assert.That(Sites(false), Is.EqualTo(new[] { 1, 2 }));

            int[] Tabled(bool apply) => db.FluentQuery().From<RawSystem>(out var s)
                .GroupBy(s, x => x.SiteId)
                .HavingIf(apply, s, x => FluentSql.Count(x.Id) > 1)
                .OrderBy(s, x => x.SiteId)
                .Select(() => new { Site = s.Row.SiteId }).Fetch().Select(x => x.Site).ToArray();
            Assert.That(Tabled(true), Is.EqualTo(new[] { 1 }));
            Assert.That(Tabled(false), Is.EqualTo(new[] { 1, 2 }));
        }

        [Test] public void RawInGroupByAndHaving()
        {
            using var db = Db();
            var r = db.FluentQuery().From<RawSystem>(out var s)
                .GroupBy(s, x => FluentSql.Raw<int>("{0}", s.Row.SiteId))
                .Having(() => FluentSql.Raw<bool>("count(*) > 1"))
                .Select(() => new { Site = s.Row.SiteId, N = FluentSql.Count() }).Fetch();
            Assert.That(r.Count, Is.EqualTo(1));
            Assert.That(r[0].N, Is.EqualTo(3));
        }

        [Test] public void ParameterOrderAcrossSelectRawAndWhere()
        {
            using var db = Db();
            var pre = "p:"; var min = 2;
            var sql = db.FluentQuery().From<RawSystem>(out var s)
                .Where(s, x => x.Id >= min)
                .Select(() => new { X = FluentSql.Raw<string>("({0} || {1})", pre, s.Row.Name) })
                .ToSql();
            TestContext.WriteLine(sql.SQL);
            TestContext.WriteLine(string.Join(",", sql.Arguments));
            var rows = db.FluentQuery().From<RawSystem>(out var s2)
                .Where(s2, x => x.Id >= min)
                .Select(() => new { X = FluentSql.Raw<string>("({0} || {1})", pre, s2.Row.Name) })
                .Fetch();
            Assert.That(rows.Select(x => x.X), Is.EqualTo(new[] { "p:b", "p:c", "p:d" }));
        }

        [Test] public void ScalarInsideCte()
        {
            using var db = Db();
            var q = db.FluentQuery();
            var r = q.With(sub => sub.From<RawSystem>(out var s)
                                     .Where(s, x => x.Active)
                                     .Select(s), out var t)
                .From(t)
                .OrderBy(t, x => x.Id)
                .Select(() => new { t.Row.Name }).Fetch();
            Assert.That(r.Select(x => x.Name), Is.EqualTo(new[] { "a", "b", "d" }));
        }

        [Test] public void RawInsideCte()
        {
            using var db = Db();
            var r = db.FluentQuery()
                .With(sub => sub.From<RawSystem>(out var s)
                                .Where(() => FluentSql.Raw<bool>("{0} in (1,4)", s.Row.Id))
                                .Select(s), out var t)
                .From(t)
                .OrderBy(t, x => x.Id)
                .Select(() => new { t.Row.Name }).Fetch();
            Assert.That(r.Select(x => x.Name), Is.EqualTo(new[] { "a", "d" }));
        }

        [Test] public void NestedSubquery()
        {
            using var db = Db();
            var q = db.FluentQuery().From<RawSite>(out var site);
            var inner = q.Subquery().From<RawSystem>(out var s1);
            var deepest = inner.Subquery().From<RawSystem>(out var s2)
                .Where(() => s2.Row.Id == s1.Row.Id && s2.Row.Active)
                .SelectScalar(s2, x => x.Id);
            var mid = inner.Where(() => s1.Row.SiteId == site.Row.Id && FluentSql.Exists(deepest))
                .SelectScalar(s1, x => FluentSql.Count());
            var r = q.OrderBy(site, x => x.Id).Select(() => new { site.Row.Name, N = FluentSql.Scalar<int>(mid) }).Fetch();
            Assert.That(r.Select(x => x.N), Is.EqualTo(new[] { 2, 1 }));
        }

        [Test] public void ScalarReturningNull()
        {
            using var db = Db();
            var q = db.FluentQuery().From<RawSite>(out var site);
            var none = q.Subquery().From<RawSystem>(out var s)
                .Where(() => s.Row.SiteId == site.Row.Id && s.Row.Name == "zzz")
                .SelectScalar(s, x => x.Name);
            var r = q.OrderBy(site, x => x.Id).Select(() => new { site.Row.Name, Missing = FluentSql.Scalar<string>(none) }).Fetch();
            Assert.That(r.All(x => x.Missing == null));
        }

        [Test] public void SameTableTypeJoinedTwice()
        {
            using var db = Db();
            var sql = db.FluentQuery().From<RawSystem>(out var a)
                .InnerJoin<RawSystem>(out var b, x => x.SiteId == a.Row.SiteId)
                .Select(() => new { A = a.Row.Name, B = b.Row.Name }).ToSql().SQL;
            TestContext.WriteLine(sql);
            Assert.That(a.Alias, Is.Not.EqualTo(b.Alias));
        }

        [Test] public void UnionWithRaw()
        {
            using var db = Db();
            var r = db.FluentQuery().From<RawSystem>(out var s)
                .Where(s, x => x.Id == 1)
                .Select(() => new { N = FluentSql.Raw<string>("upper({0})", s.Row.Name) })
                .Union(q => q.From<RawSystem>(out var s2).Where(s2, x => x.Id == 4)
                             .Select(() => new { N = FluentSql.Raw<string>("upper({0})", s2.Row.Name) }))
                .Fetch();
            Assert.That(r.Select(x => x.N).OrderBy(x => x), Is.EqualTo(new[] { "A", "D" }));
        }

        [Test] public void ScalarWithTakeInside()
        {
            using var db = Db();
            var q = db.FluentQuery().From<RawSite>(out var site);
            var first = q.Subquery().From<RawSystem>(out var s)
                .Where(() => s.Row.SiteId == site.Row.Id)
                .OrderBy(s, x => x.Id).Take(1)
                .SelectScalar(s, x => x.Name);
            var r = q.OrderBy(site, x => x.Id).Select(() => new { site.Row.Name, First = FluentSql.Scalar<string>(first) }).Fetch();
            Assert.That(r.Select(x => x.First), Is.EqualTo(new[] { "a", "d" }));
        }

        [Test] public void RawWithNullCapturedValue()
        {
            using var db = Db();
            string nothing = null;
            var r = db.FluentQuery().From<RawSystem>(out var s).Where(s, x => x.Id == 1)
                .Select(() => new { V = FluentSql.Raw<string>("coalesce({0}, {1})", nothing, s.Row.Name) }).Fetch();
            Assert.That(r.Single().V, Is.EqualTo("a"));
        }

        [Test] public void DeepNestedProjection()
        {
            using var db = Db();
            var r = db.FluentQuery().From<RawSystem>(out var s).Where(s, x => x.Id == 1)
                .Select(() => new L1 { Name = s.Row.Name, Inner = new L2 { Id = s.Row.Id, Deep = new L3 { Site = s.Row.SiteId } } })
                .Fetch();
            Assert.That(r.Single().Inner.Deep.Site, Is.EqualTo(1));
        }

        [Test] public void SameEntityProjectedTwice()
        {
            using var db = Db();
            var r = db.FluentQuery().From<RawSystem>(out var s).Where(s, x => x.Id == 1)
                .Select(() => new { First = s.Row, Second = s.Row }).Fetch();
            Assert.That(r.Single().First.Name, Is.EqualTo("a"));
            Assert.That(r.Single().Second.Name, Is.EqualTo("a"));
        }

        [Test] public void ScalarRejectsAMultiColumnSubquery()
        {
            using var db = Db();
            var q = db.FluentQuery().From<RawSite>(out var site);
            var two = q.Subquery().From<RawSystem>(out var s)
                .Where(() => s.Row.SiteId == site.Row.Id)
                .Select(() => new { s.Row.Id, s.Row.Name });
            var query = q.Select(() => new { X = FluentSql.Scalar<int>(two) });

            var exception = Assert.Throws<InvalidOperationException>(() => query.ToSql());
            Assert.That(exception.Message, Does.Contain("FluentSql.Scalar"));
            Assert.That(exception.Message, Does.Contain("projects 2"));
        }

        [Test] public void ScalarRejectsAWholeEntitySubquery()
        {
            using var db = Db();
            var q = db.FluentQuery().From<RawSite>(out var site);
            var whole = q.Subquery().From<RawSystem>(out var s)
                .Where(() => s.Row.SiteId == site.Row.Id)
                .Select(s);
            var query = q.Select(() => new { X = FluentSql.Scalar<int>(whole) });

            var exception = Assert.Throws<InvalidOperationException>(() => query.ToSql());
            Assert.That(exception.Message, Does.Contain("projects 4"));
        }

        [Test] public void InRejectsAMultiColumnSubquery()
        {
            using var db = Db();
            var two = db.FluentQuery().From<RawSystem>(out var s)
                .Select(() => new { s.Row.Id, s.Row.Name });
            var query = db.FluentQuery().From<RawSite>(out var site)
                .Where(() => site.Row.Id.In(two))
                .Select(() => new { site.Row.Name });

            var exception = Assert.Throws<InvalidOperationException>(() => query.ToSql());
            Assert.That(exception.Message, Does.Contain("FluentSql.In"));
        }

        [Test] public void ExistsAcceptsAMultiColumnSubquery()
        {
            using var db = Db();
            var q = db.FluentQuery().From<RawSite>(out var site);
            var two = q.Subquery().From<RawSystem>(out var s)
                .Where(() => s.Row.SiteId == site.Row.Id)
                .Select(() => new { s.Row.Id, s.Row.Name });

            // EXISTS does not care how many columns the subquery projects.
            var rows = q.Where(() => FluentSql.Exists(two)).Select(() => new { site.Row.Name }).Fetch();
            Assert.That(rows.Count, Is.EqualTo(2));
        }

        [Test] public void RawUsedForOrderingWithSkipTake()
        {
            using var db = Db();
            var r = db.FluentQuery().From<RawSystem>(out var s)
                .OrderBy(s, x => x.Id).Skip(1).Take(2)
                .Select(() => new { s.Row.Name }).Fetch();
            Assert.That(r.Select(x => x.Name), Is.EqualTo(new[] { "b", "c" }));
        }


        [Test] public void ParameterOrderAcrossSelectJoinWhereHaving()
        {
            using var db = Db();
            string sp = "-", pre = "x"; int minId = 0; int minCount = 1;
            var rows = db.FluentQuery().From<RawSite>(out var site)
                .InnerJoin<RawSystem>(out var sys, x => x.SiteId == site.Row.Id)
                .Where(() => FluentSql.Raw<bool>("{0} > {1}", sys.Row.Id, minId))
                .GroupBy(site, x => x.Id)
                .Having(() => FluentSql.Raw<bool>("count(*) > {0}", minCount))
                .Select(() => new { Label = FluentSql.Raw<string>("({0} || {1} || {2})", pre, sp, site.Row.Name) })
                .Fetch();
            Assert.That(rows.Single().Label, Is.EqualTo("x-north"));
        }

        [Test] public void CapturedStringWithQuotesBecomesParameterNotText()
        {
            using var db = Db();
            var nasty = "'); drop table rawsites; --";
            var sql = db.FluentQuery().From<RawSite>(out var site)
                .Where(() => FluentSql.Raw<bool>("{0} = {1}", site.Row.Name, nasty))
                .Select(() => new { site.Row.Id }).ToSql();
            Assert.That(sql.SQL, Does.Not.Contain("drop table"));
            Assert.That(sql.Arguments, Is.EqualTo(new object[] { nasty }));
        }

        [Test] public async Task AsyncFetchWithRawAndScalar()
        {
            using var db = Db();
            var q = db.FluentQuery().From<RawSite>(out var site);
            var n = q.Subquery().From<RawSystem>(out var s)
                .Where(() => s.Row.SiteId == site.Row.Id).SelectScalar(s, x => FluentSql.Count());
            var rows = await q.OrderBy(site, x => x.Id)
                .Select(() => new { Up = FluentSql.Raw<string>("upper({0})", site.Row.Name), N = FluentSql.Scalar<int>(n) })
                .FetchAsync();
            Assert.That(rows.Select(x => x.Up), Is.EqualTo(new[] { "NORTH", "SOUTH" }));
            Assert.That(rows.Select(x => x.N), Is.EqualTo(new[] { 3, 1 }));
        }

        [Test] public void SingleAndFirstWithRaw()
        {
            using var db = Db();
            var one = db.FluentQuery().From<RawSite>(out var site).Where(site, x => x.Id == 1)
                .Select(() => new { Up = FluentSql.Raw<string>("upper({0})", site.Row.Name) }).Single();
            Assert.That(one.Up, Is.EqualTo("NORTH"));
        }

        [Test] public void ToSqlIsIdempotent()
        {
            using var db = Db();
            var q = db.FluentQuery().From<RawSite>(out var site);
            var n = q.Subquery().From<RawSystem>(out var s)
                .Where(() => s.Row.SiteId == site.Row.Id).SelectScalar(s, x => FluentSql.Count());
            var pre = "p";
            var built = q.Where(site, x => x.Id > 0)
                .Select(() => new { L = FluentSql.Raw<string>("({0} || {1})", pre, site.Row.Name), N = FluentSql.Scalar<int>(n) });

            var a = built.ToSql();
            var b = built.ToSql();
            Assert.That(b.SQL, Is.EqualTo(a.SQL));
            Assert.That(b.Arguments, Is.EqualTo(a.Arguments));
        }

        [Test] public void FetchTwiceGivesSameResult()
        {
            using var db = Db();
            var q = db.FluentQuery().From<RawSite>(out var site).OrderBy(site, x => x.Id)
                .Select(() => new { Up = FluentSql.Raw<string>("upper({0})", site.Row.Name) });
            var a = q.Fetch();
            var b = q.Fetch();
            Assert.That(b.Select(x => x.Up), Is.EqualTo(a.Select(x => x.Up)));
        }

        [Test] public void RawInJoinOnCondition()
        {
            using var db = Db();
            var rows = db.FluentQuery().From<RawSite>(out var site)
                .InnerJoin<RawSystem>(out var sys, x => FluentSql.Raw<bool>("{0} = {1} and {2} = 1", x.SiteId, site.Row.Id, x.Active))
                .OrderBy(sys, x => x.Id)
                .Select(() => new { sys.Row.Name }).Fetch();
            Assert.That(rows.Select(x => x.Name), Is.EqualTo(new[] { "a", "b", "d" }));
        }

        [Test] public void ScalarInJoinOnCondition()
        {
            using var db = Db();
            var q = db.FluentQuery().From<RawSite>(out var site);
            var maxId = q.Subquery().From<RawSystem>(out var m)
                .Where(() => m.Row.SiteId == site.Row.Id).SelectScalar(m, x => FluentSql.Max(x.Id));
            var rows = q.InnerJoin<RawSystem>(out var sys, x => x.Id == FluentSql.Scalar<int>(maxId))
                .OrderBy(sys, x => x.Id)
                .Select(() => new { site.Row.Name, sys.Row.Id }).Fetch();
            Assert.That(rows.Select(x => x.Id), Is.EqualTo(new[] { 3, 4 }));
        }

        [Test] public void WhereIfFalseDoesNotEmitRaw()
        {
            using var db = Db();
            var sql = db.FluentQuery().From<RawSite>(out var site)
                .WhereIf(false, () => FluentSql.Raw<bool>("this is not valid sql"))
                .Select(() => new { site.Row.Id }).ToSql().SQL;
            Assert.That(sql, Does.Not.Contain("not valid"));
        }

        [Test] public void CaseDifferingProjectionAliases()
        {
            using var db = Db();
            var rows = db.FluentQuery().From<RawSite>(out var site).Where(site, x => x.Id == 1)
                .Select(() => new { N = site.Row.Id, n = site.Row.Name })
                .Fetch();
            TestContext.WriteLine("N=" + rows.Single().N + " n=" + rows.Single().n);
            Assert.That(rows.Single().N, Is.EqualTo(1));
            Assert.That(rows.Single().n, Is.EqualTo("north"));
        }

        [Test] public void ScalarOverAProjectedSingleMemberSubquery()
        {
            using var db = Db();
            var q = db.FluentQuery().From<RawSite>(out var site);
            var one = q.Subquery().From<RawSystem>(out var s)
                .Where(() => s.Row.SiteId == site.Row.Id).OrderBy(s, x => x.Id).Take(1)
                .Select(() => new { s.Row.Name });
            var rows = q.OrderBy(site, x => x.Id).Select(() => new { First = FluentSql.Scalar<string>(one) }).Fetch();
            Assert.That(rows.Select(x => x.First), Is.EqualTo(new[] { "a", "d" }));
        }

        [Test] public void RawProducingEnum()
        {
            using var db = Db();
            var rows = db.FluentQuery().From<RawSystem>(out var s).Where(s, x => x.Id == 1)
                .Select(() => new { State = FluentSql.Raw<Flag>("case when {0} then 1 else 0 end", s.Row.Active) }).Fetch();
            Assert.That(rows.Single().State, Is.EqualTo(Flag.On));
        }

        [Test] public void RawWithSkipTakePaging()
        {
            using var db = Db();
            var rows = db.FluentQuery().From<RawSystem>(out var s)
                .Where(() => FluentSql.Raw<bool>("{0} > 0", s.Row.Id))
                .OrderBy(s, x => x.Id).Skip(1).Take(2)
                .Select(() => new { Up = FluentSql.Raw<string>("upper({0})", s.Row.Name) }).Fetch();
            Assert.That(rows.Select(x => x.Up), Is.EqualTo(new[] { "B", "C" }));
        }

        [Test] public void DistinctWithRaw()
        {
            using var db = Db();
            var rows = db.FluentQuery().From<RawSystem>(out var s).Distinct()
                .Select(() => new { Site = FluentSql.Raw<int>("{0}", s.Row.SiteId) }).Fetch();
            Assert.That(rows.Count, Is.EqualTo(2));
        }

        [Test] public void ProjectsIntoAShapeNPocoCannotModel()
        {
            using var db = Db();
            // Members differing only by case have no valid PocoData, and need none: the row mapper
            // owns the result type, so nothing tries to build one.
            var rows = db.FluentQuery().From<RawSite>(out var site).Where(site, x => x.Id == 1)
                .Select(() => new { N = site.Row.Id, n = site.Row.Name })
                .Fetch();
            Assert.That(rows.Single().N, Is.EqualTo(1));
            Assert.That(rows.Single().n, Is.EqualTo("north"));
        }

        [Test] public void ConstructorParameterStillMatchesItsMemberCaseInsensitively()
        {
            using var db = Db();
            var rows = db.FluentQuery().From<RawSite>(out var site).OrderBy(site, x => x.Id)
                .Select(() => new Named(site.Row.Id, site.Row.Name))
                .Fetch();
            Assert.That(rows.Select(x => x.Name), Is.EqualTo(new[] { "north", "south" }));
            Assert.That(rows.Select(x => x.Id), Is.EqualTo(new[] { 1, 2 }));
        }

        public class Named
        {
            public Named(int id, string name) { Id = id; Name = name; }
            public int Id { get; }
            public string Name { get; }
        }

        public enum Flag { Off = 0, On = 1 }

        public class L1 { public string Name { get; set; } public L2 Inner { get; set; } }
        public class L2 { public int Id { get; set; } public L3 Deep { get; set; } }
        public class L3 { public int Site { get; set; } }

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
    }
}
