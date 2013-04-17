using System.Collections.Generic;
using System.Linq;
using NPoco.Expressions;
using NPoco.Tests.Common;
using NUnit.Framework;

namespace NPoco.Tests.FluentTests.QueryTests
{
    [TestFixture]
    //[NUnit.Framework.Ignore("Appearently the decorated syntax and fluent syntax are some how conflicting.")]
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
    }
}
