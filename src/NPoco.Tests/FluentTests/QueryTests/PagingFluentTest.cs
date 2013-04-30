using System.Data;
using NPoco.Tests.Common;
using NUnit.Framework;

namespace NPoco.Tests.FluentTests.QueryTests
{
    [TestFixture]
    //[NUnit.Framework.Ignore("Appearently the decorated syntax and fluent syntax are some how conflicting.")]
    public class PagingFluentTest : BaseDBFuentTest
    {
        [Test]
        public void Page()
        {
            var page = Database.Page<User>(2, 5, "SELECT * FROM Users WHERE UserID <= 15 ORDER BY UserID");

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
            var records = Database.Page<User>(2, 5, "SELECT * FROM Users WHERE UserID <= 15");
            Assert.AreEqual(records.Items.Count, 5);
        }

        [Test]
        public void Page_Distinct()
        {
            // Fetch em
            var page = Database.Page<User>(2, 5, "SELECT DISTINCT * FROM Users  WHERE UserID <= 15 ORDER BY UserID");

            // Check em
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
        public void Page_boundary()
        {
            // In this test we're checking that the page count is correct when there are
            // exactly pagesize*N records.

            // Fetch em
            var page = Database.Page<User>(3, 5, "SELECT DISTINCT * FROM Users  WHERE UserID <= 15 ORDER BY UserID");

            // Check other stats
            Assert.AreEqual(page.Items.Count, 5);
            Assert.AreEqual(page.CurrentPage, 3);
            Assert.AreEqual(page.ItemsPerPage, 5);
            Assert.AreEqual(page.TotalItems, 15);
            Assert.AreEqual(page.TotalPages, 3);
        }
    }
}
