using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using NPoco.FluentSql;
using NUnit.Framework;

namespace NPoco.Tests.FluentSqlTests
{
    /// <summary>
    /// Select has two branches: a body that builds an object shape is materialized by a
    /// ProjectionPlan, and a body that is a single value skips the plan entirely and maps through
    /// NPoco's ordinary single-column path. These tests pin down which branch each body shape
    /// takes, and that both behave the same everywhere else in the builder.
    /// </summary>
    [TestFixture]
    public class FluentSqlSelectShapeTests
    {
        private string _file;
        private string _connectionString;

        [SetUp]
        public void SetUp()
        {
            _file = Path.Combine(Path.GetTempPath(), "npoco-selectshape-" + Guid.NewGuid().ToString("N") + ".db");
            _connectionString = "Data Source=" + _file;
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText =
                    "create table selsites(id integer primary key, name text);" +
                    "create table selsystems(id integer primary key, site_id integer, name text, active integer, size real," +
                    "  state integer, at datetime, code text, amount decimal, label text, mode text);" +
                    "insert into selsites values(1,'north'),(2,'south'),(3,'empty');" +
                    "insert into selsystems values" +
                    "  (1,1,'a',1,10.5,1,'2024-03-04 05:06:07','5f9d88a0-1111-4222-8333-444444444444',12.34,'On','On')," +
                    "  (2,1,'b',0,null,0,'2025-01-02 03:04:05','6f9d88a0-1111-4222-8333-444444444444',7.5,'Off','Off')," +
                    "  (3,2,'c',1,4.0,1,'2026-05-06 07:08:09','7f9d88a0-1111-4222-8333-444444444444',0.25,null,'On');";
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

        // ---------- single-value bodies: no projection plan ----------

        [Test] public void ScalarColumnBodyFetchesTypedValuesAndEmitsNoAlias()
        {
            using var db = Db();
            var query = db.FluentQuery().From<SelSystem>(out var s)
                .OrderBy(() => s.Row.Id)
                .Select(() => s.Row.Name);

            Assert.That(query.ToSql().SQL, Does.Not.Contain(" AS "));
            Assert.That(query.Fetch(), Is.EqualTo(new[] { "a", "b", "c" }));
        }

        [Test] public void ScalarBodyMapsValueTypesAndNulls()
        {
            using var db = Db();
            var flags = db.FluentQuery().From<SelSystem>(out var s)
                .OrderBy(() => s.Row.Id)
                .Select(() => s.Row.Active).Fetch();
            Assert.That(flags, Is.EqualTo(new[] { true, false, true }));

            var sizes = db.FluentQuery().From<SelSystem>(out var s2)
                .OrderBy(() => s2.Row.Id)
                .Select(() => s2.Row.Size).Fetch();
            Assert.That(sizes, Is.EqualTo(new double?[] { 10.5, null, 4.0 }));
        }

        [Test] public void ScalarBodyFromALeftJoinYieldsNullForMissingRows()
        {
            using var db = Db();
            var names = db.FluentQuery().From<SelSite>(out var site)
                .LeftJoin<SelSystem>(out var sys, x => x.SiteId == site.Row.Id)
                .OrderBy(() => site.Row.Id)
                .ThenBy(() => sys.Row.Id)
                .Select(() => sys.Row.Name).Fetch();
            Assert.That(names, Is.EqualTo(new[] { "a", "b", "c", null }));
        }

        [Test] public void ScalarBodySupportsExpressionsSpanningTables()
        {
            using var db = Db();
            var labels = db.FluentQuery().From<SelSite>(out var site)
                .InnerJoin<SelSystem>(out var s, x => x.SiteId == site.Row.Id)
                .OrderBy(() => s.Row.Id)
                .Select(() => site.Row.Name + "/" + s.Row.Name).Fetch();
            Assert.That(labels, Is.EqualTo(new[] { "north/a", "north/b", "south/c" }));

            var scaled = db.FluentQuery().From<SelSite>(out var site2)
                .InnerJoin<SelSystem>(out var s2, x => x.SiteId == site2.Row.Id)
                .OrderBy(() => s2.Row.Id)
                .Select(() => s2.Row.Id * 10 + site2.Row.Id).Fetch();
            Assert.That(scaled, Is.EqualTo(new[] { 11, 21, 32 }));
        }

        [Test] public void ScalarBodySupportsConditionals()
        {
            using var db = Db();
            var states = db.FluentQuery().From<SelSystem>(out var s)
                .OrderBy(() => s.Row.Id)
                .Select(() => s.Row.Active ? "on" : "off").Fetch();
            Assert.That(states, Is.EqualTo(new[] { "on", "off", "on" }));
        }

        [Test] public void ScalarBodySupportsAggregates()
        {
            using var db = Db();
            var count = db.FluentQuery().From<SelSystem>(out var s)
                .Select(() => FSql.Count()).Single();
            Assert.That(count, Is.EqualTo(3));

            var viaParameter = db.FluentQuery().From<SelSystem>(out var s2)
                .Select(x => x.Count()).Single();
            Assert.That(viaParameter, Is.EqualTo(3));

            var total = db.FluentQuery().From<SelSystem>(out var s3)
                .Select(() => FSql.Sum(s3.Row.Size)).Single();
            Assert.That(total, Is.EqualTo(14.5));

            var perSite = db.FluentQuery().From<SelSystem>(out var s4)
                .GroupBy(() => s4.Row.SiteId)
                .OrderBy(() => s4.Row.SiteId)
                .Select(() => FSql.Count()).Fetch();
            Assert.That(perSite, Is.EqualTo(new[] { 2, 1 }));
        }

        [Test] public void ScalarBodySupportsRawAndCapturedValues()
        {
            using var db = Db();
            var upper = db.FluentQuery().From<SelSystem>(out var s)
                .OrderBy(() => s.Row.Id)
                .Select(() => FSql.Raw<string>("upper({0})", s.Row.Name)).Fetch();
            Assert.That(upper, Is.EqualTo(new[] { "A", "B", "C" }));

            var factor = 3;
            var scaled = db.FluentQuery().From<SelSystem>(out var s2)
                .OrderBy(() => s2.Row.Id)
                .Select(() => s2.Row.Id * factor);
            Assert.That(scaled.ToSql().Arguments, Has.Length.EqualTo(1));
            Assert.That(scaled.Fetch(), Is.EqualTo(new[] { 3, 6, 9 }));
        }

        [Test] public void ScalarBodyWorksWithDistinctOrderingAndPaging()
        {
            using var db = Db();
            var sites = db.FluentQuery().From<SelSystem>(out var s).Distinct()
                .Select(() => s.Row.SiteId).Fetch();
            Assert.That(sites.OrderBy(x => x), Is.EqualTo(new[] { 1, 2 }));

            var paged = db.FluentQuery().From<SelSystem>(out var s2)
                .OrderBy(() => s2.Row.Id).Skip(1).Take(1)
                .Select(() => s2.Row.Name).Fetch();
            Assert.That(paged, Is.EqualTo(new[] { "b" }));
        }

        [Test] public void ScalarBodyWorksWithUnion()
        {
            using var db = Db();
            var names = db.FluentQuery().From<SelSystem>(out var s)
                .Where(() => s.Row.Id == 1)
                .Select(() => s.Row.Name)
                .UnionAll(q => q.From<SelSystem>(out var s2)
                                .Where(() => s2.Row.Id == 3)
                                .Select(() => s2.Row.Name))
                .Fetch();
            Assert.That(names.OrderBy(x => x), Is.EqualTo(new[] { "a", "c" }));
        }

        [Test] public async Task ScalarBodyWorksThroughEveryExecutionPath()
        {
            using var db = Db();
            FluentSqlResult<string> Names() => db.FluentQuery().From<SelSystem>(out var s)
                .OrderBy(() => s.Row.Id)
                .Select(() => s.Row.Name);

            Assert.That(Names().Fetch(), Is.EqualTo(new[] { "a", "b", "c" }));
            Assert.That(await Names().FetchAsync(), Is.EqualTo(new[] { "a", "b", "c" }));
            Assert.That(Names().Query().ToList(), Is.EqualTo(new[] { "a", "b", "c" }));
            Assert.That(Names().First(), Is.EqualTo("a"));
            Assert.That(await Names().FirstAsync(), Is.EqualTo("a"));

            var streamed = new List<string>();
            await foreach (var name in Names().QueryAsync()) streamed.Add(name);
            Assert.That(streamed, Is.EqualTo(new[] { "a", "b", "c" }));

            var single = db.FluentQuery().From<SelSystem>(out var s2)
                .Where(() => s2.Row.Id == 2)
                .Select(() => s2.Row.Name).Single();
            Assert.That(single, Is.EqualTo("b"));
        }

        [Test] public async Task OrDefaultFormsComeBackEmptyRatherThanThrowing()
        {
            using var db = Db();

            // A single-value body takes no plan and maps through NPoco's own path.
            FluentSqlResult<string> NoName() => db.FluentQuery().From<SelSystem>(out var s)
                .Where(() => s.Row.Id == 99)
                .Select(() => s.Row.Name);

            Assert.That(NoName().SingleOrDefault(), Is.Null);
            Assert.That(NoName().FirstOrDefault(), Is.Null);
            Assert.That(await NoName().SingleOrDefaultAsync(), Is.Null);
            Assert.That(await NoName().FirstOrDefaultAsync(), Is.Null);

            // An object shape is materialized by the projection plan instead, so it is the other
            // branch of every one of these.
            FluentSqlResult<SelPair> NoPair() => db.FluentQuery().From<SelSystem>(out var s)
                .Where(() => s.Row.Id == 99)
                .Select(() => new SelPair(s.Row.Id, s.Row.Name));

            Assert.That(NoPair().SingleOrDefault(), Is.Null);
            Assert.That(NoPair().FirstOrDefault(), Is.Null);
            Assert.That(await NoPair().SingleOrDefaultAsync(), Is.Null);
            Assert.That(await NoPair().FirstOrDefaultAsync(), Is.Null);

            FluentSqlResult<SelPair> One() => db.FluentQuery().From<SelSystem>(out var s)
                .Where(() => s.Row.Id == 2)
                .Select(() => new SelPair(s.Row.Id, s.Row.Name));

            Assert.That(One().SingleOrDefault().Name, Is.EqualTo("b"));
            Assert.That(One().FirstOrDefault().Name, Is.EqualTo("b"));
            Assert.That((await One().SingleOrDefaultAsync()).Name, Is.EqualTo("b"));
            Assert.That((await One().FirstOrDefaultAsync()).Name, Is.EqualTo("b"));

            // Only the empty case is forgiven: more than one row is still a mistake.
            var many = db.FluentQuery().From<SelSystem>(out var all).Select(() => all.Row.Name);
            Assert.Throws<InvalidOperationException>(() => many.SingleOrDefault());
        }

        [Test] public async Task AsyncDatabaseEntryPointExposesAndExecutesOnlyAsyncOperations()
        {
            using var db = Db();
            IAsyncQueryDatabase asyncDb = db;

            FluentSqlAsyncResult<string> names = asyncDb.FluentQuery()
                .From<SelSystem>(out var s)
                .OrderBy(() => s.Row.Id)
                .Select(() => s.Row.Name);

            Assert.That(await names.FetchAsync(), Is.EqualTo(new[] { "a", "b", "c" }));
            Assert.That(await names.FirstAsync(), Is.EqualTo("a"));
            Assert.That(await names.FirstOrDefaultAsync(), Is.EqualTo("a"));

            var missing = asyncDb.FluentQuery()
                .From<SelSystem>(out var absent)
                .Where(() => absent.Row.Id == 99)
                .Select(() => absent.Row.Name);
            Assert.That(await missing.SingleOrDefaultAsync(), Is.Null);
            Assert.That(await missing.FirstOrDefaultAsync(), Is.Null);

            var one = asyncDb.FluentQuery()
                .From<SelSystem>(out var s2)
                .Where(() => s2.Row.Id == 2)
                .Select(() => new SelPair(s2.Row.Id, s2.Row.Name));
            var value = await one.SingleAsync();
            Assert.That(value.Id, Is.EqualTo(2));
            Assert.That(value.Name, Is.EqualTo("b"));

            var unioned = asyncDb.FluentQuery()
                .From<SelSystem>(out var first)
                .Where(() => first.Row.Id == 1)
                .Select(() => first.Row.Name)
                .UnionAll(q => q.From<SelSystem>(out var last)
                    .Where(() => last.Row.Id == 3)
                    .Select(() => last.Row.Name));
            Assert.That((await unioned.FetchAsync()).OrderBy(x => x), Is.EqualTo(new[] { "a", "c" }));

            Assert.That(typeof(FluentSqlAsyncResult<string>).GetMethod("Fetch"), Is.Null);
            Assert.That(typeof(FluentSqlAsyncResult<string>).GetMethod("Query"), Is.Null);
            Assert.That(typeof(FluentSqlAsyncResult<string>).GetMethod("Single"), Is.Null);
            Assert.That(typeof(FluentSqlAsyncResult<string>).GetMethod("First"), Is.Null);
            Assert.That(typeof(FluentSqlAsyncResult<string>).GetMethod("SingleOrDefault"), Is.Null);
            Assert.That(typeof(FluentSqlAsyncResult<string>).GetMethod("FirstOrDefault"), Is.Null);
        }

        [Test] public void ScalarBodyWorksAsASubqueryOperand()
        {
            using var db = Db();

            var active = db.FluentQuery().From<SelSystem>(out var s)
                .Where(() => s.Row.Active)
                .Select(() => s.Row.SiteId);
            var byIn = db.FluentQuery().From<SelSite>(out var site)
                .Where(() => site.Row.Id.In(active))
                .OrderBy(() => site.Row.Id)
                .Select(() => site.Row.Name).Fetch();
            Assert.That(byIn, Is.EqualTo(new[] { "north", "south" }));

            var outer = db.FluentQuery().From<SelSite>(out var site2);
            var owned = outer.Subquery().From<SelSystem>(out var s2)
                .Where(() => s2.Row.SiteId == site2.Row.Id)
                .Select(() => s2.Row.Id);
            var counted = outer.Subquery().From<SelSystem>(out var s3)
                .Where(() => s3.Row.SiteId == site2.Row.Id)
                .Select(() => FSql.Count());
            var rows = outer.Where(() => FSql.Exists(owned))
                .OrderBy(() => site2.Row.Id)
                .Select(() => new { site2.Row.Name, N = FSql.Scalar<int>(counted) }).Fetch();
            Assert.That(rows.Select(x => x.Name), Is.EqualTo(new[] { "north", "south" }));
            Assert.That(rows.Select(x => x.N), Is.EqualTo(new[] { 2, 1 }));
        }

        [Test] public void ScalarBodyConvertsTheSameTypesTheProjectionPlanDoes()
        {
            // The branches convert differently - the plan builds a converter from the PocoColumn,
            // the scalar path uses NPoco's own single-column mapping - so each conversion is
            // checked against the plan's answer as well as against a literal.
            using var db = Db();

            var states = db.FluentQuery().From<SelSystem>(out var a).OrderBy(() => a.Row.Id)
                .Select(() => a.Row.State).Fetch();
            var plannedStates = db.FluentQuery().From<SelSystem>(out var b).OrderBy(() => b.Row.Id)
                .Select(() => new { V = b.Row.State }).Fetch().Select(x => x.V);
            Assert.That(states, Is.EqualTo(new[] { SelState.On, SelState.Off, SelState.On }));
            Assert.That(states, Is.EqualTo(plannedStates));

            var dates = db.FluentQuery().From<SelSystem>(out var c).OrderBy(() => c.Row.Id)
                .Select(() => c.Row.At).Fetch();
            var plannedDates = db.FluentQuery().From<SelSystem>(out var d).OrderBy(() => d.Row.Id)
                .Select(() => new { V = d.Row.At }).Fetch().Select(x => x.V);
            Assert.That(dates[0], Is.EqualTo(new DateTime(2024, 3, 4, 5, 6, 7)));
            Assert.That(dates, Is.EqualTo(plannedDates));

            var codes = db.FluentQuery().From<SelSystem>(out var e).OrderBy(() => e.Row.Id)
                .Select(() => e.Row.Code).Fetch();
            var plannedCodes = db.FluentQuery().From<SelSystem>(out var f).OrderBy(() => f.Row.Id)
                .Select(() => new { V = f.Row.Code }).Fetch().Select(x => x.V);
            Assert.That(codes[0], Is.EqualTo(Guid.Parse("5f9d88a0-1111-4222-8333-444444444444")));
            Assert.That(codes, Is.EqualTo(plannedCodes));

            var amounts = db.FluentQuery().From<SelSystem>(out var g).OrderBy(() => g.Row.Id)
                .Select(() => g.Row.Amount).Fetch();
            var plannedAmounts = db.FluentQuery().From<SelSystem>(out var h).OrderBy(() => h.Row.Id)
                .Select(() => new { V = h.Row.Amount }).Fetch().Select(x => x.V);
            Assert.That(amounts, Is.EqualTo(new[] { 12.34m, 7.5m, 0.25m }));
            Assert.That(amounts, Is.EqualTo(plannedAmounts));

            // A computed body has no PocoColumn behind it on either branch.
            var computed = db.FluentQuery().From<SelSystem>(out var i).OrderBy(() => i.Row.Id)
                .Select(() => i.Row.Amount * 2).Fetch();
            Assert.That(computed, Is.EqualTo(new[] { 24.68m, 15m, 0.5m }));
        }

        [Test] public void ScalarBodyReadsAnEnumStoredAsAString()
        {
            using var db = Db();

            var labels = db.FluentQuery().From<SelSystem>(out var a).OrderBy(() => a.Row.Id)
                .Select(() => a.Row.Label).Fetch();
            var plannedLabels = db.FluentQuery().From<SelSystem>(out var b).OrderBy(() => b.Row.Id)
                .Select(() => new { V = b.Row.Label }).Fetch().Select(x => x.V);
            var entityLabels = db.FluentQuery().From<SelSystem>(out var c).OrderBy(() => c.Row.Id)
                .Select(c).Fetch().Select(x => x.Label);

            Assert.That(labels, Is.EqualTo(new SelState?[] { SelState.On, SelState.Off, null }));
            Assert.That(labels, Is.EqualTo(plannedLabels));
            Assert.That(labels, Is.EqualTo(entityLabels));

            var modes = db.FluentQuery().From<SelSystem>(out var e).OrderBy(() => e.Row.Id)
                .Select(() => e.Row.Mode).Fetch();
            var plannedModes = db.FluentQuery().From<SelSystem>(out var f).OrderBy(() => f.Row.Id)
                .Select(() => new { V = f.Row.Mode }).Fetch().Select(x => x.V);
            Assert.That(modes, Is.EqualTo(new[] { SelState.On, SelState.Off, SelState.On }));
            Assert.That(modes, Is.EqualTo(plannedModes));

            // The stored text is what a predicate has to compare against, not the ordinal.
            var matched = db.FluentQuery().From<SelSystem>(out var d)
                .Where(() => d.Row.Label == SelState.Off)
                .Select(() => d.Row.Name).Fetch();
            Assert.That(matched, Is.EqualTo(new[] { "b" }));
        }

        /// <summary>
        /// The compiler erases a non-nullable enum to its underlying integer inside an expression
        /// tree, so a predicate against one arrives as a plain int. It still has to be written as
        /// the name the column stores - comparing against the ordinal matches nothing at all, and
        /// says nothing about why.
        /// </summary>
        [Test] public void PredicatesCompareAStringEnumByNameNotOrdinal()
        {
            using var db = Db();

            var sql = db.FluentQuery().From<SelSystem>(out var a)
                .Where(() => a.Row.Mode == SelState.On)
                .Select(() => a.Row.Name).ToSql();
            Assert.That(sql.Arguments.Single(), Is.EqualTo("On"));

            var matched = db.FluentQuery().From<SelSystem>(out var b)
                .Where(() => b.Row.Mode == SelState.On)
                .OrderBy(() => b.Row.Id)
                .Select(() => b.Row.Name).Fetch();
            Assert.That(matched, Is.EqualTo(new[] { "a", "c" }));

            var negated = db.FluentQuery().From<SelSystem>(out var c)
                .Where(() => c.Row.Mode != SelState.On)
                .Select(() => c.Row.Name).Fetch();
            Assert.That(negated, Is.EqualTo(new[] { "b" }));

            // The same column read through a collection, where the enum type survives the tree.
            var states = new[] { SelState.On };
            var inList = db.FluentQuery().From<SelSystem>(out var d)
                .Where(() => d.Row.Mode.In(states))
                .OrderBy(() => d.Row.Id)
                .Select(() => d.Row.Name).Fetch();
            Assert.That(inList, Is.EqualTo(new[] { "a", "c" }));

            // An int-backed enum column keeps comparing by ordinal.
            var byOrdinal = db.FluentQuery().From<SelSystem>(out var e)
                .Where(() => e.Row.State == SelState.On)
                .OrderBy(() => e.Row.Id)
                .Select(() => e.Row.Name).Fetch();
            Assert.That(byOrdinal, Is.EqualTo(new[] { "a", "c" }));
        }

        [Test] public void AggregatesWorkInProjectionsHavingAndSubqueries()
        {
            using var db = Db();

            var grouped = db.FluentQuery().From<SelSystem>(out var s)
                .GroupBy(() => s.Row.SiteId)
                .OrderBy(() => s.Row.SiteId)
                .Select(f => new { Site = s.Row.SiteId, Total = f.Count(), Biggest = f.Max(s.Row.Size) })
                .Fetch();
            Assert.That(grouped.Select(x => x.Site), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(grouped.Select(x => x.Total), Is.EqualTo(new[] { 2, 1 }));
            Assert.That(grouped.Select(x => x.Biggest), Is.EqualTo(new double?[] { 10.5, 4.0 }));

            var busy = db.FluentQuery().From<SelSystem>(out var h)
                .GroupBy(() => h.Row.SiteId)
                .Having(() => FSql.Count() > 1)
                .Select(() => h.Row.SiteId).Fetch();
            Assert.That(busy, Is.EqualTo(new[] { 1 }));

            // COUNT(*) counts rows, COUNT(column) skips nulls.
            var rows = db.FluentQuery().From<SelSystem>(out var r).Select(() => FSql.Count()).Single();
            var sized = db.FluentQuery().From<SelSystem>(out var z).Select(() => FSql.Count(z.Row.Size)).Single();
            var sites = db.FluentQuery().From<SelSystem>(out var y).Select(() => FSql.CountDistinct(y.Row.SiteId)).Single();
            Assert.That(rows, Is.EqualTo(3));
            Assert.That(sized, Is.EqualTo(2));
            Assert.That(sites, Is.EqualTo(2));

            // A correlated COUNT(*), including the site with no systems at all.
            var query = db.FluentQuery().From<SelSite>(out var site);
            var perSite = query.Subquery().From<SelSystem>(out var sys)
                .Where(() => sys.Row.SiteId == site.Row.Id)
                .SelectScalar(() => FSql.Count());

            var counts = query.OrderBy(() => site.Row.Id)
                .Select(() => new { site.Row.Name, Total = FSql.Scalar<int>(perSite) })
                .Fetch();
            Assert.That(counts.Select(x => x.Name), Is.EqualTo(new[] { "north", "south", "empty" }));
            Assert.That(counts.Select(x => x.Total), Is.EqualTo(new[] { 2, 1, 0 }));
        }

        [Test] public void AggregatesOverAnEmptySetAgreeAcrossBothBranches()
        {
            using var db = Db();

            var count = db.FluentQuery().From<SelSystem>(out var s)
                .Where(() => s.Row.Id == 99)
                .Select(() => FSql.Count()).Single();
            var plannedCount = db.FluentQuery().From<SelSystem>(out var s2)
                .Where(() => s2.Row.Id == 99)
                .Select(() => new { V = FSql.Count() }).Single().V;
            Assert.That(count, Is.EqualTo(0));
            Assert.That(plannedCount, Is.EqualTo(0));

            // SUM of no rows is SQL NULL, so the result type has to be nullable on the scalar
            // path: it maps through NPoco's ordinary mapping, which has no default to fall back
            // on the way the projection plan does.
            var sum = db.FluentQuery().From<SelSystem>(out var s3)
                .Where(() => s3.Row.Id == 99)
                .Select(() => FSql.Sum(s3.Row.Size)).Single();
            var plannedSum = db.FluentQuery().From<SelSystem>(out var s4)
                .Where(() => s4.Row.Id == 99)
                .Select(() => new { V = FSql.Sum(s4.Row.Size) }).Single().V;
            Assert.That(sum, Is.Null);
            Assert.That(plannedSum, Is.Null);

            var populated = db.FluentQuery().From<SelSystem>(out var s5)
                .Select(() => FSql.Sum(s5.Row.Size)).Single();
            Assert.That(populated, Is.EqualTo(14.5));
        }

        // ---------- object bodies: still materialized by the projection plan ----------

        [Test] public void SingleMemberAnonymousBodyStillUsesThePlan()
        {
            using var db = Db();
            var query = db.FluentQuery().From<SelSystem>(out var s)
                .OrderBy(() => s.Row.Id)
                .Select(() => new { s.Row.Name });

            Assert.That(query.ToSql().SQL, Does.Contain(" AS "));
            Assert.That(query.Fetch().Select(x => x.Name), Is.EqualTo(new[] { "a", "b", "c" }));
        }

        [Test] public void EntityBodyStillMaterializesTheWholeRow()
        {
            using var db = Db();
            var rows = db.FluentQuery().From<SelSystem>(out var s)
                .OrderBy(() => s.Row.Id)
                .Select(() => s.Row).Fetch();
            Assert.That(rows.Select(x => x.Name), Is.EqualTo(new[] { "a", "b", "c" }));
            Assert.That(rows[0].Active, Is.True);
            Assert.That(rows[0].Size, Is.EqualTo(10.5));
            Assert.That(rows[1].Size, Is.Null);
        }

        [Test] public void ConvertWrappedEntityBodyStillMaterializesTheRow()
        {
            using var db = Db();
            var rows = db.FluentQuery().From<SelSystem>(out var s)
                .Where(() => s.Row.Id == 1)
                .Select(() => (object)s.Row).Fetch();
            Assert.That(((SelSystem)rows.Single()).Name, Is.EqualTo("a"));
        }

        [Test] public void MemberInitAndConstructorBodiesStillUseThePlan()
        {
            using var db = Db();
            var initialized = db.FluentQuery().From<SelSystem>(out var s)
                .Where(() => s.Row.Id == 1)
                .Select(() => new SelLabel { Id = s.Row.Id, Name = s.Row.Name }).Single();
            Assert.That(initialized.Id, Is.EqualTo(1));
            Assert.That(initialized.Name, Is.EqualTo("a"));

            var constructed = db.FluentQuery().From<SelSystem>(out var s2)
                .Where(() => s2.Row.Id == 2)
                .Select(() => new SelPair(s2.Row.Id, s2.Row.Name)).Single();
            Assert.That(constructed.Id, Is.EqualTo(2));
            Assert.That(constructed.Name, Is.EqualTo("b"));
        }

        [Test] public void NestedBodyStillMixesEntitiesWithScalarsAndNullsMissingJoins()
        {
            using var db = Db();
            var rows = db.FluentQuery().From<SelSite>(out var site)
                .LeftJoin<SelSystem>(out var sys, x => x.SiteId == site.Row.Id && x.Id == 1)
                .OrderBy(() => site.Row.Id)
                .Select(() => new { Site = site.Row.Name, System = sys.Row, Upper = FSql.Raw<string>("upper({0})", site.Row.Name) })
                .Fetch();
            Assert.That(rows.Select(x => x.Site), Is.EqualTo(new[] { "north", "south", "empty" }));
            Assert.That(rows[0].System.Name, Is.EqualTo("a"));
            Assert.That(rows[1].System, Is.Null);
            Assert.That(rows[2].System, Is.Null);
            Assert.That(rows.Select(x => x.Upper), Is.EqualTo(new[] { "NORTH", "SOUTH", "EMPTY" }));
        }

        [TableName("selsites")]
        public class SelSite
        {
            [Column("id")] public int Id { get; set; }
            [Column("name")] public string Name { get; set; }
        }

        [TableName("selsystems")]
        public class SelSystem
        {
            [Column("id")] public int Id { get; set; }
            [Column("site_id")] public int SiteId { get; set; }
            [Column("name")] public string Name { get; set; }
            [Column("active")] public bool Active { get; set; }
            [Column("size")] public double? Size { get; set; }
            [Column("state")] public SelState State { get; set; }
            [Column("at")] public DateTime At { get; set; }
            [Column("code")] public Guid Code { get; set; }
            [Column("amount")] public decimal Amount { get; set; }
            [Column("label")][ColumnType(typeof(string))] public SelState? Label { get; set; }
            [Column("mode")][ColumnType(typeof(string))] public SelState Mode { get; set; }
        }

        public enum SelState { Off = 0, On = 1 }

        public class SelLabel
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public class SelPair
        {
            public SelPair(int id, string name)
            {
                Id = id;
                Name = name;
            }

            public int Id { get; }
            public string Name { get; }
        }
    }
}
