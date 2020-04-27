using System;
using System.IO;
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
    }
}
