using System;
using NPoco.Tests.Common;
using NUnit.Framework;

namespace NPoco.Tests.FluentTests.QueryTests
{
    [TestFixture]
    //[NUnit.Framework.Ignore("Appearently the decorated syntax and fluent syntax are some how conflicting.")]
    public class SingleAndFirstQueryFluentTest : BaseDBFuentTest
    {
        [Test]
        public void SingleOrDefaultById()
        {
            var user = Database.SingleOrDefaultById<User>(1);

            Assert.NotNull(user);
            AssertUserValues(InMemoryUsers[0], user);
        }

        [Test]
        public void SingleOrDefaultByIdWithNoRecord()
        {
            var user = Database.SingleOrDefaultById<User>(-1);
            Assert.Null(user);
        }

        [Test]
        public void SingleById()
        {
            var user = Database.SingleById<User>(1);

            Assert.NotNull(user);
            AssertUserValues(InMemoryUsers[0], user);
        }

        [Test]
        public void SingleByIdWithNoRecord()
        {
            Assert.Throws<InvalidOperationException>(() => Database.SingleById<User>(-1));
        }

        [Test]
        public void SingleInto()
        {
            var u = new User();
            var user = Database.SingleInto(u, "select u.* from users u where u.userid = 1");

            AssertUserValues(InMemoryUsers[0], user);
            Assert.AreEqual(u, user);
        }

        [Test]
        public void SingleIntoWithNoRecord()
        {
            var u = new User();
            Assert.Throws<InvalidOperationException>(() => Database.SingleInto(u, "select u.* from users u where u.userid = -1"));
        }

        [Test]
        public void SingleOrDefaultInto()
        {
            var u = new User();
            var user = Database.SingleOrDefaultInto(u, "select u.* from users u where u.userid = 1");

            AssertUserValues(InMemoryUsers[0], u);
            Assert.AreEqual(u, user);
        }

        [Test]
        public void SingleOrDefaultIntoWithNoRecord()
        {
            var u = new User();
            var user = Database.SingleOrDefaultInto(u, "select u.* from users u where u.userid = -1");

            AssertUserValues(u, new User());
            Assert.Null(user);
        }

        [Test]
        public void SingleSql()
        {
            var user = Database.Single<User>("select u.* from users u where u.userid = 1");

            Assert.NotNull(user);
            AssertUserValues(InMemoryUsers[0], user);
        }

        [Test]
        public void SingleSqlWithNoRecord()
        {
            Assert.Throws<InvalidOperationException>(() => Database.Single<User>("select u.* from users u where u.userid = -1"));
        }

        [Test]
        public void SingleOrDefaultSql()
        {
            var user = Database.SingleOrDefault<User>("select u.* from users u where u.userid = 1");

            Assert.NotNull(user);
            AssertUserValues(InMemoryUsers[0], user);
        }

        [Test]
        public void SingleOrDefaultSqlWithNoRecord()
        {
            var user = Database.SingleOrDefault<User>("select u.* from users u where u.userid = -1");
            Assert.Null(user);
        }

        [Test]
        public void FirstSql()
        {
            var user = Database.First<User>("select u.* from users u order by u.userid");

            Assert.NotNull(user);
            AssertUserValues(InMemoryUsers[0], user);
        }

        [Test]
        public void FirstSqlWithNoRecord()
        {
            Assert.Throws<InvalidOperationException>(() => Database.First<User>("select u.* from users u where u.userid < 0"));
        }

        [Test]
        public void FirstOrDefaultSql()
        {
            var user = Database.FirstOrDefault<User>("select u.* from users u order by u.userid");

            Assert.NotNull(user);
            AssertUserValues(InMemoryUsers[0], user);
        }

        [Test]
        public void FirstOrDefaultSqlWithNoRecord()
        {
            var user = Database.FirstOrDefault<User>("select u.* from users u where u.userid < 0");
            Assert.Null(user);
        }
        
        [Test]
        public void SingleWithAdHocObject()
        {
            var data = Database.Single<AdHocUser>("select userid, name from users where userid = 1");
            Assert.AreEqual(999, data.UserId);
            Assert.AreEqual("Name1", data.Name);
        }

        [Test]
        public void SingleWithAdHocObjectUsingUnderscores()
        {
            var data = Database.Single<AdHocUser>("select name \"Na_me\" from users where userid = 1");
            Assert.AreEqual("Name1", data.Name);
        }
    }
}