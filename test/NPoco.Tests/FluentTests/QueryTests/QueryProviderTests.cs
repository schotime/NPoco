using System.Globalization;
using System.Linq;
using System.Threading;
using NPoco.Linq;
using NPoco.Tests.Common;
using NUnit.Framework;

namespace NPoco.Tests.FluentTests.QueryTests
{
    [TestFixture]
    public class QueryProviderTests : BaseDBFuentTest
    {
        [Test]
        public void QueryAllData()
        {
            var users = Database.Query<User>().ToList();
            Assert.AreEqual(15, users.Count);
            for (int i = 0; i < users.Count; i++)
            {
                AssertUserValues(InMemoryUsers[i], users[i]);
            }
        }

        [Test]
        public void CountQuery()
        {
            var usersCount = Database.Query<User>().Count();
            Assert.AreEqual(15, usersCount);
        }

        [Test]
        public void AnyQuery()
        {
            var userRecordsExist = Database.Query<User>().Any();
            Assert.AreEqual(true, userRecordsExist);
        }

        [Test]
        public void AnyQueryWithWhere()
        {
            var userRecordsExist = Database.Query<User>().Any(x => x.UserId == 1);
            Assert.AreEqual(true, userRecordsExist);
        }

        [Test]
        public void AnyQueryWithLimit()
        {
            var userRecordsExist = Database.Query<User>().Limit(1).Any();
            Assert.AreEqual(true, userRecordsExist);
        }

        [Test]
        public void DistinctQueryWithProjection()
        {
            var userRecordsExist = Database.Query<User>().Distinct(y => new {y.IsMale});
            Assert.AreEqual(2, userRecordsExist.Count);
        }

        [Test]
        public void DistinctQueryWithProjectionAndLimit()
        {
            var userRecordsExist = Database.Query<User>().Limit(1).Distinct(y => new { y.IsMale });
            Assert.AreEqual(1, userRecordsExist.Count);
        }

        [Test]
        public void DistinctQueryWithSimpleProjection()
        {
            var userRecordsExist = Database.Query<User>().Distinct(y => y.IsMale);
            Assert.AreEqual(2, userRecordsExist.Count);
        }

        [Test]
        public void DistinctQuery()
        {
            var userRecordsExist = Database.Query<User>().Distinct();
            Assert.AreEqual(15, userRecordsExist.Count);
        }

        [Test]
        public void QueryWithWhereTrue()
        {
            var users = Database.Query<User>().Where(x => true).ToList();
            Assert.AreEqual(15, users.Count);
        }

        [Test]
        public void QueryWithSeparateWheresProduceSameSql()
        {
            var users1 = Database.Query<User>().Where(x => x.UserId == 1).Where(x => x.UserId == 2).ToList();
            var sql1 = ((Database)Database).LastSQL;
            var users2 = Database.Query<User>().Where(x => x.UserId == 1 && x.UserId == 2).ToList();
            var sql2 = ((Database)Database).LastSQL.Replace("((","(").Replace("))", ")");

            Assert.AreEqual(sql1, sql2);
        }

        [Test]
        public void QueryWithWhereFalse()
        {
            var users = Database.Query<User>().Where(x => false).ToList();
            Assert.AreEqual(0, users.Count);
        }

        [Test]
        public void QueryWithWhereUserIdAndTrue()
        {
            var users = Database.Query<User>().Where(x => x.UserId == 1 && true).ToList();
            Assert.AreEqual(1, users.Count);
            Assert.AreEqual(1, users[0].UserId);
        }

        [Test]
        public void QueryWithWhereChar()
        {
            var users = Database.Query<User>().Where(x => x.YorN == 'Y').ToList();
            Assert.AreEqual(8, users.Count);
        }

        [Test]
        public void QueryWithWhereCharNull()
        {
            var users = Database.Query<User>().Where(x => x.YorN == null).ToList();
            Assert.AreEqual(0, users.Count);
        }

        [Test]
        public void QueryWithWhereCharNullReversed()
        {
            var users = Database.Query<User>().Where(x => null == x.YorN).ToList();
            Assert.AreEqual(0, users.Count);
        }

        [Test]
        public void QueryWithWhereCharVar()
        {
            var s = 'Y';
            var users = Database.Query<User>().Where(x => x.YorN == s && x.Age > 0).ToList();
            Assert.AreEqual(8, users.Count);
        }

