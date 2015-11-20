using System.Threading.Tasks;
using NPoco.Tests.Common;
using NUnit.Framework;

namespace NPoco.Tests.Async
{
    [TestFixture]
    public class QueryAsyncTests : BaseDBFuentTest
    {
        [Test]
        public async Task QueryAsync()
        {
            var users = await Database.Query<User>().Where(x => x.UserId == 1).ToListAsync();
            Assert.AreEqual(1, users.Count);
            Assert.AreEqual(1, users[0].UserId);
        }

        [Test]
        public async Task SingleAsync()
        {
            var user = await Database.Query<User>().Where(x => x.UserId == 1).SingleAsync();
            Assert.AreEqual(1, user.UserId);
        }

        [Test]
        public async Task CountAsync()
        {
            var userCount = await Database.Query<User>().CountAsync();
            Assert.AreEqual(15, userCount);
        }

        [Test]
        public async Task FetchByExpressionAndSelect()
        {
            var users = await Database.Query<User>().ProjectToAsync(x => new { x.Name });
            Assert.AreEqual("Name1", users[0].Name);
        }

        [Test]
        public async Task PagingAsync()
        {
            var records = await Database.PageAsync<User>(2, 5, "SELECT u.* FROM Users u WHERE UserID <= 15");
            Assert.AreEqual(records.Items.Count, 5);
        }
    }
}
