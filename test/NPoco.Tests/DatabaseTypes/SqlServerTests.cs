using System;
using NPoco.DatabaseTypes;
using NUnit.Framework;

namespace NPoco.Tests.DatabaseTypes
{
    [TestFixture]
    public class SqlServerTests
    {
        [Test]
        public void BuildPageQueryTest()
        {
            var dbtype = new SqlServerDatabaseType();

            PagingHelper.SQLParts parts;
            Assert.IsTrue(PagingHelper.SplitSQL("SELECT [table].[id], [table].[name] AS [alias] FROM [table] ORDER BY [table].[name]", out parts));

            Assert.AreEqual("SELECT [table].[id], [table].[name] AS [alias] FROM [table] ORDER BY [table].[name]", parts.sql);
            Assert.AreEqual("*", parts.sqlColumns);
            Assert.AreEqual("SELECT COUNT(*) FROM (SELECT [table].[id], [table].[name] AS [alias] FROM [table] ) npoco_tbl", parts.sqlCount);
            Assert.AreEqual("ORDER BY [table].[name]", parts.sqlOrderBy);
            Assert.AreEqual("[table].[id], [table].[name] AS [alias] FROM [table] ORDER BY [table].[name]", parts.sqlSelectRemoved);
            Assert.AreEqual("SELECT [table].[id], [table].[name] AS [alias] FROM [table] ", parts.sqlUnordered);

            var args = new object[0];
            var sql = dbtype.BuildPageQuery(25, 5, parts, ref args);

            // MUST have ORDER BY [alias] there, NOT BY [name]
            Console.WriteLine(sql);
            Assert.AreEqual("SELECT * FROM (SELECT poco_base.*, ROW_NUMBER() OVER (ORDER BY [alias]) poco_rn \n"
                + "FROM ( \n"
                + "SELECT [table].[id], [table].[name] AS [alias] FROM [table] ) poco_base ) poco_paged \n"
                + "WHERE poco_rn > @0 AND poco_rn <= @1 \n"
                + "ORDER BY poco_rn", sql);
        }
    }
}
