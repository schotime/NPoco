using System;
using System.Collections.Generic;
using System.Configuration;
using NPoco.DatabaseTypes;
using NUnit.Framework;

namespace NPoco.Tests.Common
{
    public class BaseDBDecoratedTest : BaseDBTest
    {
        public List<UserDecorated> InMemoryUsers { get; set; }
        public List<ExtraUserInfoDecorated> InMemoryExtraUserInfos { get; set; }

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
                case 3: // SQL Server
                    TestDatabase = new SQLLocalDatabase();
                    Database = new Database(TestDatabase.Connection, new SqlServer2012DatabaseType());
                    break;

                case 4:
                case 5:
                case 6:
                    Assert.Fail("Database platform not supported for unit testing");
                    return;

                default:
                    Assert.Fail("Unknown database platform specified");
                    return;
            }

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
            for (var i = 0; i < 15; i++)
            {
                var user = new UserDecorated
                {
                    Name = "Name" + (i + 1),
                    Age = 20 + (i + 1),
                    DateOfBirth = new DateTime(1970, 1, 1).AddYears(i + 1),
                    Savings = 50.00m + (1.01m * (i + 1))
                };
                InMemoryUsers.Add(user);
                Database.Insert(user);

                var extra = new ExtraUserInfoDecorated
                {
                    UserId = user.UserId,
                    Email = "email@email.com" + (i + 1),
                    Children = (i + 1)
                };
                InMemoryExtraUserInfos.Add(extra);
                Database.Insert(extra);
            }
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
