using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using NPoco.Expressions;
using NPoco.Linq;
using NPoco.Tests.Common;
using NUnit.Framework;

namespace NPoco.Tests.FluentTests.QueryTests
{
    [TestFixture]
    public class AliasFluentTests : BaseDBFuentTest
    {
        [Test]
        public void Test1()
        {
            var poco1 = Database.PocoDataFactory.ForType(typeof (User));
            var poco2 = Database.PocoDataFactory.ForType(typeof (Usersss));

            var sels = "Select " + string.Join(", ", poco1.Columns.Values.Select(x => x.ColumnName + " as " + x.AutoAlias)) + " from users";

            SqlExpression<User> exp = new DefaultSqlExpression<User>(Database);
            exp = exp.OrderBy(x => x.Age);

            var query = new SimpleQueryProvider<User>(Database, null);
            var result1 = query.Join<ExtraUserInfo>((user, decorated) => user.UserId == decorated.UserId)
                .Where(x => x.Name.StartsWith("Name"))
                .OrderBy(x => x.ExtraUserInfo.Email)
                .Limit(5)
                .ToList();

            var query2 = new SimpleQueryProvider<User>(Database, null);
            var result2 = query2.Join<ExtraUserInfo>()
                .Where(x => x.Name.StartsWith("Name"))
                .Where(x=>x.ExtraUserInfo.Email == "email2@email.com")
                .OrderBy(x => x.ExtraUserInfo.Email)
                .Limit(5)
                .ToList();

            var query3 = new SimpleQueryProvider<User>(Database, null);
            var result3 = query3.Join<ExtraUserInfo>()
                .Where(x => x.Name.StartsWith("Name"))
                .OrderBy(x => x.ExtraUserInfo.UserId)
                .OrderByDescending(x => x.IsMale)
                .ThenBy(x=>x.DateOfBirth)
                .ToPage(2, 5);
        }

    }

    public class Usersss
    {
        public int UserId { get; set; }
    }
}
