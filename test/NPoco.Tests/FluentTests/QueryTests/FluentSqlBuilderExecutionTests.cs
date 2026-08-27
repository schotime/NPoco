using System.Linq;
using NPoco.FluentSqlBuilder;
using NPoco.Tests.Common;
using NUnit.Framework;

namespace NPoco.Tests.FluentTests.QueryTests
{
    [TestFixture]
    public class FluentSqlBuilderExecutionTests : BaseDBFluentTest
    {
        [Test]
        public void FetchesEntityUsingExistingFluentMappings()
        {
            var users = Database.FluentQuery()
                .From<User>(out var user)
                .Where(user, x => x.UserId > 10)
                .OrderBy(user, x => x.UserId)
                .Select(user)
                .Fetch();

            Assert.That(users.Count, Is.EqualTo(5));
            for (var i = 0; i < users.Count; i++)
                AssertUserValues(InMemoryUsers[i + 10], users[i]);
        }

        [Test]
        public void ExecutesConditionalWhereAndCollectionIn()
        {
            var ids = new[] { 2, 4, 6 };
            var users = Database.FluentQuery()
                .From<User>(out var user)
                .WhereIf(false, user, x => x.Name == "not-used")
                .Where(user, x => x.UserId.In(ids))
                .OrderBy(user, x => x.UserId)
                .Select(user)
                .Fetch();

            Assert.That(users.Select(x => x.UserId), Is.EqualTo(ids));
        }

        [Test]
        public void ExecutesJoinIntoFlatDto()
        {
            var rows = Database.FluentQuery()
                .From<User>(out var user)
                .InnerJoin<ExtraUserInfo>(out var extra, e => e.UserId == user.Row.UserId)
                .Where(user, x => x.UserId <= 3)
                .OrderBy(user, x => x.UserId)
                .SelectInto<CustomerUser>(select => select
                    .Column(user, x => x.UserId, x => x.Id)
                    .Column(user, x => x.Name, x => x.CustomerName)
                    .Column(extra, x => x.Email, x => x.CustomerEmail))
                .Fetch();

            Assert.That(rows.Count, Is.EqualTo(3));
            Assert.That(rows[1].CustomerName, Is.EqualTo("Name2"));
            Assert.That(rows[1].CustomerEmail, Is.EqualTo("email2@email.com"));
        }

        [Test]
        public void ExecutesJoinIntoNestedResultUsingNaturalSeparators()
        {
            var row = Database.FluentQuery()
                .From<User>(out var user)
                .InnerJoin<ExtraUserInfo>(out var extra, e => e.UserId == user.Row.UserId)
                .Where(user, x => x.UserId == 1)
                .SelectInto<UserWithExtraInfo>(select => select
                    .All(user)
                    .All(extra, x => x.ExtraUserInfo))
                .Single();

            AssertUserValues(InMemoryUsers[0], row);
            Assert.That(row.ExtraUserInfo, Is.Not.Null);
            AssertExtraUserInfo(InMemoryExtraUserInfos[0], row.ExtraUserInfo);
        }

        [Test]
        public void LeftJoinMapsMissingNestedObjectToNull()
        {
            var row = Database.FluentQuery()
                .From<User>(out var user)
                .LeftJoin<ExtraUserInfo>(out var extra, e => e.UserId == -1)
                .Where(user, x => x.UserId == 1)
                .SelectInto<UserWithExtraInfo>(select => select
                    .All(user)
                    .All(extra, x => x.ExtraUserInfo))
                .Single();

            Assert.That(row.UserId, Is.EqualTo(1));
            Assert.That(row.ExtraUserInfo, Is.Null);
        }

        [Test]
        public void ExecutesAggregateGroupByAndHaving()
        {
            var rows = Database.FluentQuery()
                .From<User>(out var user)
                .InnerJoin<ExtraUserInfo>(out var extra, e => e.UserId == user.Row.UserId)
                .GroupBy(user, x => x.IsMale)
                .Having(extra, x => FluentSql.Count(x.ExtraUserInfoId) > 7)
                .OrderBy(user, x => x.IsMale)
                .SelectInto<GenderSummary>(select => select
                    .Column(user, x => x.IsMale, x => x.IsMale)
                    .Column(extra, x => FluentSql.Count(x.ExtraUserInfoId), x => x.Count))
                .Fetch();

            Assert.That(rows.Count, Is.EqualTo(1));
            Assert.That(rows[0].IsMale, Is.True);
            Assert.That(rows[0].Count, Is.EqualTo(8));
        }

        [Test]
        public void ExecutesTypedInSubquery()
        {
            var subquery = Database.FluentQuery()
                .From<ExtraUserInfo>(out var extra)
                .Where(extra, x => x.Children >= 13)
                .SelectScalar(extra, x => x.UserId);

            var users = Database.FluentQuery()
                .From<User>(out var user)
                .Where(user, x => x.UserId.In(subquery))
                .OrderBy(user, x => x.UserId)
                .Select(user)
                .Fetch();

            Assert.That(users.Select(x => x.UserId), Is.EqualTo(new[] { 13, 14, 15 }));
        }

        [Test]
        public void ExecutesScalarSelection()
        {
            var name = Database.FluentQuery()
                .From<User>(out var user)
                .Where(user, x => x.UserId == 2)
                .SelectScalar(user, x => x.Name)
                .Single();

            Assert.That(name, Is.EqualTo("Name2"));
        }

        [Test]
        public void ExecutesDistinctPagingAndGroupedOrPredicates()
        {
            var users = Database.FluentQuery()
                .From<User>(out var user)
                .WhereGroup(group => group
                    .And(() => user.Row.UserId > 2)
                    .Or(() => user.Row.UserId == 1))
                .Distinct()
                .OrderBy(user, x => x.UserId)
                .Skip(1)
                .Take(2)
                .Select(user)
                .Fetch();

            Assert.That(users.Select(x => x.UserId), Is.EqualTo(new[] { 3, 4 }));
        }

        [Test]
        public void ExecutesCorrelatedOuterApply()
        {
            if ((Database.DatabaseType.GetProviderName() ?? string.Empty).IndexOf("SqlClient", System.StringComparison.OrdinalIgnoreCase) < 0)
                Assert.Ignore("OUTER APPLY execution is only available on SQL Server in the configured integration database.");

            var rows = Database.FluentQuery()
                .From<User>(out var user)
                .OuterApply<ExtraUserInfo>(out var latestExtra, apply => apply
                    .From<ExtraUserInfo>(out var extra)
                    .Where(() => extra.Row.UserId == user.Row.UserId)
                    .OrderByDescending(extra, e => e.ExtraUserInfoId)
                    .Take(1)
                    .Select(extra))
                .Where(user, x => x.UserId <= 3)
                .OrderBy(user, x => x.UserId)
                .SelectInto<UserWithExtraInfo>(select => select
                    .All(user)
                    .All(latestExtra, x => x.ExtraUserInfo))
                .Fetch();

            Assert.That(rows.Count, Is.EqualTo(3));
            for (var i = 0; i < rows.Count; i++)
            {
                AssertUserValues(InMemoryUsers[i], rows[i]);
                AssertExtraUserInfo(InMemoryExtraUserInfos[i], rows[i].ExtraUserInfo);
            }
        }

        public class GenderSummary
        {
            public bool IsMale { get; set; }
            public int Count { get; set; }
        }
    }
}
