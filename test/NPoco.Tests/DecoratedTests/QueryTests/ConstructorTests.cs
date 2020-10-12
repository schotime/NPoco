using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;
using NPoco.Tests.Common;
using NUnit.Framework;

namespace NPoco.Tests.DecoratedTests.QueryTests
{
    [TestFixture]
    public class ConstructorTests : BaseDBDecoratedTest
    {
        [Test]
        public void TestPublicParameter()
        {
            var result = Database.Single<MultipleParameterConstructor>("select 'Name' Name, 23 Age");
            Assert.True(result.Multiple);
        }

        [Test]
        public void TestParameterLess()
        {
            var result = Database.Single<SingleParameterConstructor>("select 'Name' Name, 23 Age");
            Assert.True(result.Single);
            Assert.False(result.Multiple);
        }

        [Test]
        public void TestParameterLess2()
        {
            var result = Database.Single<GuidConstructor>($"select '{Guid.NewGuid()}' Id");
            Assert.AreNotEqual(Guid.Empty, result.Id);
        }

        public class GuidConstructor
        {
            public Guid Id { get; }

            public GuidConstructor(Guid id)
            {
                Id = id;
            }
        }

        public class MultipleParameterConstructor
        {
            public string Name { get; }
            public int Age { get; }

            public bool Multiple { get; }

            public MultipleParameterConstructor(string Name, int Age)
            {
                this.Name = Name;
                this.Age = Age;
                Multiple = true;
            }
        }

        public class SingleParameterConstructor
        {
            public string Name { get; }
            public int Age { get; }

            public bool Multiple { get; }
            public bool Single { get; }

            [Construct]
            private SingleParameterConstructor()
            {
                Single = true;
            }

            public SingleParameterConstructor(string Name, int Age)
            {
                this.Name = Name;
                this.Age = Age;
                Multiple = true;
            }
        }
    }


}
