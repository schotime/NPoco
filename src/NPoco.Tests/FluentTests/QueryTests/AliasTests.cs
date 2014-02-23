using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using NPoco.DatabaseTypes;
using NPoco.Expressions;
using NPoco.Linq;
using NPoco.Tests.Common;
using NUnit.Framework;

namespace NPoco.Tests.FluentTests.QueryTests
{
    [TestFixture]
    public class QueryProviderTests : BaseDBFuentTest
    {
        [Test]
        public void Test1()
        {
            //var poco1 = Database.PocoDataFactory.ForType(typeof(User));
            //var poco2 = Database.PocoDataFactory.ForType(typeof(Usersss));

            //var sels = "Select " + string.Join(", ", poco1.Columns.Values.Select(x => x.ColumnName + " as " + x.AutoAlias)) + " from users";

            //SqlExpression<User> exp = new DefaultSqlExpression<User>(Database);
            //exp = exp.OrderBy(x => x.Age);

            //var query = new QueryProvider<User>(Database);
            //var result1 = query
            //    .Where(x => x.Name.StartsWith("Name"))
            //    .Limit(5)
            //    .ToList();

            //var query2 = new QueryProvider<User>(Database);
            //var result2 = query2
            //    .Include(x => x.House)
            //    .Where(x => x.Name.StartsWith("Name"))
            //    .Where(x => x.House.Address.Contains("Road Street"))
            //    .OrderBy(x => x.House.HouseId)
            //    .Limit(5)
            //    .ToList();

            //var query3 = new QueryProvider<User>(Database);
            //var result3 = query3
            //    .Include(x => x.House)
            //    .Where(x => x.Name.StartsWith("Name"))
            //    .OrderByDescending(x => x.IsMale)
            //    .ThenBy(x => x.DateOfBirth)
            //    .ToPage(2, 5);

            var query4 = new QueryProvider<User>(Database);
            var result4 = query4
                .Include(x => x.House)
                .Where(x => x.Name.ToLower().Equals(x.House.Address))
                .Limit(5)
                .OrderBy(x => x.IsMale)
                .ProjectTo(x => new { Test = x.Name.ToLower() });
        }

    }

    public class Usersss
    {
        public int UserId { get; set; }
    }
}
