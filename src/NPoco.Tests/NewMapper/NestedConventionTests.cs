using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NPoco.Expressions;
using NPoco.Tests.Common;
using NPoco.Tests.NewMapper.Models;
using NUnit.Framework;

namespace NPoco.Tests.NewMapper
{
    public class NewMapperTests : BaseDBDecoratedTest
    {
        [Test]
        public void Test1()
        {
            var data = Database.Fetch<NestedConvention>("select 'Name' Name, 23 money__value, 'AUD' money__currency /*poco_dual*/").Single();
            Assert.AreEqual("Name", data.Name);
            Assert.AreEqual(23, data.Money.Value);
            Assert.AreEqual("AUD", data.Money.Currency);
        }

        [Test]
        public void Test2()
        {
            var data = Database.Fetch<NestedConvention>("select 'Name' Name, null money__value, null money__currency /*poco_dual*/").Single();
            Assert.AreEqual("Name", data.Name);
            Assert.AreEqual(null, data.Money);
        }

        [Test]
        public void Test3()
        {
            var data = Database.Fetch<NestedConvention>("select 'Name' Name, 23 money__value, null money__currency /*poco_dual*/").Single();
            Assert.AreEqual("Name", data.Name);
            Assert.AreEqual(23, data.Money.Value);
            Assert.AreEqual(null, data.Money.Currency);
        }

        [Test]
        public void Test4()
        {
            var data = Database.Fetch<string[]>("select 'Name' Name, 'AUD' money__currency /*poco_dual*/").Single();
            Assert.AreEqual("Name", data[0]);
            Assert.AreEqual("AUD", data[1]);
        }

        [Test]
        public void Test5()
        {
            var data = Database.Fetch<string>("select 'Name' /*poco_dual*/ union all select 'Name2' /*poco_dual*/");
            Assert.AreEqual("Name", data[0]);
            Assert.AreEqual("Name2", data[1]);
        }

        [Test]
        public void Test6()
        {
            var data = Database.Fetch<dynamic>("select 'Name' Name, 23 Age /*poco_dual*/").Single();
            Assert.AreEqual("Name", data.Name);
            Assert.AreEqual(23, data.Age);
        }

        [Test]
        public void Test7()
        {
            var data = Database.Fetch<Dictionary<string, object>>("select 'Name' Name, 23 Age /*poco_dual*/").Single();
            Assert.AreEqual("Name", data["Name"]);
            Assert.AreEqual(23, data["Age"]);
        }

        [Test]
        public void Test8()
        {
            var data = Database.Fetch<IDictionary<string, object>>("select 'Name' Name, 23 Age /*poco_dual*/").Single();
            Assert.AreEqual("Name", data["Name"]);
            Assert.AreEqual(23, data["Age"]);
        }

        [Test]
        public void Test9()
        {
            var sqlExpression = new DefaultSqlExpression<NestedConvention>(Database, true);
            sqlExpression.Select(x => new { x.Money.Currency });
            var selectStatement = sqlExpression.Context.ToSelectStatement();
            Console.WriteLine(selectStatement);
        }

        [Test]
        public void Test10()
        {
            var data = Database.Fetch<NestedConvention>("select 'Name' Name, 23 money__value, 'AUD' money__currency, 24 money__money2__value, 'USD' money__money2__currency /*poco_dual*/").Single();
            Assert.AreEqual("Name", data.Name);
            Assert.AreEqual(23, data.Money.Value);
            Assert.AreEqual("AUD", data.Money.Currency);
            Assert.AreEqual(24, data.Money.Money2.Value);
            Assert.AreEqual("USD", data.Money.Money2.Currency);
        }

        [Test]
        public void Test11()
        {
            var data = Database.Query<RecursionUser>()
                .Include(x => x.CreatedBy.Supervisor)
                .Include(x => x.CreatedBy.CreatedBy)
                .Include(x => x.Supervisor.CreatedBy)
                .Include(x => x.Supervisor.Supervisor)
                .ToList();

            for (int i = 0; i < data.Count; i++)
            {
                Assert.AreEqual("Name" + (i + 1), data[i].Name);
                Assert.AreEqual("Name" + 1, data[i].CreatedBy.Name);
                Assert.AreEqual("Name" + 2, data[i].CreatedBy.Supervisor.Name);
                Assert.AreEqual("Name" + 1, data[i].CreatedBy.CreatedBy.Name);
                Assert.AreEqual("Name" + 2, data[i].Supervisor.Name);
                Assert.AreEqual("Name" + 1, data[i].Supervisor.CreatedBy.Name);
                Assert.AreEqual("Name" + 2, data[i].Supervisor.Supervisor.Name);
            }
        }

