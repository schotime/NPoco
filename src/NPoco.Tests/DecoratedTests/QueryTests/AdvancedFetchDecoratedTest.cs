using System.Linq;
using NPoco.Tests.Common;
using NUnit.Framework;

namespace NPoco.Tests.DecoratedTests.QueryTests
{
    [TestFixture]
    //[NUnit.Framework.Ignore("Appearently the decorated syntax and fluent syntax are some how conflicting.")]
    public class AdvancedFetchDecoratedTest : BaseDBDecoratedTest
    {
        [Test]
        public void FetchWithComplexObjectFilledAsExpected()
        {
            var user = Database.Fetch<UserDecoratedWithExtraInfo, ExtraUserInfoDecorated>("select u.*, e.* from users u inner join extrauserinfos e on u.userid = e.userid where u.userid = 1").Single();

            Assert.NotNull(user.ExtraUserInfo);
            Assert.AreEqual(InMemoryExtraUserInfos[0].ExtraUserInfoId, user.ExtraUserInfo.ExtraUserInfoId);
            Assert.AreEqual(InMemoryExtraUserInfos[0].UserId, user.ExtraUserInfo.UserId);
            Assert.AreEqual(InMemoryExtraUserInfos[0].Email, user.ExtraUserInfo.Email);
            Assert.AreEqual(InMemoryExtraUserInfos[0].Children, user.ExtraUserInfo.Children);
        }
    }
}
