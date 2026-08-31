using System;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using NPoco.FluentSql;
using NUnit.Framework;

namespace NPoco.Tests.FluentSqlTests
{
    /// <summary>
    /// Subqueries written where they are used rather than declared above the query. C# allows no
    /// out argument inside an expression tree, so the alias comes from Table&lt;T&gt; and the
    /// subquery is built inline against it - joins included. Run against SQLite, so the SQL has to
    /// work rather than merely read correctly.
    /// </summary>
    [TestFixture]
    public class FluentSqlInlineSubqueryTests
    {
        private string _file;
        private string _connectionString;

        [SetUp]
        public void SetUp()
        {
            _file = Path.Combine(Path.GetTempPath(), "npoco-inline-" + Guid.NewGuid().ToString("N") + ".db");
            _connectionString = "Data Source=" + _file;
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText =
                    "create table inlinesites(id integer primary key, name text);" +
                    "create table inlinesystems(id integer primary key, site_id integer, type_id integer, name text, active integer);" +
                    "create table inlinetypes(id integer primary key, name text, billable integer);" +
                    "insert into inlinesites values(1,'north'),(2,'south');" +
                    "insert into inlinetypes values(10,'metered',1),(20,'flat',0);" +
                    "insert into inlinesystems values(1,1,10,'a',1),(2,1,20,'b',1),(3,1,10,'c',0),(4,2,10,'d',1);";
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

        [Test]
        public void ScalarSubqueryCanBeBuiltInsideTheProjection()
        {
            using (var database = CreateDatabase())
            {
                var query = database.FluentQuery();
                var system = query.Table<InlineSystem>();

                var rows = query
                    .From<InlineSite>(out var site)
                    .OrderBy(() => site.Row.Id)
                    .Select(() => new
                    {
                        site.Row.Name,
                        ActiveSystems = FSql.Scalar<int>(query.Subquery().From(system)
                            .Where(() => system.Row.SiteId == site.Row.Id && system.Row.Active)
                            .SelectScalar(() => FSql.Count()))
                    })
                    .Fetch();

                Assert.That(rows.Select(x => x.Name), Is.EqualTo(new[] { "north", "south" }));
                Assert.That(rows.Select(x => x.ActiveSystems), Is.EqualTo(new[] { 2, 1 }));
            }
        }

        [Test]
        public void ExistsCanBeBuiltInsideThePredicate()
        {
            using (var database = CreateDatabase())
            {
                var query = database.FluentQuery();
                var system = query.Table<InlineSystem>();

                var rows = query
                    .From<InlineSite>(out var site)
                    .Where(() => FSql.Exists(query.Subquery().From(system)
                        .Where(() => system.Row.SiteId == site.Row.Id && !system.Row.Active)
                        .SelectScalar(() => 1)))
                    .Select(() => site.Row.Name)
                    .Fetch();

                Assert.That(rows, Is.EqualTo(new[] { "north" }));
            }
        }

        [Test]
        public void AnInlineSubqueryCanJoin()
        {
            using (var database = CreateDatabase())
            {
                var query = database.FluentQuery();
                var system = query.Table<InlineSystem>();
                var type = query.Table<InlineType>();

                var rows = query
                    .From<InlineSite>(out var site)
                    .OrderBy(() => site.Row.Id)
                    .Select(() => new
                    {
                        site.Row.Name,
                        Billable = FSql.Scalar<int>(query.Subquery().From(system)
                            .InnerJoin(type, () => type.Row.Id == system.Row.TypeId)
                            .Where(() => system.Row.SiteId == site.Row.Id && system.Row.Active && type.Row.Billable)
                            .SelectScalar(() => FSql.Count()))
                    })
                    .Fetch();

                Assert.That(rows.Select(x => x.Billable), Is.EqualTo(new[] { 1, 1 }));
            }
        }

        [Test]
        public void SubqueryBeforeAnyFromIsAnError()
        {
            using (var database = CreateDatabase())
            {
                var query = database.FluentQuery();
                Assert.Throws<InvalidOperationException>(() => query.Subquery());
            }
        }

        [Test]
        public void ADeclaredReferenceIsAliasedApartFromTheRest()
        {
            using (var database = CreateDatabase())
            {
                var query = database.FluentQuery().From<InlineSystem>(out var outer);
                var inner = query.Table<InlineSystem>();

                var sql = query
                    .Where(() => FSql.Exists(query.Subquery().From(inner)
                        .Where(() => inner.Row.SiteId == outer.Row.SiteId && inner.Row.Id != outer.Row.Id)
                        .SelectScalar(() => 1)))
                    .Select(() => outer.Row.Name)
                    .ToSql();

                Assert.That(inner.Alias, Is.Not.EqualTo(outer.Alias));
                Assert.That(sql.SQL, Does.Contain(inner.Alias));
                Assert.That(sql.SQL, Does.Contain(outer.Alias));
            }
        }

        [Test]
        public void AReferenceIsAddedOnce()
        {
            using (var database = CreateDatabase())
            {
                var query = database.FluentQuery().From<InlineSite>(out var site);
                var system = query.Table<InlineSystem>();

                query.Subquery().From(system).Where(() => system.Row.SiteId == site.Row.Id).SelectScalar(() => 1);

                Assert.Throws<InvalidOperationException>(() => query.Subquery().From(system));
                Assert.Throws<InvalidOperationException>(() => query.InnerJoin(system, () => system.Row.SiteId == site.Row.Id));
            }
        }

        [Test]
        public void AReferenceFromAnotherStatementIsRejected()
        {
            using (var database = CreateDatabase())
            {
                var other = database.FluentQuery().From<InlineSite>(out _);
                var stranger = other.Table<InlineSystem>();

                var query = database.FluentQuery().From<InlineSite>(out var site);

                Assert.Throws<InvalidOperationException>(() => query.Subquery().From(stranger));
                Assert.Throws<InvalidOperationException>(() => query.InnerJoin(stranger, () => stranger.Row.SiteId == site.Row.Id));
            }
        }

        [Test]
        public void ADeclaredReferenceIsNotInScopeUntilItIsAdded()
        {
            using (var database = CreateDatabase())
            {
                var query = database.FluentQuery().From<InlineSite>(out var site);
                var system = query.Table<InlineSystem>();

                Assert.Throws<InvalidOperationException>(() => query
                    .Where(() => system.Row.SiteId == site.Row.Id)
                    .Select(() => site.Row.Name)
                    .ToSql());
            }
        }

        [Test]
        public void ADeclaredReferenceJoinsTheOuterQueryToo()
        {
            using (var database = CreateDatabase())
            {
                var query = database.FluentQuery().From<InlineSystem>(out var system);
                var type = query.Table<InlineType>();

                var rows = query
                    .InnerJoin(type, () => type.Row.Id == system.Row.TypeId)
                    .Where(() => type.Row.Billable)
                    .OrderBy(() => system.Row.Id)
                    .Select(() => system.Row.Name)
                    .Fetch();

                Assert.That(rows, Is.EqualTo(new[] { "a", "c", "d" }));
            }
        }

        [TableName("inlinesites")]
        public class InlineSite
        {
            [Column("id")] public int Id { get; set; }
            [Column("name")] public string Name { get; set; }
        }

        [TableName("inlinesystems")]
        public class InlineSystem
        {
            [Column("id")] public int Id { get; set; }
            [Column("site_id")] public int SiteId { get; set; }
            [Column("type_id")] public int TypeId { get; set; }
            [Column("name")] public string Name { get; set; }
            [Column("active")] public bool Active { get; set; }
        }

        [TableName("inlinetypes")]
        public class InlineType
        {
            [Column("id")] public int Id { get; set; }
            [Column("name")] public string Name { get; set; }
            [Column("billable")] public bool Billable { get; set; }
        }
    }
}
