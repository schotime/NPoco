using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NPoco.FluentMappings;
using NUnit.Framework;

namespace NPoco.Tests
{
    [TestFixture]
    public class SnapshotterTests
    {
        [TestFixtureSetUp]
        public void Setup()
        {
            FluentMappingConfiguration.Configure(new MyMappings());
        }

        public class MyMappings : Mappings
        {
            public MyMappings()
            {
                For<Admin>().Columns(y => y.Column(x => x.Age).WithName("TheAge"));
            }
        }

        [Test]
        public void BasicDiffUsingSnapshotter()
        {
            var user = new Admin() { UserId = 1 };
            var snap = Snapshotter.Start(user);
            
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
            var user = new Admin() { UserId = 1 };
            user.Name = "Name1";
            user.Savings = 50.50m;
            user.DateOfBirth = new DateTime(2001, 1, 1);
            user.Age = 21;

            var snap = Snapshotter.Start(user);
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
            var user = new Admin() { UserId = 1 };
            var snap = Snapshotter.Start(user);

            Assert.AreEqual(0, snap.Changes().Count);
            Assert.AreEqual(0, snap.UpdatedColumns().Count);
        }
    }
}
