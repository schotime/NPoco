using NPoco.Tests.Common;
using NUnit.Framework;

namespace NPoco.Tests.FluentTests.QueryTests
{
    [TestFixture]
    public class QueryWithDeclarationTests : BaseDBFuentTest
    {
        [SetUp]
        public void Setup()
        {
            (Database as NPoco.Database).EnableAutoSelect = false;
        }

        [Test]
        public void FetchAllWithSqlHavingDeclaredVariable()
        {
            var users = Database.Fetch<User>(
                @"declare @userid int = 1 
                  select * from users where userid > @userid 
                  order by userid");

            Assert.AreEqual(InMemoryUsers.Count - 1, users.Count);
            for (int i = 1; i < InMemoryUsers.Count; i++)
            {
                AssertUserValues(InMemoryUsers[i], users[i - 1]);
            }
        }

        [Test]
        public void FetchAllWithSqlHavingCommentedVariable()
        {
            var users = Database.Fetch<User>(
                @"select * from users --where userid > @userid 
                  order by userid");

            Assert.AreEqual(InMemoryUsers.Count, users.Count);
            for (int i = 0; i < InMemoryUsers.Count; i++)
            {
                AssertUserValues(InMemoryUsers[i], users[i]);
            }
        }

        [Test]
        public void FetchAllWithSqlHavingTableVariable()
        {
            var users = Database.Fetch<User>(
                @"declare @t table (id int)
                  insert into @t values
                  (1), (2), (3)

                  select * from users where userid in 
                  (select id from @t)
                  order by userid");

            Assert.AreEqual(3, users.Count);
            for (int i = 0; i < 3; i++)
            {
                AssertUserValues(InMemoryUsers[i], users[i]);
            }
        }

        [Test]
        public void FetchAllWithSqlHavingAtSignInString()
        {
            var users = Database.Fetch<User>(
                @"select * from users where name <> ' @userid '
                  order by userid");

            Assert.AreEqual(InMemoryUsers.Count, users.Count);
            for (int i = 0; i < InMemoryUsers.Count; i++)
            {
                AssertUserValues(InMemoryUsers[i], users[i]);
            }
        }
    }
}
