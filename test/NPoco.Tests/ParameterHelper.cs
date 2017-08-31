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

        [Test]
        public void TestMultipleInClauses()
        {
            var sql = "SELECT * FROM test WHERE testID in (@0) and test2ID in (@0)";

            var list = new List<int>() { 1, 2, 3 };
            var args = new List<object>();
            var resultSql = ParameterHelper.ProcessParams(sql, new[] { list }, args);

            var expectedSql = "SELECT * FROM test WHERE testID in (@0,@1,@2) and test2ID in (@0,@1,@2)";

            Assert.AreEqual(expectedSql, resultSql);
        }

        [Test]
        public void TestMultipleInClausesPerf()
        {
            var sql = "SELECT * FROM test WHERE testID in (@0) and test2ID in (@0) and asdf = @1 and asdf = @2 and asdf = @3 and asdf = @4 and asdf = @5";

            var list = new List<int>() { 1, 2, 3, 6, 5, 4 };
            var args = new List<object>();

            var expectedSql = "SELECT * FROM test WHERE testID in (@0,@1,@2,@3,@4,@5) and test2ID in (@0,@1,@2,@3,@4,@5) and asdf = @6 and asdf = @7 and asdf = @8 and asdf = @9 and asdf = @10";

            var resultSql = ParameterHelper.ProcessParams(sql, new object[] { list, 1,2,3,4,5 }, args);

            Assert.AreEqual(expectedSql, resultSql);
        }
    }
}
