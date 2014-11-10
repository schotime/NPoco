using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using NPoco.DatabaseTypes;
using NPoco.Expressions;
using NPoco.Linq;
using NPoco.Tests.Common;
using NUnit.Framework;

namespace NPoco.Tests.FluentTests.QueryTests
{
    [TestFixture]
    public class QueryProviderTests : BaseDBFuentTest
    {
        [Test]
        public void QueryAllData()
        {
            var users = Database.Query<User>().ToList();
            Assert.AreEqual(15, users.Count);
            for (int i = 0; i < users.Count; i++)
            {
                AssertUserValues(InMemoryUsers[i], users[i]);
            }
        }

        [Test]
        public void CountQuery()
        {
            var usersCount = Database.Query<User>().Count();
            Assert.AreEqual(15, usersCount);
        }

        [Test]
        public void AnyQuery()
        {
            var userRecordsExist = Database.Query<User>().Any();
            Assert.AreEqual(true, userRecordsExist);
        }

        [Test]
        public void AnyQueryWithWhere()
        {
            var userRecordsExist = Database.Query<User>().Any(x => x.UserId == 1);
            Assert.AreEqual(true, userRecordsExist);
        }

        [Test]
        public void QueryWithWhereTrue()
        {
            var users = Database.Query<User>().Where(x => true).ToList();
            Assert.AreEqual(15, users.Count);
        }

        [Test]
        public void QueryWithWhereFalse()
        {
            var users = Database.Query<User>().Where(x => false).ToList();
            Assert.AreEqual(0, users.Count);
        }

        [Test]
        public void QueryWithWhereUserIdAndTrue()
        {
            var users = Database.Query<User>().Where(x => x.UserId == 1 && true).ToList();
            Assert.AreEqual(1, users.Count);
            Assert.AreEqual(1, users[0].UserId);
        }

        [Test]
        public void QueryWithOrderBy()
        {
            var users = Database.Query<User>().OrderBy(x => x.DateOfBirth).ToList();
            var inmemory = InMemoryUsers.OrderBy(x => x.DateOfBirth).ToList();

            Assert.AreEqual(15, users.Count);
            for (int i = 0; i < users.Count; i++)
            {
                AssertUserValues(inmemory[i], users[i]);
            }
        }

        [Test]
        public void QueryWithOrderByDescending()
        {
            var users = Database.Query<User>().OrderByDescending(x => x.UserId).ToList();
            var inmemory = InMemoryUsers.OrderByDescending(x => x.UserId).ToList();

            Assert.AreEqual(15, users.Count);
            for (int i = 0; i < users.Count; i++)
            {
                AssertUserValues(inmemory[i], users[i]);
            }
        }

        [Test]
        public void QueryWithOrderByThenBy()
        {
            var users = Database.Query<User>().OrderBy(x => x.HouseId).ThenBy(x => x.UserId).ToList();
            var inmemory = InMemoryUsers.OrderBy(x => x.HouseId).ThenBy(x => x.UserId).ToList();

            Assert.AreEqual(15, users.Count);
            for (int i = 0; i < users.Count; i++)
            {
                AssertUserValues(inmemory[i], users[i]);
            }
        }

        [Test]
        public void QueryWithOrderByThenByDescending()
        {
            var users = Database.Query<User>().OrderBy(x => x.HouseId).ThenByDescending(x => x.UserId).ToList();
            var inmemory = InMemoryUsers.OrderBy(x => x.HouseId).ThenByDescending(x => x.UserId).ToList();

            Assert.AreEqual(15, users.Count);
            for (int i = 0; i < users.Count; i++)
            {
                AssertUserValues(inmemory[i], users[i]);
            }
        }

        [Test]
        public void QueryWithOrderByDescendingThenBy()
        {
            var users = Database.Query<User>().OrderByDescending(x => x.HouseId).ThenBy(x => x.UserId).ToList();
            var inmemory = InMemoryUsers.OrderByDescending(x => x.HouseId).ThenBy(x => x.UserId).ToList();

            Assert.AreEqual(15, users.Count);
            for (int i = 0; i < users.Count; i++)
            {
                AssertUserValues(inmemory[i], users[i]);
            }
        }

        [Test]
        public void QueryWithOrderByDescendingThenByDescending()
        {
            var users = Database.Query<User>().OrderByDescending(x => x.HouseId).ThenByDescending(x => x.UserId).ToList();
            var inmemory = InMemoryUsers.OrderByDescending(x => x.HouseId).ThenByDescending(x => x.UserId).ToList();

            Assert.AreEqual(15, users.Count);
            for (int i = 0; i < users.Count; i++)
            {
                AssertUserValues(inmemory[i], users[i]);
            }
        }

