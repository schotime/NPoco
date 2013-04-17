using System;
using NPoco.Tests.Common;
using NUnit.Framework;

namespace NPoco.Tests.DecoratedTests.CRUDTests
{
    [TestFixture]
    public class UpdateTests : BaseDBDecoratedTest
    {
        [Test]
        public void UpdatePrimaryKeyObject()
        {
            var poco = Database.SingleOrDefaultById<UserDecorated>(InMemoryUsers[1].UserId);
            Assert.IsNotNull(poco);

            poco.Age = InMemoryUsers[1].Age + 100;
            poco.Savings = (Decimal)1234.23;
            Database.Update(poco);

            var verify = Database.SingleOrDefaultById<UserDecorated>(InMemoryUsers[1].UserId);
            Assert.IsNotNull(verify);

            Assert.AreEqual(InMemoryUsers[1].UserId, verify.UserId);
            Assert.AreEqual(InMemoryUsers[1].Name, verify.Name);
            Assert.AreNotEqual(InMemoryUsers[1].Age, verify.Age);
            Assert.AreNotEqual(InMemoryUsers[1].Savings, verify.Savings);
        }

        [Test]
        public void UpdateCompositeKey()
        {
            const string dataTextData = "This is some updated text data.";

            var poco = Database.SingleOrDefault<CompositeObjectDecorated>(@"
                SELECT * 
                FROM CompositeObjects
                WHERE Key1ID = @0 AND Key2ID = @1 AND Key3ID = @2
            ", InMemoryCompositeObjects[1].Key1ID, InMemoryCompositeObjects[1].Key2ID, InMemoryCompositeObjects[1].Key3ID);
            Assert.IsNotNull(poco);

            poco.TextData = dataTextData;
            Database.Update(poco);

            var verify = Database.SingleOrDefault<CompositeObjectDecorated>(@"
                SELECT * 
                FROM CompositeObjects
                WHERE Key1ID = @0 AND Key2ID = @1 AND Key3ID = @2
            ", InMemoryCompositeObjects[1].Key1ID, InMemoryCompositeObjects[1].Key2ID, InMemoryCompositeObjects[1].Key3ID);
            Assert.IsNotNull(verify);

            Assert.AreEqual(InMemoryCompositeObjects[1].Key1ID, verify.Key1ID);
            Assert.AreEqual(InMemoryCompositeObjects[1].Key2ID, verify.Key2ID);
            Assert.AreEqual(InMemoryCompositeObjects[1].Key3ID, verify.Key3ID);
            Assert.AreEqual(dataTextData, verify.TextData);
        }
    }
}