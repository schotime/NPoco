using System.Data;
using System.Reflection;
using NPoco.DatabaseTypes;
using NPoco.FluentMappings;
using NPoco.Tests.Common;
using NPoco.Tests.FluentMappings;
using NUnit.Framework;

namespace NPoco.Tests.NewMapper
{
    public class MyNameObject : IValueObject<string>
    {
        public string Value { get; set; }
    }

    public class MyNameObject2
    {
        public string MyAwesomeValue { get; set; }
    }

    public class MyNameObject3
    {
        public string SomeOther { get; set; }
    }

    public class MyNameObject4
    {
        public MyNameObject4() {}

        public MyNameObject4(string getter)
        {
            Getter = getter;
        }

        public string Getter { get; }
    }

    [TableName("Users")]
    [PrimaryKey("UserId")]
    public class User1
    {
        public int UserId { get; }
        public MyNameObject Name { get; set; } = new MyNameObject();
    }

    [TableName("Users")]
    [PrimaryKey("UserId")]
    public class User2
    {
        public int UserId { get; }
        public MyNameObject2 Name { get; set; } = new MyNameObject2();
    }

    [TableName("Users")]
    [PrimaryKey("UserId")]
    public class User3
    {
        public int UserId { get; }
        public MyNameObject3 Name { get; set; } = new MyNameObject3();
    }

    [TableName("Users")]
    [PrimaryKey("UserId")]
    public class User4
    {
        public int UserId { get; }
        public MyNameObject4 Name { get; set; }
    }

    public class ValueObjectTests : BaseDBDecoratedTest
    {
        [Test]
        public void ValueObjectTestGet()
        {
            var s = Database.Single<User1>("select 'testtext' as Name /*poco_dual*/");
            Assert.AreEqual("testtext", s.Name.Value);
        }

        [Test]
        public void ValueObjectTestLambda()
        {
            var myNameObject = new MyNameObject(){ Value = "Name1" } ;
            var user = Database.Query<User1>().Where(x => x.Name == myNameObject).Single();
            Assert.AreEqual("Name1", user.Name.Value);
        }

        [Test]
        public void ValueObjectTestLambdaWhereReversed()
        {
            var myNameObject = new MyNameObject() { Value = "Name1" };
            var user = Database.Query<User1>().Where(x => myNameObject == x.Name).Single();
            Assert.AreEqual("Name1", user.Name.Value);
        }

        [Test]
        public void ValueObjectTestUpdate()
        {
            var myNameObject = new MyNameObject() { Value = "Name1" };
            var user = Database.Query<User1>().Where(x => x.Name == myNameObject).Single();
            user.Name.Value = "Name111";
            Database.Update(user);
            var updateUser = Database.Query<User1>().Where(x => x.Name == user.Name).Single();
            Assert.AreEqual("Name111", updateUser.Name.Value);
        }

        [Test]
        public void ValueObjectTestInsert()
        {
            var myNameObject = new MyNameObject() { Value = "Name20" };
            var user = new User1 {Name = myNameObject};
            Database.Insert(user);
            var newUser = Database.Query<User1>().Where(x => x.Name == myNameObject).Single();
            Assert.AreEqual("Name20", newUser.Name.Value);
        }

        [Test]
        public void ValueObjectTestDelete()
        {
            var myNameObject = new MyNameObject() { Value = "Name21" };
            var user = new User1 {Name = myNameObject};
            Database.Insert(user);
            var newUser = Database.Query<User1>().Where(x => x.Name == myNameObject).Single();
            Database.Delete(newUser);
            var deletedUser = Database.Query<User1>().Where(x => x.Name == myNameObject).SingleOrDefault();
            Assert.Null(deletedUser);
        }

        [Test]
        public void ValueObjectTestUpdateWhere()
        {
            var myNameObject = new MyNameObject() { Value = "Name1" };
            var myNameObject2 = new MyNameObject() { Value = "Name21" };
            var user = new User1 {Name = myNameObject2};
            Database.UpdateMany<User1>().Where(x => x.Name == myNameObject).Execute(user);
            var updateUser = Database.Query<User1>().Where(x => x.Name == myNameObject2).Single();
            Assert.AreEqual("Name21", updateUser.Name.Value);
        }

        [Test]
        public void ValueObjectTestGetWithoutInterface()
        {
            var map = FluentMappingConfiguration.Scan(s =>
            {
                s.Assembly(typeof(User2).GetTypeInfo().Assembly);
                s.IncludeTypes(t => t == typeof(User2));
                s.Columns.ValueObjectColumnWhere(y => y.GetMemberInfoType() == typeof(MyNameObject2));
            });

            var factory = DatabaseFactory.Config(x =>
            {
                x.WithFluentConfig(map);
            });
            
            var s1 = factory.Build(Database).Single<User2>("select 'testtext' as Name /*poco_dual*/");
            Assert.AreEqual("testtext", s1.Name.MyAwesomeValue);
        }

        public class MyMapping : Mappings
        {
            public MyMapping()
            {
                For<User3>()
                    .Columns(x => x.Column(y => y.Name).ValueObject(y => y.SomeOther));

                For<User4>()
                    .TableName("users")
                    .PrimaryKey(x => x.UserId);

            }
        }

        [Test]
        public void ValueObjectTestGetWithoutInterfaceWithSpecificOverride()
        {
            var map = FluentMappingConfiguration.Scan(s =>
            {
                s.Assembly(typeof(User2).GetTypeInfo().Assembly);
                s.IncludeTypes(t => t == typeof(User4));
                s.Columns.ValueObjectColumnWhere(x => x.GetMemberInfoType() == typeof(MyNameObject4));
                s.OverrideMappingsWith(new MyMapping());
            });

            var factory = DatabaseFactory.Config(x =>
            {
                x.WithFluentConfig(map);
            });

            var myNameObject = new MyNameObject4("Name1");
            var user = factory.Build(Database).Query<User4>().Where(x => x.Name == myNameObject).Single();
            var user1 = factory.Build(Database).Query<User4>().Where(x => myNameObject == x.Name).Single();
            Assert.AreEqual("Name1", user.Name.Getter);
            Assert.AreEqual("Name1", user1.Name.Getter);
        }

        [Test]
        public void ValueObjectTestWithNullValue()
        {
            var map = FluentMappingConfiguration.Scan(s =>
            {
                s.Assembly(typeof(User2).GetTypeInfo().Assembly);
                s.IncludeTypes(t => t == typeof(User4));
                s.Columns.ValueObjectColumnWhere(x => x.GetMemberInfoType() == typeof(MyNameObject4));
                s.OverrideMappingsWith(new MyMapping());
            });

            var factory = DatabaseFactory.Config(x =>
            {
                x.WithFluentConfig(map);
            });

            var user = factory.Build(Database).SingleOrDefault<User4>("select null as Name /*poco_dual*/");
            Assert.AreEqual(null, user.Name);
        }
    }
}
