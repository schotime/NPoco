using System.Data;
using System.Data.SqlClient;
using NPoco;
using NUnit.Framework;

namespace NPoco.Tests
{
    public class FormatCommandTest
    {
        [Test]
        public void FormattingWithSqlParameterThatIsNotNull()
        {
            var cmd = new SqlCommand();
            cmd.Parameters.Add(new SqlParameter("Test", SqlDbType.NVarChar));
            var db = new MyDb();
            Assert.AreEqual("\n\t -> @0 [SqlParameter, String] = \"\"", db.FormatCommand(cmd));
        }

        [Test]
        public void FormattingWithNullValue()
        {
            var db = new MyDb();
            Assert.AreEqual("sql @0\n\t -> @0 [] = \"\"", db.FormatCommand("sql @0", new object[] { null }));
        }

        [Test]
        public void FormattingWithStringValue()
        {
            var db = new MyDb();
            Assert.AreEqual("sql @0\n\t -> @0 [String] = \"value\"", db.FormatCommand("sql @0", new object [] {"value"}));
        }
        
        public class MyDb : Database
        {
            public MyDb()
                : base("test", DatabaseType.SqlServer2008, SqlClientFactory.Instance)
            {
            }
        }
    }
}
