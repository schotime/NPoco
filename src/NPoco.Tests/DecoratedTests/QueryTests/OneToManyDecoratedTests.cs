using System;
using System.Collections.Generic;
using System.Linq;
using NPoco.Tests.Common;
using NUnit.Framework;

namespace NPoco.Tests.DecoratedTests.QueryTests
{
    [TestFixture]
    public class OneToManyDecoratedTest : BaseDBDecoratedTest
    {
        [Test]
        public void OneToManyWillFillsList()
        {
            var user = Database.FetchOneToMany<UserDecoratedWithExtraInfoAsList, ExtraUserInfoDecorated>(
                x => x.UserId,
                x => x.ExtraUserInfoId,
                "select u.*, e.* from users u left join extrauserinfos e on u.userid = e.userId where u.userid = 1").Single();

            Assert.NotNull(user.ExtraUserInfo);
            Assert.AreEqual(1, user.ExtraUserInfo.Count);
            Assert.AreEqual(InMemoryExtraUserInfos[0].ExtraUserInfoId, user.ExtraUserInfo[0].ExtraUserInfoId);
            Assert.AreEqual(InMemoryExtraUserInfos[0].UserId, user.ExtraUserInfo[0].UserId);
            Assert.AreEqual(InMemoryExtraUserInfos[0].Email, user.ExtraUserInfo[0].Email);
            Assert.AreEqual(InMemoryExtraUserInfos[0].Children, user.ExtraUserInfo[0].Children);
        }

        [Test]
        public void OneToManyWillNullManyDoestException()
        {
            var user = Database.FetchOneToMany<UserDecoratedWithExtraInfoAsList, ExtraUserInfoDecorated>(
                x=>x.UserId,
                x=>x.ExtraUserInfoId,
                "select u.*, e.* from users u left join extrauserinfos e on u.userid = -1 where u.userid = 1").Single();

            Assert.False(user.ExtraUserInfo.Any());
        }
      
    }
}