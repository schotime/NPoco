using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NPoco.Expressions;
using NPoco.Tests.Common;
using NUnit.Framework;

namespace NPoco.Tests.DecoratedTests.QueryTests
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
    }

    public class More : BaseDBFuentTest
    {
        [Test]
        public void Test1()
        {
            var d = Database
                .Query<User>()
                .Include(x => x.House)
                .ToEnumerable()
                .ToList();
        }
    }

    [PrimaryKey("")]
    public class NestedConvention
    {
        public string Name { get; set; }
        [ResultColumn]
        public Money Money { get; set; }

        public int MoneyId { get; set; }
    }

    [PrimaryKey("MoneyId")]
    public class Money
    {
        public int MoneyId { get; set; }

        public Money Money2 { get; set; }

        public decimal Value { get; set; }
        public string Currency { get; set; }
    }
}
