using NPoco.FluentMappings;
using NPoco.Tests.Common;
using NUnit.Framework;

namespace NPoco.Tests.FluentMappings
{
    [TestFixture]
    public class MapTests
    {
        [Test]
        public void PrimaryKeyValueSetToPropertyName()
        {
            var typeDefinition = new TypeDefinition(typeof(User));
            var map = new Map<User>(typeDefinition);
            map.PrimaryKey(x => x.UserId);
            Assert.AreEqual("UserId", typeDefinition.PrimaryKey);
            Assert.AreEqual(true, typeDefinition.AutoIncrement);
        }

        [Test]
        public void AutoIncrementSetToValueSpecified()
        {
            var typeDefinition = new TypeDefinition(typeof(User));
            var map = new Map<User>(typeDefinition);
            map.PrimaryKey(x => x.UserId, false);
            Assert.AreEqual(false, typeDefinition.AutoIncrement);
        }

        [Test]
        public void SequenceNameSetIfSpecified()
        {
            var typeDefinition = new TypeDefinition(typeof(User));
            var map = new Map<User>(typeDefinition);
            map.PrimaryKey(x => x.UserId, "userid_seq");
            Assert.AreEqual("userid_seq", typeDefinition.SequenceName);
        }

        [Test]
        public void CompositeKeySetToBothPropertiesJoinedByAComma()
        {
            var typeDefinition = new TypeDefinition(typeof(User));
            var map = new Map<User>(typeDefinition);
            map.CompositePrimaryKey(x => x.UserId, x => x.Name);
            Assert.AreEqual("UserId,Name", typeDefinition.PrimaryKey);
        }

        [Test]
        public void TableNameSetToValueSpecified()
        {
            var typeDefinition = new TypeDefinition(typeof(User));
            var map = new Map<User>(typeDefinition);
            map.TableName("Users");
            Assert.AreEqual("Users", typeDefinition.TableName);
        }

        [Test]
        public void ExplicitColumnConfigurationSetToValuePassedIn()
        {
            var typeDefinition = new TypeDefinition(typeof(User));
            var map = new Map<User>(typeDefinition);
            map.Columns(x => x.Column(y => y.Age).Ignore(), true);
            Assert.AreEqual(true, typeDefinition.ExplicitColumns);
        }

        [Test]
        public void PrimaryKeyAndColumnNameSet()
        {
            var typeDefinition = new TypeDefinition(typeof(User));
            var map = new Map<User>(typeDefinition);
            map
                .PrimaryKey(x => x.UserId)
                .Columns(x =>
                {
                    x.Column(y => y.UserId).WithName("Id");
                });

            Assert.AreEqual("UserId", typeDefinition.PrimaryKey);
            Assert.AreEqual("Id", typeDefinition.ColumnConfiguration["UserId"].DbColumnName);
        }

        [Test]
        public void InheritColumns()
        {
            var typeDefinition = new TypeDefinition(typeof(User));
            var map = new TestMap(typeDefinition);
            
            var typeDefinition1 = new TypeDefinition(typeof(Supervisor));
            var map1 = new SupervisorMap(typeDefinition1);
            
            Assert.AreEqual(2, typeDefinition1.ColumnConfiguration.Count);
            Assert.IsTrue(typeDefinition1.ColumnConfiguration["Age"].IgnoreColumn ?? false);
            Assert.IsTrue(typeDefinition1.ColumnConfiguration["IsSupervisor"].ResultColumn ?? false);
        }

        [Test]
        public void InheritColumnsButRemoveColumnsNotFound()
        {
            var typeDefinition = new TypeDefinition(typeof(User));
            var map = new UserMap(typeDefinition);

            var typeDefinition1 = new TypeDefinition(typeof(UserWithNullableId));
            var map1 = new TestMap(typeDefinition1);

            Assert.AreEqual(1, typeDefinition1.ColumnConfiguration.Count);
        }
    }

    public class TestMap : Map<UserWithNullableId>
    {
        public TestMap(TypeDefinition typeDefinition) : base(typeDefinition)
        {
            UseMap<UserMap>();
            Columns(x => x.Column(y => y.Days).Result());
        }
    }
}
