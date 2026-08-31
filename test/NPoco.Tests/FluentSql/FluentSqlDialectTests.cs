using System;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using NPoco.DatabaseTypes;
using NPoco.FluentSql;
using NUnit.Framework;

namespace NPoco.Tests.FluentSqlTests
{
    /// <summary>
    /// The functions whose SQL differs between databases. Each one is asked of a real SQLite
    /// database so the emitted SQL has to actually run, and asked of the other dialects through
    /// generation alone, since the point of a dialect is that the same expression comes out
    /// spelled the way each database expects.
    /// </summary>
    [TestFixture]
    public class FluentSqlDialectTests
    {
        private string _file;
        private string _connectionString;

        [SetUp]
        public void SetUp()
        {
            _file = Path.Combine(Path.GetTempPath(), "npoco-dialect-" + Guid.NewGuid().ToString("N") + ".db");
            _connectionString = "Data Source=" + _file;
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText =
                    "create table dialectrows(id integer primary key, name text, amount real, offset integer);" +
                    "insert into dialectrows values" +
                    "  (1,'Alpha',10.6,3)," +
                    "  (2,'Beta',-4.4,null)," +
                    "  (3,'Gamma',0.5,7);";
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

        [Test] public void SubstringRunsAndCountsFromOneAsSqlDoes()
        {
            using var db = Db();

            var prefixes = db.FluentQuery().From<DialectRow>(out var a).OrderBy(() => a.Row.Id)
                .SelectScalar(() => a.Row.Name.Substring(0, 2)).Fetch();
            Assert.That(prefixes, Is.EqualTo(new[] { "Al", "Be", "Ga" }));

            // No length: everything from the start position on.
            var tails = db.FluentQuery().From<DialectRow>(out var b).OrderBy(() => b.Row.Id)
                .SelectScalar(() => b.Row.Name.Substring(2)).Fetch();
            Assert.That(tails, Is.EqualTo(new[] { "pha", "ta", "mma" }));

            var matched = db.FluentQuery().From<DialectRow>(out var c)
                .Where(c, x => x.Name.Substring(0, 1) == "B")
                .SelectScalar(() => c.Row.Id).Single();
            Assert.That(matched, Is.EqualTo(2));
        }

        [Test] public void StringLengthRuns()
        {
            using var db = Db();

            var lengths = db.FluentQuery().From<DialectRow>(out var a).OrderBy(() => a.Row.Id)
                .SelectScalar(() => a.Row.Name.Length).Fetch();
            Assert.That(lengths, Is.EqualTo(new[] { 5, 4, 5 }));

            var longest = db.FluentQuery().From<DialectRow>(out var b)
                .Where(() => b.Row.Name.Length > 4)
                .OrderBy(() => b.Row.Id)
                .SelectScalar(() => b.Row.Name).Fetch();
            Assert.That(longest, Is.EqualTo(new[] { "Alpha", "Gamma" }));
        }

        [Test] public void MathFunctionsRun()
        {
            using var db = Db();

            var absolute = db.FluentQuery().From<DialectRow>(out var a).OrderBy(() => a.Row.Id)
                .SelectScalar(() => Math.Abs(a.Row.Amount)).Fetch();
            Assert.That(absolute, Is.EqualTo(new[] { 10.6, 4.4, 0.5 }));

            var floored = db.FluentQuery().From<DialectRow>(out var b).OrderBy(() => b.Row.Id)
                .SelectScalar(() => Math.Floor(b.Row.Amount)).Fetch();
            Assert.That(floored, Is.EqualTo(new[] { 10.0, -5.0, 0.0 }));

            var ceilinged = db.FluentQuery().From<DialectRow>(out var c).OrderBy(() => c.Row.Id)
                .SelectScalar(() => Math.Ceiling(c.Row.Amount)).Fetch();
            Assert.That(ceilinged, Is.EqualTo(new[] { 11.0, -4.0, 1.0 }));

            var rounded = db.FluentQuery().From<DialectRow>(out var d)
                .Where(d, x => x.Id == 1)
                .SelectScalar(() => Math.Round(d.Row.Amount)).Single();
            Assert.That(rounded, Is.EqualTo(11.0));

            var toOneDigit = db.FluentQuery().From<DialectRow>(out var e)
                .Where(e, x => x.Id == 1)
                .SelectScalar(() => Math.Round(e.Row.Amount, 1)).Single();
            Assert.That(toOneDigit, Is.EqualTo(10.6));
        }

        [Test] public void GetValueOrDefaultBecomesCoalesce()
        {
            using var db = Db();

            var offsets = db.FluentQuery().From<DialectRow>(out var a).OrderBy(() => a.Row.Id)
                .SelectScalar(() => a.Row.Offset.GetValueOrDefault()).Fetch();
            Assert.That(offsets, Is.EqualTo(new[] { 3, 0, 7 }));

            var withFallback = db.FluentQuery().From<DialectRow>(out var b).OrderBy(() => b.Row.Id)
                .SelectScalar(() => b.Row.Offset.GetValueOrDefault(-1)).Fetch();
            Assert.That(withFallback, Is.EqualTo(new[] { 3, -1, 7 }));

            var present = db.FluentQuery().From<DialectRow>(out var c)
                .Where(() => c.Row.Offset.GetValueOrDefault() > 0)
                .OrderBy(() => c.Row.Id)
                .SelectScalar(() => c.Row.Id).Fetch();
            Assert.That(present, Is.EqualTo(new[] { 1, 3 }));
        }

        /// <summary>
        /// Each database spells these its own way, and the dialect is where that lives - so the
        /// same expression is generated against every one of them and read back.
        /// </summary>
        [TestCase(typeof(SQLiteDatabaseType), "SUBSTR", "LENGTH(", "CEIL(")]
        [TestCase(typeof(MySqlDatabaseType), "SUBSTRING", "LENGTH(", "CEIL(")]
        [TestCase(typeof(PostgreSQLDatabaseType), "SUBSTR", "LENGTH(", "CEIL(")]
        [TestCase(typeof(OracleDatabaseType), "SUBSTR", "LENGTH(", "CEIL(")]
        [TestCase(typeof(FirebirdDatabaseType), "SUBSTR", "LENGTH(", "CEILING(")]
        public void EachDialectSpellsTheFunctionsItsOwnWay(Type databaseType, string substring, string length, string ceiling)
        {
            using var db = new Database(_connectionString, (DatabaseType)Activator.CreateInstance(databaseType), SqliteFactory.Instance);

            var sql = db.FluentQuery().From<DialectRow>(out var row)
                .Where(() => row.Row.Name.Length > 3)
                .Select(() => new
                {
                    Head = row.Row.Name.Substring(0, 2),
                    Up = Math.Ceiling(row.Row.Amount),
                    Fallback = row.Row.Offset.GetValueOrDefault()
                })
                .ToSql();

            Assert.That(sql.SQL, Does.Contain(substring), "substring");
            Assert.That(sql.SQL, Does.Contain(length), "length");
            Assert.That(sql.SQL, Does.Contain(ceiling), "ceiling");
            Assert.That(sql.SQL, Does.Contain("COALESCE("), "coalesce");
        }

        [Test] public void CaseFunctionsRunAndComeFromTheDialect()
        {
            using var db = Db();

            var upper = db.FluentQuery().From<DialectRow>(out var a).OrderBy(() => a.Row.Id)
                .SelectScalar(() => a.Row.Name.ToUpper()).Fetch();
            Assert.That(upper, Is.EqualTo(new[] { "ALPHA", "BETA", "GAMMA" }));

            var lower = db.FluentQuery().From<DialectRow>(out var b).OrderBy(() => b.Row.Id)
                .SelectScalar(() => b.Row.Name.ToLower()).Fetch();
            Assert.That(lower, Is.EqualTo(new[] { "alpha", "beta", "gamma" }));

            // LIKE folds case through the same member, so a dialect that changes one changes both.
            var previous = SqlDialects.Resolver;
            try
            {
                SqlDialects.Resolver = _ => new TestDialect();
                var sql = db.FluentQuery().From<DialectRow>(out var c)
                    .Where(c, x => x.Name.Contains("lph"))
                    .SelectScalar(() => c.Row.Name.ToUpper()).ToSql();

                Assert.That(sql.SQL, Does.Contain("UCASE("), "ToUpper");
                Assert.That(sql.SQL, Does.Contain("UCASE([dr].[name]) LIKE"), "LIKE folds case through the same member");
            }
            finally
            {
                SqlDialects.Resolver = previous;
            }
        }

        [Test] public void TheDialectCanBeReplacedForEveryQuery()
        {
            var previous = SqlDialects.Resolver;
            try
            {
                SqlDialects.Resolver = _ => new TestDialect();
                using var db = Db();
                var sql = db.FluentQuery().From<DialectRow>(out var row)
                    .SelectScalar(() => row.Row.Name.Length).ToSql();

                Assert.That(sql.SQL, Does.Contain("CHAR_LENGTH("));
            }
            finally
            {
                SqlDialects.Resolver = previous;
            }
        }

        /// <summary>
        /// A database type written before dialects existed names no dialect, so the one it gets is
        /// read off its provider name - the way the builder chose its SQL back then. Without that
        /// fallback such a type would silently drop to standard SQL and stop being written the
        /// TOP, DATEADD and + concatenation it was written before.
        /// </summary>
        [TestCase("MySql.Data.MySqlClient", typeof(MySqlSqlDialect))]
        [TestCase("Npgsql", typeof(PostgreSqlDialect))]
        [TestCase("System.Data.SQLite", typeof(SqliteSqlDialect))]
        [TestCase("Oracle.ManagedDataAccess.Client", typeof(OracleSqlDialect))]
        [TestCase("FirebirdSql.Data.FirebirdClient", typeof(FirebirdSqlDialect))]
        [TestCase("System.Data.SqlServerCe.4.0", typeof(SqlServerCeSqlDialect))]
        [TestCase("Microsoft.Data.SqlClient", typeof(SqlServerSqlDialect))]
        [TestCase("Some.Other.Provider", typeof(AnsiSqlDialect))]
        public void ADatabaseTypeWithNoDialectIsReadOffItsProviderName(string providerName, Type expected)
        {
            var databaseType = new ProviderNamedDatabaseType(providerName);

            Assert.That(SqlDialects.For(databaseType), Is.TypeOf(expected));
        }

        [Test] public void ADatabaseTypeThatNamesNoProviderStaysOnSqlServer()
        {
            // DatabaseType.GetProviderName() answers Microsoft.Data.SqlClient, so a type that
            // overrides neither was treated as SQL Server and has to stay that way.
            Assert.That(SqlDialects.For(new BareDatabaseType()), Is.TypeOf<SqlServerSqlDialect>());
        }

        private sealed class BareDatabaseType : DatabaseType
        {
        }

        private sealed class ProviderNamedDatabaseType : DatabaseType
        {
            private readonly string _providerName;

            public ProviderNamedDatabaseType(string providerName) => _providerName = providerName;

            public override string GetProviderName() => _providerName;
        }

        private sealed class TestDialect : SqlDialect
        {
            public override string StringLength(string value) => "CHAR_LENGTH(" + value + ")";
            public override string Upper(string value) => "UCASE(" + value + ")";
        }

        [TableName("dialectrows")]
        public class DialectRow
        {
            [Column("id")] public int Id { get; set; }
            [Column("name")] public string Name { get; set; }
            [Column("amount")] public double Amount { get; set; }
            [Column("offset")] public int? Offset { get; set; }
        }
    }
}
