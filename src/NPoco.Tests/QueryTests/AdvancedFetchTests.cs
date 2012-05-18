using System.Linq;
using NUnit.Framework;

namespace NPoco.Tests.QueryTests
{
    [TestFixture]
    public class AdvancedFetchTests : QueryTests
    {
        [Test]
        public void FetchWithComplexObjectFilledAsExpected()
        {
            var user = Database.Fetch<UserWithExtraInfo, ExtraInfo>("select u.*, e.* from users u inner join extrainfos e on u.userid = e.userid where u.userid = 1").Single();
            
            Assert.NotNull(user.ExtraInfo);
            Assert.AreEqual(InMemoryExtraUserInfos[0].ExtraInfoId, user.ExtraInfo.ExtraInfoId);
            Assert.AreEqual(InMemoryExtraUserInfos[0].UserId, user.ExtraInfo.UserId);
            Assert.AreEqual(InMemoryExtraUserInfos[0].Email, user.ExtraInfo.Email);
            Assert.AreEqual(InMemoryExtraUserInfos[0].Children, user.ExtraInfo.Children);
        }
    }
}
