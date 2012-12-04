using System;
using NPoco.DatabaseTypes;
using NPoco.Tests.Common;
using NUnit.Framework;

namespace NPoco.Tests.DecoratedTests
{
    [TestFixture]
    //[NUnit.Framework.Ignore("Appearently the decorated syntax and fluent syntax are some how conflicting.")]
    public class TransactionDecoratedTests : BaseDBDecoratedTest
    {
        [Test]
        public void ExternalTransactionComplete()
        {
            using (var scope = Database.GetTransaction())
            {
                var user = new UserDecorated
                {
                    Name = "Name" + 16,
                    Age = 20 + 16,
                    DateOfBirth = new DateTime(1970, 1, 1).AddYears(16),
                    Savings = 50.00m + (1.01m * 16)
                };
                InMemoryUsers.Add(user);
                Database.Insert(user);

                var extra = new ExtraUserInfoDecorated
                {
                    UserId = user.UserId,
                    Email = "email" + 16 + "@email.com",
                    Children = 16
                };
                InMemoryExtraUserInfos.Add(extra);
                Database.Insert(extra);

                scope.Complete();
            }

            var count = Database.ExecuteScalar<long>("SELECT COUNT(*) FROM Users");
            Assert.AreEqual(count, 16);
        }

        [Test]
        public void NestedTransactionThatFailsAbortsWhole()
        {
            using (var scope = Database.GetTransaction())
            {
                using (var scope2 = Database.GetTransaction())
                {
                    

                    var user1 = new UserDecorated
                    {
                        Name = "Name" + 16,
                        Age = 20 + 16,
                        DateOfBirth = new DateTime(1970, 1, 1).AddYears(16),
                        Savings = 50.00m + (1.01m * 16)
                    };
                    InMemoryUsers.Add(user1);
                    Database.Insert(user1);

                    var extra1 = new ExtraUserInfoDecorated
                    {
                        UserId = user1.UserId,
                        Email = "email" + 16 + "@email.com",
                        Children = 16
                    };
                    InMemoryExtraUserInfos.Add(extra1);
                    Database.Insert(extra1);
                }

                var user = new UserDecorated
                {
                    Name = "Name" + 16,
                    Age = 20 + 16,
                    DateOfBirth = new DateTime(1970, 1, 1).AddYears(16),
                    Savings = 50.00m + (1.01m * 16)
                };
                InMemoryUsers.Add(user);
                Database.Insert(user);

                var extra = new ExtraUserInfoDecorated
                {
                    UserId = user.UserId,
                    Email = "email" + 16 + "@email.com",
                    Children = 16
                };
                InMemoryExtraUserInfos.Add(extra);
                Database.Insert(extra);

                

                //scope.Complete();
            }

            var count = Database.ExecuteScalar<long>("SELECT COUNT(*) FROM Users");
            Assert.AreEqual(15, count);
        }

        [Test]
        public void NestedTransactionsBothComplete()
        {
            using (var scope = Database.GetTransaction())
            {
                var user = new UserDecorated
                {
                    Name = "Name" + 16,
                    Age = 20 + 16,
                    DateOfBirth = new DateTime(1970, 1, 1).AddYears(16),
                    Savings = 50.00m + (1.01m * 16)
                };
                InMemoryUsers.Add(user);
                Database.Insert(user);

                var extra = new ExtraUserInfoDecorated
                {
                    UserId = user.UserId,
                    Email = "email" + 16 + "@email.com",
                    Children = 16
                };
                InMemoryExtraUserInfos.Add(extra);
                Database.Insert(extra);

                using (var scope2 = Database.GetTransaction())
                {
                    var user1 = new UserDecorated
                    {
                        Name = "Name" + 16,
                        Age = 20 + 16,
                        DateOfBirth = new DateTime(1970, 1, 1).AddYears(16),
                        Savings = 50.00m + (1.01m * 16)
                    };
                    InMemoryUsers.Add(user1);
                    Database.Insert(user1);

                    var extra1 = new ExtraUserInfoDecorated
                    {
                        UserId = user1.UserId,
                        Email = "email" + 16 + "@email.com",
                        Children = 16
                    };
                    InMemoryExtraUserInfos.Add(extra1);
                    Database.Insert(extra1);
                    
                    scope2.Complete();
                }

                scope.Complete();
            }

            var count = Database.ExecuteScalar<long>("SELECT COUNT(*) FROM Users");
            Assert.AreEqual(17, count);
        }

