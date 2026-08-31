using System;
using System.IO;
using System.Reflection;
using Microsoft.Data.Sqlite;
using NPoco.FluentSql;
using NUnit.Framework;

namespace NPoco.Tests.FluentSqlTests
{
    /// <summary>
    /// Conversions that belong to a column rather than to its type - a member's own from-db
    /// converter, a UTC column - have to reach a scalar projection too. NPoco's plain single-column
    /// mapping is handed no column and cannot apply them, so the builder plans the read itself and
    /// the three ways of asking for the value have to agree.
    /// </summary>
    [TestFixture]
    public class FluentSqlScalarConversionTests
    {
        private string _file;
        private string _connectionString;

        [SetUp]
        public void SetUp()
        {
            _file = Path.Combine(Path.GetTempPath(), "npoco-conv-" + Guid.NewGuid().ToString("N") + ".db");
            _connectionString = "Data Source=" + _file;
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText =
                    "create table convrows(id integer primary key, code text, occurred datetime);" +
                    "insert into convrows values(1,'abc','2024-01-02 03:04:05');";
                command.ExecuteNonQuery();
            }
        }

        [TearDown]
        public void TearDown()
        {
            SqliteConnection.ClearAllPools();
            try { File.Delete(_file); } catch (IOException) { }
        }

        private Database CreateDatabase()
        {
            var database = new Database(_connectionString, DatabaseType.SQLite, SqliteFactory.Instance);
            database.Mappers.Add(new CodeMapper());
            return database;
        }

        [Test]
        public void AMemberConverterAppliesToEveryWayOfReadingTheColumn()
        {
            using (var database = CreateDatabase())
            {
                var entity = database.FluentQuery().From<ConvRow>(out var whole).Select(whole).Single();

                var projected = database.FluentQuery()
                    .From<ConvRow>(out var member)
                    .Select(() => new { member.Row.Code })
                    .Single();

                var scalar = database.FluentQuery()
                    .From<ConvRow>(out var alone)
                    .SelectScalar(() => alone.Row.Code)
                    .Single();

                Assert.That(entity.Code, Is.EqualTo("code:abc"));
                Assert.That(projected.Code, Is.EqualTo(entity.Code));
                Assert.That(scalar, Is.EqualTo(entity.Code));
            }
        }

        // ForceToUtc only fires when the reader hands back a DateTime, and SQLite reports a declared
        // datetime column as something else, so this pins the weaker claim the provider allows: the
        // three reads agree. The conversion itself travels the same path the converter above does.
        [Test]
        public void EveryWayOfReadingADateColumnAgrees()
        {
            using (var database = CreateDatabase())
            {
                var entity = database.FluentQuery().From<ConvRow>(out var whole).Select(whole).Single();

                var projected = database.FluentQuery()
                    .From<ConvRow>(out var member)
                    .Select(() => new { member.Row.Occurred })
                    .Single();

                var scalar = database.FluentQuery()
                    .From<ConvRow>(out var alone)
                    .SelectScalar(() => alone.Row.Occurred)
                    .Single();

                Assert.That(entity.Occurred, Is.EqualTo(new DateTime(2024, 1, 2, 3, 4, 5)));
                Assert.That(projected.Occurred, Is.EqualTo(entity.Occurred));
                Assert.That(projected.Occurred.Kind, Is.EqualTo(entity.Occurred.Kind));
                Assert.That(scalar, Is.EqualTo(entity.Occurred));
                Assert.That(scalar.Kind, Is.EqualTo(entity.Occurred.Kind));
            }
        }

        [Test]
        public void AnExpressionThatNamesNoColumnStillReadsByItsType()
        {
            using (var database = CreateDatabase())
            {
                var count = database.FluentQuery()
                    .From<ConvRow>(out var row)
                    .SelectScalar(() => FSql.Count())
                    .Single();

                var joined = database.FluentQuery()
                    .From<ConvRow>(out var other)
                    .SelectScalar(() => other.Row.Code + "!")
                    .Single();

                Assert.That(count, Is.EqualTo(1));
                Assert.That(joined, Is.EqualTo("abc!"));
            }
        }

        private class CodeMapper : DefaultMapper
        {
            public override Func<object, object> GetFromDbConverter(MemberInfo memberInfo, Type sourceType)
            {
                if (memberInfo.DeclaringType == typeof(ConvRow) && memberInfo.Name == nameof(ConvRow.Code))
                    return value => "code:" + value;
                return base.GetFromDbConverter(memberInfo, sourceType);
            }
        }

        [TableName("convrows")]
        public class ConvRow
        {
            [Column("id")] public int Id { get; set; }
            [Column("code")] public string Code { get; set; }
            [Column("occurred", ForceToUtc = true)] public DateTime Occurred { get; set; }
        }
    }
}
