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
                    "create table convrows(id integer primary key, code text, occurred datetime, perms text, note text, data blob);" +
                    "insert into convrows values(1,'abc','2024-01-02 03:04:05','[\"read\",\"write\"]',null,x'0102ff');";
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
        public void AnExpressionIsReadBackThroughTheMemberItIsWrittenOnto()
        {
            using (var database = CreateDatabase())
            {
                // The fragment is a value the projection computes, not a column it reads, so nothing
                // in the query says how to read it back. The member it lands on is serialized, and
                // that is what says so - the same thing NPoco reads the row by.
                var row = database.FluentQuery()
                    .From<ConvRow>(out var source)
                    .Select(() => new PermissionRow
                    {
                        Id = source.Row.Id,
                        Permissions = FSql.Raw<string[]>("{0}", source.Row.Perms)
                    })
                    .Single();

                Assert.That(row.Id, Is.EqualTo(1));
                Assert.That(row.Permissions, Is.EqualTo(new[] { "read", "write" }));
            }
        }

        [Test]
        public void AValueThatAlreadyArrivesAsTheMemberTypeIsNotConvertedAgain()
        {
            using (var database = CreateDatabase())
            {
                // The member is serialized, so its column says the value is stored as text - but
                // this one was materialized by the provider and is already a byte[]. Reading it as
                // stored text would be a cast that cannot work.
                var row = database.FluentQuery()
                    .From<ConvRow>(out var source)
                    .Select(() => new BlobRow { Data = FSql.Raw<byte[]>("{0}", source.Row.Data) })
                    .Single();

                Assert.That(row.Data, Is.EqualTo(new byte[] { 1, 2, 255 }));
            }
        }

        [Test]
        public void AProjectionWithNoMappedMemberToReadBackThroughStillReadsByType()
        {
            using (var database = CreateDatabase())
            {
                // An anonymous projection maps no member, so there is no column on either side and
                // the value is read by its type - which cannot turn the text into an array.
                Assert.Throws<InvalidCastException>(() => database.FluentQuery()
                    .From<ConvRow>(out var source)
                    .Select(() => new { Permissions = FSql.Raw<string[]>("{0}", source.Row.Perms) })
                    .Single());
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

        [Test]
        public void TheColumnTheValueIsReadFromWinsOverTheOneItIsWrittenTo()
        {
            using (var database = CreateDatabase())
            {
                // Both members carry a converter, and which prefix comes back says which column the
                // read was planned from: the column named by the expression, or - where the
                // expression names none - the member it lands on.
                var row = database.FluentQuery()
                    .From<ConvRow>(out var source)
                    .Select(() => new PermissionRow
                    {
                        Code = source.Row.Code,
                        Permissions = FSql.Raw<string[]>("{0}", source.Row.Perms)
                    })
                    .Single();

                var computed = database.FluentQuery()
                    .From<ConvRow>(out var other)
                    .Select(() => new PermissionRow { Code = FSql.Raw<string>("{0}", other.Row.Code) })
                    .Single();

                Assert.That(row.Code, Is.EqualTo("code:abc"));
                Assert.That(computed.Code, Is.EqualTo("dest:abc"));
            }
        }

        [Test]
        public void AConstructedProjectionReadsBackThroughTheMemberItsArgumentFills()
        {
            using (var database = CreateDatabase())
            {
                // The constructor names its arguments, not its members, so the member each argument
                // fills is found by name - and it is that member the value is read back through.
                var row = database.FluentQuery()
                    .From<ConvRow>(out var source)
                    .Select(() => new PermissionRecord(source.Row.Id, FSql.Raw<string[]>("{0}", source.Row.Perms)))
                    .Single();

                Assert.That(row.Id, Is.EqualTo(1));
                Assert.That(row.Permissions, Is.EqualTo(new[] { "read", "write" }));
            }
        }

        [Test]
        public void ANestedProjectionReadsBackThroughTheMemberOfTheTypeItSitsOn()
        {
            using (var database = CreateDatabase())
            {
                var row = database.FluentQuery()
                    .From<ConvRow>(out var source)
                    .Select(() => new PermissionWrapper
                    {
                        Id = source.Row.Id,
                        Inner = new PermissionRow { Permissions = FSql.Raw<string[]>("{0}", source.Row.Perms) }
                    })
                    .Single();

                Assert.That(row.Id, Is.EqualTo(1));
                Assert.That(row.Inner.Permissions, Is.EqualTo(new[] { "read", "write" }));
            }
        }

        [Test]
        public void AValueObjectMemberWrapsAComputedValueToo()
        {
            using (var database = CreateDatabase())
            {
                var row = database.FluentQuery()
                    .From<ConvRow>(out var source)
                    .Select(() => new LabelRow { Label = FSql.Raw<CodeValue>("{0}", source.Row.Code) })
                    .Single();

                Assert.That(row.Label, Is.Not.Null);
                Assert.That(row.Label.Value, Is.EqualTo("abc"));
            }
        }

        [Test]
        public void ANullIsNotHandedToTheConversionOnEitherSide()
        {
            using (var database = CreateDatabase())
            {
                var written = database.FluentQuery()
                    .From<ConvRow>(out var source)
                    .Select(() => new PermissionRow { Permissions = FSql.Raw<string[]>("{0}", source.Row.Note) })
                    .Single();

                var read = database.FluentQuery()
                    .From<ConvRow>(out var other)
                    .Select(() => new { other.Row.Note })
                    .Single();

                var alone = database.FluentQuery()
                    .From<ConvRow>(out var third)
                    .SelectScalar(() => third.Row.Note)
                    .Single();

                Assert.That(written.Permissions, Is.Null);
                Assert.That(read.Note, Is.Null);
                Assert.That(alone, Is.Null);
            }
        }

        [Test]
        public void EveryWayOfProjectingOneColumnConvertsIt()
        {
            using (var database = CreateDatabase())
            {
                var selectScalar = database.FluentQuery()
                    .From<ConvRow>(out var a).SelectScalar(() => a.Row.Code).Single();

                var selectScalarFromRow = database.FluentQuery()
                    .From<ConvRow>(out var b).SelectScalar(b, x => x.Code).Single();

                var selectOfOneColumn = database.FluentQuery()
                    .From<ConvRow>(out var c).Select(() => c.Row.Code).Single();

                var member = database.FluentQuery()
                    .From<ConvRow>(out var d).Select(() => new { d.Row.Code }).Single();

                Assert.That(selectScalar, Is.EqualTo("code:abc"));
                Assert.That(selectScalarFromRow, Is.EqualTo("code:abc"));
                Assert.That(selectOfOneColumn, Is.EqualTo("code:abc"));
                Assert.That(member.Code, Is.EqualTo("code:abc"));
            }
        }

        private class CodeMapper : DefaultMapper
        {
            public override Func<object, object> GetFromDbConverter(MemberInfo memberInfo, Type sourceType)
            {
                if (memberInfo.DeclaringType == typeof(ConvRow) && memberInfo.Name == nameof(ConvRow.Code))
                    return value => "code:" + value;
                if (memberInfo.DeclaringType == typeof(PermissionRow) && memberInfo.Name == nameof(PermissionRow.Code))
                    return value => "dest:" + value;
                return base.GetFromDbConverter(memberInfo, sourceType);
            }
        }

        [TableName("convrows")]
        public class ConvRow
        {
            [Column("id")] public int Id { get; set; }
            [Column("code")] public string Code { get; set; }
            [Column("occurred", ForceToUtc = true)] public DateTime Occurred { get; set; }
            [Column("perms")] public string Perms { get; set; }
            [Column("note")] public string Note { get; set; }
            [Column("data")] public byte[] Data { get; set; }
        }

        [TableName("convrows")]
        public class PermissionRow
        {
            [Column("id")] public int Id { get; set; }
            [Column("perms")] [SerializedColumn] public string[] Permissions { get; set; }
            [Column("code")] public string Code { get; set; }
        }

        [TableName("convrows")]
        public class PermissionRecord
        {
            public PermissionRecord(int id, string[] permissions)
            {
                Id = id;
                Permissions = permissions;
            }

            [Column("id")] public int Id { get; set; }
            [Column("perms")] [SerializedColumn] public string[] Permissions { get; set; }
        }

        // A serialized member whose value the provider materializes itself - a blob here, a
        // Postgres array or json column elsewhere - so it arrives as the member's own type rather
        // than as the text a serialized column is stored as.
        [TableName("convrows")]
        public class BlobRow
        {
            [Column("data")] [SerializedColumn] public byte[] Data { get; set; }
        }

        [TableName("convrows")]
        public class LabelRow
        {
            [Column("code")] public CodeValue Label { get; set; }
        }

        public class CodeValue : IValueObject<string>
        {
            public string Value { get; set; }
        }

        public class PermissionWrapper
        {
            public int Id { get; set; }
            public PermissionRow Inner { get; set; }
        }
    }
}