        [Test]
        public void QueryWithOrderBy()
        {
            var users = Database.Query<User>().OrderBy(x => x.DateOfBirth).ToList();
            var inmemory = InMemoryUsers.OrderBy(x => x.DateOfBirth).ToList();

            Assert.AreEqual(15, users.Count);
            for (int i = 0; i < users.Count; i++)
            {
                AssertUserValues(inmemory[i], users[i]);
            }
        }

        [Test]
        public void QueryWithOrderByDescending()
        {
            var users = Database.Query<User>().OrderByDescending(x => x.UserId).ToList();
            var inmemory = InMemoryUsers.OrderByDescending(x => x.UserId).ToList();

            Assert.AreEqual(15, users.Count);
            for (int i = 0; i < users.Count; i++)
            {
                AssertUserValues(inmemory[i], users[i]);
            }
        }

        [Test]
        public void QueryWithOrderByThenBy()
        {
            var users = Database.Query<User>().Include(x => x.House).OrderBy(x => x.House.HouseId).ThenBy(x => x.UserId).ToList();
            var inmemory = InMemoryUsers.OrderBy(x => x.House != null ? x.House.HouseId : -1).ThenBy(x => x.UserId).ToList();

            Assert.AreEqual(15, users.Count);
            for (int i = 0; i < users.Count; i++)
            {
                AssertUserValues(inmemory[i], users[i]);
            }
        }

        [Test]
        public void QueryWithOrderByThenByDescending()
        {
            var users = Database.Query<User>().Include(x => x.House).OrderBy(x => x.House.HouseId).ThenByDescending(x => x.UserId).ToList();
            var inmemory = InMemoryUsers.OrderBy(x => x.House != null ? x.House.HouseId : -1).ThenByDescending(x => x.UserId).ToList();

            Assert.AreEqual(15, users.Count);
            for (int i = 0; i < users.Count; i++)
            {
                AssertUserValues(inmemory[i], users[i]);
            }
        }

        [Test]
        public void QueryWithOrderByDescendingThenBy()
        {
            var users = Database.Query<User>().Include(x=>x.House).OrderByDescending(x => x.House.HouseId).ThenBy(x => x.UserId).ToList();
            var inmemory = InMemoryUsers.OrderByDescending(x => x.House != null ? x.House.HouseId : -1).ThenBy(x => x.UserId).ToList();

            Assert.AreEqual(15, users.Count);
            for (int i = 0; i < users.Count; i++)
            {
                AssertUserValues(inmemory[i], users[i]);
            }
        }

        [Test]
        public void QueryWithOrderByDescendingThenByDescending()
        {
            var users = Database.Query<User>().Include(x => x.House).OrderByDescending(x => x.House.HouseId).ThenByDescending(x => x.UserId).ToList();
            var inmemory = InMemoryUsers.OrderByDescending(x => x.House != null ? x.House.HouseId : -1).ThenByDescending(x => x.UserId).ToList();

            Assert.AreEqual(15, users.Count);
            for (int i = 0; i < users.Count; i++)
            {
                AssertUserValues(inmemory[i], users[i]);
            }
        }

        [Test]
        public void QueryWithLimit()
        {
            var users = Database.Query<User>().OrderBy(x=>x.UserId).Limit(5).ToList();
            var inmemory = InMemoryUsers.OrderBy(x=>x.UserId).Take(5).ToList();

            Assert.AreEqual(5, users.Count);
            for (int i = 0; i < users.Count; i++)
            {
                AssertUserValues(inmemory[i], users[i]);
            }
        }

        [Test]
        public void QueryWithLimitWithOffset()
        {
            var users = Database.Query<User>().OrderBy(x => x.UserId).Limit(5,5).ToList();
            var inmemory = InMemoryUsers.OrderBy(x => x.UserId).Skip(5).Take(5).ToList();

            Assert.AreEqual(5, users.Count);
            for (int i = 0; i < users.Count; i++)
            {
                AssertUserValues(inmemory[i], users[i]);
            }
        }

