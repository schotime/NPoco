using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace NPoco.Tests
{
    [TestFixture]
    public class HashCodeCombinerTests
    {

        [Test]
        public void HashCombiner_Test_String()
        {
            var combiner1 = new HashCodeCombiner();
            combiner1.AddCaseInsensitiveString("Hello");

            var combiner2 = new HashCodeCombiner();
            combiner2.AddCaseInsensitiveString("hello");

            Assert.AreEqual(combiner1.GetCombinedHashCode(), combiner2.GetCombinedHashCode());

            combiner2.AddCaseInsensitiveString("world");

            Assert.AreNotEqual(combiner1.GetCombinedHashCode(), combiner2.GetCombinedHashCode());
        }

        [Test]
        public void HashCombiner_Test_Int()
        {
            var combiner1 = new HashCodeCombiner();
            combiner1.AddInt(1234);

            var combiner2 = new HashCodeCombiner();
            combiner2.AddInt(1234);

            Assert.AreEqual(combiner1.GetCombinedHashCode(), combiner2.GetCombinedHashCode());

            combiner2.AddInt(1);

            Assert.AreNotEqual(combiner1.GetCombinedHashCode(), combiner2.GetCombinedHashCode());
        }

    }
}