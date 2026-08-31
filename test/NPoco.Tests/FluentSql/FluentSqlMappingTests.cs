using System;
using System.Linq;
using NPoco.FluentSql;
using NPoco.Tests.Common;
using NPoco.Tests.NewMapper;
using NPoco.Tests.NewMapper.Models;
using NUnit.Framework;

namespace NPoco.Tests.FluentSqlTests
{
    /// <summary>
    /// What the projection plan has to know about NPoco's mapping. A projected entity is built by
    /// the plan rather than by NPoco's own row mapper, so every mapping feature that lives in
    /// PocoData - complex members, value objects, serialized columns, converters - has to work the
    /// same on both paths or the two ways of asking for a row disagree.
    /// </summary>
    [TestFixture]
    public class FluentSqlMappingTests : BaseDBFluentTest
    {
        [Test] public void ProjectedEntityAgreesWithThePlainEntitySelect()
        {
            var plain = Database.FluentQuery().From<User>(out var a)
                .Where(a, x => x.UserId <= 4).OrderBy(a, x => x.UserId).Select(a).Fetch();

            var projected = Database.FluentQuery().From<User>(out var b)
                .Where(b, x => x.UserId <= 4).OrderBy(b, x => x.UserId)
                .Select(() => b.Row).Fetch();

            Assert.That(projected.Count, Is.EqualTo(plain.Count));
            for (var i = 0; i < plain.Count; i++)
            {
                var p = plain[i];
                var q = projected[i];
                Assert.That(q.UserId, Is.EqualTo(p.UserId), "UserId");
                Assert.That(q.Name, Is.EqualTo(p.Name), "Name");
                Assert.That(q.DateOfBirth, Is.EqualTo(p.DateOfBirth), "DateOfBirth");
                Assert.That(q.DateOfBirth?.Kind, Is.EqualTo(p.DateOfBirth?.Kind), "DateOfBirth.Kind");
                Assert.That(q.Savings, Is.EqualTo(p.Savings), "Savings");
                Assert.That(q.TestEnum, Is.EqualTo(p.TestEnum), "TestEnum");
                Assert.That(q.TimeSpan, Is.EqualTo(p.TimeSpan), "TimeSpan");
                Assert.That(q.UniqueId, Is.EqualTo(p.UniqueId), "UniqueId");
                Assert.That(q.YorN, Is.EqualTo(p.YorN), "YorN");
                Assert.That(q.YorNBoolean, Is.EqualTo(p.YorNBoolean), "YorNBoolean");
                Assert.That(q.Expires, Is.EqualTo(p.Expires), "Expires");
                Assert.That(q.AnsiString, Is.EqualTo(p.AnsiString), "AnsiString");
                Assert.That(q.StringObject?.MyValue, Is.EqualTo(p.StringObject?.MyValue), "StringObject");
                Assert.That(q.Address?.Street, Is.EqualTo(p.Address?.Street), "Address.Street");
                Assert.That(q.Address?.City, Is.EqualTo(p.Address?.City), "Address.City");
                Assert.That(q.House?.HouseId, Is.EqualTo(p.House?.HouseId), "House");
                Assert.That(q.ExtraUserInfo?.Email, Is.EqualTo(p.ExtraUserInfo?.Email), "ExtraUserInfo");
            }
        }

        // A complex-mapped column sets its value on the nested object, not on the row, so an entity
        // that has one is the case that catches a plan setting every column on the root instance.
        [Test] public void EntityWithAComplexMemberMaterializesInsideAnAnonymousType()
        {
            var rows = Database.FluentQuery()
                .From<User>(out var user)
                .InnerJoin<ExtraUserInfo>(out var extra, e => e.UserId == user.Row.UserId)
                .Where(user, x => x.UserId <= 3)
                .OrderBy(user, x => x.UserId)
                .Select(() => new { User = user.Row, Extra = extra.Row })
                .Fetch();

            Assert.That(rows.Count, Is.EqualTo(3));
            for (var i = 0; i < rows.Count; i++)
            {
                AssertUserValues(InMemoryUsers[i], rows[i].User);
                AssertExtraUserInfo(InMemoryExtraUserInfos[i], rows[i].Extra);
            }
        }

        [Test] public void EntityWithAComplexMemberMaterializesInsideAMemberInitialiser()
        {
            var rows = Database.FluentQuery()
                .From<User>(out var user)
                .Where(user, x => x.UserId <= 3)
                .OrderBy(user, x => x.UserId)
                .Select(() => new Holder { User = user.Row, Label = user.Row.Name })
                .Fetch();

            for (var i = 0; i < rows.Count; i++)
            {
                AssertUserValues(InMemoryUsers[i], rows[i].User);
                Assert.That(rows[i].Label, Is.EqualTo(InMemoryUsers[i].Name));
            }
        }