        [Test]
        public void QueryWithLimit()
        {
            var users = Database.Query<User>().OrderBy(x=>x.UserId).Limit(5).ToList();
            var inmemory = InMemoryUsers.OrderBy(x=>x.UserId).Take(5).ToList();

            Assert.AreEqual(5, users.Count);
            for (int i = 0; i < users.Count; i++)
            {
                AssertUserValues(inmemory[i], users[i]);
            }
        }

        [Test]
        public void QueryWithLimitWithOffset()
        {
            var users = Database.Query<User>().OrderBy(x => x.UserId).Limit(5,5).ToList();
            var inmemory = InMemoryUsers.OrderBy(x => x.UserId).Skip(5).Take(5).ToList();

            Assert.AreEqual(5, users.Count);
            for (int i = 0; i < users.Count; i++)
            {
                AssertUserValues(inmemory[i], users[i]);
            }
        }

        [Test]
        public void QueryWithWhere()
        {
            var users = Database.Query<User>().Where(x => x.UserId > 10).ToList();
            var inmemory = InMemoryUsers.Where(x => x.UserId > 10).ToList();

            Assert.AreEqual(5, users.Count);
            for (int i = 0; i < users.Count; i++)
            {
                AssertUserValues(inmemory[i], users[i]);
            }
        }

        [Test]
        public void QueryWithWhereAnd()
        {
            var users = Database.Query<User>().Where(x => x.UserId > 10 && x.UserId < 12).ToList();
            var inmemory = InMemoryUsers.Where(x => x.UserId > 10 && x.UserId < 12).ToList();

            Assert.AreEqual(1, users.Count);
            for (int i = 0; i < users.Count; i++)
            {
                AssertUserValues(inmemory[i], users[i]);
            }
        }

        [Test]
        public void QueryWithInclude()
        {
            var users = Database.Query<User>().Include(x=>x.House).ToList();
            InMemoryUsers.ForEach(x=>x.House = InMemoryHouses.SingleOrDefault(y=>y.HouseId == x.HouseId));

            Assert.AreEqual(15, users.Count);
            for (int i = 0; i < users.Count; i++)
            {
                AssertUserValues(InMemoryUsers[i], users[i]);
                AssertUserHouseValues(InMemoryUsers[i], users[i]);
            }
        }

        [Test]
        public void QueryWithIncludeAndNestedWhere()
        {
            var users = Database.Query<User>().Include(x => x.House).Where(x=> x.House.Address == InMemoryHouses[0].Address).ToList();
            InMemoryUsers.ForEach(x => x.House = InMemoryHouses.SingleOrDefault(y => y.HouseId == x.HouseId));
            var inmemory = InMemoryUsers.Where(x => x.House != null && x.House.Address == InMemoryHouses[0].Address).ToList();

            Assert.AreEqual(1, users.Count);
            for (int i = 0; i < users.Count; i++)
            {
                AssertUserValues(inmemory[i], users[i]);
                AssertUserHouseValues(inmemory[i], users[i]);
            }
        }

        [Test]
        public void QueryWithIncludeAndNestedOrderBy()
        {
            var users = Database.Query<User>().Include(x => x.House).Where(x=>x.HouseId != null).OrderBy(x => x.House.Address).ToList();
            InMemoryUsers.ForEach(x => x.House = InMemoryHouses.SingleOrDefault(y => y.HouseId == x.HouseId));
            var inmemory = InMemoryUsers.Where(x => x.HouseId != null).OrderBy(x => x.House.Address).ToList();

            Assert.AreEqual(7, users.Count);
            for (int i = 0; i < users.Count; i++)
            {
                AssertUserValues(inmemory[i], users[i]);
                AssertUserHouseValues(inmemory[i], users[i]);
            }
        }

        [Test]
        public void QueryWithProjectionAndEnclosedMethod()
        {
            var users = Database.Query<User>()
                .ProjectTo(x => new ProjectUser2 { FormattedAge = string.Format("{0:n}", x.Age) });

            Assert.AreEqual("21.00", users[0].FormattedAge);
            Assert.AreEqual(15, users.Count);
        }

        [Test]
        public void QueryWithProjectionAndEnclosedMethod2()
        {
            var users = Database.Query<User>()
                .ProjectTo(x => new ProjectUser2 { FormattedAge = FormatAge(x) });

            Assert.AreEqual("Age: 21, IsMale: True", users[0].FormattedAge);
            Assert.AreEqual(15, users.Count);
        }

        [Test]
        public void QueryWithProjectionAndEnclosedMethod3()
        {
            var users = Database.Query<User>()
                .ProjectTo(x => new ProjectUser2 { FormattedAge = x.Age + FormatAge(x) });

            Assert.AreEqual("21Age: 21, IsMale: True", users[0].FormattedAge);
            Assert.AreEqual(15, users.Count);
        }