        [Test]
        public void QueryWithWhere()
        {
            var users = Database.Query<User>().Where(x => x.UserId > 10).ToList();
            var inmemory = InMemoryUsers.Where(x => x.UserId > 10).ToList();

            Assert.AreEqual(5, users.Count);
            for (int i = 0; i < users.Count; i++)
            {
                AssertUserValues(inmemory[i], users[i]);
            }
        }

        [Test]
        public void QueryWithWhereReversed()
        {
            var users = Database.Query<User>().Where(x => 10 < x.UserId).ToList();
            var inmemory = InMemoryUsers.Where(x => 10 < x.UserId).ToList();

            Assert.AreEqual(5, users.Count);
            for (int i = 0; i < users.Count; i++)
            {
                AssertUserValues(inmemory[i], users[i]);
            }
        }

        [Test]
        public void QueryWithWhereAnd()
        {
            var users = Database.Query<User>().Where(x => x.UserId > 10 && x.UserId < 12).ToList();
            var inmemory = InMemoryUsers.Where(x => x.UserId > 10 && x.UserId < 12).ToList();

            Assert.AreEqual(1, users.Count);
            for (int i = 0; i < users.Count; i++)
            {
                AssertUserValues(inmemory[i], users[i]);
            }
        }

        [Test]
        public void QueryWithInclude()
        {
            var users = Database.Query<User>().Include(x=>x.House).ToList();

            Assert.AreEqual(15, users.Count);
            for (int i = 0; i < users.Count; i++)
            {
                AssertUserValues(InMemoryUsers[i], users[i]);
                AssertUserHouseValues(InMemoryUsers[i], users[i]);
            }
        }

        [Test]
        public void QueryWithIncludeOneToOne()
        {
            var users = Database.Query<User>().Include(x => x.ExtraUserInfo).ToList();

            Assert.AreEqual(15, users.Count);
            for (int i = 0; i < users.Count; i++)
            {
                AssertUserValues(InMemoryUsers[i], users[i]);
                AssertExtraUserInfo(InMemoryExtraUserInfos[i], users[i].ExtraUserInfo);
            }
        }

        [Test]
        public void QueryWithIncludeAndNestedWhere()
        {
            var users = Database.Query<User>().Include(x => x.House).Where(x=> x.House.Address == InMemoryHouses[0].Address).ToList();
            var inmemory = InMemoryUsers.Where(x => x.House != null && x.House.Address == InMemoryHouses[0].Address).ToList();

            Assert.AreEqual(1, users.Count);
            for (int i = 0; i < users.Count; i++)
            {
                AssertUserValues(inmemory[i], users[i]);
                AssertUserHouseValues(inmemory[i], users[i]);
            }
        }

        [Test]
        public void QueryWithIncludeAndNestedOrderBy()
        {
            var users = Database.Query<User>().Include(x => x.House).Where(x=>x.House != null).OrderBy(x => x.House.Address).ToList();
            var inmemory = InMemoryUsers.Where(x => x.House != null).OrderBy(x => x.House.Address).ToList();

            Assert.AreEqual(7, users.Count);
            for (int i = 0; i < users.Count; i++)
            {
                AssertUserValues(inmemory[i], users[i]);
                AssertUserHouseValues(inmemory[i], users[i]);
            }
        }

        private static void SetCurrentCulture(CultureInfo culture)
        {
#if NET35 || NET40 || NET45 || NET451 || NET452 || NET462 || DNX451 || DNX452
            // In the .NET Framework 4.5.2 and earlier versions, the CurrentCulture property is read-only
            Thread.CurrentThread.CurrentCulture = culture;
#else
            // Starting with the .NET Framework 4.6, the CurrentCulture property is read-write
            // and Core does not have Thread.CurrentThread?
            CultureInfo.CurrentCulture = culture;
#endif
        }

        [Test]
        public void QueryWithProjectionAndEnclosedMethod()
        {
            var culture = CultureInfo.CurrentCulture;

            try
            {
                // pretend the current culture is en-US
                // else the test may fail on non-english envt due to comma vs dot in numbers
                SetCurrentCulture(new CultureInfo("en-US"));

                var users = Database.Query<User>()
                    .ProjectTo(x => new ProjectUser2 { FormattedAge = string.Format("{0:n}", x.Age) });

                Assert.AreEqual("21.00", users[0].FormattedAge);
                Assert.AreEqual(15, users.Count);
            }
            finally
            {
                // restore
                SetCurrentCulture(culture);
            }
        }

