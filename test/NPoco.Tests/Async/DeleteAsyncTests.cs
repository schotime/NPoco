﻿using System;
using System.IO;
using System.Threading;
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

        [Test]
        public async Task DeletePrimaryKeyObject_WithCancelledCancellationToken_ShouldThrow()
        {
            try
            {
                var poco = await Database.QueryAsync<User>().Where(x => x.UserId == InMemoryUsers[1].UserId)
                    .SingleOrDefault();
                Assert.IsNotNull(poco);

                var source = new CancellationTokenSource();
                source.Cancel();

                Assert.ThrowsAsync<TaskCanceledException>(() => Database.DeleteAsync(poco, source.Token));

                var verify = await Database.QueryAsync<User>().Where(x => x.UserId == InMemoryUsers[1].UserId)
                    .SingleOrDefault();
                Assert.IsNotNull(verify);
            }
            catch (Exception ex)
            {
                Assert.Null(ex);
            }
        }

        [Test]
        public async Task DeletePrimaryKeyObject_WithCancellationToken_ShouldNotThrow()
        {
            try
            {
                var poco = await Database.QueryAsync<User>().Where(x => x.UserId == InMemoryUsers[1].UserId)
                    .SingleOrDefault();
                Assert.IsNotNull(poco);

                var source = new CancellationTokenSource();

                await Database.DeleteAsync(poco, source.Token);

                var verify = await Database.QueryAsync<User>().Where(x => x.UserId == InMemoryUsers[1].UserId)
                    .SingleOrDefault();
                Assert.IsNull(verify);
            }
            catch (Exception ex)
            {
                Assert.Null(ex);
            }
        }
    }
}
