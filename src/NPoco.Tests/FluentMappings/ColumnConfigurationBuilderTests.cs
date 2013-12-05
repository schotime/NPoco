using System.Collections.Generic;
using NPoco.FluentMappings;
using NPoco.Tests.Common;
using NUnit.Framework;

namespace NPoco.Tests.FluentMappings
{
    [TestFixture]
    public class ColumnConfigurationBuilderTests
    {
        [Test]
        public void WithNameReturnsDbColumnNameCorrectly()
        {
            var columnDefinitions = new Dictionary<string, ColumnDefinition>();
            var columnBuilder = new ColumnConfigurationBuilder<User>(columnDefinitions);

            columnBuilder
                .Column(x => x.UserId).WithName("Id");

            Assert.AreEqual("Id", columnDefinitions["UserId"].DbColumnName);
        }

        [Test]
        public void WithDbTypeReturnsDbTypeCorrectly()
        {
            var columnDefinitions = new Dictionary<string, ColumnDefinition>();
            var columnBuilder = new ColumnConfigurationBuilder<User>(columnDefinitions);

            columnBuilder
                .Column(x => x.UserId).WithDbType(typeof(long));

            Assert.AreEqual(typeof(long), columnDefinitions["UserId"].DbColumnType);
        }

        [Test]
        public void WithGenericDbTypeReturnsDbTypeCorrectly()
        {
            var columnDefinitions = new Dictionary<string, ColumnDefinition>();
            var columnBuilder = new ColumnConfigurationBuilder<User>(columnDefinitions);

            columnBuilder
                .Column(x => x.UserId).WithDbType<long>();

            Assert.AreEqual(typeof(long), columnDefinitions["UserId"].DbColumnType);
        }

        [Test]
        public void VersionReturnsVersionColumn()
        {
            var columnDefinitions = new Dictionary<string, ColumnDefinition>();
            var columnBuilder = new ColumnConfigurationBuilder<User>(columnDefinitions);

            columnBuilder
                .Column(x => x.UserId).Version();

            Assert.AreEqual(true, columnDefinitions["UserId"].VersionColumn);
        }

        [Test]
        public void ResultReturnsResultColumn()
        {
            var columnDefinitions = new Dictionary<string, ColumnDefinition>();
            var columnBuilder = new ColumnConfigurationBuilder<User>(columnDefinitions);

            columnBuilder
                .Column(x => x.UserId).Result();

            Assert.AreEqual(true, columnDefinitions["UserId"].ResultColumn);
        }

        [Test]
        public void IgnoreReturnsIgnoreColumn()
        {
            var columnDefinitions = new Dictionary<string, ColumnDefinition>();
            var columnBuilder = new ColumnConfigurationBuilder<User>(columnDefinitions);

            columnBuilder
                .Column(x => x.UserId).Ignore();

            Assert.AreEqual(true, columnDefinitions["UserId"].IgnoreColumn);
        }

        [Test]
        public void MultpleOptionsChainedAreAllSet()
        {
            var columnDefinitions = new Dictionary<string, ColumnDefinition>();
            var columnBuilder = new ColumnConfigurationBuilder<User>(columnDefinitions);

            columnBuilder
                .Column(x => x.UserId)
                .WithName("Id")
                .WithDbType(typeof(long))
                .Result();

            Assert.AreEqual("Id", columnDefinitions["UserId"].DbColumnName);
            Assert.AreEqual(typeof(long), columnDefinitions["UserId"].DbColumnType);
            Assert.AreEqual(true, columnDefinitions["UserId"].ResultColumn);
            Assert.AreEqual(PropertyHelper<User>.GetProperty(x => x.UserId), columnDefinitions["UserId"].MemberInfo);
        }

        [Test]
        public void ScanIgnoresColumnsDefinedByConvention()
        {
            var map = FluentMappingConfiguration.Scan(scan =>
            {
                scan.Assembly(this.GetType().Assembly);
                scan.IncludeTypes(x => x == typeof (User));
                scan.Columns.IgnoreWhere(x => x.Name == "Age");
            });

            var pd = map.Config(new Mapper())(typeof (User));
            Assert.False(pd.Columns.ContainsKey("Age"));
        }

        [Test]
        public void ScanSetsResultColumnsDefinedByConvention()
        {
            var map = FluentMappingConfiguration.Scan(scan =>
            {
                scan.Assembly(this.GetType().Assembly);
                scan.IncludeTypes(x => x == typeof(User));
                scan.Columns.ResultWhere(x => x.Name == "Age");
            });

            var pd = map.Config(new Mapper())(typeof(User));
            Assert.True(pd.Columns.ContainsKey("Age"));
            Assert.True(pd.Columns["Age"].ResultColumn);
        }

        [Test]
        public void ScanSetsNameOfColumnDefinedByConvention()
        {
            var map = FluentMappingConfiguration.Scan(scan =>
            {
                scan.Assembly(this.GetType().Assembly);
                scan.IncludeTypes(x => x == typeof(User));
                scan.Columns.Named(x =>x.Name + "000");
            });

            var pd = map.Config(new Mapper())(typeof(User));
            Assert.True(pd.Columns.ContainsKey("Age000"));
            Assert.AreEqual("Age", pd.Columns["Age000"].MemberInfo.Name);
        }
    }
}