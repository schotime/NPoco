using System.Collections.Generic;
using NPoco;
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
    }
}
