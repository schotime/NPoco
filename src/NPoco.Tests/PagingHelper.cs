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

        [Test]
        public void ExtractOrderbyCorrectlyWithWhereWithOrderBy()
        {
            var sql = @"select test.*, (select top 1 d from test2 order by c) from test where (select top 1 e from test2 order by e) > 5 order by a, b";

            PagingHelper.SQLParts parts;
            PagingHelper.SplitSQL(sql, out parts);

            Assert.AreEqual("order by a, b", parts.sqlOrderBy);
            Assert.AreEqual("SELECT COUNT(*) FROM (select test.*, (select top 1 d from test2 order by c) from test where (select top 1 e from test2 order by e) > 5 ) peta_tbl", parts.sqlCount);
        }

        [Test]
        public void ExtractOrderbyCorrectlyWithComplexOrderBy()
        {
            var sql = @"select test.* from test order by len(a), b";

            PagingHelper.SQLParts parts;
            PagingHelper.SplitSQL(sql, out parts);

            Assert.AreEqual("order by len(a), b", parts.sqlOrderBy);
            Assert.AreEqual("SELECT COUNT(*) FROM (select test.* from test ) peta_tbl", parts.sqlCount);
        }

        [Test]
        public void EnsureTheSubselectOrderByIsNotConsumed()
        {
            var sql = @"select test.*, (select top 1 d from test2 order by c) from test";

            PagingHelper.SQLParts parts;
            PagingHelper.SplitSQL(sql, out parts);

            Assert.AreEqual(null, parts.sqlOrderBy);
            Assert.AreEqual("SELECT COUNT(*) FROM (select test.*, (select top 1 d from test2 order by c) from test) peta_tbl", parts.sqlCount);
        }
    }
}