        [Test] public void EntityWithAComplexMemberMaterializesThroughAConstructor()
        {
            var rows = Database.FluentQuery()
                .From<User>(out var user)
                .Where(user, x => x.UserId <= 3)
                .OrderBy(user, x => x.UserId)
                .Select(() => new CtorHolder(user.Row, user.Row.Name))
                .Fetch();

            for (var i = 0; i < rows.Count; i++) AssertUserValues(InMemoryUsers[i], rows[i].User);
        }

        [Test] public void EntityWithAComplexMemberMaterializesTwoLevelsDown()
        {
            var rows = Database.FluentQuery()
                .From<User>(out var user)
                .Where(user, x => x.UserId <= 3)
                .OrderBy(user, x => x.UserId)
                .Select(() => new { Inner = new { U = user.Row } })
                .Fetch();

            for (var i = 0; i < rows.Count; i++) AssertUserValues(InMemoryUsers[i], rows[i].Inner.U);
        }

        // A CTE addresses its columns by the alias the inner query gave them, not by column name.
        [Test] public void EntityWithAComplexMemberMaterializesThroughACte()
        {
            var rows = Database.FluentQuery()
                .With(q => q.From<User>(out var inner)
                    .Where(inner, x => x.UserId <= 3)
                    .Select(inner), out var cte)
                .From(cte)
                .OrderBy(cte, x => x.UserId)
                .Select(() => new { U = cte.Row })
                .Fetch();

            Assert.That(rows.Count, Is.EqualTo(3));
            for (var i = 0; i < rows.Count; i++) AssertUserValues(InMemoryUsers[i], rows[i].U);
        }

        [Test] public void AComplexMemberCanBeProjectedOnItsOwn()
        {
            var rows = Database.FluentQuery()
                .From<User>(out var user)
                .Where(user, x => x.UserId == 2)
                .Select(() => new { user.Row.Address })
                .Fetch();

            Assert.That(rows[0].Address.Street, Is.EqualTo(InMemoryUsers[1].Address.Street));
            Assert.That(rows[0].Address.City, Is.EqualTo(InMemoryUsers[1].Address.City));
        }

        [Test] public void AColumnUnderAComplexMemberCanBeProjectedOnItsOwn()
        {
            var street = Database.FluentQuery()
                .From<User>(out var user)
                .Where(user, x => x.UserId == 2)
                .SelectScalar(user, x => x.Address.Street)
                .Single();

            Assert.That(street, Is.EqualTo(InMemoryUsers[1].Address.Street));
        }

        [Test] public void AComplexMemberCanBeUsedInAPredicateAndASort()
        {
            var names = Database.FluentQuery()
                .From<User>(out var user)
                .Where(() => user.Row.Address.City != null)
                .OrderByDescending(user, x => x.Address.City)
                .Take(2)
                .SelectScalar(user, x => x.Name)
                .Fetch();

            Assert.That(names.Count, Is.EqualTo(2));
        }

        // The nested object is only created for a value that is actually there, so a row whose
        // complex columns all came back null keeps a null member rather than an empty one.
        [Test] public void AComplexMemberWhoseColumnsAreAllNullStaysNull()
        {
            var rows = Database.FluentQuery()
                .From<User>(out var user)
                .Where(user, x => x.UserId == 1)
                .Select(() => new { U = user.Row })
                .Fetch();

            Assert.That(InMemoryUsers[0].Address, Is.Null, "seed data changed");
            Assert.That(rows[0].U.Address, Is.Null);
        }

        [Test] public void AnUnmatchedLeftJoinProjectsANullEntity()
        {
            var rows = Database.FluentQuery()
                .From<User>(out var user)
                .LeftJoin<ExtraUserInfo>(out var extra, e => e.UserId == user.Row.UserId + 10000)
                .Where(user, x => x.UserId == 1)
                .Select(() => new { U = user.Row, E = extra.Row })
                .Fetch();

            Assert.That(rows[0].E, Is.Null);
            Assert.That(rows[0].U, Is.Not.Null);
        }

        [Test] public void OneEntityProjectedFromTwoAliasesOfTheSameTable()
        {
            var rows = Database.FluentQuery()
                .From<User>(out var user)
                .InnerJoin<User>(out var again, s => s.UserId == user.Row.UserId)
                .Where(user, x => x.UserId == 2)
                .Select(() => new { A = user.Row, B = again.Row })
                .Fetch();

            AssertUserValues(InMemoryUsers[1], rows[0].A);
            AssertUserValues(InMemoryUsers[1], rows[0].B);
        }

