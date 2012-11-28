using System;
using NPoco.Tests.Common;
using NUnit.Framework;

namespace NPoco.Tests.DecoratedTests.QueryTests
{
    [TestFixture]
    [NUnit.Framework.Ignore("not sure enough how this part of the code base works to make the decorated test work to test utc conversion ")]
    public class ConverterDecoratedTests : BaseDBDecoratedTest
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
            var data = Database.SingleById<UserDecorated>(1);
            Assert.AreEqual(DateTimeKind.Unspecified, data.DateOfBirth.Kind);
        }
    }
}
