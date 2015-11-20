using NPoco;
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

        [Test]
        public void HashCombiner_Test_Type()
        {
            var combiner1 = new HashCodeCombiner();
            combiner1.AddType(typeof(HashCodeCombiner));

            var combiner2 = new HashCodeCombiner();
            combiner2.AddType(typeof(HashCodeCombiner));

            var combiner3 = new HashCodeCombiner();
            combiner3.AddType(typeof(HashCodeCombinerTests));

            Assert.AreEqual(combiner1.GetCombinedHashCode(), combiner2.GetCombinedHashCode());
            Assert.AreNotEqual(combiner2.GetCombinedHashCode(), combiner3.GetCombinedHashCode());

            combiner2.AddType(typeof(HashCodeCombiner));

            Assert.AreNotEqual(combiner1.GetCombinedHashCode(), combiner2.GetCombinedHashCode());
        }

        [Test]
        public void HashCombiner_Test_Bool()
        {
            var combiner1 = new HashCodeCombiner();
            combiner1.AddBool(true);

            var combiner2 = new HashCodeCombiner();
            combiner2.AddBool(true);

            var combiner3 = new HashCodeCombiner();
            combiner3.AddBool(false);

            Assert.AreEqual(combiner1.GetCombinedHashCode(), combiner2.GetCombinedHashCode());
            Assert.AreNotEqual(combiner2.GetCombinedHashCode(), combiner3.GetCombinedHashCode());

            combiner2.AddBool(true);

            Assert.AreNotEqual(combiner1.GetCombinedHashCode(), combiner2.GetCombinedHashCode());
        }
    }
}