using System;
using System.Threading.Tasks;
using NPoco.Tests.Common;
using NUnit.Framework;

namespace NPoco.Tests.Async
{
    public class BaseDbFluentTestsWithNoData : BaseDBFuentTest
    {
        protected override void InsertData()
        {
        }
    }

    [TestFixture]
    public class InsertAsyncTests : BaseDbFluentTestsWithNoData
    {
        [Test]
        public async Task InsertPocoIntoDatabaseUsingInsertAsync()
        {
            var user = new User()
            {
                Age = 10,
                DateOfBirth = DateTime.Now
            };

            var pk = await Database.InsertAsync(user);

            var userDb = Database.Query<User>().Where(x => x.UserId == user.UserId).Single();
            Assert.AreEqual(user.Age, userDb.Age);
            Assert.AreEqual(pk, userDb.UserId);
            Assert.AreEqual(user.UserId, userDb.UserId);
        }

        [Test]
        public async Task InsertTwoPocoIntoDatabaseUsingInsertAsync()
        {
            var user1 = new User() { Age = 10, DateOfBirth = DateTime.Now };
            var user2 = new User() { Age = 11, DateOfBirth = DateTime.Now };

            var pk1 = await Database.InsertAsync(user1);
            var userDb1 = Database.Query<User>().Where(x => x.UserId == user1.UserId).Single();
            Assert.AreEqual(user1.Age, userDb1.Age);
            Assert.AreEqual(pk1, userDb1.UserId);

            var pk2 = await Database.InsertAsync(user2);
            var userDb2 = Database.Query<User>().Where(x => x.UserId == user2.UserId).Single();
            Assert.AreEqual(user2.Age, userDb2.Age);
            Assert.AreEqual(pk2, userDb2.UserId);
        }
        
        //[Test, NUnit.Framework.Ignore("LocalDB cannot insert more than one at a time")]
        public void InsertTwoPocoIntoDatabaseUsingInsertAsyncWaitingForAll1()
        {
            var user1 = new User() { Age = 10, DateOfBirth = DateTime.Now };
            var user2 = new User() { Age = 11, DateOfBirth = DateTime.Now };

            var task1 = Database.InsertAsync(user1).ContinueWith(y =>
            {
                var userDb1 = Database.Query<User>().Where(x => x.UserId == user1.UserId).Single();
                Assert.AreEqual(user1.Age, userDb1.Age);
                Console.WriteLine(user1.Age);
            });
            var task2 = Database.InsertAsync(user2).ContinueWith(y =>
            {
                var userDb2 = Database.Query<User>().Where(x => x.UserId == user2.UserId).Single();
                Assert.AreEqual(user2.Age, userDb2.Age);
                Console.WriteLine(user2.Age);
            });

            Task.WaitAll(task1, task2);
        }
    }
}
