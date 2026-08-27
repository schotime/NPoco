using System.Text.Json;
using NUnit.Framework;

namespace NPoco.Tests
{
    public class SystemTextJsonColumnSerializerTests
    {
        public class Sample
        {
            public string Name { get; set; }
            public int Age { get; set; }
        }

        [Test]
        public void SerializeThenDeserialize_RoundTrips()
        {
            var serializer = new SystemTextJsonColumnSerializer();
            var value = new Sample { Name = "Bob", Age = 30 };

            var json = serializer.Serialize(value);
            var result = (Sample)serializer.Deserialize(json, typeof(Sample));

            Assert.AreEqual(value.Name, result.Name);
            Assert.AreEqual(value.Age, result.Age);
        }

        [Test]
        public void Serialize_Null_ReturnsJsonNullLiteral()
        {
            var serializer = new SystemTextJsonColumnSerializer();

            Assert.AreEqual("null", serializer.Serialize(null));
        }

        [Test]
        public void SerializerOptions_AreHonoured()
        {
            var serializer = new SystemTextJsonColumnSerializer
            {
                SerializerOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
            };

            var json = serializer.Serialize(new Sample { Name = "Bob", Age = 30 });

            StringAssert.Contains("\"name\":\"Bob\"", json);
            StringAssert.Contains("\"age\":30", json);
        }
    }
}
