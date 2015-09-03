using System.Linq;
using NPoco.Tests.Common;
using NUnit.Framework;

namespace NPoco.Tests.DecoratedTests.QueryTests
{
    [TestFixture]
    public class AdvancedFetchDecoratedTest : BaseDBDecoratedTest
    {
        [Test]
        public void FetchWithComplexObjectFilledAsExpectedWithExplicitNaming()
        {
            var user = Database.Fetch<UserDecoratedWithExtraInfo>("select u.*, e.ExtraUserInfoId as ExtraUserInfo__ExtraUserInfoId,e.UserId as ExtraUserInfo__UserId,e.Email as ExtraUserInfo__Email,e.Children as ExtraUserInfo__Children from users u inner join extrauserinfos e on u.userid = e.userid where u.userid = 1").Single();

            Assert.NotNull(user.ExtraUserInfo);
            Assert.AreEqual(InMemoryExtraUserInfos[0].ExtraUserInfoId, user.ExtraUserInfo.ExtraUserInfoId);
            Assert.AreEqual(InMemoryExtraUserInfos[0].UserId, user.ExtraUserInfo.UserId);
            Assert.AreEqual(InMemoryExtraUserInfos[0].Email, user.ExtraUserInfo.Email);
            Assert.AreEqual(InMemoryExtraUserInfos[0].Children, user.ExtraUserInfo.Children);
        }

        [Test]
        public void FetchWithComplexObjectFilledAsExpectedUsingOldConvention()
        {
            var user = Database.Fetch<UserDecoratedWithExtraInfo>("select u.*, e.* from users u inner join extrauserinfos e on u.userid = e.userid where u.userid = 1").Single();

            Assert.NotNull(user.ExtraUserInfo);
            Assert.AreEqual(InMemoryExtraUserInfos[0].ExtraUserInfoId, user.ExtraUserInfo.ExtraUserInfoId);
            Assert.AreEqual(InMemoryExtraUserInfos[0].UserId, user.ExtraUserInfo.UserId);
            Assert.AreEqual(InMemoryExtraUserInfos[0].Email, user.ExtraUserInfo.Email);
            Assert.AreEqual(InMemoryExtraUserInfos[0].Children, user.ExtraUserInfo.Children);
        }

        [Test]
        public void FetchWithComplexObjectFilledAsExpectedUsingNewConvention()
        {
            var user = Database.Fetch<UserDecoratedWithExtraInfo>("select u.*, null npoco_ExtraUserInfo, e.* from users u inner join extrauserinfos e on u.userid = e.userid where u.userid = 1").Single();

            Assert.NotNull(user.ExtraUserInfo);
            Assert.AreEqual(InMemoryExtraUserInfos[0].ExtraUserInfoId, user.ExtraUserInfo.ExtraUserInfoId);
            Assert.AreEqual(InMemoryExtraUserInfos[0].UserId, user.ExtraUserInfo.UserId);
            Assert.AreEqual(InMemoryExtraUserInfos[0].Email, user.ExtraUserInfo.Email);
            Assert.AreEqual(InMemoryExtraUserInfos[0].Children, user.ExtraUserInfo.Children);
        }
    }
}
