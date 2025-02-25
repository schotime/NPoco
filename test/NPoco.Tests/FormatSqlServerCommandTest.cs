using System;
using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using NPoco.DatabaseTypes;
using NUnit.Framework;

namespace NPoco.Tests
{
    public class FormatSqlServerCommandTest
    {
        private Database db = new MyDb();
        private Database sqlDb = new MyDb2();

        [Test]
        public void FormattingWithSqlParameterThatIsNotNull()
        {
            Assert.AreEqual("DECLARE @0 NVarChar(4000) = 'value'\n\nsql @0", db.FormatCommand("sql @0", [GetSqlParameter(db, "value")]));
            Assert.AreEqual("DECLARE @0 Int = '32'\n\nsql @0", db.FormatCommand("sql @0", [GetSqlParameter(db, 32)]));
            Assert.AreEqual("DECLARE @0 DateTime = '" + DateTime.Today + "'\n\nsql @0", db.FormatCommand("sql @0", [GetSqlParameter(db, DateTime.Today)]));

            var guid = Guid.NewGuid();
            Assert.AreEqual("DECLARE @0 UniqueIdentifier = '" + guid + "'\n\nsql @0", db.FormatCommand("sql @0", [GetSqlParameter(db, guid)]));
        }

        [Test]
        public void FormattingWithSqlParameterThatIsNotNullSqlServerDatabase()
        {
            Assert.AreEqual("DECLARE @0 NVarChar(4000) = 'value'\n\nsql @0", sqlDb.FormatCommand("sql @0", [GetSqlParameter(sqlDb, "value")]));
            Assert.AreEqual("DECLARE @0 Int = '32'\n\nsql @0", sqlDb.FormatCommand("sql @0", [GetSqlParameter(sqlDb, 32)]));
            Assert.AreEqual("DECLARE @0 DateTime = '" + DateTime.Today + "'\n\nsql @0", sqlDb.FormatCommand("sql @0", [GetSqlParameter(sqlDb, DateTime.Today)]));

            var guid = Guid.NewGuid();
            Assert.AreEqual("DECLARE @0 UniqueIdentifier = '" + guid + "'\n\nsql @0", sqlDb.FormatCommand("sql @0", [GetSqlParameter(sqlDb, guid)]));
        }

        [Test]
        public void FormattingWithNullValue()
        {
            Assert.AreEqual("DECLARE @0 NVarChar = null\n\nsql @0", db.FormatCommand("sql @0", [GetSqlParameter(db, null)]));
        }

        [Test]
        public void FormattingWithNullValueSqlServerDatabase()
        {
            Assert.AreEqual("DECLARE @0 NVarChar = null\n\nsql @0", sqlDb.FormatCommand("sql @0", [GetSqlParameter(sqlDb, null)]));
        }

        [Test]
        public void FormattingWithStringValue()
        {
            Assert.AreEqual("DECLARE @0 NVarChar(4000) = 'value'\n\nsql @0", db.FormatCommand("sql @0", [GetSqlParameter(db, "value")]));
        }

        [Test]
        public void FormattingWithStringValueSqlServerDatabase()
        {
            Assert.AreEqual("DECLARE @0 NVarChar(4000) = 'value'\n\nsql @0", sqlDb.FormatCommand("sql @0", [GetSqlParameter(sqlDb, "value")]));
        }

        private DbParameter GetSqlParameter(IDatabase db, object value)
        {
            var sqlParam = new SqlParameter();
            sqlParam.ParameterName = "@0";
            ParameterHelper.SetParameterValue(db.DatabaseType, sqlParam, value);
            return sqlParam;
        }

        public class MyDb2 : NPoco.SqlServer.SqlServerDatabase
        {
            public MyDb2()
                : base("test")
            {
            }
        }

        public class MyDb : Database
        {
            public MyDb()
                : base("test", new SqlServerDatabaseType(), SqlClientFactory.Instance)
            {
            }
        }
    }
}
