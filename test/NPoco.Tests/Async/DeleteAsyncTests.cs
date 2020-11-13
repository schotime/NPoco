using System;
using System.IO;
using System.Threading.Tasks;
using NPoco.Tests.Common;
using NUnit.Framework;

namespace NPoco.Tests.Async
{
    public class DeleteAsyncTests : BaseDBFluentTest
    {
        [Test]
        public async Task DeletePrimaryKeyObject()
        {
            var poco = await Database.QueryAsync<User>().Where(x => x.UserId == InMemoryUsers[1].UserId).SingleOrDefault();
            Assert.IsNotNull(poco);

            await Database.DeleteAsync(poco);

            var verify = await Database.QueryAsync<User>().Where(x => x.UserId == InMemoryUsers[1].UserId).SingleOrDefault();
            Assert.IsNull(verify);
        }
    }
}
