using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NPoco.Tests.Common;
using NUnit.Framework;

namespace NPoco.Tests.Async
{
    public class UpdateAsyncTests : BaseDBFluentTest
    {
        [Test]
        public async Task UpdatePrimaryKeyObject()
        {
            var poco = await Database.QueryAsync<User>().Where(x=>x.UserId == InMemoryUsers[1].UserId).SingleOrDefault();
            Assert.IsNotNull(poco);

            poco.Age = InMemoryUsers[1].Age + 100;
            poco.Savings = (Decimal)1234.23;
            await Database.UpdateAsync(poco);

            var verify = await Database.QueryAsync<User>().Where(x => x.UserId == InMemoryUsers[1].UserId).SingleOrDefault();
            Assert.IsNotNull(verify);

            Assert.AreEqual(InMemoryUsers[1].UserId, verify.UserId);
            Assert.AreEqual(InMemoryUsers[1].Name, verify.Name);
            Assert.AreNotEqual(InMemoryUsers[1].Age, verify.Age);
            Assert.AreNotEqual(InMemoryUsers[1].Savings, verify.Savings);
        }

        [Test]
        public async Task UpdateBatchAsyncTest()
        {
            var users = InMemoryUsers.Select(x => UpdateBatch.For(x, Database.StartSnapshot(x))).Select(x => { x.Poco.Age = 30; return x; });
            var updated = await Database.UpdateBatchAsync(users, new BatchOptions() { BatchSize = 10 });
            var result = await Database.QueryAsync<User>().ToList();

            Assert.AreEqual(15, result.Count);
            foreach (var u in result)
            {
                Assert.AreEqual(30, u.Age);
            }
            Assert.AreEqual(14, updated);
        }
    }
}
