using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NPoco.Tests.Common;
using NUnit.Framework;

namespace NPoco.Tests.Async
{
    [TestFixture]
    public class QueryAsyncTests : BaseDBFluentTest
    {
        [Test]
        public async Task QueryAsync()
        {
            var users = await Database.Query<User>().Where(x => x.UserId == 1).ToListAsync();
            Assert.AreEqual(1, users.Count);
            Assert.AreEqual(1, users[0].UserId);
        }

        [Test]
        public async Task QueryAsync_WithCancellationToken_ShouldNotThrow()
        {
            var source = new CancellationTokenSource();
            var users = await Database.Query<User>().Where(x => x.UserId == 1).ToListAsync(source.Token);
            Assert.AreEqual(1, users.Count);
            Assert.AreEqual(1, users[0].UserId);
        }

        [Test]
        public void QueryAsync_WithCancelledCancellationToken_ShouldThrow()
        {
            var source = new CancellationTokenSource();
            source.Cancel();

            Assert.ThrowsAsync<TaskCanceledException>(() =>
                Database.Query<User>().Where(x => x.UserId == 1).ToListAsync(source.Token));
        }

        [Test]
        public async Task SingleAsync()
        {
            var user = await Database.Query<User>().Where(x => x.UserId == 1).SingleAsync();
            Assert.AreEqual(1, user.UserId);
        }

        [Test]
        public async Task SingleAsync_WithCancellationToken_ShouldNotThrow()
        {
            var source = new CancellationTokenSource();
            var user = await Database.Query<User>().Where(x => x.UserId == 1).SingleAsync(source.Token);
            Assert.AreEqual(1, user.UserId);
        }

        [Test]
        public void SingleAsync_WithCancelledCancellationToken_ShouldThrow()
        {
            var source = new CancellationTokenSource(1);
            source.Cancel();

            Assert.ThrowsAsync<TaskCanceledException>(() =>
                Database.Query<User>().Where(x => x.UserId == 1).SingleAsync(source.Token));
        }

        [Test]
        public async Task CountAsync()
        {
            var userCount = await Database.Query<User>().CountAsync();
            Assert.AreEqual(15, userCount);
        }

        [Test]
        public async Task CountAsync_WithCancellationToken_ShouldNotThrow()
        {
            var source = new CancellationTokenSource();
            var userCount = await Database.Query<User>().CountAsync(source.Token);
            Assert.AreEqual(15, userCount);
        }

        [Test]
        public void CountAsync_WithCancelledCancellationToken_ShouldThrow()
        {
            var source = new CancellationTokenSource();
            source.Cancel();

            Assert.ThrowsAsync<TaskCanceledException>(() =>
                Database.Query<User>().CountAsync(source.Token));
        }

        [Test]
        public async Task FetchByExpressionAndSelect()
        {
            var users = await Database.Query<User>().ProjectToAsync(x => new { x.Name });
            Assert.AreEqual("Name1", users[0].Name);
        }

        [Test]
        public async Task FetchByExpressionAndSelect_WithCancellationToken_ShouldNotThrow()
        {
            var source = new CancellationTokenSource();
            var users = await Database.Query<User>().ProjectToAsync(x => new { x.Name }, source.Token);
            Assert.AreEqual("Name1", users[0].Name);
        }

        [Test]
        public void FetchByExpressionAndSelect_WithCancelledCancellationToken_ShouldThrow()
        {
            var source = new CancellationTokenSource();
            source.Cancel();
            Assert.ThrowsAsync<OperationCanceledException>(() => Database.Query<User>().ProjectToAsync(x => new {x.Name}, source.Token));
        }

        [Test]
        public async Task PagingAsync()
        {
            var records = await Database.PageAsync<User>(2, 5, "SELECT u.* FROM Users u WHERE UserID <= 15");
            Assert.AreEqual(records.Items.Count, 5);
        }

