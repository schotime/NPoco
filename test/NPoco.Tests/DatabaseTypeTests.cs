using System;
using System.Collections.Generic;
using System.Diagnostics;
using NPoco;
using NPoco.DatabaseTypes;
using NUnit.Framework;

namespace NPoco.Tests
{
    [TestFixture]
    public class DatabaseTypeTests
    {
        [Test]
        public void AddOrderByIfNoneFound()
        {
            var dbType = new SqlServer2012DatabaseType();

            var args = new object[] {1};
            var pagedQuery = dbType.BuildPageQuery(0, 10, new PagingHelper.SQLParts { sql = "select * from test where id = @0" }, ref args);

            Assert.AreEqual(pagedQuery, "select * from test where id = @0\nORDER BY (SELECT NULL)\nOFFSET @1 ROWS FETCH NEXT @2 ROWS ONLY");
            Assert.AreEqual(args, new object[] {1, 0, 10});
        }

        [Test]
        public void AddPagingOnWithExistingOrderBy()
        {
            var dbType = new SqlServer2012DatabaseType();

            var args = new object[] { 1 };
            var pagedQuery = dbType.BuildPageQuery(0, 10, new PagingHelper.SQLParts { sql = "select * from test where id = @0 order by 1" }, ref args);

            Assert.AreEqual(pagedQuery, "select * from test where id = @0 order by 1\nOFFSET @1 ROWS FETCH NEXT @2 ROWS ONLY");
            Assert.AreEqual(args, new object[] { 1, 0, 10 });
        }

        [Test]
        public void SupportNestedOrderBys()
        {
            var dbType = new SqlServer2012DatabaseType();

            var args = new object[] { 1 };
            var pagedQuery = dbType.BuildPageQuery(0, 10, new PagingHelper.SQLParts
            {
                sql = "select * from test outer apply (select top 1 1 from test2 order by 2) where id = @0"
            }, ref args);

            Assert.AreEqual("select * from test outer apply (select top 1 1 from test2 order by 2) where id = @0\nORDER BY (SELECT NULL)\nOFFSET @1 ROWS FETCH NEXT @2 ROWS ONLY", pagedQuery);
            Assert.AreEqual(args, new object[] { 1, 0, 10 });
        }
    }
}
