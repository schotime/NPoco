using NPoco.Tests.Common;
using NUnit.Framework;

namespace NPoco.Tests
{
    public class GetterOnlyTests : BaseDBDecoratedTest
    {
        [Test]
        public void GetOnlyProperties()
        {
            var result = Database.Single<GetzOnly>("select 'aaa' Name1, 'bbb' Name2");
            Assert.AreEqual("aaa", result.Name1);
            Assert.AreEqual("bbb", result.Name2);
        }

        public class GetzOnly
        {
            public string Name1 { get; }
            public string Name2 { get; } = "Default";
        }
    }
}