        [Test]
        public void QueryWithProjectionAndEnclosedMethod1()
        {
            var culture = CultureInfo.CurrentCulture;

            try
            {
                // pretend the current culture is en-US
                // else the test may fail on non-english envt due to comma vs dot in numbers
                SetCurrentCulture(new CultureInfo("en-US"));

                // use enough args to force the string.Format overload with "params object[]" args,
                // these arguments are properly supported (ProcessMethodSearchRecursively supports
                // NewArrayExpression).
                var users = Database.Query<User>()
                    .ProjectTo(x => new ProjectUser2 { FormattedAge = string.Format("{0:n} {1:n} {2:n} {3:n} {4:n} {5:n} {6:n}",
                        x.Age, x.Age, x.Age, x.Age, x.Age, x.Age, x.Age) });

                Assert.AreEqual("21.00 21.00 21.00 21.00 21.00 21.00 21.00", users[0].FormattedAge);
                Assert.AreEqual(15, users.Count);
            }
            finally
            {
                // restore
                SetCurrentCulture(culture);
            }
        }

        [Test]
        public void QueryWithProjectionAndEnclosedMethod2()
        {
            var users = Database.Query<User>()
                .ProjectTo(x => new ProjectUser2 { FormattedAge = FormatAge(x) });

            Assert.AreEqual("Age: 21, IsMale: True", users[0].FormattedAge);
            Assert.AreEqual(15, users.Count);
        }

        [Test]
        public void QueryWithProjectionAndEnclosedMethod3()
        {
            var users = Database.Query<User>()
                .ProjectTo(x => new ProjectUser2 { FormattedAge = FormatAge2(string.Format("{0}", string.Format("{0}", x.Age))) });

            Assert.AreEqual("Age: 21", users[0].FormattedAge);
            Assert.AreEqual(15, users.Count);
        }

        [Test]
        public void QueryWithProjectionAndEnclosedMethod4()
        {
            var users = Database.Query<User>()
                .ProjectTo(x => new ProjectUser2 { FormattedAge = x.Age + FormatAge(x) });

            Assert.AreEqual("21Age: 21, IsMale: True", users[0].FormattedAge);
            Assert.AreEqual(15, users.Count);
        }

        private string FormatAge2(string u)
        {
            return string.Format("Age: {0}", u);
        }

        private string FormatAge(User u)
        {
            return string.Format("Age: {0}, IsMale: {1}", u.Age, u.IsMale);
        }

        [Test]
        public void QueryWithProjectionAndMethod()
        {
            var users = Database.Query<User>()
                .ProjectTo(x => new ProjectUser2 { Age = x.Age, Date = x.DateOfBirth.ToString("yyyy-MM-dd") });

            Assert.AreEqual(21, users[0].Age);
            Assert.AreEqual("1969-01-01", users[0].Date);
            Assert.AreEqual(15, users.Count);
        }

        [Test]
        public void QueryWithProjection()
        {
            var users = Database.Query<User>()
                .ProjectTo(x => new ProjectUser2 { Age = x.Age });

            Assert.AreEqual(21, users[0].Age);
            Assert.AreEqual(15, users.Count);
        }

        [Test]
        public void QueryWithProjectionAndLimit()
        {
            var users = Database.Query<User>()
                .Limit(10)
                .ProjectTo(x => new { x.Age });

            Assert.AreEqual(21, users[0].Age);
            Assert.AreEqual(10, users.Count);
        }

        public class ProjectUser2
        {
            public int Age { get; set; }
            public string FormattedAge { get; set; }
            public string Date { get; set; }
        }

        [Test]
        public void QueryWithIncludeNestedOrderByLimitAndProjection()
        {
            var users = Database.Query<User>()
                .Include(x => x.House)
                .Where(x => x.House != null)
                .OrderBy(x => x.House.HouseId)
                .Limit(5)
                .ProjectTo(x => new { Address = (x.House != null ? x.House.Address : string.Empty), x.House.HouseId});

            var inmemory = InMemoryUsers.Where(x => x.House != null).OrderBy(x => x.House.HouseId).Select(x => new {x.House.Address, x.House.HouseId}).ToList();

            Assert.AreEqual(5, users.Count);
            for (int i = 0; i < users.Count; i++)
            {
                Assert.AreEqual(inmemory[i].Address, users[i].Address);
                Assert.AreEqual(inmemory[i].HouseId, users[i].HouseId);
            }
        }

