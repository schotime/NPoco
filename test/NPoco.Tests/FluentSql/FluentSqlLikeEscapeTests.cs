using System;
using System.IO;
using Microsoft.Data.Sqlite;
using NPoco.FluentSql;
using NUnit.Framework;

namespace NPoco.Tests.FluentSqlTests
{
    /// <summary>
    /// Contains, StartsWith and EndsWith become LIKE, so a % or _ sitting in the value being
    /// searched for would act as a wildcard rather than matching itself. The builder escapes those
    /// with <see cref="ISqlDialect.LikeEscapeCharacter"/> and names the same character in the
    /// ESCAPE clause; these run against a real SQLite database because the two agreeing is the
    /// whole point, and asserting the emitted SQL would not notice them disagreeing.
    ///
    /// Each case pairs a row holding the literal character with a row that only matches if the
    /// character was left as a wildcard.
    /// </summary>
    [TestFixture]
    public class FluentSqlLikeEscapeTests
    {
        private string _file;
        private string _connectionString;

        [SetUp]
        public void SetUp()
        {
            _file = Path.Combine(Path.GetTempPath(), "npoco-likeescape-" + Guid.NewGuid().ToString("N") + ".db");
            _connectionString = "Data Source=" + _file;
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText =
                    "create table escaperows(id integer primary key, name text);" +
                    "insert into escaperows values" +
                    "  (1,'50% off')," +
                    "  (2,'500 units')," +
                    "  (3,'a_b')," +
                    "  (4,'axb')," +
                    "  (5,'wow!')," +
                    "  (6,'wowa');";
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

        private string[] NamesContaining(string value)
        {
            using var db = Db();
            return db.FluentQuery().From<EscapeRow>(out var a)
                .Where(a, x => x.Name.Contains(value))
                .OrderBy(() => a.Row.Id)
                .SelectScalar(() => a.Row.Name).Fetch().ToArray();
        }

        [Test] public void ContainsMatchesAPercentLiterally()
        {
            // Unescaped, '%50%%' would also take '500 units'.
            Assert.That(NamesContaining("50%"), Is.EqualTo(new[] { "50% off" }));
        }

        [Test] public void ContainsMatchesAnUnderscoreLiterally()
        {
            // Unescaped, _ matches any single character, which would also take 'axb'.
            Assert.That(NamesContaining("a_b"), Is.EqualTo(new[] { "a_b" }));
        }

        [Test] public void ContainsMatchesTheEscapeCharacterItselfLiterally()
        {
            // The escape character has to be escaped first of all, or it would escape the % that
            // Contains appends and stop that being a wildcard.
            Assert.That(NamesContaining("wow!"), Is.EqualTo(new[] { "wow!" }));
        }

        [Test] public void StartsWithMatchesAnUnderscoreLiterally()
        {
            using var db = Db();

            var names = db.FluentQuery().From<EscapeRow>(out var a)
                .Where(a, x => x.Name.StartsWith("a_"))
                .OrderBy(() => a.Row.Id)
                .SelectScalar(() => a.Row.Name).Fetch();

            Assert.That(names, Is.EqualTo(new[] { "a_b" }));
        }

        [Test] public void EndsWithMatchesAPercentLiterally()
        {
            using var db = Db();

            var names = db.FluentQuery().From<EscapeRow>(out var a)
                .Where(a, x => x.Name.EndsWith("0%"))
                .OrderBy(() => a.Row.Id)
                .SelectScalar(() => a.Row.Name).Fetch();

            Assert.That(names, Is.Empty, "the % is literal, so nothing ends with it here");
        }

        [Test] public void ContainsStillWildcardsWhatItAppendsItself()
        {
            // The prefix and suffix % are added after escaping, so they stay wildcards.
            Assert.That(NamesContaining("wow"), Is.EqualTo(new[] { "wow!", "wowa" }));
        }

        [TableName("escaperows")]
        public class EscapeRow
        {
            [Column("id")] public int Id { get; set; }
            [Column("name")] public string Name { get; set; }
        }
    }
}
