using System;
using NPoco.Tests.Common;
using NUnit.Framework;

namespace NPoco.Tests.FluentTests.QueryTests
{
    [TestFixture]
    public class ConverterFluentTest : BaseDBFuentTest
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
            Assert.AreEqual(DateTimeKind.Utc, data.DateOfBirth.Kind);
        }
    }
}
