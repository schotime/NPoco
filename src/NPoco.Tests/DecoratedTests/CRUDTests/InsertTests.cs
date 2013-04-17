using System;
using NPoco.Tests.Common;
using NUnit.Framework;

namespace NPoco.Tests.DecoratedTests.CRUDTests
{
    [TestFixture]
    public class InsertTests : BaseDBDecoratedTest
    {
        [Test]
        public void InsertPrimaryKeyAutoIncrement()
        {
            const string dataName = "John Doe";
            const int dataAge = 56;
            const decimal dataSavings = (decimal)345.23;
            var dataDateOfBirth = DateTime.Now;

            var poco = new UserDecorated();
            poco.Name = dataName;
            poco.Age = dataAge;
            poco.Savings = dataSavings;
            poco.DateOfBirth = dataDateOfBirth;
            Database.Insert(poco);

            Assert.IsTrue(poco.UserId > 0, "POCO failed to insert.");

            var verify = Database.SingleOrDefaultById<UserDecorated>(poco.UserId);
            Assert.IsNotNull(verify);

            Assert.AreEqual(poco.UserId, verify.UserId);
            Assert.AreEqual(dataName, verify.Name);
            Assert.AreEqual(dataAge, verify.Age);
            Assert.AreEqual(dataSavings, verify.Savings);
        }

        [Test]
        public void InsertPrimaryKeyAssigned()
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
            Database.Insert(poco);

            Assert.IsTrue(poco.Key1ID > 0, "POCO failed to insert.");

            var verify = Database.SingleOrDefaultById<AssignedPkObjectDecorated>(poco.Key1ID);
            Assert.IsNotNull(verify);

            Assert.AreEqual(dataKey1ID, verify.Key1ID);
            Assert.AreEqual(dataKey2ID, verify.Key2ID);
            Assert.AreEqual(dataKey3ID, verify.Key3ID);
            Assert.AreEqual(dataTextData, verify.TextData);
        }

        [Test]
        public void InsertCompositeKey()
        {
            const int dataKey1ID = 100;
            const int dataKey2ID = 200;
            const int dataKey3ID = 300;
            const string dataTextData = "This is some text data.";
            var dataDateCreated = DateTime.Now;

            var poco = new CompositeObjectDecorated();
            poco.Key1ID = dataKey1ID;
            poco.Key2ID = dataKey2ID;
            poco.Key3ID = dataKey3ID;
            poco.TextData = dataTextData;
            poco.DateEntered = dataDateCreated;
            Database.Insert(poco);

            var verify = Database.SingleOrDefault<CompositeObjectDecorated>(@"
                SELECT * 
                FROM CompositeObjects
                WHERE Key1ID = @0 AND Key2ID = @1 AND Key3ID = @2
            ", dataKey1ID, dataKey2ID, dataKey3ID);
            Assert.IsNotNull(verify);

            Assert.AreEqual(dataKey1ID, verify.Key1ID);
            Assert.AreEqual(dataKey2ID, verify.Key2ID);
            Assert.AreEqual(dataKey3ID, verify.Key3ID);
            Assert.AreEqual(dataTextData, verify.TextData);
        }
    }
}