        [Test]
        // This will fail using SQLite in-memory as the transaction wraps the whole connection :/ Will probably need 
        // to switch over to SQLite file based at some point. So that one connection can create the DB and a different connection can 
        // do the tests. 
        public void ExternalTransactionDiscarded()
        {
            using (var scope = Database.GetTransaction())
            {
                var user = new UserDecorated
                {
                    Name = "Name" + 16,
                    Age = 20 + 16,
                    DateOfBirth = new DateTime(1970, 1, 1).AddYears(16),
                    Savings = 50.00m + (1.01m * 16)
                };
                InMemoryUsers.Add(user);
                Database.Insert(user);

                var extra = new ExtraUserInfoDecorated
                {
                    UserId = user.UserId,
                    Email = "email" + 16 + "@email.com",
                    Children = 16
                };
                InMemoryExtraUserInfos.Add(extra);
                Database.Insert(extra);

                scope.Dispose();
            }

            var count = Database.ExecuteScalar<long>("SELECT COUNT(*) FROM Users");
            Assert.AreEqual(count, 15);
        }

        [Test]
        public void TransactionSettingsDontCauseLocksAndTransationCompletes()
        {
            var nameInsert = "Name" + 16;
            var ageInsert = 20 + 16;
            var nameUpdate = "Name" + 99;
            var ageUpdate = 20 + 99;

            var user = new UserDecorated
            {
                Name = nameInsert,
                Age = ageInsert,
                DateOfBirth = new DateTime(1970, 1, 1).AddYears(16),
                Savings = 50.00m + (1.01m * 16)
            };
            Database.Insert(user);

            var userAfterCreate = Database.SingleOrDefault<UserDecorated>("WHERE UserID = @0", user.UserId);
            Assert.IsNotNull(userAfterCreate);
            Assert.AreEqual(userAfterCreate.Name, nameInsert);


            var dbTrans = new Database(TestDatabase.ConnectionString, new SqlServer2012DatabaseType());
            dbTrans.BeginTransaction();

            user.Name = nameUpdate;
            user.Age = ageUpdate;
            dbTrans.Update(user);

            var userPreCommitInside = dbTrans.SingleOrDefault<UserDecorated>("WHERE UserID = @0", user.UserId);
            Assert.IsNotNull(userPreCommitInside);
            Assert.AreEqual(nameUpdate, userPreCommitInside.Name);
            Assert.AreEqual(ageUpdate, userPreCommitInside.Age);


            dbTrans.CompleteTransaction();
            dbTrans.Dispose();

            var userPostCommit = Database.SingleOrDefault<UserDecorated>("WHERE UserID = @0", user.UserId);
            Assert.IsNotNull(userPostCommit);
            Assert.AreEqual(nameUpdate, userPostCommit.Name);
            Assert.AreEqual(ageUpdate, userPostCommit.Age);
        }


        [Test]
        public void TransactionSettingsDontCauseLocksAndTransationRollback()
        {
            var nameInsert = "Name" + 16;
            var ageInsert = 20 + 16;
            var nameUpdate = "Name" + 99;
            var ageUpdate = 20 + 99;

            var user = new UserDecorated
            {
                Name = nameInsert,
                Age = ageInsert,
                DateOfBirth = new DateTime(1970, 1, 1).AddYears(16),
                Savings = 50.00m + (1.01m * 16)
            };
            Database.Insert(user);

            var userAfterCreate = Database.SingleOrDefault<UserDecorated>("WHERE UserID = @0", user.UserId);
            Assert.IsNotNull(userAfterCreate);
            Assert.AreEqual(userAfterCreate.Name, nameInsert);


            var dbTrans = new Database(TestDatabase.ConnectionString, new SqlServer2012DatabaseType());
            dbTrans.BeginTransaction();

            user.Name = nameUpdate;
            user.Age = ageUpdate;
            dbTrans.Update(user);

            // Verify inside of transaction
            var userPreCommitInside = dbTrans.SingleOrDefault<UserDecorated>("WHERE UserID = @0", user.UserId);
            Assert.IsNotNull(userPreCommitInside);
            Assert.AreEqual(nameUpdate, userPreCommitInside.Name);
            Assert.AreEqual(ageUpdate, userPreCommitInside.Age);

            dbTrans.AbortTransaction();
            dbTrans.Dispose();

            var userPostCommit = Database.SingleOrDefault<UserDecorated>("WHERE UserID = @0", user.UserId);
            Assert.IsNotNull(userPostCommit);
            Assert.AreEqual(nameInsert, userPostCommit.Name);
            Assert.AreEqual(ageInsert, userPostCommit.Age);
        }
    }
}
