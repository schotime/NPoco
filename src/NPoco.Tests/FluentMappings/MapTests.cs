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
    }
}
