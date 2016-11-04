using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
#if !DNXCORE50
using FirebirdSql.Data.FirebirdClient;
#endif
using NPoco;
using NPoco.DatabaseTypes;
using NPoco.Tests.NewMapper.Models;
using NUnit.Framework;
using Microsoft.Extensions.Configuration;

namespace NPoco.Tests.Common
{
    public class BaseDBDecoratedTest : BaseDBTest
    {
        public List<UserDecorated> InMemoryUsers { get; set; }
        public List<ExtraUserInfoDecorated> InMemoryExtraUserInfos { get; set; }
        public List<CompositeObjectDecorated> InMemoryCompositeObjects { get; set; }
        public List<HouseDecorated> InMemoryHouses { get; set; }


        [OneTimeSetUp]
        public void SetUp()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("config.json")
                .Build();
            
            var testDBType = Convert.ToInt32(configuration.GetSection("TestDBType").Value);
            switch (testDBType)
            {
                case 1: // SQLite In-Memory
                    TestDatabase = new InMemoryDatabase();
                    Database = new Database(TestDatabase.Connection);
                    break;

                case 2: // SQL Local DB
                    var dataSource = configuration.GetSection("TestDbDataSource").Value;
                    TestDatabase = new SQLLocalDatabase(dataSource);
                    Database = new Database(TestDatabase.Connection, new SqlServer2008DatabaseType() { UseOutputClause = false }, IsolationLevel.ReadUncommitted); // Need read uncommitted for the transaction tests
                    break;

                case 3: // SQL Server
                case 4: // SQL CE
                case 5: // MySQL
                case 6: // Oracle
                case 7: // Postgres
                    Assert.Fail("Database platform not supported for unit testing");
                    return;
#if !DNXCORE50
                case 8: // Firebird
                    TestDatabase = new FirebirdDatabase();
                    Database = new Database(TestDatabase.Connection, new FirebirdDatabaseType(), IsolationLevel.ReadUncommitted);
                    break;
#endif

                default:
                    Assert.Fail("Unknown database platform specified");
                    return;
            }

            // Insert test data
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

        protected void InsertData()
        {
            InMemoryUsers = new List<UserDecorated>();
            InMemoryExtraUserInfos = new List<ExtraUserInfoDecorated>();
            InMemoryCompositeObjects = new List<CompositeObjectDecorated>();
            InMemoryHouses = new List<HouseDecorated>();

            for (var i = 0; i < 5; i++)
            {
                var house = new HouseDecorated()
                {
                    Address = i + " Road Street, Suburb"
                };
                Database.Insert(house);
                InMemoryHouses.Add(house);
            }

            for (var i = 0; i < 15; i++)
            {
                var pos = i + 1;

                var user = new UserDecorated
                {
                    Name = "Name" + (i + 1),
                    Age = 20 + (i + 1),
                    DateOfBirth = new DateTime(1970, 1, 1).AddYears(i + 1),
                    Savings = 50.00m + (1.01m * (i + 1)),
                    IsMale = (i%2==0),
                    HouseId = i % 2 == 0 ? (int?)null : InMemoryHouses[i % 5].HouseId
                };
                Database.Insert(user);
                InMemoryUsers.Add(user);

                var extra = new ExtraUserInfoDecorated
                {
                    UserId = user.UserId,
                    Email = "email" + (i + 1) + "@email.com",
                    Children = (i + 1)
                };
                Database.Insert(extra);
                InMemoryExtraUserInfos.Add(extra);

                var composite = new CompositeObjectDecorated
                {
                    Key1ID = pos,
                    Key2ID = i + 2,
                    Key3ID = i + 4,
                    TextData = "This is some text data.",
                    DateEntered = DateTime.Now
                };
                Database.Insert(composite);
                InMemoryCompositeObjects.Add(composite);

                var recursionUser = new RecursionUser
                {
                    Name = "Name" + (i + 1),
                    CreatedBy = new RecursionUser() {Id = 1},
                    Supervisor = new RecursionUser() {Id = 2}
                };
                Database.Insert(recursionUser);

                var one = new One()
                {
                    Name = "Name" + (i + 1),
                };
                Database.Insert(one);

                for (int j = 0; j < (i%3); j++)
                {
                    var many = new Many()
                    {
                        OneId = one.OneId,
                        Currency = "Cur" + (i + j + 1),
                        Value = (i + j + 1)
                    };
                    Database.Insert(many);
                }

                var userWithAddress = new UserWithAddress()
                {
                    Name = "Name" + (i + 1),
                    Address = new UserWithAddress.MyAddress()
                    {
                        StreetNo = i + 1,
                        StreetName = "Street" + (i + 1),
                        MovedInOn = new DateTime(1970, 1, 1).AddYears(i + 1),
                        AddressFurtherInfo = new UserWithAddress.MyAddress.AddressInfo()
                        {
                            PostCode = "99999"
                        }
                    }
                };

                Database.Insert(userWithAddress);
            }
            
            // Verify DB record counts
            var userCount = Database.ExecuteScalar<int>("SELECT COUNT(UserId) FROM Users");
            Assert.AreEqual(InMemoryUsers.Count, userCount, "Test User Data not in sync db has " + userCount + " records, but the in memory copy has only " + InMemoryUsers.Count + " records.");
            System.Diagnostics.Debug.WriteLine("Created " + userCount + " test users for the unit tests.");

            var userExtraInfoCount = Database.ExecuteScalar<int>("SELECT COUNT(ExtraUserInfoId) FROM ExtraUserInfos");
            Assert.AreEqual(InMemoryExtraUserInfos.Count, userExtraInfoCount, "Test User Extra Info Data not in sync db has " + userExtraInfoCount + " records, but the in memory copy has only " + InMemoryExtraUserInfos.Count + " records.");
            System.Diagnostics.Debug.WriteLine("Created " + userExtraInfoCount + " test extra user info records for the unit tests.");

            var compositeObjectCount = Database.ExecuteScalar<int>("SELECT COUNT(Key1_ID) FROM CompositeObjects");
            Assert.AreEqual(InMemoryCompositeObjects.Count, compositeObjectCount, "Test Composite Object Data not in sync db has " + compositeObjectCount + " records, but the in memory copy has only " + InMemoryCompositeObjects.Count + " records.");
            System.Diagnostics.Debug.WriteLine("Created " + compositeObjectCount + " test composite PK objects for the unit tests.");
        }

        protected void AssertUserValues(UserDecorated expected, UserDecorated actual)
        {
            Assert.AreEqual(expected.UserId, actual.UserId);
            Assert.AreEqual(expected.Name, actual.Name);
            Assert.AreEqual(expected.Age, actual.Age);
            Assert.AreEqual(expected.DateOfBirth, actual.DateOfBirth);
            Assert.AreEqual(expected.Savings, actual.Savings);
        }
    }
}
