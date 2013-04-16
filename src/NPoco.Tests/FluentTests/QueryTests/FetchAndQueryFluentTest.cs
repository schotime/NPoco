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

        [Test]
        public void FetchByExpressionAdvanced()
        {
            var users = Database.FetchBy<User>(y => y.Where(x => x.UserId > 10).OrderBy(x=>x.UserId));

            Assert.AreEqual(5, users.Count);
            for (int i = 0; i < users.Count; i++)
            {
                AssertUserValues(InMemoryUsers[i + 10], users[i]);
            }
        }

        [Test]
        public void FetchWhereExpression()
        {
            var users = Database.FetchWhere<User>(y => y.UserId == 2 && !y.IsMale);
            Assert.AreEqual(1, users.Count);
        }

        [Test]
        public void FetchByExpressionAndColumnAlias()
        {
            var users = Database.FetchBy<UserDecorated>(y => y.Where(x => x.IsMale).OrderBy(x => x.UserId));
            Assert.AreEqual(8, users.Count);
        }

        [Test]
        public void FetchByExpressionAndLimit()
        {
            var users = Database.FetchBy<UserDecorated>(y => y.OrderBy(x => x.UserId).Limit(5, 5));
            Assert.AreEqual(5, users.Count);
        }

        [Test]
        public void FetchByExpressionAndSubstringInWhere()
        {
            var users = Database.FetchBy<UserDecorated>(y => y.Where(x=>x.Name.Substring(0, 4) == "Name"));
            Assert.AreEqual(15, users.Count);
        }

        [Test]
        public void FetchByExpressionAndSubstringAndUpper()
        {
            var users = Database.FetchBy<UserDecorated>(y => y.Where(x => x.Name.ToUpper().Substring(0, 4) == "NAME"));
            Assert.AreEqual(15, users.Count);
        }

        [Test]
        public void FetchByExpressionAndLower()
        {
            var users = Database.FetchBy<UserDecorated>(y => y.Where(x => x.Name.ToLower() == "name1"));
            Assert.AreEqual(1, users.Count);
        }

        [Test]
        public void FetchByExpressionAndContains()
        {
            var users = Database.FetchBy<UserDecorated>(y => y.Where(x => x.Name.Contains("ame")));
            Assert.AreEqual(15, users.Count);
        }

        [Test]
        public void FetchByExpressionAndStartsWith()
        {
            var users = Database.FetchBy<UserDecorated>(y => y.Where(x => x.Name.StartsWith("Na")));
            Assert.AreEqual(15, users.Count);
        }

        [Test]
        public void FetchByExpressionAndEndsWith()
        {
            var users = Database.FetchBy<UserDecorated>(y => y.Where(x => x.Name.EndsWith("e2")));
            Assert.AreEqual(1, users.Count);
        }

        [Test]
        public void FetchByExpressionAndSelectWithSubstring()
        {
            var users = Database.FetchBy<UserDecorated>(y => y.Select(x=>new { Name = x.Name.Substring(0,2)}));
            Assert.AreEqual("Na", users[0].Name);
        }

        [Test]
        public void FetchByExpressionAndSelectWithLower()
        {
            var users = Database.FetchBy<UserDecorated>(y => y.Select(x => new { Name = x.Name.ToLower() }));
            Assert.AreEqual("name1", users[0].Name);
        }

        [Test]
        public void FetchByExpressionAndSelectWithUpper()
        {
            var users = Database.FetchBy<UserDecorated>(y => y.Select(x => new { Name = x.Name.ToUpper() }));
            Assert.AreEqual("NAME1", users[0].Name);
        }

        [Test]
        public void FetchByExpressionAndSelect()
        {
            var users = Database.FetchBy<UserDecorated>(y => y.Select(x => new { x.Name }));
            Assert.AreEqual("Name1", users[0].Name);
        }

        [Test]
        public void FetchWithWhereExpressionContains()
        {
            var list = new [] {1, 2, 3, 4};
            var users = Database.FetchBy<User>(y => y.Where(x => list.Contains(x.UserId)));

            Assert.AreEqual(4, users.Count);
            for (int i = 0; i < users.Count; i++)
            {
                AssertUserValues(InMemoryUsers[i], users[i]);
            }
        }

    }
}
