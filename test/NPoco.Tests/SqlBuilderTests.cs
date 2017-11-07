using System;
using System.Collections.Generic;
using NPoco;
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
            Assert.AreEqual("select * from test where ( id2 = @1 )\n and id = @0", temp.RawSql);
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

            Assert.AreEqual("select * from test where ( id2 in (@0,@1) )\n and id = @0", temp.RawSql);
            Assert.AreEqual(2, temp.Parameters.Length);
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
            Assert.AreEqual("select * from test2 where ( id2 = @1 )\n and id = @0", temp2.RawSql);
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
            Assert.AreEqual("select * from test where ( id2 = @1 )\n and id = @0", sql1.SQL);

            sqlBuilder.Where("id3 = @0", 3);

            Sql sql2 = temp2;
            Assert.AreEqual(3, sql2.Arguments.Length);
            Assert.AreEqual("select * from test2 where ( id2 = @1 ) AND ( id3 = @2 )\n and id = @0", sql2.SQL);
        }

        [Test]
        public void Test7()
        {
            var sqlBuilder = new SqlBuilder();
            var temp = sqlBuilder.AddTemplate("select * from test where /**where**/ and id = @0", 1);
            temp.TokenReplacementRequired = true;

            Assert.Throws<Exception>(() =>
            {
                var sql = temp.RawSql;
            });
        }

        [Test]
        public void Test8()
        {
            var sqlBuilder = new SqlBuilder();
            var temp = sqlBuilder.AddTemplate("select * from test where /**where**/ and /**where(test)**/ and id = @0", 1);

            sqlBuilder.WhereNamed("test", "id2 = @0", 2);

            Assert.AreEqual(2, temp.Parameters.Length);
            Assert.AreEqual("select * from test where  1=1  and ( id2 = @1 )\n and id = @0", temp.RawSql);
        }

        [Test]
        public void Test9()
        {
            var sqlBuilder = new SqlBuilder();
            var temp = sqlBuilder.AddTemplate("select * from test where /**where(test)**/ and id = @0", 1);

            Assert.AreEqual(1, temp.Parameters.Length);
            Assert.AreEqual("select * from test where  1=1  and id = @0", temp.RawSql);
        }

        [Test]
        public void Test10()
        {
            var sqlBuilder = new SqlBuilder();
            var temp = sqlBuilder.AddTemplate("select * from test where /**where**/ and id = @0", 1);

            sqlBuilder.Where("id2 = 2 OR id2 = 3");
            sqlBuilder.Where("id2 = 4");

            Assert.AreEqual(1, temp.Parameters.Length);
            Assert.AreEqual("select * from test where ( id2 = 2 OR id2 = 3 ) AND ( id2 = 4 )\n and id = @0", temp.RawSql);
        }

        [Test]
        public void Test11()
        {
            var sqlBuilder = new SqlBuilder(new Dictionary<string, string>()
                                            {
                                                {"where(test)", null}
                                            });
            
            var temp = sqlBuilder.AddTemplate("select * from test where /**where(test)**/ and id = @0", 1);

            sqlBuilder.Where("id2 = 4");

            Assert.AreEqual(1, temp.Parameters.Length);
            Assert.AreEqual("select * from test where /**where(test)**/ and id = @0", temp.RawSql);
        }

        [Test]
        public void Test12()
        {
            var sqlBuilder = new SqlBuilder(new Dictionary<string, string>()
                                            {
                                                {"where(test)", "1<>1"}
                                            });

            var temp = sqlBuilder.AddTemplate("select * from test where /**where(test)**/ and id = @0", 1);

            sqlBuilder.Where("id2 = 4");

            Assert.AreEqual(1, temp.Parameters.Length);
            Assert.AreEqual("select * from test where  1<>1  and id = @0", temp.RawSql);
        }

        [Test]
        public void Test13()
        {
            var sqlBuilder = new SqlBuilder(new Dictionary<string, string>()
                                            {
                                                {"where", "1<>2"}
                                            });

            var temp = sqlBuilder.AddTemplate("select * from test where /**where**/ and id = @0", 1);

            Assert.AreEqual(1, temp.Parameters.Length);
            Assert.AreEqual("select * from test where  1<>2  and id = @0", temp.RawSql);
        }

        [Test]
        public void Test14()
        {
            var builder = new SqlBuilder();

            var template = builder.AddTemplate("SELECT /**select**/, COUNT(*) FROM test /**groupby**/");

            Assert.AreEqual("SELECT  1 , COUNT(*) FROM test ", template.RawSql);
        }
    }
}
