using System.Collections.Generic;
using System.Linq;
using NPoco;
using NPoco.FluentMappings;
using NPoco.Tests.Common;
using NUnit.Framework;
using System.Reflection;

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
        public void WithAliasReturnsDbColumnNameCorrectly()
        {
            var columnDefinitions = new Dictionary<string, ColumnDefinition>();
            var columnBuilder = new ColumnConfigurationBuilder<User>(columnDefinitions);

            columnBuilder
                .Column(x => x.UserId).WithAlias("Identity");

            Assert.AreEqual("Identity", columnDefinitions["UserId"].DbColumnAlias);
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
            Assert.AreEqual(MemberHelper<User>.GetMembers(x => x.UserId).Last(), columnDefinitions["UserId"].MemberInfo);
        }

        [Test]
        public void ScanIgnoresColumnsDefinedByConvention()
        {
            var map = FluentMappingConfiguration.Scan(scan =>
            {
                scan.Assembly(this.GetType().GetTypeInfo().Assembly);
                scan.IncludeTypes(x => x == typeof(User) || ReflectionUtils.GetFieldsAndPropertiesForClasses(typeof(User)).Select(y=>y.GetMemberInfoType()).Contains(x));
                scan.Columns.IgnoreWhere(x => x.Name == "Age");
            });

            var pd = map.Config(new MapperCollection()).Resolver(typeof(User), new PocoDataFactory(new MapperCollection())).Build();
            Assert.False(pd.Columns.ContainsKey("Age"));
        }

        [Test]
        public void ScanSetsResultColumnsDefinedByConvention()
        {
            var map = FluentMappingConfiguration.Scan(scan =>
            {
                scan.Assembly(this.GetType().GetTypeInfo().Assembly);
                scan.IncludeTypes(x => x == typeof(User) || ReflectionUtils.GetFieldsAndPropertiesForClasses(typeof(User)).Select(y => y.GetMemberInfoType()).Contains(x));
                scan.Columns.ResultWhere(x => x.Name == "Age");
            });

            var pd = map.Config(new MapperCollection()).Resolver(typeof(User), new PocoDataFactory(new MapperCollection())).Build();
            Assert.True(pd.Columns.ContainsKey("Age"));
            Assert.True(pd.Columns["Age"].ResultColumn);
        }

        [Test]
        public void ScanSetsNameOfColumnDefinedByConvention()
        {
            var map = FluentMappingConfiguration.Scan(scan =>
            {
                scan.Assembly(this.GetType().GetTypeInfo().Assembly);
                scan.IncludeTypes(x => x == typeof(User) || ReflectionUtils.GetFieldsAndPropertiesForClasses(typeof(User)).Select(y => y.GetMemberInfoType()).Contains(x));
                scan.Columns.Named(x => x.Name + "000");
                scan.Columns.ReferenceNamed(x => x.Name + "Id000");
            });

            var pd = map.Config(new MapperCollection()).Resolver(typeof(User), new PocoDataFactory(new MapperCollection())).Build();
            Assert.True(pd.Columns.ContainsKey("Age000"));
            Assert.AreEqual("Age", pd.Columns["Age000"].MemberInfoData.Name);
        }

        [Test]
        public void PrimaryKeyShouldGetRemapped()
        {
            var map = FluentMappingConfiguration.Scan(scan =>
            {
                scan.Assembly(this.GetType().GetTypeInfo().Assembly);
                scan.IncludeTypes(x => x == typeof(User) || ReflectionUtils.GetFieldsAndPropertiesForClasses(typeof(User)).Select(y => y.GetMemberInfoType()).Contains(x));
                scan.OverrideMappingsWith(new MyPKMappings());
            });

            var pd = map.Config(new MapperCollection()).Resolver(typeof(User), new PocoDataFactory(new MapperCollection())).Build();
            Assert.True(pd.Columns.ContainsKey("user_id"));
            Assert.AreEqual("user_id", pd.TableInfo.PrimaryKey);
        }

        public class MyPKMappings : Mappings
        {
            public MyPKMappings()
            {
                For<User>().PrimaryKey(y => y.UserId, false).Columns(x=>x.Column(y=>y.UserId).WithName("user_id"));
            }
        }

        [Test]
        public void NestedColumn()
        {
            var columnDefinitions = new Dictionary<string, ColumnDefinition>();
            var columnBuilder = new ColumnConfigurationBuilder<User>(columnDefinitions);

            columnBuilder
                .Column(x => x.House.Address);

            Assert.AreEqual(true, columnDefinitions.ContainsKey("House__Address"));
            Assert.AreEqual(MemberHelper<User>.GetMembers(x => x.House.Address).Last(), columnDefinitions["House__Address"].MemberInfo);
        }

        public class FluentMappingOverrides : Mappings
        {
            public FluentMappingOverrides()
            {
                For<User>().Columns(x =>
                {
                    x.Column(y => y.Address).ComplexMapping("CM");
                });
            }
        }

        [Test]
        public void FluentMappingOverridesShouldOverrideComplexMappingPrefix()
        {
            var map = FluentMappingConfiguration.Scan(s =>
            {
                s.Assembly(typeof(User).GetTypeInfo().Assembly);
                s.IncludeTypes(t => t == typeof(User));
                s.Columns.ComplexPropertiesWhere(y => ColumnInfo.FromMemberInfo(y).ComplexMapping);
                s.OverrideMappingsWith(new FluentMappingOverrides());
            });

            var pd = map.Config(new MapperCollection()).Resolver(typeof(User), new PocoDataFactory(new MapperCollection())).Build();
            Assert.AreEqual(true, pd.Columns.ContainsKey("CM__Street"));
        }
    }
}