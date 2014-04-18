using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace NPoco.Tests
{
    public class PagingHelperTests
    {
        [Test]
        public void ExtractOrderbyCorrectly()
        {
            var sql = @"select test.*, (select top 1 d from test2 order by c) from test order by a, b";

            PagingHelper.SQLParts parts;
            PagingHelper.SplitSQL(sql, out parts);

            Assert.AreEqual("order by a, b", parts.sqlOrderBy);
            Assert.AreEqual("SELECT COUNT(*) FROM (select test.*, (select top 1 d from test2 order by c) from test ) peta_tbl", parts.sqlCount);
        }
    }
}
