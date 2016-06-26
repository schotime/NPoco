using System;
using System.Data;
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
        public void UpdatePrimaryKeyObjectOverridingPrimaryKey()
        {
            var poco = Database.SingleOrDefaultById<UserDecorated>(InMemoryUsers[1].UserId);
            Assert.IsNotNull(poco);

            poco.Age = InMemoryUsers[1].Age + 100;
            poco.Savings = (Decimal)1234.23;
            Database.Update(poco, InMemoryUsers[2].UserId);

            var verify = Database.SingleOrDefaultById<UserDecorated>(InMemoryUsers[2].UserId);
            Assert.IsNotNull(verify);

            Assert.AreEqual(InMemoryUsers[2].UserId, verify.UserId);
            Assert.AreNotEqual(InMemoryUsers[2].Age, verify.Age);
            Assert.AreNotEqual(InMemoryUsers[2].Savings, verify.Savings);
        }

        [Test]
        public void UpdateCompositeKey()
        {
            const string dataTextData = "This is some updated text data.";

            var poco = Database.SingleOrDefault<CompositeObjectDecorated>(@"
                SELECT * 
                FROM CompositeObjects
                WHERE Key1_ID = @0 AND Key2ID = @1 AND Key3ID = @2
            ", InMemoryCompositeObjects[1].Key1ID, InMemoryCompositeObjects[1].Key2ID, InMemoryCompositeObjects[1].Key3ID);
            Assert.IsNotNull(poco);

            poco.TextData = dataTextData;
            Database.Update(poco);

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
        }

        [Test]
        public void UpdateWithFields()
        {
            var poco = Database.SingleOrDefaultById<UserDecorated>(1);
            Assert.IsNotNull(poco);

            poco.Age = poco.Age + 100;
            poco.Savings = (Decimal)1234.23;
            Database.Update(poco, x=>x.Age);

            var verify = Database.SingleOrDefaultById<UserDecorated>(1);
            Assert.IsNotNull(verify);

            Assert.AreEqual(poco.UserId, verify.UserId);
            Assert.AreEqual(poco.Name, verify.Name);
            Assert.AreNotEqual(InMemoryUsers[0].Age, verify.Age);
            Assert.AreNotEqual(poco.Savings, verify.Savings);
        }

        [Test]
        public void UpdatePrimaryKeyVersionConcurrencyException()
        {
            var poco1 = Database.SingleOrDefaultById<UserTimestampVersionDecorated>(InMemoryUsers[1].UserId);
            var poco2 = Database.SingleOrDefaultById<UserTimestampVersionDecorated>(InMemoryUsers[1].UserId);
            
            poco1.Age = 100;
            Database.Update(poco1);

            poco2.Age = 200;
#if DNXCORE50
            Assert.Throws<Exception>(() => Database.Update(poco2));
#else
            Assert.Throws<DBConcurrencyException>(() => Database.Update(poco2));
#endif

        }

        [Test]
        public void UpdatePrimaryKeyNoVersionConcurrencyException()
        {
            var poco1 = Database.SingleOrDefaultById<UserTimestampVersionDecorated>(InMemoryUsers[1].UserId);
            
            poco1.Age = 100;
            Database.Update(poco1);

            var poco2 = Database.SingleOrDefaultById<UserTimestampVersionDecorated>(InMemoryUsers[1].UserId);

            poco2.Age = 200;
            Database.Update(poco2);

            var verify = Database.SingleOrDefaultById<UserTimestampVersionDecorated>(InMemoryUsers[1].UserId);

            Assert.AreEqual(200, verify.Age);
        }

        [Test]
        public void UpdatePrimaryKeyVersionIntConcurrencyException()
        {
            var poco1 = Database.SingleOrDefaultById<UserIntVersionDecorated>(InMemoryUsers[1].UserId);
            var poco2 = Database.SingleOrDefaultById<UserIntVersionDecorated>(InMemoryUsers[1].UserId);

            poco1.Age = 100;
            Database.Update(poco1);

            poco2.Age = 200;
#if DNXCORE50
            Assert.Throws<Exception>(() => Database.Update(poco2));
#else
            Assert.Throws<DBConcurrencyException>(() => Database.Update(poco2));
#endif
        }

        [Test]
        public void UpdatePrimaryKeyNoVersionIntConcurrencyException()
        {
            var poco1 = Database.SingleOrDefaultById<UserIntVersionDecorated>(InMemoryUsers[1].UserId);

            poco1.Age = 100;
            Database.Update(poco1);

            var poco2 = Database.SingleOrDefaultById<UserIntVersionDecorated>(InMemoryUsers[1].UserId);

            poco2.Age = 200;
            Database.Update(poco2);

            var verify = Database.SingleOrDefaultById<UserIntVersionDecorated>(InMemoryUsers[1].UserId);

            Assert.AreEqual(200, verify.Age);
        }
    }
}