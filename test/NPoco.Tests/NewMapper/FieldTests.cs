using NPoco.Tests.Common;
using NUnit.Framework;

namespace NPoco.Tests
{
    public class FieldTests : BaseDBDecoratedTest
    {
        [Test]
        public void FieldAsPrimaryKey()
        {
            var user = Database.SingleById<Super>(1);
            user.Name = "NameChanged";
            Database.Save(user);
            var userChanged = Database.SingleById<Super>(1);
            Assert.AreEqual("NameChanged", userChanged.Name);
        }

        public abstract class Base
        {
            public int UserId;
        }

        [TableName("Users"), PrimaryKey("UserId")]
        public class Super : Base
        {
            public string Name { get; set; }
        }
    }
}