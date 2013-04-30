using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using NPoco.DatabaseTypes;
using NUnit.Framework;

namespace NPoco.Tests.Common
{
    public class BaseDBDecoratedTest : BaseDBTest
    {
        public List<UserDecorated> InMemoryUsers { get; set; }
        public List<ExtraUserInfoDecorated> InMemoryExtraUserInfos { get; set; }
        public List<CompositeObjectDecorated> InMemoryCompositeObjects { get; set; }


        [SetUp]
        public void SetUp()
        {
            var testDBType = Convert.ToInt32(ConfigurationManager.AppSettings["TestDBType"]);
            switch (testDBType)
            {
                case 1: // SQLite In-Memory
                    TestDatabase = new InMemoryDatabase();
                    Database = new Database(TestDatabase.Connection);
                    break;

                case 2: // SQL Local DB
                    TestDatabase = new SQLLocalDatabase();
                    Database = new Database(TestDatabase.Connection, new SqlServer2008DatabaseType(), IsolationLevel.ReadUncommitted); // Need read uncommitted for the transaction tests
                    break;

                case 3: // SQL Server
                case 4: // SQL CE
                case 5: // MySQL
                case 6: // Oracle
                case 7: // Postgres
                    Assert.Fail("Database platform not supported for unit testing");
                    return;

                default:
                    Assert.Fail("Unknown database platform specified");
                    return;
            }

            // Insert test data
            InsertData();
        }

        [TearDown]
        public void CleanUp()
        {
            if (TestDatabase == null) return;

            TestDatabase.CleanupDataBase();
            TestDatabase.Dispose();
        }

        protected void InsertData()
        {
            InMemoryUsers = new List<UserDecorated>();
            InMemoryExtraUserInfos = new List<ExtraUserInfoDecorated>();
            InMemoryCompositeObjects = new List<CompositeObjectDecorated>();

            for (var i = 0; i < 15; i++)
            {
                var pos = i + 1;

                var user = new UserDecorated
                {
                    Name = "Name" + (i + 1),
                    Age = 20 + (i + 1),
                    DateOfBirth = new DateTime(1970, 1, 1).AddYears(i + 1),
                    Savings = 50.00m + (1.01m * (i + 1)),
                    IsMale = (i%2==0)
                };
                Database.Insert(user);
                InMemoryUsers.Add(user);

                var extra = new ExtraUserInfoDecorated
                {
                    UserId = user.UserId,
                    Email = "email" + (i + 1) + "@email.com",
                    Children = (i + 1)
                };
                Database.Insert(extra);
                InMemoryExtraUserInfos.Add(extra);

                var composite = new CompositeObjectDecorated
                {
                    Key1ID = pos,
                    Key2ID = i + 2,
                    Key3ID = i + 4,
                    TextData = "This is some text data.",
                    DateEntered = DateTime.Now
                };
                Database.Insert(composite);
                InMemoryCompositeObjects.Add(composite);
            }

            // Verify DB record counts
            var userCount = Database.ExecuteScalar<int>("SELECT COUNT(UserId) FROM Users");
            Assert.AreEqual(InMemoryUsers.Count, userCount, "Test User Data not in sync db has " + userCount + " records, but the in memory copy has only " + InMemoryUsers.Count + " records.");
            System.Diagnostics.Debug.WriteLine("Created " + userCount + " test users for the unit tests.");

            var userExtraInfoCount = Database.ExecuteScalar<int>("SELECT COUNT(ExtraUserInfoId) FROM ExtraUserInfos");
            Assert.AreEqual(InMemoryExtraUserInfos.Count, userExtraInfoCount, "Test User Extra Info Data not in sync db has " + userExtraInfoCount + " records, but the in memory copy has only " + InMemoryExtraUserInfos.Count + " records.");
            System.Diagnostics.Debug.WriteLine("Created " + userExtraInfoCount + " test extra user info records for the unit tests.");

            var compositeObjectCount = Database.ExecuteScalar<int>("SELECT COUNT(Key1ID) FROM CompositeObjects");
            Assert.AreEqual(InMemoryCompositeObjects.Count, compositeObjectCount, "Test Composite Object Data not in sync db has " + compositeObjectCount + " records, but the in memory copy has only " + InMemoryCompositeObjects.Count + " records.");
            System.Diagnostics.Debug.WriteLine("Created " + compositeObjectCount + " test composite PK objects for the unit tests.");
        }

        protected void AssertUserValues(UserDecorated expected, UserDecorated actual)
        {
            Assert.AreEqual(expected.UserId, actual.UserId);
            Assert.AreEqual(expected.Name, actual.Name);
            Assert.AreEqual(expected.Age, actual.Age);
            Assert.AreEqual(expected.DateOfBirth, actual.DateOfBirth);
            Assert.AreEqual(expected.Savings, actual.Savings);
        }
    }
}
