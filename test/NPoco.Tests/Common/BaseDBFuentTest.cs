using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
#if !DNXCORE50
using FirebirdSql.Data.FirebirdClient;
#endif
using NPoco.DatabaseTypes;
using NPoco.FluentMappings;
using NPoco.Tests.FluentMappings;
using NPoco.Tests.FluentTests.QueryTests;
using NUnit.Framework;
using Microsoft.Extensions.Configuration;
using NPoco.Tests.NewMapper.Models;

namespace NPoco.Tests.Common
{
    public class BaseDBFuentTest : BaseDBTest
    {
        public List<User> InMemoryUsers { get; set; }
        public List<ExtraUserInfo> InMemoryExtraUserInfos { get; set; }
        public List<House> InMemoryHouses { get; set; }

        private static string ToLowerIf(string s, bool clause)
        {
            return clause ? s.ToLowerInvariant() : s;
        }

        [OneTimeSetUp]
        public void SetUp()
        {
            var types = new[] { typeof(User), typeof(ExtraUserInfo), typeof(UserWithExtraInfo), typeof(Usersss), typeof(House), typeof(Supervisor) };
            var dbFactory = new DatabaseFactory();
            var config = FluentMappingConfiguration.Scan(s =>
            {
                s.Assembly(typeof(User).GetTypeInfo().Assembly);
                s.IncludeTypes(types.Contains);
                s.PrimaryKeysNamed(y => ToLowerIf(y.Name + "Id", false));
                s.TablesNamed(y => ToLowerIf(Inflector.MakePlural(y.Name), false));
                s.Columns.Named(x => ToLowerIf(x.Name, false));
                s.Columns.ForceDateTimesToUtcWhere(x => x.GetMemberInfoType() == typeof(DateTime) || x.GetMemberInfoType() == typeof(DateTime?));
                s.Columns.ResultWhere(y => ColumnInfo.FromMemberInfo(y).ResultColumn);
                s.OverrideMappingsWith(new FluentMappingOverrides());
                s.OverrideMappingsWith(new OneToManyMappings());
            });
            dbFactory.Config().WithFluentConfig(config);

            var configuration = new ConfigurationBuilder()
                .AddJsonFile("config.json")
                .Build();

            var testDBType = Convert.ToInt32(configuration.GetSection("TestDBType").Value);
            switch (testDBType)
            {
                case 1: // SQLite In-Memory
                    TestDatabase = new InMemoryDatabase();
                    Database = dbFactory.Build(new Database(TestDatabase.Connection));
                    break;

                case 2: // SQL Local DB
                case 3: // SQL Server
                    var dataSource = configuration.GetSection("TestDbDataSource").Value;
                    TestDatabase = new SQLLocalDatabase(dataSource);
                    Database = dbFactory.Build(new Database(TestDatabase.Connection, new SqlServer2008DatabaseType()));
                    break;

                case 4: // SQL CE
                case 5: // MySQL
                case 6: // Oracle
                case 7: // Postgres
                    Assert.Fail("Database platform not supported for unit testing");
                    return;
#if !DNXCORE50
                case 8: // Firebird
                    TestDatabase = new FirebirdDatabase();
                    var db = new Database(TestDatabase.Connection, new FirebirdDatabaseType());
                    db.Mappers.Insert(0, new FirebirdDefaultMapper());
                    Database = dbFactory.Build(db);
                    break;
#endif

                default:
                    Assert.Fail("Unknown database platform specified");
                    return;
            }

            InsertData();
        }

        [SetUp]
        public void BeforeTest()
        {
            if (!NoTransaction)
            {
                Database.BeginTransaction();
            }
            else
            {
                SetUp();
            }
        }

        public bool NoTransaction { get; set; }

        [TearDown]
        public void CleanUp()
        {
            if (TestDatabase == null) return;

            if (!NoTransaction)
            {
                Database.AbortTransaction();
            }

            TestDatabase.CleanupDataBase();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            TestDatabase.Dispose();
        }