        private string FormatAge(User u)
        {
            return string.Format("Age: {0}, IsMale: {1}", u.Age, u.IsMale);
        }

        [Test]
        public void QueryWithProjectionAndMethod()
        {
            var users = Database.Query<User>()
                .ProjectTo(x => new ProjectUser2 { Age = x.Age, Date = x.DateOfBirth.ToString("yyyy-MM-dd") });

            Assert.AreEqual(21, users[0].Age);
            Assert.AreEqual("1969-01-01", users[0].Date);
            Assert.AreEqual(15, users.Count);
        }

        [Test]
        public void QueryWithProjection()
        {
            var users = Database.Query<User>()
                .ProjectTo(x => new ProjectUser2 { Age = x.Age });

            Assert.AreEqual(21, users[0].Age);
            Assert.AreEqual(15, users.Count);
        }

        public class ProjectUser2
        {
            public int Age { get; set; }
            public string FormattedAge { get; set; }
            public string Date { get; set; }
        }

        [Test]
        public void QueryWithIncludeNestedOrderByLimitAndProjection()
        {
            var users = Database.Query<User>()
                .Include(x => x.House)
                .Where(x => x.HouseId != null)
                .OrderBy(x => x.House.HouseId)
                .Limit(5)
                .ProjectTo(x => new { Address = (x.HouseId != null ? x.House.Address : string.Empty), x.House.HouseId});

            InMemoryUsers.ForEach(x => x.House = InMemoryHouses.SingleOrDefault(y => y.HouseId == x.HouseId));
            var inmemory = InMemoryUsers.Where(x => x.HouseId != null).OrderBy(x => x.House.HouseId).Select(x => new {x.House.Address, x.HouseId}).ToList();

            Assert.AreEqual(5, users.Count);
            for (int i = 0; i < users.Count; i++)
            {
                Assert.AreEqual(inmemory[i].Address, users[i].Address);
                Assert.AreEqual(inmemory[i].HouseId, users[i].HouseId);
            }
        }

        [Test]
        public void QueryWithIncludeNestedOrderByLimitAndProjectionToProjectUser()
        {
            var users = Database.Query<User>()
                .Include(x => x.House)
                .Where(x => x.HouseId != null)
                .OrderBy(x => x.House.HouseId)
                .Limit(5)
                .ProjectTo(x => new ProjectUser() { NameWithAge = x.Name + x.Age });

            InMemoryUsers.ForEach(x => x.House = InMemoryHouses.SingleOrDefault(y => y.HouseId == x.HouseId));
            var inmemory = InMemoryUsers.Where(x => x.HouseId != null).OrderBy(x => x.House.HouseId).ToList();

            Assert.AreEqual(5, users.Count);
            for (int i = 0; i < users.Count; i++)
            {
                Assert.AreEqual(inmemory[i].Name + inmemory[i].Age, users[i].NameWithAge);
            }
        }

        [Test]
        public void QueryWithIncludeNestedOrderByLimitAndProjectionToProjectUserWithList()
        {
            var users = Database.Query<User>()
                .Include(x => x.House)
                .Where(x => x.HouseId != null)
                .OrderBy(x => x.House.HouseId)
                .Limit(5)
                .ProjectTo(x => new ProjectUser() { Array = new object[] { x.Name, x.Age } });

            InMemoryUsers.ForEach(x => x.House = InMemoryHouses.SingleOrDefault(y => y.HouseId == x.HouseId));
            var inmemory = InMemoryUsers.Where(x => x.HouseId != null).OrderBy(x => x.House.HouseId).ToList();

            Assert.AreEqual(5, users.Count);
            for (int i = 0; i < users.Count; i++)
            {
                Assert.AreEqual(inmemory[i].Name, users[i].Array[0]);
                Assert.AreEqual(inmemory[i].Age, users[i].Array[1]);
            }
        }

        [Test]
        public void QueryWithInheritedTypesAliasCorrectly()
        {
            var users = Database.Query<Supervisor>().Where(x => x.UserId == 1).ToList();
            Assert.AreEqual(users.Count, 1);
        }

        [Test]
        public void QueryWithInheritedTypesAliasCorrectlyWithJoin()
        {
            var users = Database.Query<User>()
                .Include(x=>x.Supervisor, (user, supervisor) => user.SupervisorId == supervisor.UserId)
                .Where(x => x.UserId.In(new[] {1,2}))
                .ToList();

            Assert.AreEqual(users.Count, 2);
            Assert.NotNull(users[1].Supervisor);
        }
    }

    public class ProjectUser
    {
        public string NameWithAge { get; set; }
        public object[] Array { get; set; }
    }

    public class Usersss
    {
        public int UserId { get; set; }
    }
}
