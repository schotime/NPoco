using System.Linq;
using NPoco.Expressions;
using NPoco.Tests.Common;
using NUnit.Framework;

namespace NPoco.Tests.FluentTests.QueryTests
{
    [TestFixture]
    public class ExpressionFluentTests : BaseDBFuentTest
    {
        [Test]
        public void FetchByExpressionAdvanced()
        {
            var users = Database.Query<User>().Where(y => y.UserId > 10).OrderBy(x => x.UserId).ToList();

            Assert.AreEqual(5, users.Count);
            for (int i = 0; i < users.Count; i++)
            {
                AssertUserValues(InMemoryUsers[i + 10], users[i]);
            }
        }

        [Test]
        public void FetchWhereExpression()
        {
            var users = Database.Query<User>().Where(y => y.UserId == 2 && !y.IsMale).ToList();
            Assert.AreEqual(1, users.Count);
        }

        [Test]
        public void FetchWhereExpressionEquals()
        {
            var users = Database.Query<User>().Where(y => y.UserId.Equals(2)).ToList();
            Assert.AreEqual(1, users.Count);
        }

        //[Test, NUnit.Framework.Ignore("Not Supported For Now")]
        public void FetchOnWithSecondGenericType()
        {
            var s = new DefaultSqlExpression<CustomerUserJoin>(Database, true);
            var joinexp = s.On<CustomerUser>((x, y) => x.Name == y.CustomerName);
            
            string expected = string.Format("({0}.{1} = {2}.{3})", 
                TestDatabase.DbType.EscapeTableName("CUJ"),
                TestDatabase.DbType.EscapeTableName("Name"),
                TestDatabase.DbType.EscapeTableName("CU"),
                TestDatabase.DbType.EscapeTableName("CustomerName"));

            Assert.AreEqual(expected, joinexp);
        }

        [Test]
        public void FetchByExpressionAndColumnAlias()
        {
            var users = Database.Query<UserDecorated>().Where(x => x.IsMale).OrderBy(x => x.UserId).ToList();
            Assert.AreEqual(8, users.Count);
        }

        [Test]
        public void FetchByExpressionWithParametersAndOrderBy()
        {
            var users = Database.Query<UserDecorated>()
                .Where(x => x.Name == "Name1")
                .OrderBy(x => x.UserId)
                .ProjectTo(x => new { x.Name });
            Assert.AreEqual(1, users.Count);
            Assert.AreEqual("Name1", users[0].Name);
        }

        [Test]
        public void FetchByExpressionAndLimit()
        {
            var users = Database.Query<UserDecorated>().OrderBy(x => x.UserId).Limit(5, 5).ToList();
            Assert.AreEqual(5, users.Count);
        }

        [Test]
        public void FetchByExpressionAndSubstringInWhere()
        {
            var users = Database.Query<UserDecorated>().Where(x => x.Name.Substring(0, 4) == "Name").ToList();
            Assert.AreEqual(15, users.Count);
        }

        [Test]
        public void FetchByExpressionAndSubstringAndUpper()
        {
            var users = Database.Query<UserDecorated>().Where(x => x.Name.ToUpper().Substring(0, 4) == "NAME").ToList();
            Assert.AreEqual(15, users.Count);
        }

        [Test]
        public void FetchByExpressionAndLower()
        {
            var users = Database.Query<UserDecorated>().Where(x => x.Name.ToLower() == "name1").ToList();
            Assert.AreEqual(1, users.Count);
        }

        [Test]
        public void FetchByExpressionAndContains()
        {
            var users = Database.Query<UserDecorated>().Where(x => x.Name.Contains("ame")).ToList();
            Assert.AreEqual(15, users.Count);
        }

        [Test]
        public void FetchByExpressionAndStartsWith()
        {
            var users = Database.Query<UserDecorated>().Where(x => x.Name.StartsWith("Na")).ToList();
            Assert.AreEqual(15, users.Count);
        }
       
        [Test]
        public void FetchByExpressionAndDoesNotStartsWith()
        {
            var users = Database.Query<UserDecorated>().Where(x => !x.Name.StartsWith("Na")).ToList();
            Assert.AreEqual(0, users.Count);
        }

        [Test]
        public void FetchByExpressionAndEndsWith()
        {
            var users = Database.Query<UserDecorated>().Where(x => x.Name.EndsWith("e2")).ToList();
            Assert.AreEqual(1, users.Count);
        }

        [Test]
        public void FetchByExpressionAndSelectWithSubstring()
        {
            var users = Database.Query<UserDecorated>().ProjectTo(x => new {Name = x.Name.Substring(0, 2)}).ToList();
            Assert.AreEqual("Na", users[0].Name);
        }

        [Test]
        public void FetchByExpressionAndSelectWithSubstring2()
        {
            var users = Database.Query<UserDecorated>().ProjectTo(x => new { Name = x.Name.Substring(2) }).ToList();
            Assert.AreEqual("me1", users[0].Name);
        }

        [Test]
        public void FetchByExpressionAndSelectWithLower()
        {
            var users = Database.Query<UserDecorated>().ProjectTo(x => new { Name = x.Name.ToLower() }).ToList();
            Assert.AreEqual("name1", users[0].Name);
        }

        [Test]
        public void FetchByExpressionAndSelectWithUpper()
        {
            var users = Database.Query<UserDecorated>().ProjectTo(x => new { Name = x.Name.ToUpper() }).ToList();
            Assert.AreEqual("NAME1", users[0].Name);
        }

