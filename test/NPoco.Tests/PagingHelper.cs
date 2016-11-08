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
            Assert.AreEqual("SELECT COUNT(*) FROM (select test.*, (select top 1 d from test2 order by c) from test ) npoco_tbl", parts.sqlCount);
        }

        [Test]
        public void ExtractOrderbyCorrectlyWithWhereWithOrderBy()
        {
            var sql = @"select test.*, (select top 1 d from test2 order by c) from test where (select top 1 e from test2 order by e) > 5 order by a, b";

            PagingHelper.SQLParts parts;
            PagingHelper.SplitSQL(sql, out parts);

            Assert.AreEqual("order by a, b", parts.sqlOrderBy);
            Assert.AreEqual("SELECT COUNT(*) FROM (select test.*, (select top 1 d from test2 order by c) from test where (select top 1 e from test2 order by e) > 5 ) npoco_tbl", parts.sqlCount);
        }

        [Test]
        public void ExtractOrderbyCorrectlyWithComplexOrderBy()
        {
            var sql = @"select test.* from test order by len(a), b";

            PagingHelper.SQLParts parts;
            PagingHelper.SplitSQL(sql, out parts);

            Assert.AreEqual("order by len(a), b", parts.sqlOrderBy);
            Assert.AreEqual("SELECT COUNT(*) FROM (select test.* from test ) npoco_tbl", parts.sqlCount);
        }


        [Test]
        public void ExtractOrderbyCorrectlyWithMultiline()
        {
            var sql = "select test.* from test order by len(a),\r\nb";

            PagingHelper.SQLParts parts;
            PagingHelper.SplitSQL(sql, out parts);

            Assert.AreEqual("order by len(a),\r\nb", parts.sqlOrderBy);
            Assert.AreEqual("SELECT COUNT(*) FROM (select test.* from test ) npoco_tbl", parts.sqlCount);
        }

        [Test]
        public void EnsureTheSubselectOrderByIsNotConsumed()
        {
            var sql = @"select test.*, (select top 1 d from test2 order by c) from test";

            PagingHelper.SQLParts parts;
            PagingHelper.SplitSQL(sql, out parts);

            Assert.AreEqual(null, parts.sqlOrderBy);
            Assert.AreEqual("SELECT COUNT(*) FROM (select test.*, (select top 1 d from test2 order by c) from test) npoco_tbl", parts.sqlCount);
        }

        [Test]
        public void EnsureTheOrderIsExtractedWhenThereIsAcolumnCalledFrom()
        {
            var sql = @"select id, date_from from test1 order by date_from";

            PagingHelper.SQLParts parts;
            PagingHelper.SplitSQL(sql, out parts);

            Assert.AreEqual("order by date_from", parts.sqlOrderBy);
            Assert.AreEqual("SELECT COUNT(*) FROM (select id, date_from from test1 ) npoco_tbl", parts.sqlCount);
        }

        [Test]
        public void EnsureTheOrderIsExtractedWhenThereIsAcolumnCalledFromWithNestedSubselect()
        {
            var sql = @"select id, date_from from(select id, date_from from test1) a order by date_from";

            PagingHelper.SQLParts parts;
            PagingHelper.SplitSQL(sql, out parts);

            Assert.AreEqual("order by date_from", parts.sqlOrderBy);
            Assert.AreEqual("SELECT COUNT(*) FROM (select id, date_from from(select id, date_from from test1) a ) npoco_tbl", parts.sqlCount);
        }
    }
}
