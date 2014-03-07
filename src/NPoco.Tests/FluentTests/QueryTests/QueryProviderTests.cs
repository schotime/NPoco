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
    }

    public class Usersss
    {
        public int UserId { get; set; }
    }
}
