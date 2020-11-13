using System;
using System.Data;
using System.Linq;
using NPoco.Tests.Common;
using NUnit.Framework;

namespace NPoco.Tests.DecoratedTests.CRUDTests
{
    [TestFixture]
    public class DeleteTests : BaseDBDecoratedTest
    {
        [Test]
        public void DeletePrimaryKeyObject()
        {
            var poco = Database.SingleOrDefaultById<UserDecorated>(InMemoryUsers[1].UserId);
            Assert.IsNotNull(poco);

            Database.Delete(poco);

            var verify = Database.SingleOrDefaultById<UserDecorated>(InMemoryUsers[1].UserId);
            Assert.IsNull(verify);
        }

        [Test]
        public void DeleteCompositeKey()
        {
            var poco = Database.SingleOrDefault<CompositeObjectDecorated>(@"
                SELECT * 
                FROM CompositeObjects
                WHERE Key1_ID = @0 AND Key2ID = @1 AND Key3ID = @2
            ", InMemoryCompositeObjects[1].Key1ID, InMemoryCompositeObjects[1].Key2ID, InMemoryCompositeObjects[1].Key3ID);
            Assert.IsNotNull(poco);

            Database.Delete(poco);

            var verify = Database.SingleOrDefault<CompositeObjectDecorated>(@"
                SELECT * 
                FROM CompositeObjects
                WHERE Key1_ID = @0 AND Key2ID = @1 AND Key3ID = @2
            ", InMemoryCompositeObjects[1].Key1ID, InMemoryCompositeObjects[1].Key2ID, InMemoryCompositeObjects[1].Key3ID);
            Assert.IsNull(verify);
        }

        [Test]
        public void DeletePrimaryKeyVersionConcurrencyException()
        {
            var poco1 = Database.SingleOrDefaultById<UserTimestampVersionDecorated>(InMemoryUsers[1].UserId);
            var poco2 = Database.SingleOrDefaultById<UserTimestampVersionDecorated>(InMemoryUsers[1].UserId);

            poco1.Age = 100;
            Database.Update(poco1);

            Assert.Throws<DBConcurrencyException>(() => Database.Delete(poco2));
        }


        [Test]
        public void DeletePrimaryKeyVersionIntConcurrencyException()
        {
            var poco1 = Database.SingleOrDefaultById<UserIntVersionDecorated>(InMemoryUsers[1].UserId);
            var poco2 = Database.SingleOrDefaultById<UserIntVersionDecorated>(InMemoryUsers[1].UserId);

            poco1.Age = 100;
            Database.Update(poco1);

            Assert.Throws<DBConcurrencyException>(() => Database.Delete(poco2));
        }
    }
}