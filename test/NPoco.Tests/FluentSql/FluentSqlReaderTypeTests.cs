using System;
using System.Collections;
using System.Data.Common;
using System.IO;
using Microsoft.Data.Sqlite;
using NPoco.FluentSql;
using NUnit.Framework;

namespace NPoco.Tests.FluentSqlTests
{
    /// <summary>
    /// What a projection does when the reader's declared type and the value it hands back disagree.
    /// A provider is free to describe a computed column loosely - an aggregate has no column behind
    /// it to describe - and still return a fully materialized value, so a plan that decided how to
    /// read the value from the declared type alone would decide wrongly. Driving the plan against a
    /// reader written to disagree is the only way to pin that down: a real provider that does it is
    /// exactly the one not installed here.
    /// </summary>
    [TestFixture]
    public class FluentSqlReaderTypeTests
    {
        private string _file;
        private string _connectionString;

        [SetUp]
        public void SetUp()
        {
            _file = Path.Combine(Path.GetTempPath(), "npoco-readertype-" + Guid.NewGuid().ToString("N") + ".db");
            _connectionString = "Data Source=" + _file;
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText =
                    "create table readerrows(id integer primary key, perms text);" +
                    "insert into readerrows values(1,'[\"read\"]');";
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

        /// <summary>
        /// The Postgres case: <c>array_agg</c> hands back a string[] the provider materialized, while
        /// the column it came from is described only as loosely as an expression can be. The member
        /// it lands on is serialized, which says a value read from that column arrives as text - but
        /// this one did not, and reading it as text is a cast that cannot work.
        /// </summary>
        [Test]
        public void AMaterializedValueIsNotDeserializedWhenTheReaderDescribesItLoosely()
        {
            using (var database = CreateDatabase())
            {
                var plan = Plan(database);
                var reader = new StubReader("Permissions", typeof(object), new[] { "read", "write" });

                plan.Init(reader, database.PocoDataFactory.ForType(typeof(ReaderRow)));
                var row = (PermissionsRow)plan.Map(reader, new NPoco.RowMappers.RowMapperContext());

                Assert.That(row.Permissions, Is.EqualTo(new[] { "read", "write" }));
            }
        }

        /// <summary>
        /// The same member, reading a value that did arrive as the text a serialized column stores.
        /// That one is deserialized, whatever the reader called the column.
        /// </summary>
        [Test]
        public void TextIsStillDeserializedWhenTheReaderDescribesItLoosely()
        {
            using (var database = CreateDatabase())
            {
                var plan = Plan(database);
                var reader = new StubReader("Permissions", typeof(object), "[\"read\",\"write\"]");

                plan.Init(reader, database.PocoDataFactory.ForType(typeof(ReaderRow)));
                var row = (PermissionsRow)plan.Map(reader, new NPoco.RowMappers.RowMapperContext());

                Assert.That(row.Permissions, Is.EqualTo(new[] { "read", "write" }));
            }
        }

        // The projection under test: the value is computed, so it carries no column of its own and
        // the member it is written onto is what says how to read it back.
        private static ProjectionPlan Plan(Database database)
        {
            var result = database.FluentQuery()
                .From<ReaderRow>(out var source)
                .Select(() => new PermissionsRow { Permissions = FSql.Raw<string[]>("{0}", source.Row.Perms) });
            return result.InnerQuery.ProjectionPlan;
        }

        /// <summary>One row, one column, whose declared type is whatever the test says it is.</summary>
        private sealed class StubReader : DbDataReader
        {
            private readonly string _name;
            private readonly Type _fieldType;
            private readonly object _value;

            internal StubReader(string name, Type fieldType, object value)
            {
                _name = name;
                _fieldType = fieldType;
                _value = value;
            }

            public override int FieldCount => 1;
            public override string GetName(int ordinal) => _name;
            public override Type GetFieldType(int ordinal) => _fieldType;
            public override object GetValue(int ordinal) => _value;
            public override bool IsDBNull(int ordinal) => _value == null;

            public override int GetValues(object[] values)
            {
                values[0] = _value;
                return 1;
            }

            public override int GetOrdinal(string name) => 0;
            public override string GetDataTypeName(int ordinal) => _fieldType.Name;
            public override bool HasRows => true;
            public override int Depth => 0;
            public override bool IsClosed => false;
            public override int RecordsAffected => 0;
            public override bool NextResult() => false;
            public override bool Read() => true;
            public override IEnumerator GetEnumerator() => throw new NotSupportedException();
            public override object this[int ordinal] => GetValue(ordinal);
            public override object this[string name] => GetValue(0);
            public override bool GetBoolean(int ordinal) => throw new NotSupportedException();
            public override byte GetByte(int ordinal) => throw new NotSupportedException();
            public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length) => throw new NotSupportedException();
            public override char GetChar(int ordinal) => throw new NotSupportedException();
            public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length) => throw new NotSupportedException();
            public override DateTime GetDateTime(int ordinal) => throw new NotSupportedException();
            public override decimal GetDecimal(int ordinal) => throw new NotSupportedException();
            public override double GetDouble(int ordinal) => throw new NotSupportedException();
            public override float GetFloat(int ordinal) => throw new NotSupportedException();
            public override Guid GetGuid(int ordinal) => throw new NotSupportedException();
            public override short GetInt16(int ordinal) => throw new NotSupportedException();
            public override int GetInt32(int ordinal) => throw new NotSupportedException();
            public override long GetInt64(int ordinal) => throw new NotSupportedException();
            public override string GetString(int ordinal) => throw new NotSupportedException();
        }

        [TableName("readerrows")]
        public class ReaderRow
        {
            [Column("id")] public int Id { get; set; }
            [Column("perms")] public string Perms { get; set; }
        }

        [TableName("readerrows")]
        public class PermissionsRow
        {
            [Column("perms")] [SerializedColumn] public string[] Permissions { get; set; }
        }
    }
}
