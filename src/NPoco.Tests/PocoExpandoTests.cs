using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NPoco.Tests.Common;
using NUnit.Framework;

namespace NPoco.Tests
{
    public class PocoExpandoTests : BaseDBDecoratedTest
    {
        [Test]
        public void CanAccessPropertiesWithAnyCase()
        {
            var results = Database.Fetch<dynamic>("select * from users");
            Assert.AreEqual(results[0].userid, 1);
            Assert.AreEqual(results[1].USERID, 2);
            Assert.AreEqual(results[2].USERid, 3);
        }

        [Test]
        public void IsNewReturnsTrue()
        {
            dynamic results = new PocoExpando();
            Assert.True(Database.IsNew<dynamic>(results));
        }

        [Test]
        public void CanGetPocoDataForPocoExpando()
        {
            dynamic result = new PocoExpando();
            result.Name = "Name1";
            PocoData pd = Database.PocoDataFactory.ForObject(result, "UserId");
            Assert.AreEqual(pd.Columns.Count, 2);
            Assert.True(pd.Columns.ContainsKey("UserId"));
            Assert.True(pd.Columns.ContainsKey("Name"));
        }
    }
}
