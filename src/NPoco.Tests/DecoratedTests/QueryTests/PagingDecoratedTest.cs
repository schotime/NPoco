﻿using System;
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
            var page = Database.Page<UserDecorated>(2, 5, "SELECT u.* FROM Users u WHERE u.userId <= 15 ORDER BY u.UserID, u.Age DESC");

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

        [Test]
        public void Page_MultiPoco()
        {
            var page = Database.Page<UserDecorated, ExtraUserInfoDecorated, CustomerUser>((user, extra) =>
            {
                return new CustomerUser
                {
                    Id = user.UserId,
                    CustomerName = user.Name,
                    CustomerEmail = extra.Email
                };
            }, 2, 5, "SELECT Users.UserId AS UserId, Users.Name, ExtraUserInfos.Email FROM Users INNER JOIN ExtraUserInfos ON Users.UserId = ExtraUserInfos.UserId");

            foreach (var customer in page.Items)
            {
                var found = false;
                var emailMatch = false;
                foreach (var inMemoryUser in InMemoryUsers)
                {
                    if (customer.CustomerName == inMemoryUser.Name)
                    {
                        found = true;
                        emailMatch = InMemoryExtraUserInfos.Exists(info => info.UserId == customer.Id && info.Email == customer.CustomerEmail);
                        break;
                    }
                }
                if (!found) Assert.Fail("Could not find user '" + customer.CustomerName + "' in InMemoryUsers.");
                if (!emailMatch) Assert.Fail("Email doesn't match for user '" + customer.CustomerName + "' in InMemoryExtraUserInfos.");
            }

            // Check other stats
            Assert.AreEqual(page.Items.Count, 5);
            Assert.AreEqual(page.CurrentPage, 2);
            Assert.AreEqual(page.ItemsPerPage, 5);
            Assert.AreEqual(page.TotalItems, 15);
            Assert.AreEqual(page.TotalPages, 3);
        }
    }
}
