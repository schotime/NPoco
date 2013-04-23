using System.Linq;
using NPoco.Tests.Common;
using NUnit.Framework;

namespace NPoco.Tests.FluentTests.QueryTests
{
    [TestFixture]
    //[NUnit.Framework.Ignore("Appearently the decorated syntax and fluent syntax are some how conflicting.")]
    public class AdvancedFetchFluentTest : BaseDBFuentTest
    {
        [Test]
        public void FetchWithComplexObjectFilledAsExpected()
        {
            var user = Database.Fetch<UserWithExtraInfo, ExtraUserInfo>("select u.*, e.* from users u inner join extrauserinfos e on u.userid = e.userid where u.userid = 1").Single();

            Assert.NotNull(user.ExtraUserInfo);
            Assert.AreEqual(InMemoryExtraUserInfos[0].ExtraUserInfoId, user.ExtraUserInfo.ExtraUserInfoId);
            Assert.AreEqual(InMemoryExtraUserInfos[0].UserId, user.ExtraUserInfo.UserId);
            Assert.AreEqual(InMemoryExtraUserInfos[0].Email, user.ExtraUserInfo.Email);
            Assert.AreEqual(InMemoryExtraUserInfos[0].Children, user.ExtraUserInfo.Children);
        }

        [Test]
        public void FetchWithComplexReturnsNullExtraUserInfoPropertyIfAllColumnsNull()
        {
            var user = Database.Fetch<UserWithExtraInfo, ExtraUserInfo>("select u.*, e.* from users u left join extrauserinfos e on u.userid = -1 where u.userid = 1").Single();

            Assert.Null(user.ExtraUserInfo);
            Assert.True(user.UserId > 0);
        }

        [Test]
        public void FetchWithComplexReturnsSecondObjectIfFirstIsNull()
        {
            var user = Database.Fetch<UserWithExtraInfo, ExtraUserInfo>("select u.*, e.* from extrauserinfos u left join users e on u.userid = -1 where u.userid = 1").Single();

            Assert.NotNull(user.ExtraUserInfo);
            Assert.True(user.UserId == 0);
        }


        [Test]
        public void FetchWithAllNullsReturnsNonNullObject()
        {
            var user = Database.Fetch<UserWithExtraInfo>("select e.* from users u left join extrauserinfos e on u.userid = -1 where u.userid = 1").Single();

            Assert.NotNull(user);
        }
    }
}
