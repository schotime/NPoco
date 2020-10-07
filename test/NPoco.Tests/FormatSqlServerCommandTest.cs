using System;
using System.Data;
using Microsoft.Data.SqlClient;
using NUnit.Framework;

namespace NPoco.Tests
{
    public class FormatSqlServerCommandTest
    {
        [Test]
        public void FormattingWithSqlParameterThatIsNotNull()
        {
            var cmd = new SqlCommand();
            cmd.Parameters.Add(new SqlParameter("Test", SqlDbType.NVarChar));
            var db = new MyDb();
            Assert.AreEqual("DECLARE @0 NVarChar[4000] = 'value'\n\nsql @0", db.FormatCommand("sql @0", new object[] { "value" }));
            Assert.AreEqual("DECLARE @0 Int = '32'\n\nsql @0", db.FormatCommand("sql @0", new object[] { 32 }));
            Assert.AreEqual("DECLARE @0 DateTime = '" + DateTime.Today + "'\n\nsql @0", db.FormatCommand("sql @0", new object[] { DateTime.Today }));

            var guid = Guid.NewGuid();
            Assert.AreEqual("DECLARE @0 UniqueIdentifier = '" + guid + "'\n\nsql @0", db.FormatCommand("sql @0", new object[] { guid }));
        }

        [Test]
        public void FormattingWithNullValue()
        {
            var db = new MyDb();
            Assert.AreEqual("DECLARE @0 NVarChar = null\n\nsql @0", db.FormatCommand("sql @0", new object[] { null }));
        }

        [Test]
        public void FormattingWithStringValue()
        {
            var db = new MyDb();
            Assert.AreEqual("DECLARE @0 NVarChar[4000] = 'value'\n\nsql @0", db.FormatCommand("sql @0", new object[] { "value" }));
        }

        public class MyDb : NPoco.SqlServer.SqlServerDatabase
        {
            public MyDb()
                : base("test")
            {
            }
        }
    }
}
