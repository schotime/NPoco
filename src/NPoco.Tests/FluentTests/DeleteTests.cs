using NPoco.Tests.Common;
using NUnit.Framework;

namespace NPoco.Tests.FluentTests
{
    [TestFixture]
    public class DeleteTests : BaseDBFuentTest
    {
        [Test]
        public void TestDelete()
        {
            Database.Delete<User>(1);
            var user1 = Database.SingleOrDefaultById<User>(1);
            Assert.IsNull(user1);
        }
    }
}
