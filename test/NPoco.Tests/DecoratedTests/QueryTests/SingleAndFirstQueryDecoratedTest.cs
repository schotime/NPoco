using System;
using System.Collections.Generic;
using NPoco;
using NPoco.Expressions;
using NPoco.Tests.Common;
using NUnit.Framework;

namespace NPoco.Tests.DecoratedTests.QueryTests
{
    [TestFixture]
    public class SingleAndFirstQueryDecoratedTest : BaseDBDecoratedTest
    {
        [Test]
        public void SingleOrDefaultById()
        {
            var old = ((Database) Database).EnableAutoSelect;
            ((Database) Database).EnableAutoSelect = false;
            var user = Database.SingleOrDefaultById<UserDecorated>(1);
            ((Database)Database).EnableAutoSelect = old;

            Assert.NotNull(user);
            AssertUserValues(InMemoryUsers[0], user);
        }

        [Test]
        public void SingleOrDefaultByIdWithNoRecord()
        {
            var user = Database.SingleOrDefaultById<UserDecorated>(-1);
            Assert.Null(user);
        }

        [Test]
        public void SingleById()
        {
            var user = Database.SingleById<UserDecorated>(1);

            Assert.NotNull(user);
            AssertUserValues(InMemoryUsers[0], user);
        }

        [Test]
        public void SingleByIdWithNoRecord()
        {
            Assert.Throws<InvalidOperationException>(() => Database.SingleById<UserDecorated>(-1));
        }

        [Test]
        public void SingleInto()
        {
            var u = new UserDecorated();
            var user = Database.SingleInto(u, "select u.* from users u where u.userid = 1");

            AssertUserValues(InMemoryUsers[0], user);
            Assert.AreEqual(u, user);
        }

        [Test]
        public void SingleIntoWithNoRecord()
        {
            var u = new UserDecorated();
            Assert.Throws<InvalidOperationException>(() => Database.SingleInto(u, "select u.* from users u where u.userid = -1"));
        }

        [Test]
        public void SingleOrDefaultInto()
        {
            var u = new UserDecorated();
            var user = Database.SingleOrDefaultInto(u, "select u.* from users u where u.userid = 1");

            AssertUserValues(InMemoryUsers[0], u);
            Assert.AreEqual(u, user);
        }

        [Test]
        public void SingleOrDefaultIntoWithNoRecord()
        {
            var u = new UserDecorated();
            var user = Database.SingleOrDefaultInto(u, "select u.* from users u where u.userid = -1");

            AssertUserValues(u, new UserDecorated());
            Assert.Null(user);
        }

        [Test]
        public void SingleSql()
        {
            var user = Database.Single<UserDecorated>("select u.* from users u where u.userid = 1");

            Assert.NotNull(user);
            AssertUserValues(InMemoryUsers[0], user);
        }

        [Test]
        public void SingleSqlWithNoRecord()
        {
            Assert.Throws<InvalidOperationException>(() => Database.Single<UserDecorated>("select u.* from users u where u.userid = -1"));
        }

        [Test]
        public void SingleOrDefaultSql()
        {
            var user = Database.SingleOrDefault<UserDecorated>("select u.* from users u where u.userid = 1");

            Assert.NotNull(user);
            AssertUserValues(InMemoryUsers[0], user);
        }

        [Test]
        public void SingleOrDefaultSqlWithNoRecord()
        {
            var user = Database.SingleOrDefault<UserDecorated>("select u.* from users u where u.userid = -1");
            Assert.Null(user);
        }

        [Test]
        public void FirstSql()
        {
            var user = Database.First<UserDecorated>("select u.* from users u order by u.userid");

            Assert.NotNull(user);
            AssertUserValues(InMemoryUsers[0], user);
        }

        [Test]
        public void FirstSqlWithNoRecord()
        {
            Assert.Throws<InvalidOperationException>(() => Database.First<UserDecorated>("select u.* from users u where u.userid < 0"));
        }

        [Test]
        public void FirstOrDefaultSql()
        {
            var user = Database.FirstOrDefault<UserDecorated>("select u.* from users u order by u.userid");

            Assert.NotNull(user);
            AssertUserValues(InMemoryUsers[0], user);
        }

        [Test]
        public void FirstOrDefaultSqlWithNoRecord()
        {
            var user = Database.FirstOrDefault<UserDecorated>("select u.* from users u where u.userid < 0");
            Assert.Null(user);
        }

        [Test]
        public void SingleByIdWithCompositePrimaryKey()
        {
            var user = Database.SingleById<CompositeObjectDecorated>(new { Key1_ID = 1, Key2ID = 2, Key3ID = 4 });
            Assert.NotNull(user);
        }