        [Test]
        public async Task PagingAsync_WithCancellationToken_ShouldNotThrow()
        {
            var source = new CancellationTokenSource();
            var records = await Database.PageAsync<User>(2, 5, "SELECT u.* FROM Users u WHERE UserID <= 15", source.Token);
            Assert.AreEqual(records.Items.Count, 5);
        }

        [Test]
        public void PagingAsync_WithCancelledCancellationToken_ShouldThrow()
        {
            var source = new CancellationTokenSource();
            source.Cancel();
            Assert.ThrowsAsync<TaskCanceledException>(() => Database.PageAsync<User>(2, 5, "SELECT u.* FROM Users u WHERE UserID <= 15", source.Token));
        }

        [Test]
        public async Task FetchMultipleAsync()
        {
            var (users, houses) = await Database.FetchMultipleAsync<User, House>("select * from users;select * from houses");
            Assert.AreEqual(15, users.Count);
            Assert.AreEqual(6, houses.Count);
        }

        [Test]
        public async Task FetchMultipleAsync_WithCancellationToken_ShouldNotThrow()
        {
            var source = new CancellationTokenSource();
            var (users, houses) = await Database.FetchMultipleAsync<User, House>("select * from users;select * from houses", source.Token);
            Assert.AreEqual(15, users.Count);
            Assert.AreEqual(6, houses.Count);
        }

        [Test]
        public void FetchMultipleAsync_WithCancelledCancellationToken_ShouldThrow()
        {
            var source = new CancellationTokenSource();
            source.Cancel();
            Assert.ThrowsAsync<TaskCanceledException>(() => Database.FetchMultipleAsync<User, House>("select * from users;select * from houses", source.Token));
        }

        [Test]
        public async Task QueryAsyncSql()
        {
            var i = 1;
            var userCount = Database.QueryAsync<User>("select * from users");
            await foreach (var item in userCount)
            {
                Assert.AreEqual(item.UserId, i++);
            }
        }

        [Test]
        public async Task QueryAsyncSql_WithCancellationToken_ShouldNotThrow()
        {
            var source = new CancellationTokenSource();
            var i = 1;
            var userCount = Database.QueryAsync<User>("select * from users", source.Token);
            await foreach (var item in userCount)
            {
                Assert.AreEqual(item.UserId, i++);
            }
        }

        [Test]
        public void QueryAsyncSql_WithCancelledCancellationToken_ShouldThrow()
        {
            var source = new CancellationTokenSource();
            source.Cancel();
            var i = 1;
            var userCount = Database.QueryAsync<User>("select * from users", source.Token);
            Assert.ThrowsAsync<TaskCanceledException>(async () =>
            {
                await foreach (var item in userCount)
                {
                    Assert.AreEqual(item.UserId, i++);
                }
            });
        }

        [Test]
        public async Task QueryAsyncFirst()
        {
            var user = await Database.FirstOrDefaultAsync<User>("where userid = @0", 1);
            Assert.AreEqual(1, user.UserId);
            var user1 = await Database.FirstAsync<User>("where userid = @0", 1);
            Assert.AreEqual(1, user1.UserId);
        }

        [Test]
        public async Task QueryAsyncFirst_WithCancellationToken_ShouldNotThrow()
        {
            var source = new CancellationTokenSource();
            var user = await Database.FirstOrDefaultAsync<User>("where userid = @0", source.Token, 1);
            Assert.AreEqual(1, user.UserId);
            var user1 = await Database.FirstAsync<User>("where userid = @0", source.Token, 1);
            Assert.AreEqual(1, user1.UserId);
        }

        [Test]
        public void QueryAsyncFirst_WithCancelledCancellationToken_ShouldThrow()
        {
            var source = new CancellationTokenSource();
            source.Cancel();

            Assert.ThrowsAsync<TaskCanceledException>(() =>
                Database.FirstOrDefaultAsync<User>("where userid = @0", source.Token, 1));
            Assert.ThrowsAsync<TaskCanceledException>(() =>
                Database.FirstAsync<User>("where userid = @0", source.Token, 1));
        }
    }
}
