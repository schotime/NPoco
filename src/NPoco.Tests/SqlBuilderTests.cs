using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace NPoco.Tests
{
    [TestFixture]
    public class SqlBuilderTests
    {
        [Test]
        public void Test1()
        {
            var sqlBuilder = new SqlBuilder();
            var temp = sqlBuilder.AddTemplate("select * from test where /**where**/ and id = @0", 1);

            sqlBuilder.Where("id2 = @0", 2);

            Assert.AreEqual(2, temp.Parameters.Length);
            Assert.AreEqual("select * from test where  ( id2 = @1 )\n and id = @0", temp.RawSql);
        }

        [Test]
        public void Test2()
        {
            var sqlBuilder = new SqlBuilder();
            var temp = sqlBuilder.AddTemplate("select * from test where /**where**/ and id = @0", 1);

            Assert.AreEqual(1, temp.Parameters.Length);
            Assert.AreEqual("select * from test where  1=1  and id = @0", temp.RawSql);
        }

        [Test]
        public void Test3()
        {
            var sqlBuilder = new SqlBuilder();
            var temp = sqlBuilder.AddTemplate("select * from test where /**where**/ and id = @0", 1);

            var test = new[] {1, 2};
            sqlBuilder.Where("id2 in (@test)", new { test });

            Assert.AreEqual("select * from test where  ( id2 in (@0,@1) )\n and id = @0", temp.RawSql);
            Assert.AreEqual(2, temp.Parameters.Length);

            Console.WriteLine(temp.RawSql);
        }
        
        [Test]
        public void Test4()
        {
            var sqlBuilder = new SqlBuilder();
            var temp = sqlBuilder.AddTemplate("select * from test where id = @0", 1);

            Assert.AreEqual(1, temp.Parameters.Length);
            Assert.AreEqual("select * from test where id = @0", temp.RawSql);
        }

        [Test]
        public void Test5()
        {
            var sqlBuilder = new SqlBuilder();
            var temp = sqlBuilder.AddTemplate("select * from test where /**where**/ and id = @0", 1);
            var temp2 = sqlBuilder.AddTemplate("select * from test2 where /**where**/ and id = @0", 1);

            Assert.AreEqual(1, temp.Parameters.Length);
            Assert.AreEqual("select * from test where  1=1  and id = @0", temp.RawSql);

            sqlBuilder.Where("id2 = @0", 2);

            Assert.AreEqual(2, temp2.Parameters.Length);
            Assert.AreEqual("select * from test2 where  ( id2 = @1 )\n and id = @0", temp2.RawSql);
        }
        [Test]
        public void Test6()
        {
            var sqlBuilder = new SqlBuilder();
            var temp = sqlBuilder.AddTemplate("select * from test where /**where**/ and id = @0", 1);
            var temp2 = sqlBuilder.AddTemplate("select * from test2 where /**where**/ and id = @0", 1);

            sqlBuilder.Where("id2 = @0", 2);

            Sql sql1 = temp;
            Assert.AreEqual(2, sql1.Arguments.Length);
            Assert.AreEqual("select * from test where  ( id2 = @1 )\n and id = @0", sql1.SQL);

            sqlBuilder.Where("id3 = @0", 3);

            Sql sql2 = temp2;
            Assert.AreEqual(3, sql2.Arguments.Length);
            Assert.AreEqual("select * from test2 where  ( id2 = @1 AND id3 = @2 )\n and id = @0", sql2.SQL);
        }
    }
}
