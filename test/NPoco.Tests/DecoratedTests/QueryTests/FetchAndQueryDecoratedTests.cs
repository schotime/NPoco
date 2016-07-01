using System.Collections.Generic;
using System.Linq;
using NPoco.Tests.Common;
using NUnit.Framework;
using System.Data;
using System.Data.SqlClient;

namespace NPoco.Tests.DecoratedTests.QueryTests
{
    [TestFixture]
    public class FetchAndQueryDecoratedTest : BaseDBDecoratedTest
    {
        [Test]
        public void FetchAllFields()
        {
            var users = Database.Fetch<UserFieldDecorated>();
            Assert.AreEqual(15, users.Count);
        }
        
        [Test]
        public void FetchAll()
        {
            var users = Database.Fetch<UserDecorated>();

            Assert.AreEqual(InMemoryUsers.Count, users.Count);
            for (int i = 0; i < InMemoryUsers.Count; i++)
            {
                AssertUserValues(InMemoryUsers[i], users[i]);
            }
        }

        [Test]
        public void FetchAllWithSql()
        {
            var users = Database.Fetch<UserDecorated>("select * from users order by userid");

            Assert.AreEqual(InMemoryUsers.Count, users.Count);
            for (int i = 0; i < InMemoryUsers.Count; i++)
            {
                AssertUserValues(InMemoryUsers[i], users[i]);
            }
        }

        [Test]
        public void FetchWithWhere()
        {
            var users = Database.Fetch<UserDecorated>("where userid > 10 order by userid");

            Assert.AreEqual(5, users.Count);
            for (int i = 0; i < users.Count; i++)
            {
                AssertUserValues(InMemoryUsers[i + 10], users[i]);
            }
        }

        [Test]
        public void FetchWithPaging()
        {
            var users = Database.Fetch<UserDecorated>(2, 5, "where userid > 0 order by userid");

            Assert.AreEqual(5, users.Count);
            for (int i = 0; i < users.Count; i++)
            {
                AssertUserValues(InMemoryUsers[i + 5], users[i]);
            }
        }
        
        [Test]
        public void FetchWithAlias()
        {
            var user = Database.Fetch<UserDecoratedWithAlias>("select name as fullname from users where userid = 1").Single();

            Assert.NotNull(user);
            Assert.True(!string.IsNullOrWhiteSpace(user.Name));
        }

        [Test]
        public void FetchWithAliasUsingAutoSelect()
        {
            var user = Database.Fetch<UserDecoratedWithAlias>("where userid = 1").Single();

            Assert.NotNull(user);
            Assert.True(!string.IsNullOrWhiteSpace(user.Name));
        }

        [Test]
        public void BasicPaging()
        {
            var users = Database.Page<UserDecorated>(3, 5, "order by userid");

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
            var users = Database.SkipTake<UserDecorated>(3, 2, "order by userid");

            Assert.AreEqual(2, users.Count);
            for (int i = 0; i < users.Count; i++)
            {
                AssertUserValues(InMemoryUsers[i + 3], users[i]);
            }
        }

        [Test]
        public void BasicSkipTakeWithNoResults()
        {
            var users = Database.SkipTake<UserDecorated>(50, 10, "order by userid");
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
        public void ReturnStringArray()
        {
            var data = Database.Fetch<string[]>("select name, 'test' from users where userid = 2");
            Assert.AreEqual("Name2", data[0][0]);
            Assert.AreEqual("test", data[0][1]);
        }
        
        [Test]
        public void ReturnDictionaryStringObject()
        {
            var data = Database.Fetch<Dictionary<string, object>>("select userid, name from users where userid = 2");
            Assert.AreEqual(2, data[0]["userid"]);
            Assert.AreEqual("Name2", data[0]["name"]);
        }

        [Test]
        public void FetchWithWhereExpression()
        {
            var users = Database.Query<UserDecorated>().Where(x => x.IsMale).OrderBy(x => x.UserId).ToList();
            Assert.AreEqual(8, users.Count);
        }

        [Test]
        public void FetchWithStoredProcedure()
        {
            var theName = "TheName";
            var name = Database.ExecuteScalar<string>("TestProc", CommandType.StoredProcedure, new SqlParameter("Name", theName));
            Assert.AreEqual(theName, name);
        }

        [Test]
        public void FetchWithStoredProcedure2()
        {
            var theName = "TheName";
            var name = Database.ExecuteScalar<string>("TestProc", CommandType.StoredProcedure, new { Name = theName });
            Assert.AreEqual(theName, name);
        }
    }
}
