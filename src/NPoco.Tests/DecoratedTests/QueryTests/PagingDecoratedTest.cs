using System;
using NPoco.Tests.Common;
using NUnit.Framework;

namespace NPoco.Tests.DecoratedTests.QueryTests
{
    [TestFixture]
    //[NUnit.Framework.Ignore("Appearently the decorated syntax and fluent syntax are some how conflicting.")]
    public class PagingDecoratedTest : BaseDBDecoratedTest
    {
        [Test]
        public void Page()
        {
            var page = Database.Page<UserDecorated>(2, 5, "SELECT * FROM Users WHERE UserID <= 15 ORDER BY UserID");

            foreach (var user in page.Items)
            {
                var found = false;
                foreach (var inMemoryUser in InMemoryUsers)
                {
                    if (user.Name == inMemoryUser.Name)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found) Assert.Fail("Could not find use '" + user.Name + "' in InMemoryUsers.");
            }

            // Check other stats
            Assert.AreEqual(page.Items.Count, 5);
            Assert.AreEqual(page.CurrentPage, 2);
            Assert.AreEqual(page.ItemsPerPage, 5);
            Assert.AreEqual(page.TotalItems, 15);
            Assert.AreEqual(page.TotalPages, 3);
        }

        [Test]
        public void PageWithAlias()
        {
            var page = Database.Page<UserDecorated>(2, 5, "SELECT u.* FROM Users u WHERE u.userId <= 15 ORDER BY u.UserID DESC");

            foreach (var user in page.Items)
            {
                var found = false;
                foreach (var inMemoryUser in InMemoryUsers)
                {
                    if (user.Name == inMemoryUser.Name)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found) Assert.Fail("Could not find use '" + user.Name + "' in InMemoryUsers.");
            }

            // Check other stats
            Assert.AreEqual(page.Items.Count, 5);
            Assert.AreEqual(page.CurrentPage, 2);
            Assert.AreEqual(page.ItemsPerPage, 5);
            Assert.AreEqual(page.TotalItems, 15);
            Assert.AreEqual(page.TotalPages, 3);
        }

        [Test]
        public void Page_NoOrderBy()
        {
            var records = Database.Page<UserDecorated>(2, 5, "SELECT * FROM Users WHERE UserID <= 15");
            Assert.AreEqual(records.Items.Count, 5);
        }

        [Test]
        public void Page_boundary()
        {
            // In this test we're checking that the page count is correct when there are
            // exactly pagesize*N records.

            // Fetch em
            var page = Database.Page<UserDecorated>(3, 5, "SELECT * FROM Users  WHERE UserID <= 15 ORDER BY UserID");

            // Check other stats
            Assert.AreEqual(page.Items.Count, 5);
            Assert.AreEqual(page.CurrentPage, 3);
            Assert.AreEqual(page.ItemsPerPage, 5);
            Assert.AreEqual(page.TotalItems, 15);
            Assert.AreEqual(page.TotalPages, 3);
        }
    }
}
