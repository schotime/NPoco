using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace NPoco.Tests.QueryTests
{
    [TableName("Users"), PrimaryKey("UserId")]
    public class AttrUser
    {
        public int UserId { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public DateTime DateOfBirth { get; set; }
        public decimal Savings { get; set; }
    }

    [TestFixture]
    public class ConverterTests : QueryTests
    {
        [Test]
        public void DateIsOfKindUtcWithSmartConventions()
        {
            var data = Database.SingleById<User>(1);
            Assert.AreEqual(DateTimeKind.Utc, data.DateOfBirth.Kind);
        }

        [Test]
        public void DateIsOfUnspecifiedByDefault()
        {
            var data = Database.SingleById<AttrUser>(1);
            Assert.AreEqual(DateTimeKind.Unspecified, data.DateOfBirth.Kind);
        }
    }
}
