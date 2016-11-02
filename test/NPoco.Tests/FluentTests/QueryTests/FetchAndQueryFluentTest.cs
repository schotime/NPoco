using System;
using System.Collections.Generic;
using NPoco.Tests.Common;
using NUnit.Framework;

namespace NPoco.Tests.FluentTests.QueryTests
{
    [TestFixture]
    public class FetchAndQueryFluentTest : BaseDBFuentTest
    {
        [Test]
        public void FetchAll()
        {
            var users = Database.Fetch<User>();

            Assert.AreEqual(InMemoryUsers.Count, users.Count);
            for (int i = 0; i < InMemoryUsers.Count; i++)
            {
                AssertUserValues(InMemoryUsers[i], users[i]);
            }
        }

        [Test]
        public void FetchAllWithSql()
        {
            var users = Database.Fetch<User>("select * from users order by userid");

            Assert.AreEqual(InMemoryUsers.Count, users.Count);
            for (int i = 0; i < InMemoryUsers.Count; i++)
            {
                AssertUserValues(InMemoryUsers[i], users[i]);
            }
        }

        [Test]
        public void FetchWithWhere()
        {
            var users = Database.Fetch<User>("where userid > 10 order by userid");

            Assert.AreEqual(5, users.Count);
            for (int i = 0; i < users.Count; i++)
            {
                AssertUserValues(InMemoryUsers[i + 10], users[i]);
            }
        }

        [Test]
        public void FetchWithPaging()
        {
            var users = Database.Fetch<User>(2, 5, "where userid > 0 order by userid");

            Assert.AreEqual(5, users.Count);
            for (int i = 0; i < users.Count; i++)
            {
                AssertUserValues(InMemoryUsers[i + 5], users[i]);
            }
        }

        [Test]
        public void BasicPaging()
        {
            var users = Database.Page<User>(3, 5, "order by userid");

            Assert.AreEqual(5, users.Items.Count);
            for (int i = 0; i < users.Items.Count; i++)
            {
                AssertUserValues(InMemoryUsers[i + 10], users.Items[i]);
            }

            Assert.AreEqual(3, users.CurrentPage);
            Assert.AreEqual(5, users.ItemsPerPage);
            Assert.AreEqual(InMemoryUsers.Count, users.TotalItems);
            Assert.AreEqual(3, users.TotalPages);
        }

        [Test]
        public void BasicSkipTake()
        {
            var users = Database.SkipTake<User>(3, 2, "order by userid");

            Assert.AreEqual(2, users.Count);
            for (int i = 0; i < users.Count; i++)
            {
                AssertUserValues(InMemoryUsers[i + 3], users[i]);
            }
        }

        [Test]
        public void BasicSkipTakeWithNoResults()
        {
            var users = Database.SkipTake<User>(50, 10, "order by userid");
            Assert.AreEqual(0, users.Count);
        }

        [Test]
        public void ReturnObjectArray()
        {
            var data = Database.Fetch<object[]>("select userid, name from users where userid = 2");
            Assert.AreEqual(2, data[0][0]);
            Assert.AreEqual("Name2", data[0][1]);
        }

        [Test]
        public void ReturnDictionaryStringObject()
        {
            var data = Database.Fetch<Dictionary<string, object>>("select userid, name from users where userid = 2");
            Assert.AreEqual(2, data[0]["userid"]);
            Assert.AreEqual("Name2", data[0]["name"]);
        }
        
        [Test]
        public void QueryDictionary()
        {
            var data = Database.Dictionary<string, string>("select name, address__street from users");
            Assert.AreEqual(null, data["Name1"]);
            Assert.AreEqual("1 Road Street", data["Name2"]);
        }

        [Test]
        public void EmptyInQueryWithGuids()
        {
            var list = new List<Guid>();
            var data = Database.Fetch<User>("select * from users where uniqueid in (@1) and userid = @0 and name = @2", 1, list, "name");
            Assert.AreEqual(0, data.Count);
        }
    }
}