        [Test]
        public void QueryWithIncludeNestedOrderByLimitAndProjectionToProjectUser()
        {
            var users = Database.Query<User>()
                .Include(x => x.House)
                .Where(x => x.House.HouseId > 2)
                .OrderBy(x => x.House.HouseId)
                .Limit(5)
                .ProjectTo(x => new ProjectUser() { UserId = x.UserId, NameWithAge = x.Name + x.Age });

            var inmemory = InMemoryUsers.Where(x => x.House != null).OrderBy(x => x.House.HouseId).ToList();

            Assert.AreEqual(4, users.Count);
            for (int i = 0; i < users.Count; i++)
            {
                var inMem = inmemory.First(x => x.UserId == users[i].UserId);
                Assert.AreEqual(inMem.Name + inMem.Age, users[i].NameWithAge);
            }
        }

        [Test]
        public void QueryWithIncludeNestedOrderByLimitAndProjectionToProjectUserWithList()
        {
            var users = Database.Query<User>()
                .Include(x => x.House)
                .Where(x => x.House != null)
                .OrderBy(x => x.House.HouseId)
                .Limit(5)
                .ProjectTo(x => new ProjectUser() { Array = new object[] { x.Name, x.Age } });

            var inmemory = InMemoryUsers.Where(x => x.House != null).OrderBy(x => x.House.HouseId).ToList();

            Assert.AreEqual(5, users.Count);
            for (int i = 0; i < users.Count; i++)
            {
                Assert.AreEqual(inmemory[i].Name, users[i].Array[0]);
                Assert.AreEqual(inmemory[i].Age, users[i].Array[1]);
            }
        }

        [Test]
        public void QueryWithInheritedTypesAliasCorrectly()
        {
            var users = Database.Query<Supervisor>().Where(x => x.UserId == 1).ToList();
            Assert.AreEqual(users.Count, 1);
        }

        [Test]
        public void QueryUsingForWhereQueryBuilder()
        {
            var queryBuilder = new QueryBuilder<User>().Where(x => x.UserId == 1);
            var user = Database.Query<User>().From(queryBuilder).Single();
            Assert.AreEqual(1, user.UserId);
        }

        [Test]
        public void QueryWithWhereContainsStartsWithUnderscore()
        {
            var houses = Database.Query<House>().Where(o => o.Address.StartsWith("_")).ToList();
            Assert.AreEqual(1, houses.Count);
        }

        [Test]
        public void QueryWithWhereContainsStartsWithEscapeChar()
        {
            var houses = Database.Query<House>().Where(o => o.Address.Contains("\\")).ToList();
            Assert.AreEqual(1, houses.Count);
        }

        [Test]
        public void QueryWithIncludeInheritedReturnsNotNullObject()
        {
            var ex = Database.Query<Supervisor>().Include(i => i.House).ToList();
            Assert.NotNull(ex[1].House);
        }

        //[Test]
        //public void QueryWithInheritedTypesAliasCorrectlyWithJoin()
        //{
        //    var users = Database.Query<User>()
        //        .Include(x=>x.Supervisor, (user, supervisor) => user.SupervisorId == supervisor.UserId)
        //        .Where(x => x.UserId.In(new[] {1,2}))
        //        .ToList();

        //    Assert.AreEqual(users.Count, 2);
        //    Assert.NotNull(users[1].Supervisor);
        //}
    }

    public class ProjectUser
    {
        public string NameWithAge { get; set; }
        public object[] Array { get; set; }
        public int UserId { get; set; }
    }

    public class Usersss
    {
        public int UserId { get; set; }
    }
}

//VB Tests for query provider
//Imports NPoco
//Imports NPoco.Expressions

//Module Module1
//    Sub Main()
//        Dim Db = New Database("asdf", "System.Data.SqlClient")
//        Dim exp = New DefaultSqlExpression (Of User)(Db)
//        Dim whered = exp.Where(Function(item) (item.Name = "Test"))
//        Console.WriteLine(whered.Context.ToSelectStatement())
//    End Sub
//End Module

//Public Class User
//    Public Property Name As String
//End Class