        [Test]
        public void Test12()
        {
            var user = Database.Fetch<UsersNameProjection>().First();
            Assert.AreEqual("Name1", user._TheName);
        }

        [Test]
        public void Test13()
        {
            var user = Database.FetchOneToMany<One>(x => x.Items, new Sql(@"
select 1 OneId, 'Name1' Name, null Items__Value, null Items__Currency /*poco_dual*/
union all
select 1 OneId,'Name1' Name, 12 Items__Value, 'USD' Items__Currency /*poco_dual*/
union all
select 2 OneId,'Name2' Name, 14 Items__Value, 'YEN' Items__Currency /*poco_dual*/
union all
select 2 OneId,'Name2' Name, 15 Items__Value, 'GBP' Items__Currency /*poco_dual*/
union all
select 3 OneId,'Name3' Name, 16 Items__Value, 'EUR' Items__Currency /*poco_dual*/
union all 
select 4 OneId,'Name4' Name, null Items__Value, null Items__Currency /*poco_dual*/
union all 
select 5 OneId,'Name5' Name, 17 Items__Value, 'CHN' Items__Currency /*poco_dual*/
union all 
select 5 OneId,'Name5' Name, null Items__Value, null Items__Currency /*poco_dual*/
")).ToList();


            Assert.AreEqual(5, user.Count);
            
            Assert.AreEqual(1, user[0].OneId);
            Assert.AreEqual("Name1", user[0].Name);
            Assert.AreEqual(1, user[0].Items.Count);
            Assert.AreEqual(12, user[0].Items[0].Value);
            Assert.AreEqual("USD", user[0].Items[0].Currency);

            Assert.AreEqual(2, user[1].OneId);
            Assert.AreEqual("Name2", user[1].Name);
            Assert.AreEqual(2, user[1].Items.Count);
            Assert.AreEqual(14, user[1].Items[0].Value);
            Assert.AreEqual("YEN", user[1].Items[0].Currency);
            Assert.AreEqual(15, user[1].Items[1].Value);
            Assert.AreEqual("GBP", user[1].Items[1].Currency);

            Assert.AreEqual(3, user[2].OneId);
            Assert.AreEqual("Name3", user[2].Name);
            Assert.AreEqual(1, user[2].Items.Count);
            Assert.AreEqual(16, user[2].Items[0].Value);
            Assert.AreEqual("EUR", user[2].Items[0].Currency);

            Assert.AreEqual(4, user[3].OneId);
            Assert.AreEqual("Name4", user[3].Name);
            Assert.AreEqual(null, user[3].Items);

            Assert.AreEqual(5, user[4].OneId);
            Assert.AreEqual("Name5", user[4].Name);
            Assert.AreEqual(1, user[4].Items.Count);
            Assert.AreEqual(17, user[4].Items[0].Value);
            Assert.AreEqual("CHN", user[4].Items[0].Currency);
        }

        [Test]
        public void Test14()
        {
            var data = Database.Fetch<UserWithAddress>().First();
            Assert.AreEqual("Name1", data.Name);
            Assert.AreEqual("Street1", data.Address.StreetName);
            Assert.AreEqual(1, data.Address.StreetNo);
            Assert.AreEqual(new DateTime(1971, 01, 01), data.Address.MovedInOn);
        }

        [Test]
        public void Test15()
        {
            var ones = Database.Query<One>()
                .IncludeMany(x => x.Items)
                .OrderBy(x => x.OneId)
                .ToList();

            Assert.AreEqual(15, ones.Count);

            for (int i = 0; i < ones.Count; i++)
            {
                if (i % 3 == 0)
                    Assert.AreEqual(null, ones[i].Items);
                else 
                    Assert.AreEqual(i%3, ones[i].Items.Count);
            }
        }
    }
}
