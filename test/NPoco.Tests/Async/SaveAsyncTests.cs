using System;
using System.Threading.Tasks;
using NPoco.Tests.Common;
using NUnit.Framework;

namespace NPoco.Tests.Async
{
    [TestFixture]
    public class SaveAsyncTests : BaseDBFluentTest
    {
        [Test]
        public async Task SavePrimaryKeyAutoIncrementNew()
        {
            const string dataName = "John Doe";
            const int dataAge = 56;
            const decimal dataSavings = (decimal)345.23;
            var dataDateOfBirth = DateTime.Now;

            var poco = new UserDecorated
            {
                Name = dataName,
                Age = dataAge,
                Savings = dataSavings,
                DateOfBirth = dataDateOfBirth
            };

            await Database.SaveAsync<UserDecorated>(poco);

            Assert.IsTrue(poco.UserId > 0, "POCO failed to Save.");

            var verify = Database.SingleOrDefaultById<UserDecorated>(poco.UserId);
            Assert.IsNotNull(verify);

            Assert.AreEqual(poco.UserId, verify.UserId);
            Assert.AreEqual(dataName, verify.Name);
            Assert.AreEqual(dataAge, verify.Age);
            Assert.AreEqual(dataSavings, verify.Savings);
        }

        [Test]
        public async Task SavePrimaryKeyAutoIncrementExisting()
        {
            var poco = Database.SingleOrDefaultById<UserDecorated>(InMemoryUsers[1].UserId);
            Assert.IsNotNull(poco);

            poco.Age = InMemoryUsers[1].Age + 100;
            poco.Savings = (Decimal)1234.23;
            await Database.SaveAsync<UserDecorated>(poco);

            var verify = Database.SingleOrDefaultById<UserDecorated>(InMemoryUsers[1].UserId);
            Assert.IsNotNull(verify);

            Assert.AreEqual(InMemoryUsers[1].UserId, verify.UserId);
            Assert.AreEqual(InMemoryUsers[1].Name, verify.Name);
            Assert.AreNotEqual(InMemoryUsers[1].Age, verify.Age);
            Assert.AreNotEqual(InMemoryUsers[1].Savings, verify.Savings);
        }

        [Test]
        public async Task SavePrimaryKeyAssignedNew()
        {
            const int dataKey1ID = 100;
            const int dataKey2ID = 200;
            const int dataKey3ID = 300;
            const string dataTextData = "This is some text data.";
            var dataDateCreated = DateTime.Now;

            var poco = new AssignedPkObjectDecorated();
            poco.Key1ID = dataKey1ID;
            poco.Key2ID = dataKey2ID;
            poco.Key3ID = dataKey3ID;
            poco.TextData = dataTextData;
            poco.DateEntered = dataDateCreated;
            await Database.SaveAsync<AssignedPkObjectDecorated>(poco);

            Assert.IsTrue(poco.Key1ID > 0, "POCO failed to Save.");

            var verify = Database.SingleOrDefaultById<AssignedPkObjectDecorated>(poco.Key1ID);
            Assert.IsNotNull(verify);

            Assert.AreEqual(dataKey1ID, verify.Key1ID);
            Assert.AreEqual(dataKey2ID, verify.Key2ID);
            Assert.AreEqual(dataKey3ID, verify.Key3ID);
            Assert.AreEqual(dataTextData, verify.TextData);
        }

        [Test]
        public async Task SavePrimaryKeyAssignedExisting()
        {
            const string dataTextData = "This is some updated text data.";

            var poco = Database.SingleOrDefaultById<AssignedPkObjectDecorated>(InMemoryCompositeObjects[1].Key1ID);
            Assert.IsNotNull(poco);

            poco.TextData = dataTextData;
            await Database.SaveAsync<AssignedPkObjectDecorated>(poco);

            var verify = Database.SingleOrDefaultById<AssignedPkObjectDecorated>(InMemoryCompositeObjects[1].Key1ID);
            Assert.IsNotNull(verify);

            Assert.AreEqual(InMemoryCompositeObjects[1].Key1ID, verify.Key1ID);
            Assert.AreEqual(InMemoryCompositeObjects[1].Key2ID, verify.Key2ID);
            Assert.AreEqual(InMemoryCompositeObjects[1].Key3ID, verify.Key3ID);
            Assert.AreEqual(dataTextData, verify.TextData);
            Assert.AreNotEqual(InMemoryCompositeObjects[1].TextData, verify.TextData);
        }

