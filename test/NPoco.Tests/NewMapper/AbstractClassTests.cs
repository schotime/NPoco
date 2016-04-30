using System.Linq;
using NPoco.Tests.Common;
using NUnit.Framework;

namespace NPoco.Tests.NewMapper
{
    public class AbstractClassTests : BaseDBDecoratedTest
    {
        [Test]
        public void InsertBaseUser()
        {
            var user = new SuperUser();
            var user2 = new SuperUser2();
            Database.Insert<BaseUser>(user);
            Database.Insert<BaseUser>(user2);
            Database.Mappers.RegisterFactory<BaseUser>(x =>
            {
                return x["Name"].ToString() == "Super"
                    ? (BaseUser)new SuperUser()
                    : new SuperUser2();
            });
            var baseUser = Database.Fetch<BaseUser>("where name = @0", "Super").First();
            var baseUser2 = Database.Fetch<BaseUser>("where name = @0", "Super2").First();
            Assert.AreEqual(baseUser.GetType(), typeof(SuperUser));
            Assert.AreEqual(baseUser2.GetType(), typeof(SuperUser2));
            Assert.AreEqual(baseUser.Name, "Super");
            Assert.AreEqual(baseUser2.Name, "Super2");
            Database.Mappers.ClearFactories();
        }

        [TableName("Users"), PrimaryKey("UserId")]
        public abstract class BaseUser
        {
            public int UserId { get; set; }
            public abstract string Name { get; }
        }

        public class SuperUser : BaseUser
        {
            public override string Name => "Super";
        }

        public class SuperUser2 : BaseUser
        {
            public override string Name => "Super2";
        }
    }
}
