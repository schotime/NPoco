using System;
using System.Collections.Generic;
using System.Linq;
using NPoco;
using NPoco.Expressions;
using NPoco.Linq;
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
        public void Test4_1()
        {
            var data = Database.Fetch<string[]>("select 'Name' Name, 1 poco_rn, 'AUD' money__currency /*poco_dual*/").Single();
            Assert.AreEqual("Name", data[0]);
            Assert.AreEqual("AUD", data[1]);
        }

        [Test]
        public void Test4_2()
        {
            var data = Database.Fetch<string[]>("select 'Name' Name, null npoco_wow, '4' Day, 'AUD' money__currency /*poco_dual*/").Single();
            Assert.AreEqual("Name", data[0]);
            Assert.AreEqual("4", data[1]);
            Assert.AreEqual("AUD", data[2]);
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
            var data = Database.Fetch<NestedConvention>("select 'Name' Name, 23 money__value, 'AUD' money__currency, 24 money__money2__value, 'USD' money__money2__currency /*poco_dual*/").Single();
            Assert.AreEqual("Name", data.Name);
            Assert.AreEqual(23, data.Money.Value);
            Assert.AreEqual("AUD", data.Money.Currency);
            Assert.AreEqual(24, data.Money.Money2.Value);
            Assert.AreEqual("USD", data.Money.Money2.Currency);
        }

        [Test]
        public void Test10()
        {
            var data = Database.Fetch<NestedConvention>("select 'Name' Name, 24 money__money2__value, 'USD' money__money2__currency /*poco_dual*/").Single();
            Assert.AreEqual("Name", data.Name);
            Assert.AreEqual(0, data.Money.Value);
            Assert.AreEqual(null, data.Money.Currency);
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
            Assert.AreEqual(new DateTime(1971, 01, 01, 0, 0, 0), data.Address.MovedInOn);
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
                    Assert.AreEqual(i % 3, ones[i].Items.Count);
            }
        }

        [Test]
        public void Test16()
        {
            var users = Database.Query<RecursionUser>()
                .UsingAlias("TEST")
                .Include(x => x.CreatedBy, "CREATEDBY")
                .Where(x => x.Id.In(new[] { 2, 3 }))
                .Where("CREATEDBY.Id in (@list)", new { list = new[] { 1, 3 } })
                .ToList();

            Database.Query<RecursionUser>()
                .UsingAlias("TEST1")
                .Include(x => x.CreatedBy)
                .Where(x => x.Id.In(new[] { 2, 3 }))
                .Where("RU4.Id in (@list)", new { list = new[] { 1, 3 } })
                .ToList();

            Database.Query<RecursionUser>()
                .Where(x => new Sql(string.Format("{0}.Id in (@list)", x.GetAliasFor(z => z)), new { list = new[] { 1, 3 } }))
                .ToList();

            Database.Query<RecursionUser>()
                .Include(x => x.CreatedBy)
                .Where(x => new Sql(string.Format("{0}.Id in (@list)", x.GetAliasFor(z => z.CreatedBy)), new { list = new[] { 1, 3 } }))
                .ToList();

            Database.Query<RecursionUser>()
                .Include(x => x.CreatedBy)
                .IncludeSecurity()
                .ToList();

            Assert.AreEqual(2, users.Count);
        }

        [Test]
        public void Test17()
        {
            var ones = Database.FetchOneToMany<One>(x => x.Items, @"
select o.*, null npoco_items, m.*
from ones o
left join manys m on o.oneid = m.oneid");

            Assert.AreEqual(15, ones.Count);
            AssertOnes(ones);
        }

        [Test]
        public void Test17_1()
        {
            var ones = Database.FetchOneToMany<One>(x => x.Items, @"
select o.*, 'MyName' nested__name, null npoco_items, m.*
from ones o
left join manys m on o.oneid = m.oneid");

            Assert.AreEqual(15, ones.Count);
            Assert.AreEqual("MyName", ones[0].Nested.Name);
            AssertOnes(ones);
        }

        [Test]
        public void Test17_2()
        {
            var ones = Database.FetchOneToMany<One>(x => x.Items, @"
select o.*, 'MyName' nested__name, m.*
from ones o
left join manys m on o.oneid = m.oneid");

            Assert.AreEqual(15, ones.Count);
            Assert.AreEqual("MyName", ones[0].Nested.Name);
            AssertOnes(ones);
        }

        private static void AssertOnes(List<One> ones)
        {
            Assert.AreEqual(null, ones[0].Items);
            Assert.AreEqual(1, ones[1].Items.Count);
            Assert.NotNull(ones[1].Items[0].Currency);
            Assert.AreEqual(2, ones[2].Items.Count);
        }

        [Test]
        public void Test18()
        {
            var data = Database.Fetch<RecursionUser>(@"
select r.*
, null npoco_createdby, createdby1.*
, null npoco_supervisor, supervisor1.*
, null npoco_createdby__supervisor, supervisor2.*
, null npoco_createdby__createdby, createdby3.*
, null npoco_supervisor__createdby, createdby2.*
, null npoco_supervisor__supervisor, supervisor3.*
from RecursionUser r
    inner join RecursionUser  createdby1 on r.createdbyid = createdby1.Id
    inner join RecursionUser supervisor1 on r.supervisorid = supervisor1.Id
    inner join RecursionUser supervisor2 on createdby1.supervisorid = supervisor2.Id
    inner join RecursionUser  createdby2 on supervisor1.createdbyid = createdby2.Id
    inner join RecursionUser supervisor3 on createdby2.supervisorid = supervisor3.Id
    inner join RecursionUser  createdby3 on supervisor2.createdbyid = createdby3.Id
");

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
        public void Test18_1()
        {
            var data = Database.Fetch<RecursionUser2>(@"
select r.*
, null npoco_createdby, createdby1.*
, null npoco_supervisor, supervisor1.*
, null npoco_createdby__supervisor, supervisor2.*
, null npoco_createdby__createdby, createdby3.*
, null npoco_supervisor__createdby, createdby2.*
, null npoco_supervisor__supervisor, supervisor3.*
from RecursionUser r
    inner join RecursionUser  createdby1 on r.createdbyid = createdby1.Id
    inner join RecursionUser supervisor1 on r.supervisorid = supervisor1.Id
    inner join RecursionUser supervisor2 on createdby1.supervisorid = supervisor2.Id
    inner join RecursionUser  createdby2 on supervisor1.createdbyid = createdby2.Id
    inner join RecursionUser supervisor3 on createdby2.supervisorid = supervisor3.Id
    inner join RecursionUser  createdby3 on supervisor2.createdbyid = createdby3.Id
");

            for (int i = 0; i < data.Count; i++)
            {
                Assert.AreEqual("Name" + (i + 1), data[i].TheName);
                Assert.AreEqual("Name" + 1, data[i].CreatedBy.TheName);
                Assert.AreEqual("Name" + 2, data[i].CreatedBy.Supervisor.TheName);
                Assert.AreEqual("Name" + 1, data[i].CreatedBy.CreatedBy.TheName);
                Assert.AreEqual("Name" + 2, data[i].Supervisor.TheName);
                Assert.AreEqual("Name" + 1, data[i].Supervisor.CreatedBy.TheName);
                Assert.AreEqual("Name" + 2, data[i].Supervisor.Supervisor.TheName);
            }
        }

        [Test]
        public void Test18_2()
        {
            var data = Database.Fetch<RecursionUser2>(@"select r.* from RecursionUser r");

            for (int i = 0; i < data.Count; i++)
            {
                Assert.AreEqual((i + 1), data[i].TheId);
                Assert.AreEqual("Name" + (i + 1), data[i].TheName);
            }
        }

        [Test]
        public void Test19()
        {
            var nestedConvention = new NestedConvention() { Name = "Name1" };
            Database.SingleInto(nestedConvention, @"
select null name /*poco_dual*/");

            Assert.AreEqual(null, nestedConvention.Name);
        }

        [Test]
        public void Test20()
        {
            var nestedConvention = new NestedConvention() { Money = new Models.Money() { Currency = "AUD" } };
            Database.SingleInto(nestedConvention, @"
select 22 Money__Value /*poco_dual*/");

            Assert.AreEqual(22, nestedConvention.Money.Value);
            Assert.AreEqual("AUD", nestedConvention.Money.Currency);
        }

        [Test]
        public void Test21()
        {
            var data = Database.Fetch(typeof(NestedConvention), "select 'Name' Name, 23 money__value, 'AUD' money__currency /*poco_dual*/").Cast<NestedConvention>().Single();
            Assert.AreEqual("Name", data.Name);
            Assert.AreEqual(23, data.Money.Value);
            Assert.AreEqual("AUD", data.Money.Currency);
        }

        [Test]
        public void Test22()
        {
            var data = Database.Query<MyUserDec>()
                .Include(x => x.House)
                .ToList();

            Assert.AreEqual(15, data.Count);
            Assert.AreEqual(2, data[1].HouseId);
            Assert.AreEqual(2, data[1].House.HouseId);
            Assert.NotNull(data[1].House.Address);
        }

        [Test]
        public void Test22_1()
        {
            var data = Database.Query<MyUserDec>()
                .Include(x => x.House)
                .ProjectTo(x => x.House)
                .ToList();

            Assert.AreEqual(15, data.Count);
            Assert.AreEqual(2, data[1].HouseId);
            Assert.AreEqual("1 Road Street, Suburb", data[1].Address);
        }

        [TableName("Users"), PrimaryKey("UserId")]
        public class MyUserDec
        {
            public int UserId { get; set; }
            public int HouseId { get; set; }

            [Reference(ReferenceType.OneToOne, ColumnName = "HouseId", ReferenceMemberName = "HouseId")]
            public HouseDecorated House { get; set; }
        }

        [Test]
        public void Test23()
        {
            Database.Mappers.ClearFactories(typeof(ContentBase));
            Database.Mappers.RegisterFactory<ContentBase>(reader => new Post());
            var data = Database.Fetch<ContentBase>("select 'Name' Name /*poco_dual*/").Single();
            Assert.AreEqual("Name", data.Name);
        }

        [Test]
        public void Test24()
        {
            Database.Mappers.ClearFactories(typeof(IContentBase));
            Database.Mappers.RegisterFactory<IContentBase>(reader => new Post());
            var data = Database.Fetch<IContentBase>("select 'Name' Name /*poco_dual*/").Single();
            Assert.AreEqual("Name", data.Name);
        }

        [Test]
        public void Test25()
        {
            Database.Mappers.ClearFactories(typeof(ContentBase));
            Database.Mappers.RegisterFactory<ContentBase>(reader =>
            {
                var type = (string)reader["type"];
                if (type == "Post")
                    return new Post();
                if (type == "Answer")
                    return new Answer();
                return null;
            });
            var data = Database.Fetch<ContentBase>(@"
select 'NamePost' Name, 'Post' type /*poco_dual*/
union
select 'NameAnswer' Name, 'Answer' type /*poco_dual*/
").ToList();

            Assert.AreEqual("NamePost", data[0].Name);
            Assert.AreEqual("Post", data[0].Type);
            Assert.AreEqual("NameAnswer", data[1].Name);
            Assert.AreEqual("Answer", data[1].Type);
            Assert.True(data[0] is Post);
            Assert.True(data[1] is Answer);

        }

        [Test]
        public void Test26()
        {
            var data = Database.Fetch<OldConv>("select 3 Id, 'Name1' Name, 'Name2' Name, 'Name4' Name, 'Name3' Name").Single();
            Assert.AreEqual(3, data.Id);
            Assert.AreEqual("Name1", data.Name);
            Assert.AreEqual("Name2", data.Nest1.Name);
            Assert.AreEqual("Name4", data.Nest1.Nest3.Name);
            Assert.AreEqual("Name3", data.Nest2.Name);
        }

        [Test]
        public void Test26_1()
        {
            var data = Database.Fetch<OldConv>("select 3 Id, 4 Id, 'Name2' Name, 'Name4' Name, 'Name3' Name").Single();
            Assert.AreEqual(3, data.Id);
            Assert.AreEqual(4, data.Nest1.Id);
            Assert.AreEqual("Name2", data.Nest1.Name);
            Assert.AreEqual("Name4", data.Nest1.Nest3.Name);
            Assert.AreEqual("Name3", data.Nest2.Name);
        }

        public class OldConv
        {
            public int Id { get; set; }
            public string Name { get; set; }

            public NestOldConv1 Nest1 { get; set; }
            public NestOldConv2 Nest2 { get; set; }

            public class NestOldConv1
            {
                public int Id { get; set; }
                public string Name { get; set; }
                public NestOldConv2 Nest3 { get; set; }
            }

            public class NestOldConv2
            {
                public string Name { get; set; }
            }
        }

        [Test]
        public void Test27()
        {
            var data = Database.Fetch<Test27Class>("select 3 Id, 'Name' Name").Single();
            Assert.AreEqual(3, data.Id);
            Assert.AreEqual("3", data.Name);
        }

        public class Test27Class
        {
            public int Id { get; set; }
            public string Name { get { return Id.ToString(); } }
        }

        [Test]
        public void Test28()
        {
            var data = Database.Fetch<Test28Class>("select 3 Id, 'Name' Name, null, 'dyn' Dynamic__Value1, 'dict' Dict__Value2").Single();
            Assert.AreEqual(3, data.Id);
            Assert.AreEqual("Name", data.Name);
            Assert.AreEqual("dyn", data.Dynamic.Value1);
            Assert.AreEqual("dict", data.Dict["Value2"]);
        }

        public class Test28Class
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public dynamic Dynamic { get; set; }
            public Dictionary<string, object> Dict { get; set; }
        }

        [Test]
        public void Test29()
        {
            var data = Database.Fetch<Test29Class>("select 3 Id, 4 Dyn").Single();
            Assert.AreEqual(3, data.Id);
            Assert.AreEqual(4, data.Dyn);
        }

        public class Test29Class
        {
            public object Id { get; set; }
            public dynamic Dyn { get; set; }
        }

        [Test]
        public void Test30()
        {
            var data = Database.Fetch<UserWithPrivateParamLessConstructor>("select * from users");
            Assert.AreEqual(15, data.Count);
        }

        [Test]
        public void Test31()
        {
            Assert.Throws<Exception>(() =>
            {
                var fastCreate = new FastCreate(typeof(ContentBase), new MapperCollection());
            });
        }

        [Test]
        public void Test32()
        {
            var data = Database.Insert(new NoPrimaryKey());
        }

        [Test]
        public void Test33()
        {
            var users = Database.Query<UserDecorated>()
                .Where(x=> new Sql($"{x.DatabaseType.EscapeTableName(x.PocoData.TableInfo.AutoAlias)}.UserId in (@list)", new {list = new[] {2}}))
                .Where(x => x.UserId.In(new[] { 1, 2 }))
                .OrderBy(x => x.UserId)
                .ToList();

            Assert.AreEqual(1, users.Count);
            Assert.AreEqual(2, users[0].UserId);
        }

        [Test]
        public void Test34()
        {
            var users = Database.Query<UserDecorated>()
                .Where(x => x.UserId.In(new[] { 2 }))
                .Where(x => new Sql($"{x.DatabaseType.EscapeTableName(x.PocoData.TableInfo.AutoAlias)}.UserId in (@list)", new { list = new[] { 1, 2 } }))
                .OrderBy(x => x.UserId)
                .ToList();

            Assert.AreEqual(1, users.Count);
            Assert.AreEqual(2, users[0].UserId);
        }
    }

    public class NoPrimaryKey
    {
        public string Name { get; set; }
    }

    public class Post : ContentBase
    {

    }

    public class Answer : ContentBase
    {

    }

    public abstract class ContentBase : IContentBase
    {
        public string Name { get; set; }
        public string Type { get; set; }
    }

    public interface IContentBase
    {
        string Name { get; set; }
    }

    public static class Ext
    {
        public static IQueryProvider<RecursionUser> IncludeSecurity(this IQueryProvider<RecursionUser> query)
        {
            return query.Where(x => new Sql(string.Format("exists (select 1 from {1} where Id = {0}.Id)", x.DatabaseType.EscapeTableName(x.GetAliasFor(z => z.CreatedBy)), x.GetPocoDataFor<RecursionUser>().TableInfo.TableName)));
        }
    }
}
