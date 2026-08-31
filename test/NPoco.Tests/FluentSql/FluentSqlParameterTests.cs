using System;
using System.Linq;
using NPoco.FluentSql;
using NPoco.Tests.Common;
using NUnit.Framework;

namespace NPoco.Tests.FluentSqlTests
{
    /// <summary>
    /// Values crossing into a predicate. A column knows how it is stored - a value object wraps
    /// its value, an enum may be a name rather than an ordinal, an AnsiString is not an nvarchar -
    /// and a comparison has to send what the column stores, or it matches nothing and says nothing
    /// about why. Every type the seeded user carries is pinned here for that reason.
    /// </summary>
    [TestFixture]
    public class FluentSqlParameterTests : BaseDBFluentTest
    {

        [Test] public void DateOnlyParameter()
        {
            var expected = InMemoryUsers.Where(x => x.Expires == new DateOnly(2099, 10, 10)).Select(x => x.UserId).ToArray();
            Assert.That(expected, Is.Not.Empty, "seed data changed");

            var ids = Database.FluentQuery().From<User>(out var user)
                .Where(() => user.Row.Expires == new DateOnly(2099, 10, 10))
                .OrderBy(user, x => x.UserId)
                .SelectScalar(user, x => x.UserId).Fetch();

            Assert.That(ids, Is.EqualTo(expected));
        }

        [Test] public void GuidParameter()
        {
            var expected = InMemoryUsers.First(x => x.UniqueId.HasValue);

            var ids = Database.FluentQuery().From<User>(out var user)
                .Where(() => user.Row.UniqueId == expected.UniqueId)
                .SelectScalar(user, x => x.UserId).Fetch();

            Assert.That(ids, Is.EqualTo(new[] { expected.UserId }));
        }

        [Test] public void TimeSpanParameter()
        {
            var span = new TimeSpan(1, 1, 1);

            var count = Database.FluentQuery().From<User>(out var user)
                .Where(() => user.Row.TimeSpan == span)
                .Select(() => FSql.Count()).Single();

            Assert.That(count, Is.EqualTo(InMemoryUsers.Count(x => x.TimeSpan == span)));
        }

        [Test] public void DecimalParameter()
        {
            var threshold = 55.0m;

            var ids = Database.FluentQuery().From<User>(out var user)
                .Where(() => user.Row.Savings > threshold)
                .OrderBy(user, x => x.UserId)
                .SelectScalar(user, x => x.UserId).Fetch();

            Assert.That(ids, Is.EqualTo(InMemoryUsers.Where(x => x.Savings > threshold).Select(x => x.UserId).ToArray()));
        }

        [Test] public void CharParameter()
        {
            var ids = Database.FluentQuery().From<User>(out var user)
                .Where(() => user.Row.YorN == 'Y')
                .OrderBy(user, x => x.UserId)
                .SelectScalar(user, x => x.UserId).Fetch();

            Assert.That(ids, Is.EqualTo(InMemoryUsers.Where(x => x.YorN == 'Y').Select(x => x.UserId).ToArray()));
        }

        [Test] public void AnsiStringParameter()
        {
            var expected = InMemoryUsers[1];

            var ids = Database.FluentQuery().From<User>(out var user)
                .Where(() => user.Row.AnsiString == expected.AnsiString)
                .SelectScalar(user, x => x.UserId).Fetch();

            Assert.That(ids, Is.EqualTo(new[] { expected.UserId }));
        }

        [Test] public void StringBackedEnumParameter()
        {
            var expected = InMemoryUsers.Where(x => x.TestEnum == TestEnum.All).Select(x => x.UserId).ToArray();
            Assert.That(expected, Is.Not.Empty, "seed data changed");

            var sql = Database.FluentQuery().From<User>(out var a)
                .Where(() => a.Row.TestEnum == TestEnum.All)
                .SelectScalar(a, x => x.UserId).ToSql();
            Assert.That(sql.Arguments.Single(), Is.EqualTo("All"), "the column stores the name, not the ordinal");

            var ids = Database.FluentQuery().From<User>(out var b)
                .Where(() => b.Row.TestEnum == TestEnum.All)
                .OrderBy(b, x => x.UserId)
                .SelectScalar(b, x => x.UserId).Fetch();
            Assert.That(ids, Is.EqualTo(expected));
        }

        [Test] public void NullableComparisonsAgainstNull()
        {
            var missing = Database.FluentQuery().From<User>(out var a)
                .Where(() => a.Row.UniqueId == null)
                .OrderBy(a, x => x.UserId)
                .SelectScalar(a, x => x.UserId).Fetch();
            Assert.That(missing, Is.EqualTo(InMemoryUsers.Where(x => x.UniqueId == null).Select(x => x.UserId).ToArray()));

            var present = Database.FluentQuery().From<User>(out var b)
                .Where(() => b.Row.UniqueId.HasValue)
                .OrderBy(b, x => x.UserId)
                .SelectScalar(b, x => x.UserId).Fetch();
            Assert.That(present, Is.EqualTo(InMemoryUsers.Where(x => x.UniqueId != null).Select(x => x.UserId).ToArray()));
        }

        [Test] public void CollectionParametersConvertPerElement()
        {
            var wanted = new[] { TestEnum.All };

            var ids = Database.FluentQuery().From<User>(out var user)
                .Where(() => user.Row.TestEnum.In(wanted))
                .OrderBy(user, x => x.UserId)
                .SelectScalar(user, x => x.UserId).Fetch();

            Assert.That(ids, Is.EqualTo(InMemoryUsers.Where(x => x.TestEnum == TestEnum.All).Select(x => x.UserId).ToArray()));
        }

        /// <summary>
        /// The dialect functions again, this time on SQL Server: the names differ from SQLite's,
        /// so running them here is what proves the dialect is actually being consulted.
        /// </summary>
        [Test] public void DialectFunctionsRunOnSqlServer()
        {
            var heads = Database.FluentQuery().From<User>(out var a)
                .Where(a, x => x.UserId <= 2)
                .OrderBy(a, x => x.UserId)
                .SelectScalar(() => a.Row.Name.Substring(0, 4)).Fetch();
            Assert.That(heads, Is.EqualTo(new[] { "Name", "Name" }));

            var lengths = Database.FluentQuery().From<User>(out var b)
                .Where(b, x => x.UserId == 1)
                .SelectScalar(() => b.Row.Name.Length).Single();
            Assert.That(lengths, Is.EqualTo("Name1".Length));

            var absolute = Database.FluentQuery().From<User>(out var c)
                .Where(c, x => x.UserId == 1)
                .SelectScalar(() => Math.Abs(c.Row.Age)).Single();
            Assert.That(absolute, Is.EqualTo(InMemoryUsers[0].Age));

            var fallback = Database.FluentQuery().From<User>(out var d)
                .Where(d, x => x.UserId == 1)
                .SelectScalar(() => d.Row.SupervisorId.GetValueOrDefault(-1)).Single();
            Assert.That(fallback, Is.EqualTo(InMemoryUsers[0].SupervisorId ?? -1));
        }
    }
}
