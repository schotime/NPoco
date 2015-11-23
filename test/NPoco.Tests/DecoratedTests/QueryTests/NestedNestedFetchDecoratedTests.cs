using System.Linq;
using NPoco;
using NPoco.Tests.Common;
using NUnit.Framework;

namespace NPoco.Tests.DecoratedTests.QueryTests
{
    [TestFixture]
    public class NestedNestedFetchDecoratedTests : BaseDBDecoratedTest
    {
        [Test]
        public void FetchWithComplexObjectFilledAsExpected()
        {
            var user = Database.Fetch<NestedUser1>("select '1' Name, '2' User2__Name, '3' User2__User3__Name /*poco_dual*/").Single();

            Assert.AreEqual("1", user.Name);
            Assert.AreEqual("2", user.User2.Name);
            Assert.AreEqual("3", user.User2.User3.Name);
        }

        [Test]
        public void FetchWithComplexObjectFilledAsExpectedWhenBaseIsNull()
        {
            var user = Database.Fetch<NestedUser1>("select null Name, '2' User2__Name, '3' User2__User3__Name /*poco_dual*/").Single();

            Assert.AreEqual(null, user.Name);
            Assert.AreEqual("2", user.User2.Name);
            Assert.AreEqual("3", user.User2.User3.Name);
        }

        [Test]
        public void FetchWithComplexObjectFilledAsExpectedWhenNestedIsNull()
        {
            var user = Database.Fetch<NestedUser1>("select '1' Name, null User2__Name, '3' User2__User3__Name /*poco_dual*/").Single();

            Assert.AreEqual("1", user.Name);
            Assert.NotNull(user.User2);
        }

        [Test]
        public void FetchWithComplexObjectFilledAsExpectedWhenNestedNestedIsNull()
        {
            var user = Database.Fetch<NestedUser1>("select '1' Name, '2' User2__Name, null User2__User3__Name /*poco_dual*/").Single();

            Assert.AreEqual("1", user.Name);
            Assert.AreEqual("2", user.User2.Name);
            Assert.Null(user.User2.User3);
        }

        public class NestedUser1
        {
            public string Name { get; set; }
            [ComplexMapping]
            public NestedUser2 User2 { get; set; }
        }

        public class NestedUser2
        {
            public string Name { get; set; }
            [ComplexMapping]
            public NestedUser3 User3 { get; set; }
        }

        public class NestedUser3
        {
            public string Name { get; set; }
        }
    }
}
