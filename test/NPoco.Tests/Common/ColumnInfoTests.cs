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

        [Test]
        public void ColumnInfoShouldGetNameFromFirstAttributeWhereNotNull()
        {
            var memberInfo = ColumnInfo.FromMemberInfo(typeof(Test1).GetMember("Result")[0]);
            Assert.AreEqual("MyResult", memberInfo.ColumnName);
            Assert.AreEqual(true, memberInfo.ForceToUtc);
        }

        [Test]
        public void ColumnInfoShouldAggregateForceToUtcColumnValues()
        {
            var memberInfo = ColumnInfo.FromMemberInfo(typeof(Test1).GetMember("ForceToUtc")[0]);
            Assert.AreEqual("ForceToUtc", memberInfo.ColumnName);
            Assert.AreEqual(false, memberInfo.ForceToUtc);
        }

        [Test]
        public void ColumnInfoShouldPreferResultColumnFirst()
        {
            var memberInfo = ColumnInfo.FromMemberInfo(typeof(Test1).GetMember("ResultWins")[0]);
            Assert.AreEqual("ResultWins", memberInfo.ColumnName);
            Assert.AreEqual(true, memberInfo.ResultColumn);
            Assert.AreEqual(false, memberInfo.ComputedColumn);
            Assert.AreEqual(false, memberInfo.VersionColumn);
        }

        [Test]
        public void ColumnInfoShouldPreferVersionColumnFirst()
        {
            var memberInfo = ColumnInfo.FromMemberInfo(typeof(Test1).GetMember("VersionWins")[0]);
            Assert.AreEqual("VersionWins", memberInfo.ColumnName);
            Assert.AreEqual(false, memberInfo.ResultColumn);
            Assert.AreEqual(false, memberInfo.ComputedColumn);
            Assert.AreEqual(true, memberInfo.VersionColumn);
        }

        [Test]
        public void ColumnInfoShouldPreferComputednColumnFirst()
        {
            var memberInfo = ColumnInfo.FromMemberInfo(typeof(Test1).GetMember("ComputedWins")[0]);
            Assert.AreEqual("ComputedWins", memberInfo.ColumnName);
            Assert.AreEqual(false, memberInfo.ResultColumn);
            Assert.AreEqual(true, memberInfo.ComputedColumn);
            Assert.AreEqual(false, memberInfo.VersionColumn);
        }
    }

    public class Test1
    {
        [Column("TestId")]
        public virtual int Id { get; set; }

        [Column("MyResult")]
        [ResultColumn("MyResult2")]
        public string Result { get; set; }

        [Column(ForceToUtc = true)]
        [ResultColumn(ForceToUtc = false)]
        public string ForceToUtc { get; set; }

        [Column]
        [ComputedColumn]
        [ResultColumn]
        [VersionColumn]
        public string ResultWins { get; set; }

        [Column]
        [ComputedColumn]
        [VersionColumn]
        public string VersionWins { get; set; }

        [Column]
        [ComputedColumn]
        public string ComputedWins { get; set; }
    }

    public class OverrideTest1 : Test1
    {
        public override int Id { get; set; }
    }
}