        [Test] public void ValueTupleAndRecordProjectionsMaterialize()
        {
            var tuple = Database.FluentQuery().From<User>(out var user)
                .Where(user, x => x.UserId == 2)
                .Select(() => new ValueTuple<int, string>(user.Row.UserId, user.Row.Name))
                .Single();

            Assert.That(tuple.Item1, Is.EqualTo(2));
            Assert.That(tuple.Item2, Is.EqualTo("Name2"));

            var record = Database.FluentQuery().From<User>(out var other)
                .Where(other, x => x.UserId == 2)
                .Select(() => new UserRecord(other.Row.UserId, other.Row.Name, other.Row))
                .Single();

            Assert.That(record.Id, Is.EqualTo(2));
            AssertUserValues(InMemoryUsers[1], record.Row);
        }

        public class Holder
        {
            public User User { get; set; }
            public string Label { get; set; }
        }

        public class CtorHolder
        {
            public CtorHolder(User user, string label)
            {
                User = user;
                Label = label;
            }

            public User User { get; }
            public string Label { get; }
        }

        public record UserRecord(int Id, string Name, User Row);
    }

    /// <summary>
    /// The same, for the mapping features that only the attribute-decorated pocos carry: value
    /// objects and serialized columns.
    /// </summary>
    [TestFixture]
    public class FluentSqlDecoratedMappingTests : BaseDBDecoratedTest
    {
        [Test] public void AValueObjectColumnMaterializesOnAProjectedEntity()
        {
            var rows = Database.FluentQuery().From<User1>(out var user)
                .Where(() => user.Row.UserId == 1)
                .Select(() => new { U = user.Row })
                .Fetch();

            Assert.That(rows[0].U.Name.Value, Is.EqualTo("Name1"));
        }

        // The wrapper is built by the column, so a query that reads only that column has to go
        // through it too rather than handing back whatever the reader returned.
        [Test] public void AValueObjectColumnMaterializesWhenSelectedAsAScalar()
        {
            var fromTable = Database.FluentQuery().From<User1>(out var user)
                .Where(() => user.Row.UserId == 1)
                .SelectScalar(user, x => x.Name)
                .Single();

            Assert.That(fromTable.Value, Is.EqualTo("Name1"));

            var fromRow = Database.FluentQuery().From<User1>(out var other)
                .Where(() => other.Row.UserId == 1)
                .SelectScalar(() => other.Row.Name)
                .Single();

            Assert.That(fromRow.Value, Is.EqualTo("Name1"));
        }

        [Test] public void AValueObjectColumnMaterializesInsideAProjection()
        {
            var rows = Database.FluentQuery().From<User1>(out var user)
                .Where(() => user.Row.UserId == 1)
                .Select(() => new { user.Row.Name, user.Row.UserId })
                .Fetch();

            Assert.That(rows[0].UserId, Is.EqualTo(1));
            Assert.That(rows[0].Name.Value, Is.EqualTo("Name1"));
        }

        [Test] public void ASerializedColumnMaterializesOnAProjectedEntity()
        {
            var plain = Database.Fetch<UserWithAddress>().First();

            var rows = Database.FluentQuery().From<UserWithAddress>(out var user)
                .Where(() => user.Row.Id == plain.Id)
                .Select(() => new { U = user.Row })
                .Fetch();

            Assert.That(rows[0].U.Address, Is.Not.Null);
            Assert.That(rows[0].U.Address.StreetName, Is.EqualTo(plain.Address.StreetName));
            Assert.That(rows[0].U.Address.AddressFurtherInfo?.PostCode,
                Is.EqualTo(plain.Address.AddressFurtherInfo?.PostCode));
        }

        [Test] public void ASerializedColumnMaterializesAsAProjectedMember()
        {
            var plain = Database.Fetch<UserWithAddress>().First();

            var rows = Database.FluentQuery().From<UserWithAddress>(out var user)
                .Where(() => user.Row.Id == plain.Id)
                .Select(() => new { user.Row.Id, user.Row.Address })
                .Fetch();

            Assert.That(rows[0].Address, Is.Not.Null);
            Assert.That(rows[0].Address.StreetName, Is.EqualTo(plain.Address.StreetName));
        }

        [Test] public void ASerializedColumnMaterializesOnItsOwn()
        {
            var plain = Database.Fetch<UserWithAddress>().First();

            var address = Database.FluentQuery().From<UserWithAddress>(out var user)
                .Where(() => user.Row.Id == plain.Id)
                .SelectScalar(() => user.Row.Address)
                .Single();

            Assert.That(address, Is.Not.Null);
            Assert.That(address.StreetName, Is.EqualTo(plain.Address.StreetName));
        }
    }
}
