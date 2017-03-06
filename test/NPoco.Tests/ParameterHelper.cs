using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace NPoco.Tests
{
    public class ParameterHelperTests
    {
        [Test]
        public void TestGenericDictionaryAsParameter()
        {
            var sql = "SELECT * FROM test WHERE testID = @testID AND testName = @testName";
            const string testName = "testName";
            const int testID = 1;
            
            var dict = new Dictionary<string, object>();
            dict["testName"] = testName;
            dict["testID"] = testID;
            
            var args = new List<object>();
            var resultSql = ParameterHelper.ProcessParams(sql, new[] {dict}, args);

            Assert.AreEqual(testID, args[0]);
            Assert.AreEqual(testName, args[1]);
            Assert.AreEqual("SELECT * FROM test WHERE testID = @0 AND testName = @1", resultSql);
        }

        [Test]
        public void TestEmptyListInParameters()
        {
            var sql = "SELECT * FROM test WHERE testID in (@0)";

            var list = new List<int>();
            var args = new List<object>();
            var resultSql = ParameterHelper.ProcessParams(sql, new[] { list }, args);

            Assert.AreEqual(default(int), args[0]);
        }

        [Test]
        public void TestExpandListInParameters()
        {
            var sql = "SELECT * FROM test WHERE testID in (@0)";

            var list = new List<int>() { 1, 2, 1 };
            var args = new List<object>();
            var resultSql = ParameterHelper.ProcessParams(sql, new[] { list }, args);

            Assert.AreEqual(sql.Replace("@0", "@0,@1,@0"), resultSql);
            Assert.AreEqual(1, args[0]);
            Assert.AreEqual(2, args[1]);
        }

        [Test]
        public void DontDuplicateParametersWithTheSameName()
        {
            var sql = "SELECT * FROM test WHERE testID in (@0, @1, @0)";

            var args = new List<object>();
            var resultSql = ParameterHelper.ProcessParams(sql, new object[] { 99, 89 }, args);

            Assert.AreEqual(2, args.Count);
            Assert.AreEqual(sql, resultSql);
            Assert.AreEqual(99, args[0]);
        }

        [Test]
        public void DontDuplicateParametersWithTheSameNameStrings()
        {
            var sql = "SELECT * FROM test WHERE testID in (@test, @wow, @test)";

            var args = new List<object>();
            var resultSql = ParameterHelper.ProcessParams(sql, new[] { new {test = 99, wow = 89} }, args);

            Assert.AreEqual(2, args.Count);
            Assert.AreEqual(sql.Replace("@test", "@0").Replace("@wow", "@1"), resultSql);
            Assert.AreEqual(99, args[0]);
            Assert.AreEqual(89, args[1]);
        }
    }
}
