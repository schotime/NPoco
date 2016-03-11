using System.Linq;
using NPoco.Tests.Common;
using NPoco.Tests.NewMapper.Models;
using NUnit.Framework;

namespace NPoco.Tests.NewMapper
{
    public class IncludesWithLinqProvider : BaseDBFuentTest
    {
        [Test]
        public void Test1()
        {
            var result = Database
                .Query<User>()
                .Include(x => x.House)
                .Include(x => x.ExtraUserInfo)
                .ToEnumerable()
                .ToList();

            for (int i = 0; i < result.Count; i++)
            {
                AssertUserValues(InMemoryUsers[i], result[i]);
                AssertExtraUserInfo(InMemoryExtraUserInfos[i], result[i].ExtraUserInfo);
                AssertUserHouseValues(InMemoryUsers[i], result[i]);
            }
        }

        [Test]
        public void Test2()
        {
            var result = Database
                .Query<User>()
                .Include(x => x.ExtraUserInfo)
                .ToDynamicList();

            Assert.AreEqual(1, result[0].extrauserinfo__extrauserinfoid);
        }

        [Test]
        public void Test3()
        {
            var result = Database
                .Query<One>()
                .IncludeMany(x => x.Items)
                .ToList();

            Assert.AreEqual(null, result[0].Items);
            Assert.AreEqual(1, result[1].Items.Count);
            Assert.AreEqual(2, result[2].Items.Count);
        }
    }
}