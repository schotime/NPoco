using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using NPoco.DatabaseTypes;
using NPoco.FluentMappings;
using NUnit.Framework;

namespace NPoco.Tests.Common
{
    public class BaseDBFuentTest : BaseDBTest
    {
        public List<User> InMemoryUsers { get; set; }
        public List<ExtraUserInfo> InMemoryExtraUserInfos { get; set; }

        [SetUp]
        public void SetUp()
        {
            var types = new[] { typeof(User), typeof(ExtraUserInfo) };
            var dbFactory = new DatabaseFactory();
            dbFactory.Config().WithFluentConfig(
                FluentMappingConfiguration.Scan(s =>
                {
                    s.Assembly(typeof (User).Assembly);
                    s.IncludeTypes(types.Contains);
                    s.WithSmartConventions();
                    s.OverrideMappingsWith(new FluentMappingOverrides());
                })
            );

            var testDBType = Convert.ToInt32(ConfigurationManager.AppSettings["TestDBType"]);
            switch (testDBType)
            {
                case 1: // SQLite In-Memory
                    TestDatabase = new InMemoryDatabase();
                    Database = dbFactory.Build(new Database(TestDatabase.Connection));
                    break;

                case 2: // SQL Local DB
                case 3: // SQL Server
                    TestDatabase = new SQLLocalDatabase();
                    Database = dbFactory.Build(new Database(TestDatabase.Connection, new SqlServer2008DatabaseType()));
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
            InMemoryUsers = new List<User>();
            InMemoryExtraUserInfos = new List<ExtraUserInfo>();
            for (var i = 0; i < 15; i++)
            {
                var user = new User
                {
                    Name = "Name" + (i + 1),
                    Age = 20 + (i + 1),
                    DateOfBirth = new DateTime(1970, 1, 1).AddYears(i + 1),
                    Savings = 50.00m + (1.01m * (i + 1)),
                    IsMale = (i%2 == 0)
                };
                Database.Insert(user);
                InMemoryUsers.Add(user);

                var extra = new ExtraUserInfo
                {
                    UserId = user.UserId,
                    Email = "email" + (i + 1) + "@email.com",
                    Children = (i + 1)
                };
                Database.Insert(extra);
                InMemoryExtraUserInfos.Add(extra);
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

    public class FluentMappingOverrides : Mappings
    {
        public FluentMappingOverrides()
        {
            For<User>().Columns(x => x.Column(y => y.IsMale).WithName("is_male"));
        }
    }
}