        [Test]
        public void SingleByIdWithCompositePrimaryKeyUsingDictionary()
        {
            var d = new Dictionary<string, object>();
            d["Key1_ID"] = 1;
            d["Key2ID"] = 2;
            d["Key3ID"] = 4;
            var user = Database.SingleById<CompositeObjectDecorated>(d);
            Assert.NotNull(user);
        }

        [Test]
        public void SingleOrDefaultByIdWithReadOnlyField()
        {
            var user = Database.SingleOrDefaultById<UserReadOnlyFieldDecorated>(1);

            Assert.Null(user.Name);
            Assert.AreEqual(1, user.UserId);
        }

        [Test]
        public void SingleOrDefaultFromIntToNullableLong()
        {
            var user = Database.SingleOrDefault<UserWithNullableId>("select u.userid from users u where u.userid = 1");
            Assert.NotNull(user);
            Assert.AreEqual(1, user.UserId);
        }

        [Test]
        public void SingleOrDefaultFromIntToNullableEnum()
        {
            var user = Database.SingleOrDefault<UserWithNullableId>("select u.userid, u.userid userid2 from users u where u.userid = 1");
            Assert.NotNull(user);
            Assert.AreEqual(UserIdEnum.UserIdTrue, user.UserId2);
        }

        [Test]
        public void SingleOrDefaultFromNullToNullableEnum()
        {
            var user = Database.SingleOrDefault<UserWithNullableId>("select u.userid, null userid2 from users u where u.userid = 1");
            Assert.NotNull(user);
            Assert.Null(user.UserId2);
        }

        [Test]
        public void SingleOrDefaultFromStringToNullableEnum()
        {
            var user = Database.SingleOrDefault<UserWithNullableId>("select 'Bill' nameenum from users u where u.userid = 1");
            Assert.NotNull(user);
            Assert.AreEqual(NameEnum.Bill, user.NameEnum);
        }

        [Test]
        public void SingleOrDefaultFromStringToMissingEnumThrowsException()
        {
            Assert.Throws<Exception>(() =>
            {
                Database.SingleOrDefault<UserWithNullableId>("select 'John' nameenum from users u where u.userid = 1");
            });
        }

        [Test]
        public void SingleOrDefaultFromIntToByteEnum()
        {
            var user = Database.SingleOrDefault<UserWithNullableId>("select 2 days from users u where u.userid = 1");
            Assert.NotNull(user);
            Assert.AreEqual(Days.Mon, user.Days);
        }

        [Test]
        public void WhereExpressionWithNullableEnum()
        {
            var sqlExpression = new DefaultSqlExpression<UserWithNullableId>(Database);
            sqlExpression.Where(x => x.NameEnum == NameEnum.Bobby);
            Assert.AreEqual(sqlExpression.Context.Params[0], "Bobby");
        }

        [Test]
        public void WhereExpressionWithNullableEnumContains()
        {
            var sqlExpression = new DefaultSqlExpression<UserWithNullableId>(Database);
            sqlExpression.Where(x => new List<NameEnum?> { NameEnum.Bill, NameEnum.Bobby }.Contains(x.NameEnum));
            Assert.AreEqual(sqlExpression.Context.Params[0], "Bill");
            Assert.AreEqual(sqlExpression.Context.Params[1], "Bobby");
        }

        [Test]
        public void WhereExpressionWithNullableEnumAndValueIsNull()
        {
            var sqlExpression = new DefaultSqlExpression<UserWithNullableId>(Database);
            sqlExpression.Where(x => x.NameEnum == null);
            Assert.AreEqual(sqlExpression.Context.Params.Length, 0);
        }
        
        [Test]
        public void Exists()
        {
            var userExists = Database.Exists<UserDecorated>(1);

            Assert.True(userExists);
        }

        [Test]
        public void NotExists()
        {
            var userExists = Database.Exists<UserDecorated>(100);

            Assert.False(userExists);
        }

        [Test]
        public void ExistsComposite()
        {
            var userExists = Database.Exists<CompositeObjectDecorated>(new
            {
                Key1_ID = 1,
                Key2ID = 2,
                Key3ID = 4
            });

            Assert.True(userExists);
        }

        [Test]
        public void DefaultToUtcKindForDatetime()
        {
            var user = Database.Single<MyDateTimeClass>("select u.* from users u where u.userid = 1");

            Assert.NotNull(user);
            Assert.AreEqual(DateTimeKind.Utc, user.DateOfBirth.Kind);
        }

        public class MyDateTimeClass
        {
            public DateTime DateOfBirth { get; set; }
        }

        [Test]
        public void SingleOrDefaultForValueType()
        {
            var boolean = Database.Single<bool>("select 1 /*poco_dual*/");

            Assert.AreEqual(true, boolean);
        }
    }
}