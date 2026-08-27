using System;
using System.Data;
using System.Data.Common;
using NPoco.DatabaseTypes;
using NUnit.Framework;

namespace NPoco.Tests.DatabaseTypes
{
    public class PostgreSQLDatabaseTypeTests
    {
        // Minimal stand-in for Npgsql's real NpgsqlParameter/NpgsqlDbType. NPoco doesn't take a
        // compile-time dependency on Npgsql, so PostgreSQLDatabaseType finds these reflectively by
        // name - this fake just needs to match that shape (see PostgreSQLDatabaseType.SetParameterValue).
        public enum FakeNpgsqlDbType
        {
            Unknown = 0,
            Timestamp,
            TimestampTz
        }

        public class NpgsqlParameter : DbParameter
        {
            public FakeNpgsqlDbType NpgsqlDbType { get; set; }
            public override DbType DbType { get; set; }
            public override ParameterDirection Direction { get; set; } = ParameterDirection.Input;
            public override bool IsNullable { get; set; }
            public override string ParameterName { get; set; }
            public override string SourceColumn { get; set; }
            public override object Value { get; set; }
            public override bool SourceColumnNullMapping { get; set; }
            public override int Size { get; set; }
            public override void ResetDbType() { }
        }

        private static NpgsqlParameter SetAndGetParameter(object value)
        {
            var p = new NpgsqlParameter { ParameterName = "@0" };
            new PostgreSQLDatabaseType().SetParameterValue(p, value);
            return p;
        }

        [Test]
        public void UtcDateTime_MapsToTimestampTz()
        {
            var p = SetAndGetParameter(new DateTime(2026, 8, 27, 0, 0, 0, DateTimeKind.Utc));
            Assert.AreEqual(FakeNpgsqlDbType.TimestampTz, p.NpgsqlDbType);
        }

        [Test]
        public void UnspecifiedDateTime_MapsToTimestamp()
        {
            var p = SetAndGetParameter(new DateTime(2026, 8, 27, 0, 0, 0, DateTimeKind.Unspecified));
            Assert.AreEqual(FakeNpgsqlDbType.Timestamp, p.NpgsqlDbType);
        }

        [Test]
        public void LocalDateTime_MapsToTimestamp()
        {
            var p = SetAndGetParameter(new DateTime(2026, 8, 27, 0, 0, 0, DateTimeKind.Local));
            Assert.AreEqual(FakeNpgsqlDbType.Timestamp, p.NpgsqlDbType);
        }

        [Test]
        public void NullableUtcDateTimeWithValue_MapsToTimestampTz()
        {
            // Boxing a Nullable<DateTime> that HasValue produces a plain boxed DateTime,
            // not a boxed Nullable<DateTime> - confirms the Kind check catches DateTime? too.
            DateTime? value = new DateTime(2026, 8, 27, 0, 0, 0, DateTimeKind.Utc);
            var p = SetAndGetParameter(value);
            Assert.AreEqual(FakeNpgsqlDbType.TimestampTz, p.NpgsqlDbType);
        }

        [Test]
        public void NullDateTime_SetsDbNullWithoutThrowing()
        {
            DateTime? value = null;
            var p = SetAndGetParameter(value);

            Assert.AreEqual(DBNull.Value, p.Value);
            Assert.AreEqual(FakeNpgsqlDbType.Unknown, p.NpgsqlDbType); // untouched
        }

        [Test]
        public void NonNpgsqlParameter_IsUnaffected()
        {
            // Guard: a plain DbParameter (no NpgsqlDbType property) must not throw and must
            // still get the normal DbType.DateTime treatment via the base implementation.
            var p = new PlainFakeParameter { ParameterName = "@0" };
            new PostgreSQLDatabaseType().SetParameterValue(p, new DateTime(2026, 8, 27, 0, 0, 0, DateTimeKind.Unspecified));

            Assert.AreEqual(DbType.DateTime, p.DbType);
        }

        public class PlainFakeParameter : DbParameter
        {
            public override DbType DbType { get; set; }
            public override ParameterDirection Direction { get; set; } = ParameterDirection.Input;
            public override bool IsNullable { get; set; }
            public override string ParameterName { get; set; }
            public override string SourceColumn { get; set; }
            public override object Value { get; set; }
            public override bool SourceColumnNullMapping { get; set; }
            public override int Size { get; set; }
            public override void ResetDbType() { }
        }
    }
}
