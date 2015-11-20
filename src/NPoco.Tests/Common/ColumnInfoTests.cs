using System.Reflection;
using NPoco;
using NUnit.Framework;

namespace NPoco.Tests.Common
{
    public class ColumnInfoTests
    {
        [Test]
        public void ColumnInfoShouldInheritTheBaseClassPropertyAttributes()
        {
            var memberInfo = ColumnInfo.FromMemberInfo(typeof (OverrideTest1).GetMember("Id")[0]);
            Assert.AreEqual("TestId", memberInfo.ColumnName);
        }
    }

    public class Test1
    {
        [Column("TestId")]
        public virtual int Id { get; set; }
    }

    public class OverrideTest1 : Test1
    {
        public override int Id { get; set; }
    }
}
