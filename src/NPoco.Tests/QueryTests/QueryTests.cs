using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using NPoco.DatabaseTypes;
using NPoco.FluentMappings;
using NPoco.Tests.Common;
using NUnit.Framework;

namespace NPoco.Tests.QueryTests
{
    public class QueryTests
    {
        public IDatabase Database { get; set; }
        public TestDatabase TestDatabase { get; set; }

        public List<User> InMemoryUsers { get; set; }
        public List<ExtraInfo> InMemoryExtraUserInfos { get; set; }

        [SetUp]
        public void SetUpFixture()
        {
            var types = new[] { typeof(User), typeof(ExtraInfo) };
            FluentMappingConfiguration.Scan(s =>
            {
                s.Assembly(typeof(User).Assembly);
                s.IncludeTypes(types.Contains);
                s.WithSmartConventions();
            });

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
        public void CleanUpFixture()
        {
            if (TestDatabase == null) return;

            TestDatabase.CleanupDataBase();
            TestDatabase.Dispose();
        }

        private void InsertData()
        {
            InMemoryUsers = new List<User>();
            InMemoryExtraUserInfos = new List<ExtraInfo>();
            for (int i = 0; i < 15; i++)
            {
                var user = new User()
                {
                    Name = "Name" + (i + 1),
                    Age = 20 + (i + 1),
                    DateOfBirth = new DateTime(1970, 1, 1).AddYears(i + 1),
                    Savings = 50.00m + (1.01m * (i + 1))
                };
                InMemoryUsers.Add(user);
                Database.Insert(user);

                var extra = new ExtraInfo()
                {
                    UserId = user.UserId,
                    Email = "email@email.com" + (i + 1),
                    Children = (i + 1)
                };
                InMemoryExtraUserInfos.Add(extra);
                Database.Insert(extra);
            }
        }

        protected void AssertUserValues(User expected, User actual)
        {
            Assert.AreEqual(expected.UserId, actual.UserId);
            Assert.AreEqual(expected.Name, actual.Name);
            Assert.AreEqual(expected.Age, actual.Age);
            Assert.AreEqual(expected.DateOfBirth, actual.DateOfBirth);
            Assert.AreEqual(expected.Savings, actual.Savings);
        }
    }
}