        [Test]
        public async Task SaveCompositeKeyNew()
        {
            const int dataKey1ID = 100;
            const int dataKey2ID = 200;
            const int dataKey3ID = 300;
            const string dataTextData = "This is some text data.";
            var dataDateCreated = DateTime.Now;

            var poco = new CompositeObjectDecorated
            {
                Key1ID = dataKey1ID,
                Key2ID = dataKey2ID,
                Key3ID = dataKey3ID,
                TextData = dataTextData,
                DateEntered = dataDateCreated
            };

            await Database.SaveAsync<CompositeObjectDecorated>(poco);

            var verify = Database.SingleOrDefault<CompositeObjectDecorated>(@"
                SELECT * 
                FROM CompositeObjects
                WHERE Key1_ID = @0 AND Key2ID = @1 AND Key3ID = @2
            ", dataKey1ID, dataKey2ID, dataKey3ID);
            Assert.IsNotNull(verify);

            Assert.AreEqual(dataKey1ID, verify.Key1ID);
            Assert.AreEqual(dataKey2ID, verify.Key2ID);
            Assert.AreEqual(dataKey3ID, verify.Key3ID);
            Assert.AreEqual(dataTextData, verify.TextData);
        }

        [Test]
        public async Task SaveCompositeKeyNewWithSingleOrDefaultById()
        {
            const int dataKey1ID = 100;
            const int dataKey2ID = 200;
            const int dataKey3ID = 300;
            const string dataTextData = "This is some text data.";
            var dataDateCreated = DateTime.Now;

            var poco = new CompositeObjectDecorated
            {
                Key1ID = dataKey1ID,
                Key2ID = dataKey2ID,
                Key3ID = dataKey3ID,
                TextData = dataTextData,
                DateEntered = dataDateCreated
            };

            await Database.SaveAsync<CompositeObjectDecorated>(poco);

            var verify = Database.SingleOrDefaultById<CompositeObjectDecorated>(poco);
            Assert.IsNotNull(verify);

            Assert.AreEqual(dataKey1ID, verify.Key1ID);
            Assert.AreEqual(dataKey2ID, verify.Key2ID);
            Assert.AreEqual(dataKey3ID, verify.Key3ID);
            Assert.AreEqual(dataTextData, verify.TextData);
        }

        [Test]
        public async Task SaveCompositeKeyExisting()
        {
            const string dataTextData = "This is some updated text data.";

            var poco = Database.SingleOrDefault<CompositeObjectDecorated>(@"
                SELECT * 
                FROM CompositeObjects
                WHERE Key1_ID = @0 AND Key2ID = @1 AND Key3ID = @2
            ", InMemoryCompositeObjects[1].Key1ID, InMemoryCompositeObjects[1].Key2ID, InMemoryCompositeObjects[1].Key3ID);
            Assert.IsNotNull(poco);

            poco.TextData = dataTextData;
            await Database.SaveAsync<CompositeObjectDecorated>(poco);

            var verify = Database.SingleOrDefault<CompositeObjectDecorated>(@"
                SELECT * 
                FROM CompositeObjects
                WHERE Key1_ID = @0 AND Key2ID = @1 AND Key3ID = @2
            ", InMemoryCompositeObjects[1].Key1ID, InMemoryCompositeObjects[1].Key2ID, InMemoryCompositeObjects[1].Key3ID);
            Assert.IsNotNull(verify);

            Assert.AreEqual(InMemoryCompositeObjects[1].Key1ID, verify.Key1ID);
            Assert.AreEqual(InMemoryCompositeObjects[1].Key2ID, verify.Key2ID);
            Assert.AreEqual(InMemoryCompositeObjects[1].Key3ID, verify.Key3ID);
            Assert.AreEqual(dataTextData, verify.TextData);
            Assert.AreNotEqual(InMemoryCompositeObjects[1].TextData, verify.TextData);
        }
    }
}
