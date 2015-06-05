using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NPoco.Expressions;
using NPoco.RowMappers;
using NPoco.Tests.Common;
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
            var user = Database.Fetch<Name>().First();
            Assert.AreEqual("Name1", user._TheName);
        }

        [Test]
        public void Test13()
        {
            var user = ((Database)Database).QueryImp<One>(null, x => x.Items, new Sql(@"
select 'Name1' Name, null Items__Value, null Items__Currency /*poco_dual*/
union all
select 'Name1' Name, 12 Items__Value, 'USD' Items__Currency /*poco_dual*/
union all
select 'Name2' Name, 14 Items__Value, 'YEN' Items__Currency /*poco_dual*/
union all
select 'Name2' Name, 15 Items__Value, 'GBP' Items__Currency /*poco_dual*/
union all
select 'Name3' Name, 16 Items__Value, 'EUR' Items__Currency /*poco_dual*/
union all 
select 'Name4' Name, null Items__Value, null Items__Currency /*poco_dual*/
union all 
select 'Name5' Name, 17 Items__Value, 'CHN' Items__Currency /*poco_dual*/
union all 
select 'Name5' Name, null Items__Value, null Items__Currency /*poco_dual*/
")).ToList();


            Assert.AreEqual(5, user.Count);
            
            Assert.AreEqual("Name1", user[0].Name);
            Assert.AreEqual(1, user[0].Items.Count);
            Assert.AreEqual(12, user[0].Items[0].Value);
            Assert.AreEqual("USD", user[0].Items[0].Currency);

            Assert.AreEqual("Name2", user[1].Name);
            Assert.AreEqual(2, user[1].Items.Count);
            Assert.AreEqual(14, user[1].Items[0].Value);
            Assert.AreEqual("YEN", user[1].Items[0].Currency);
            Assert.AreEqual(15, user[1].Items[1].Value);
            Assert.AreEqual("GBP", user[1].Items[1].Currency);
            
            Assert.AreEqual("Name3", user[2].Name);
            Assert.AreEqual(1, user[2].Items.Count);
            Assert.AreEqual(16, user[2].Items[0].Value);
            Assert.AreEqual("EUR", user[2].Items[0].Currency);

            Assert.AreEqual("Name4", user[3].Name);
            Assert.AreEqual(null, user[3].Items);

            Assert.AreEqual("Name5", user[4].Name);
            Assert.AreEqual(1, user[4].Items.Count);
            Assert.AreEqual(17, user[4].Items[0].Value);
            Assert.AreEqual("CHN", user[4].Items[0].Currency);

        }

        [Test]
        public void Test14()
        {
            var ones = Database.Query<One>()
                .IncludeMany(x => x.Items)
                .ToList();

        }
    }

    [TableName("Users")]
    public class Name
    {
        [Column("Name")]
        public virtual string _TheName { get; set; }
    }

    public class PerfTests
    {
        [Test]
        public void Test11()
        {
            var fakeReader = new FakeReader();

            var sw = Stopwatch.StartNew();

            for (int j = 0; j < 1000; j++)
            {
                var newPropertyMapper = new PropertyMapper();
                var pocoData = new PocoDataFactory((IMapper)null).ForType(typeof(NestedConvention));
                newPropertyMapper.Init(fakeReader, pocoData);

                for (int i = 0; i < 1000; i++)
                {
                    newPropertyMapper.Map(fakeReader, new RowMapperContext() { PocoData = pocoData });
                }
            }

            sw.Stop();

            Console.WriteLine(sw.ElapsedMilliseconds);
        }

        [Test]
        public void Test1()
        {
            var pocoData = new PocoDataFactory((IMapper)null).ForType(typeof(RecursionUser));

            Assert.AreEqual(4, pocoData.Members.Count);
            Assert.AreEqual("Id", pocoData.Members[0].Name);
            Assert.AreEqual("Name", pocoData.Members[1].Name);
            Assert.AreEqual("Supervisor", pocoData.Members[2].Name);
            Assert.AreEqual("CreatedBy", pocoData.Members[3].Name);
        }
    }

    [TableName("Ones"), PrimaryKey("Id")]
    public class One
    {
        public int Id { get; set; }
        public string Name { get; set; }

        [Reference(ReferenceMappingType.Many, Name =  "Id", ReferenceName = "One")]
        public List<Many> Items { get; set; }
    }

    [TableName("Manys"), PrimaryKey("Id")]
    public class Many
    {
        public int Id { get; set; }
        [Reference(ReferenceMappingType.Foreign, Name = "OneId", ReferenceName = "Id")]
        public One One { get; set; }
        public int Value { get; set; }
        public string Currency { get; set; }
    }

    public class RecursionUser
    {
        public int Id { get; set; }
        public string Name { get; set; }
        [Reference(ReferenceName = "Id")]
        public RecursionUser Supervisor { get; set; }
        [Reference(ReferenceName = "Id")]
        public RecursionUser CreatedBy { get; set; }
    }

    public class More : BaseDBFuentTest
    {
        [Test]
        public void Test1()
        {
            var result = Database
                .Query<User>()
                .Include(x => x.House)
                .Include(x => x.ExtraUserInfo)
                .ToEnumerable()
                .ToList();

            for (int i = 0; i < result.Count; i++)
            {
                AssertUserValues(InMemoryUsers[i], result[i]);
                AssertExtraUserInfo(InMemoryExtraUserInfos[i], result[i].ExtraUserInfo);
                AssertUserHouseValues(InMemoryUsers[i], result[i]);
            }
        }

        [Test]
        public void Test2()
        {
            var result = Database
                .Query<User>()
                .Include(x => x.ExtraUserInfo)
                .ToDynamicList();

            Assert.AreEqual(1, result[0].extrauserinfo__extrauserinfoid);
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

        public Money2 Money2 { get; set; }

        public decimal Value { get; set; }
        public string Currency { get; set; }
    }

    public class Money2
    {
        public decimal Value { get; set; }
        public string Currency { get; set; }
    }
}
