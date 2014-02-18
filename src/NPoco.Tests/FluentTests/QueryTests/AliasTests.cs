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
            query.Join<ExtraUserInfo>((user, decorated) => user.UserId == decorated.UserId, x=>x.OrderBy(z=>z.Email))
                .Where(x => x.Name == "hi")
                .Limit(5)
                .ToList();
        }

    }

    public class Usersss
    {
        public int UserId { get; set; }
    }
}
