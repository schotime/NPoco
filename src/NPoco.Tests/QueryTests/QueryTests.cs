using System;
using System.Collections.Generic;
using System.Linq;
using NPoco.FluentMappings;
using NPoco.Tests.Common;
using NUnit.Framework;

namespace NPoco.Tests.QueryTests
{
    public class QueryTests
    {
        public IDatabase Database { get; set; }
        public InMemoryDatabase InMemoryDB { get; set; }
        
        public List<User> InMemoryUsers { get; set; }
        public List<ExtraInfo> InMemoryExtraUserInfos { get; set; }

        [TestFixtureSetUp]
        public void SetUpFixture()
        {
            var types = new[] {typeof (User), typeof(ExtraInfo)};
            FluentMappingConfiguration.Scan(s =>
            {
                s.Assembly(typeof(User).Assembly);
                s.IncludeTypes(types.Contains);
                s.WithSmartConventions();
            });

            InMemoryDB = new InMemoryDatabase();
            Database = new Database(InMemoryDB.Connection);

            Database.Execute("CREATE TABLE Users(UserId INTEGER PRIMARY KEY, Name nvarchar(200), Age int, DateOfBirth datetime, Savings Decimal(10,5));");
            Database.Execute("CREATE TABLE ExtraInfos(ExtraInfoId INTEGER PRIMARY KEY, UserId int, Email nvarchar(200), Children int);");

            InsertData();
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
