using System.Linq;
using NPoco.Tests.Common;
using NUnit.Framework;

namespace NPoco.Tests.DecoratedTests.QueryTests
{
    [TestFixture]
    //[NUnit.Framework.Ignore("Appearently the decorated syntax and fluent syntax are some how conflicting.")]
    public class NestedNestedFetchDecoratedTests : BaseDBDecoratedTest
    {
        [Test]
        public void FetchWithComplexObjectFilledAsExpected()
        {
            var user = Database.Fetch<NestedUser1, NestedUser2, NestedUser3>("select '1' Name, '2' Name, '3' Name").Single();

            Assert.AreEqual("1", user.Name);
            Assert.AreEqual("2", user.User2.Name);
            Assert.AreEqual("3", user.User2.User3.Name);
        }

        [Test]
        public void FetchWithComplexObjectFilledAsExpectedWhenBaseIsNull()
        {
            var user = Database.Fetch<NestedUser1, NestedUser2, NestedUser3>("select null Name, '2' Name, '3' Name").Single();

            Assert.AreEqual(null, user.Name);
            Assert.AreEqual("2", user.User2.Name);
            Assert.AreEqual("3", user.User2.User3.Name);
        }

        [Test]
        public void FetchWithComplexObjectFilledAsExpectedWhenNestedIsNull()
        {
            var user = Database.Fetch<NestedUser1, NestedUser2, NestedUser3>("select '1' Name, null Name, '3' Name").Single();

            Assert.AreEqual("1", user.Name);
            Assert.Null(user.User2);
        }

        [Test]
        public void FetchWithComplexObjectFilledAsExpectedWhenNestedNestedIsNull()
        {
            var user = Database.Fetch<NestedUser1, NestedUser2, NestedUser3>("select '1' Name, '2' Name, null Name").Single();

            Assert.AreEqual("1", user.Name);
            Assert.AreEqual("2", user.User2.Name);
            Assert.Null(user.User2.User3);
        }

        public class NestedUser1
        {
            public string Name { get; set; }
            public NestedUser2 User2 { get; set; }
        }

        public class NestedUser2
        {
            public string Name { get; set; }
            public NestedUser3 User3 { get; set; }
        }

        public class NestedUser3
        {
            public string Name { get; set; }
        }
    }
}
