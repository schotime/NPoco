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
    public class AliasFluentTests : BaseDBFuentTest
    {
        [Test]
        public void Test1()
        {
            var poco1 = Database.PocoDataFactory.ForType(typeof (User));
            var poco2 = Database.PocoDataFactory.ForType(typeof (Usersss));

            var sels = "Select " + string.Join(", ", poco1.Columns.Values.Select(x => x.ColumnName + " as " + x.AutoAlias)) + " from users";

            //var result = Database.Fetch<User>(sels);

        }

    }

    public class Usersss
    {
        public int UserId { get; set; }
    }
}