        protected virtual void InsertData()
        {
            InMemoryUsers = new List<User>();
            InMemoryExtraUserInfos = new List<ExtraUserInfo>();
            InMemoryHouses = new List<House>();

            for (var i = 0; i < 5; i++)
            {
                var house = new House()
                {
                    Address = i + " Road Street, Suburb"
                };
                Database.Insert(house);
                InMemoryHouses.Add(house);
            }

            Database.Insert(new House
                {
                    Address = "_ Road\\Street, Suburb"
                }
            );

            for (var i = 0; i < 15; i++)
            {
                var user = new User
                {
                    Name = "Name" + (i + 1),
                    Age = 20 + (i + 1),
                    DateOfBirth = new DateTime(1970, 1, 1).AddYears(i - 1),
                    Savings = 50.00m + (1.01m * (i + 1)),
                    IsMale = (i % 2 == 0),
                    YorN = (i % 2 == 0) ? 'Y' : 'N',
                    UniqueId = (i % 2 != 0 ? Guid.NewGuid() : (Guid?)null),
                    TimeSpan = new TimeSpan(1, 1, 1),
                    House = i % 2 == 0 ? null : InMemoryHouses[i % 5],
                    SupervisorId = (i + 1) % 2 == 0 ? (i + 1) : (int?)null,
                    Address = i % 10 == 0 ? null : new Address()
                    {
                        Street = i + " Road Street",
                        City = "City " + i
                    },
                    TestEnum = (i + 1) % 2 == 0 ? TestEnum.All : TestEnum.None
                };
                Database.Insert(user);
                InMemoryUsers.Add(user);

                var extra = new ExtraUserInfo
                {
                    UserId = user.UserId,
                    Email = "email" + (i + 1) + "@email.com",
                    Children = (i + 1)
                };
                Database.Insert(extra);
                InMemoryExtraUserInfos.Add(extra);

                var one = new One()
                {
                    Name = "Name" + (i + 1),
                };
                Database.Insert(one);

                for (int j = 0; j < (i % 3); j++)
                {
                    var many = new Many()
                    {
                        OneId = one.OneId,
                        Currency = "Cur" + (i + j + 1),
                        Value = (i + j + 1)
                    };
                    Database.Insert(many);
                }
            }
        }

        protected void AssertUserValues(User expected, User actual)
        {
            Assert.AreEqual(expected.UserId, actual.UserId);
            Assert.AreEqual(expected.Name, actual.Name);
            Assert.AreEqual(expected.Age, actual.Age);
            Assert.AreEqual(expected.DateOfBirth, actual.DateOfBirth);
            Assert.AreEqual(expected.Savings, actual.Savings);
            if (expected.Address != null)
            {
                Assert.AreEqual(expected.Address.Street, actual.Address.Street);
                Assert.AreEqual(expected.Address.City, actual.Address.City);
            }
            else
            {
                Assert.AreEqual(expected.Address, actual.Address);
            }
        }

        protected void AssertExtraUserInfo(ExtraUserInfo extraUserInfo, ExtraUserInfo actualUserInfo)
        {
            Assert.AreEqual(extraUserInfo.UserId, actualUserInfo.UserId);
            Assert.AreEqual(extraUserInfo.Email, actualUserInfo.Email);
        }

        protected void AssertUserHouseValues(User expected, User actual)
        {
            if (actual.House == null)
                Assert.Null(expected.House);
            else
                Assert.AreEqual(expected.House.HouseId, actual.House.HouseId);
        }
    }

    public class FluentMappingOverrides : Mappings
    {
        public FluentMappingOverrides()
        {
            For<User>().Columns(x =>
            {
                x.Column(y => y.IsMale).WithName("is_male");
                x.Column(y => y.TestEnum).WithDbType<string>();
                x.Column(y => y.Address).ComplexMapping();
                x.Column(y => y.House).WithName("HouseId").Reference(z => z.HouseId);
                x.Column(y => y.ExtraUserInfo).WithName("UserId").Reference(z => z.UserId, ReferenceType.OneToOne);
            });
            For<Supervisor>().UseMap<SupervisorMap>();
            For<Supervisor>().TableName("users").Columns(x => x.Column(y => y.IsMale).WithName("is_male"));
        }
    }

    public class OneToManyMappings : Mappings
    {
        public OneToManyMappings()
        {
            For<One>()
                .TableName("Ones")
                .PrimaryKey(x => x.OneId)
                .Columns(x =>
                {
                    x.Column(y => y.OneId);
                    x.Column(y => y.Name);
                    x.Column(y => y.Nested).ComplexMapping().Result();
                    x.Many(y => y.Items).WithName("OneId").Reference(y => y.OneId);
                }, true);

            For<Many>()
                .TableName("Manys")
                .PrimaryKey(x => x.ManyId)
                .Columns(x =>
                {
                    x.Column(y => y.ManyId);
                    x.Column(y => y.Value);
                    x.Column(y => y.Currency);
                    x.Column(y => y.OneId);
                    x.Column(y => y.One).WithName("OneId").Reference(y => y.OneId, ReferenceType.OneToOne);
                }, true);
        }
    }
}
