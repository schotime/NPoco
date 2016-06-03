using NPoco;
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
        public void CanInsertDynamic()
        {
            var result = Database.FirstOrDefault<dynamic>("select UserId, Name, Age from users where userid = 1");
            var id = Database.Insert("Users", "UserId", result);
            Assert.AreEqual(id, 16);
        }

        [Test]
        public void CanUpdateDynamic()
        {
            var result = Database.FirstOrDefault<dynamic>("select UserId, Name, Age from users where userid = 1");
            result.Name = "changed";
            Database.Update("users", "UserId", result, new[] { "Name" });
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
            PocoData pd = Database.PocoDataFactory.ForObject(result, "UserId", true);
            Assert.AreEqual(pd.Columns.Count, 2);
            Assert.True(pd.Columns.ContainsKey("UserId"));
            Assert.True(pd.Columns.ContainsKey("Name"));
        }

        [Test]
        public void CanGetPocoDataWithTypeForPocoExpando()
        {
            dynamic result = new PocoExpando();
            result.Name = "Name1";
            PocoData pd = Database.PocoDataFactory.ForObject(result, "UserId", true);
            Assert.AreEqual(pd.Columns.Count, 2);
            Assert.True(pd.Columns.ContainsKey("UserId"));
            Assert.AreEqual(typeof(object), pd.Columns["UserId"].ColumnType);
            Assert.True(pd.Columns.ContainsKey("Name"));
            Assert.AreEqual(typeof(string), pd.Columns["Name"].ColumnType);
        }
    }
}