        [Test]
        public void FetchByExpressionAndSelect()
        {
            var users = Database.Query<UserDecorated>().ProjectTo(x => new { x.Name }).ToList();
            Assert.AreEqual("Name1", users[0].Name);
        }

        [Test]
        public void FetchWithWhereExpressionContains()
        {
            var list = new[] {1, 2, 3, 4};
            var users = Database.Query<User>().Where(x => list.Contains(x.UserId)).ToList();

            Assert.AreEqual(4, users.Count);
            for (int i = 0; i < users.Count; i++)
            {
                AssertUserValues(InMemoryUsers[i], users[i]);
            }
        }

        [Test]
        public void FetchWithWhereExpressionContainsWithNullable()
        {
            var list = new[] { 2 };
            var users = Database.Query<User>().Where(x => list.Contains(x.SupervisorId.Value)).ToList();

            Assert.AreEqual(1, users.Count);
            for (int i = 0; i < users.Count; i++)
            {
                AssertUserValues(InMemoryUsers.Single(x => x.UserId == users[i].UserId), users[i]);
            }
        }

        [Test]
        public void FetchWithWhereExpressionNotContains()
        {
            var list = new[] { 1, 2, 3, 4 };
            var users = Database.Query<User>().Where(x => !list.Contains(x.UserId)).ToList();

            Assert.AreEqual(11, users.Count);
            for (int i = 0; i < users.Count; i++)
            {
                AssertUserValues(InMemoryUsers[i+4], users[i]);
            }
        }

        [Test]
        public void FetchWithWhereExpressionContainsWithEmptyList()
        {
            var list = new int[] {};
            var users = Database.Query<User>().Where(x => list.Contains(x.UserId)).ToList();

            Assert.AreEqual(0, users.Count);
        }

        [Test]
        public void FetchWithWhereExpressionInAsExtensionMethod()
        {
            var list = new[] {1, 2, 3, 4};
            var users = Database.Query<User>().Where(x => x.UserId.In(list)).ToList();

            Assert.AreEqual(4, users.Count);
            for (int i = 0; i < users.Count; i++)
            {
                AssertUserValues(InMemoryUsers[i], users[i]);
            }
        }

        [Test]
        public void FetchWithWhereExpressionInAsStaticMethod()
        {
            var list = new[] {1, 2, 3, 4};
            var users = Database.Query<User>().Where(x => S.In(x.UserId, list)).ToList();

            Assert.AreEqual(4, users.Count);
            for (int i = 0; i < users.Count; i++)
            {
                AssertUserValues(InMemoryUsers[i], users[i]);
            }
        }

        [Test]
        public void FetchWithWhereInWithNoList()
        {
            var list = new int[0];
            var users = Database.Query<User>().Where(x => S.In(x.UserId, list)).ToList();

            Assert.AreEqual(0, users.Count);
        }

        [Test]
        public void UpdateWhere()
        {
            var list = new[] {1, 2, 3, 4};

            Database.UpdateMany<User>()
                .Where( x => x.UserId.In(list))
                //.ExcludeDefaults()
                .OnlyFields(x => x.Name)
                .Execute(new User() {Name = "test"});

            var users = Database.Fetch<User>();

            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual("test", users[i].Name);
            }
            for (int i = 4; i < 15; i++)
            {
                Assert.AreEqual("Name" + (i + 1), users[i].Name);
            }
        }

        [Test]
        public void UpdateWhere1()
        {
            var age = InMemoryUsers[0].Age;
            InMemoryUsers[0].Age = 99;

            Database.UpdateWhere(InMemoryUsers[0], "Name = @0", InMemoryUsers[0].Name);
            
            var users = Database.SingleById<User>(InMemoryUsers[0].UserId);

            Assert.AreEqual(99, users.Age);
            InMemoryUsers[0].Age = age;
        }

        [Test]
        public void DeleteWhere()
        {
            var list = new[]
            {
                new User() {UserId = 1},
                new User() {UserId = 2},
                new User() {UserId = 3},
                new User() {UserId = 4},
            };

            Database.DeleteMany<User>().Where(x => list.Select(y => y.UserId).Contains(x.UserId)).Execute();

            var users = Database.Fetch<User>();

            Assert.AreEqual(11, users.Count);
        }

        [Test]
        public void SelectStatementDoesNotRenderPropertyNameAsAlias()
        {
            var sqlExpression = new DefaultSqlExpression<UserDecorated>(Database);
            sqlExpression.Select(x => new {x.IsMale, x.Name});
            var selectStatement = sqlExpression.Context.ToSelectStatement();

            string expected = string.Format("SELECT {0}, {1} \nFROM {2}",
                                            TestDatabase.DbType.EscapeSqlIdentifier("is_male"),
                                            TestDatabase.DbType.EscapeSqlIdentifier("Name"),
                                            TestDatabase.DbType.EscapeTableName("Users"));
                
            Assert.AreEqual(expected, selectStatement);
        }

        [Test]
        public void BitwiseSupport()
        {
            var users = Database.Query<User>().Where(x => (x.UserId & (int)TestEnum.None) == (int)TestEnum.None).ToList();
            Assert.AreEqual(8, users.Count);
            Assert.AreEqual(1, users[0].UserId);
        }
    }
}
