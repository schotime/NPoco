using System;
using NPoco.Tests.Common;
using NUnit.Framework;

namespace NPoco.Tests.DecoratedTests.QueryTests
{
    [TestFixture]
    public class ConverterDecoratedTests : BaseDBDecoratedTest
    {
        [Test]
        public void DateIsOfUnspecifiedByDefault()
        {
            var data = Database.SingleById<UserDecorated>(1);
            Assert.AreEqual(DateTimeKind.Unspecified, data.DateOfBirth.Kind);
        }
    }
}
