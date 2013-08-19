using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
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
            var users = Database.FetchBy<User>(y => y.Where(x => x.UserId > 10).OrderBy(x => x.UserId));

            Assert.AreEqual(5, users.Count);
            for (int i = 0; i < users.Count; i++)
            {
                AssertUserValues(InMemoryUsers[i + 10], users[i]);
            }
        }

        [Test]
        public void FetchWhereExpression()
        {
            var users = Database.FetchWhere<User>(y => y.UserId == 2 && !y.IsMale);
            Assert.AreEqual(1, users.Count);
        }

        [Test]
        public void FetchByExpressionAndColumnAlias()
        {
            var users = Database.FetchBy<UserDecorated>(y => y.Where(x => x.IsMale).OrderBy(x => x.UserId));
            Assert.AreEqual(8, users.Count);
        }

        [Test]
        public void FetchByExpressionAndLimit()
        {
            var users = Database.FetchBy<UserDecorated>(y => y.OrderBy(x => x.UserId).Limit(5, 5));
            Assert.AreEqual(5, users.Count);
        }

        [Test]
        public void FetchByExpressionAndSubstringInWhere()
        {
            var users = Database.FetchBy<UserDecorated>(y => y.Where(x => x.Name.Substring(0, 4) == "Name"));
            Assert.AreEqual(15, users.Count);
        }

        [Test]
        public void FetchByExpressionAndSubstringAndUpper()
        {
            var users = Database.FetchBy<UserDecorated>(y => y.Where(x => x.Name.ToUpper().Substring(0, 4) == "NAME"));
            Assert.AreEqual(15, users.Count);
        }

        [Test]
        public void FetchByExpressionAndLower()
        {
            var users = Database.FetchBy<UserDecorated>(y => y.Where(x => x.Name.ToLower() == "name1"));
            Assert.AreEqual(1, users.Count);
        }

        [Test]
        public void FetchByExpressionAndContains()
        {
            var users = Database.FetchBy<UserDecorated>(y => y.Where(x => x.Name.Contains("ame")));
            Assert.AreEqual(15, users.Count);
        }

        [Test]
        public void FetchByExpressionAndStartsWith()
        {
            var users = Database.FetchBy<UserDecorated>(y => y.Where(x => x.Name.StartsWith("Na")));
            Assert.AreEqual(15, users.Count);
        }

        [Test]
        public void FetchByExpressionAndEndsWith()
        {
            var users = Database.FetchBy<UserDecorated>(y => y.Where(x => x.Name.EndsWith("e2")));
            Assert.AreEqual(1, users.Count);
        }

        [Test]
        public void FetchByExpressionAndSelectWithSubstring()
        {
            var users = Database.FetchBy<UserDecorated>(y => y.Select(x => new { Name = x.Name.Substring(0, 2) }));
            Assert.AreEqual("Na", users[0].Name);
        }

        [Test]
        public void FetchByExpressionAndSelectWithLower()
        {
            var users = Database.FetchBy<UserDecorated>(y => y.Select(x => new { Name = x.Name.ToLower() }));
            Assert.AreEqual("name1", users[0].Name);
        }

        [Test]
        public void FetchByExpressionAndSelectWithUpper()
        {
            var users = Database.FetchBy<UserDecorated>(y => y.Select(x => new { Name = x.Name.ToUpper() }));
            Assert.AreEqual("NAME1", users[0].Name);
        }

        [Test]
        public void FetchByExpressionAndSelect()
        {
            var users = Database.FetchBy<UserDecorated>(y => y.Select(x => new { x.Name }));
            Assert.AreEqual("Name1", users[0].Name);
        }

        [Test]
        public void FetchWithWhereExpressionContains()
        {
            var list = new[] { 1, 2, 3, 4 };
            var users = Database.FetchBy<User>(y => y.Where(x => list.Contains(x.UserId)));

            Assert.AreEqual(4, users.Count);
            for (int i = 0; i < users.Count; i++)
            {
                AssertUserValues(InMemoryUsers[i], users[i]);
            }
        }

        [Test]
        public void FetchWithWhereExpressionInAsExtensionMethod()
        {
            var list = new[] { 1, 2, 3, 4 };
            var users = Database.FetchBy<User>(y => y.Where(x => x.UserId.In(list)));

            Assert.AreEqual(4, users.Count);
            for (int i = 0; i < users.Count; i++)
            {
                AssertUserValues(InMemoryUsers[i], users[i]);
            }
        }

        [Test]
        public void FetchWithWhereExpressionInAsStaticMethod()
        {
            var list = new[] { 1, 2, 3, 4 };
            var users = Database.FetchBy<User>(y => y.Where(x => S.In(x.UserId, list)));

            Assert.AreEqual(4, users.Count);
            for (int i = 0; i < users.Count; i++)
            {
                AssertUserValues(InMemoryUsers[i], users[i]);
            }
        }

        [Test]
        public void UpdateWhere()
        {
            var list = new[] { 1, 2, 3, 4 };

            Database.UpdateWhere(new User() {Name = "test"}, x => x.Name, x => x.UserId.In(list));

            var users = Database.Fetch<User>();

            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual("test", users[i].Name);
            }
            for (int i = 4; i < 15; i++)
            {
                Assert.AreEqual("Name"+(i+1), users[i].Name);
            }
        }

        [Test]
        public void DeleteWhere()
        {
            var list = new[] { 1, 2, 3, 4 };

            Database.DeleteWhere<User>(x => x.UserId.In(list));

            var users = Database.Fetch<User>();

            Assert.AreEqual(11, users.Count);
        }
    }

    public static class ss
    {
        public static int UpdateWhere<T, TKey>(this IDatabase database, T obj, Expression<Func<T, TKey>> onlyFields = null, Expression<Func<T, bool>> where = null)
        {
            if (onlyFields == null)
                throw new ArgumentNullException("onlyFields");

            var ev =  database.DatabaseType.ExpressionVisitor<T>(database, PocoData.ForType(typeof(T), database.PocoDataFactory));
            ev.Update(onlyFields);
            ev.Where(where);
            var updateStatement = ev.Context.ToUpdateStatement(obj);
            return database.Execute(updateStatement, ev.Context.Params);
        }

        public static int UpdateWhere<T>(this IDatabase database, string where, params object[] parameters)
        {
            var ev = database.DatabaseType.ExpressionVisitor<T>(database, PocoData.ForType(typeof(T), database.PocoDataFactory));
            ev.Where(where, parameters);
            var sql = ev.Context.ToDeleteStatement();
            return database.Execute(sql, ev.Context.Params);
        }

        public static int UpdateBy<T>(this IDatabase database, T obj, Func<SqlExpression<T>, SqlExpression<T>> sqlExpression)
        {
            var ev = database.DatabaseType.ExpressionVisitor<T>(database, PocoData.ForType(typeof(T), database.PocoDataFactory));
            return database.Execute(sqlExpression(ev).Context.ToUpdateStatement(obj), ev.Context.Params);
        }

        public static int DeleteWhere<T>(this IDatabase database, Expression<Func<T, bool>> where)
        {
            var ev = database.DatabaseType.ExpressionVisitor<T>(database, PocoData.ForType(typeof(T), database.PocoDataFactory));
            ev.Where(where);
            return database.Execute(ev.Context.ToDeleteStatement(), ev.Context.Params);
        }

        public static int DeleteWhere<T>(this IDatabase database, string where, params object[] parameters)
        {
            var ev = database.DatabaseType.ExpressionVisitor<T>(database, PocoData.ForType(typeof(T), database.PocoDataFactory));
            ev.Where(where, parameters);
            return database.Execute(ev.Context.ToDeleteStatement(), ev.Context.Params);
        }

        public static int DeleteBy<T>(this IDatabase database, Func<SqlExpression<T>, SqlExpression<T>> sqlExpression)
        {
            var ev = database.DatabaseType.ExpressionVisitor<T>(database, PocoData.ForType(typeof(T), database.PocoDataFactory));
            return database.Execute(sqlExpression(ev).Context.ToDeleteStatement(), ev.Context.Params);
        }
    }
}
