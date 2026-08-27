using System.Data;
using System.Data.Common;
using NPoco.DatabaseTypes;
using NUnit.Framework;

namespace NPoco.Tests.DatabaseTypes
{
    // Covers the reflection-based branches in DatabaseType.SetParameterValue (SqlCeParameter oversized
    // NText, SqlGeography/SqlGeometry UDTs) after switching them from PropertyInfo.SetValue to a cached
    // compiled setter (ReflectionCache.GetSetter) - these had no direct test coverage before.
    public class DatabaseTypeSetParameterValueTests
    {
        // Name must match exactly - DatabaseType.SetParameterValue keys off p.GetType().Name == "SqlCeParameter".
        public class SqlCeParameter : DbParameter
        {
            public SqlDbType SqlDbType { get; set; }
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

        // Any DbParameter with a UdtTypeName property, mirroring real SqlClient's SqlParameter.
        public class UdtCapableParameter : DbParameter
        {
            public string UdtTypeName { get; set; }
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

        // Only the type name is checked (value.GetType().Name == "SqlGeography"/"SqlGeometry"), so these
        // stand-ins don't need any members.
        public class SqlGeography { }
        public class SqlGeometry { }

        private static DatabaseType DbType => new SQLiteDatabaseType();

        [Test]
        public void OversizedStringOnSqlCeParameter_SwitchesToNText()
        {
            var p = new SqlCeParameter { ParameterName = "@0" };
            var value = new string('x', 4001);

            DbType.SetParameterValue(p, value);

            Assert.AreEqual(System.Data.SqlDbType.NText, p.SqlDbType);
            Assert.AreEqual(value, p.Value);
        }

        [Test]
        public void RegularSizedStringOnSqlCeParameter_LeavesSqlDbTypeUntouched()
        {
            var p = new SqlCeParameter { ParameterName = "@0" };

            DbType.SetParameterValue(p, "short value");

            Assert.AreEqual(default(System.Data.SqlDbType), p.SqlDbType);
        }

        [Test]
        public void SqlGeographyValue_SetsUdtTypeName()
        {
            var p = new UdtCapableParameter { ParameterName = "@0" };
            var value = new SqlGeography();

            DbType.SetParameterValue(p, value);

            Assert.AreEqual("geography", p.UdtTypeName);
            Assert.AreSame(value, p.Value);
        }

        [Test]
        public void SqlGeometryValue_SetsUdtTypeName()
        {
            var p = new UdtCapableParameter { ParameterName = "@0" };
            var value = new SqlGeometry();

            DbType.SetParameterValue(p, value);

            Assert.AreEqual("geometry", p.UdtTypeName);
            Assert.AreSame(value, p.Value);
        }
    }
}
