using System;
using System.Data.SqlClient;
using System.Linq;
using NPoco;
using NPoco.FluentMappings;
using NPoco.Tests.Common;
using NUnit.Framework;

namespace NPoco.Tests
{
    [TestFixture]
    public class SnapshotterTests
    {
        private IDatabase _database;

        [OneTimeSetUp]
        public void Setup()
        {
            var dbfactory = new DatabaseFactory();
            dbfactory
                .Config()
                .UsingDatabase(() => new Database("", DatabaseType.SqlServer2012, SqlClientFactory.Instance))
                .WithFluentConfig(FluentMappingConfiguration.Configure(new MyMappings()));
            
            _database = dbfactory.GetDatabase();
        }

        public class MyMappings : Mappings
        {
            public MyMappings()
            {
                For<Admin>().Columns(y => y.Column(x => x.Age).WithName("TheAge"));
                For<Admin>().Columns(y => y.Column(x => x.Savings).WithName("Savings"));
                For<Admin>().Columns(y => y.Column(x => x.DateOfBirth).WithName("DateOfBirth"));
                For<Admin>().Columns(y => y.Column(x => x.Name).WithName("Name"));
            }
        }

        [Test]
        public void BasicDiffUsingSnapshotter()
        {
            var user = new Admin { UserId = 1 };
            var snap = _database.StartSnapshot(user);

            user.Name = "Name1";
            user.Savings = 50.50m;
            user.DateOfBirth = new DateTime(2001, 1, 1);
            user.Age = 21;

            Assert.AreEqual(4, snap.Changes().Count);
            Assert.AreEqual(4, snap.UpdatedColumns().Count);
        }

        [Test]
        public void ValuesAlreadySetUsingSnapshotter()
        {
            var user = new Admin { UserId = 1 };
            user.Name = "Name1";
            user.Savings = 50.50m;
            user.DateOfBirth = new DateTime(2001, 1, 1);
            user.Age = 21;

            var snap = _database.StartSnapshot(user);
            user.Age = 22;

            Assert.AreEqual("Age", snap.Changes().First().Name);
            Assert.AreEqual("TheAge", snap.Changes().First().ColumnName);
            Assert.AreEqual(21, snap.Changes().First().OldValue);
            Assert.AreEqual(22, snap.Changes().First().NewValue);
            Assert.AreEqual(1, snap.UpdatedColumns().Count);
        }

        [Test]
        public void NoChangesUsingSnapshotter()
        {
            var user = new Admin { UserId = 1 };
            var snap = _database.StartSnapshot(user);

            Assert.AreEqual(0, snap.Changes().Count);
            Assert.AreEqual(0, snap.UpdatedColumns().Count);
        }

        [Test]
        public void OverrideTrackedObjectUsingSnapshotter()
        {
            var user = new Admin { UserId = 1 };
            var snap = _database.StartSnapshot(user);

            var user1 = new Admin { UserId = 1 };
            user1.Name = "Name1";
            user1.Savings = 50.50m;
            user1.DateOfBirth = new DateTime(2001, 1, 1);
            user1.Age = 21;

            snap.OverrideTrackedObject(user1);

            Assert.AreEqual(4, snap.Changes().Count);
            Assert.AreEqual(4, snap.UpdatedColumns().Count);
        }

        [Test]
        public void GenerateCloneWIthPrivateConstructor()
        {
            var user = new UserWithPrivateParamLessConstructor(1);
            var snap = _database.StartSnapshot(user);

            user.Name = "Changed";

            Assert.AreEqual(1, snap.Changes().Count);
            Assert.AreEqual(1, snap.UpdatedColumns().Count);
            Assert.AreEqual("Name", snap.UpdatedColumns()[0]);
        }

        [Test]
        public void GetDiffIncludingObjectPropertiesOnlyByReferenceThough()
        {
            var user = new SnapshotOnClass()
            {
                Phone = new SnapshotOnClass.PhoneClass()
                {
                    Value = "2" + (char)22
                }
            };
            var snap = _database.StartSnapshot(user);

            user.Name = "Changed";
            user.Phone.PhoneId = 21;
            user.Phone.Value = "324";

            Assert.AreEqual(2, snap.Changes().Count);
            Assert.AreEqual(2, snap.UpdatedColumns().Count);
            Assert.AreEqual("Name", snap.UpdatedColumns()[0]);
            Assert.AreEqual("phone", snap.UpdatedColumns()[1]);
        }

        [Test]
        public void GetDiffIncludingLists()
        {
            var user = new SnapshotOnClass()
            {
                Values = new[] {1, 2, 3}
            };
            var snap = _database.StartSnapshot(user);
            user.Values[0] = 2;
            
            Assert.AreEqual(1, snap.Changes().Count);
            Assert.AreEqual(1, snap.UpdatedColumns().Count);
            Assert.AreEqual("Values", snap.UpdatedColumns()[0]);
        }
    }

    public class SnapshotOnClass
    {
        public string Name { get; set; }
        [Reference(ReferenceType.Foreign, ColumnName = "phone", ReferenceMemberName = "PhoneId")]
        public PhoneClass Phone { get; set; }
        public int[] Values { get; set; }

        public class PhoneClass
        {
            public int PhoneId { get; set; }
            public string Value { get; set; }
        }
    }
}
