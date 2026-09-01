using System;
using Microsoft.Data.Sqlite;
using NPoco.FluentSql;
using NUnit.Framework;

namespace NPoco.Tests.FluentSqlTests
{
    /// <summary>
    /// The builder's refusals. Every one of these is a mistake that produces invalid SQL, silently
    /// wrong SQL, or a database error far from its cause, so each has to fail while the query is
    /// being built and say why.
    /// </summary>
    [TestFixture]
    public class FluentSqlGuardTests
    {
        private SqliteConnection _connection;
        private Database _database;

        [SetUp]
        public void SetUp()
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();
            _database = new Database(_connection, DatabaseType.SQLite);
            _database.Execute("create table guarditems(id integer primary key, name text)");
        }

        [TearDown]
        public void TearDown()
        {
            _database?.Dispose();
            _connection?.Dispose();
        }

        [Test] public void ScalarRejectsASubqueryProjectingMoreThanOneColumn()
        {
            var outer = _database.FluentQuery().From<GuardItem>(out var item);
            var two = outer.Subquery().From<GuardItem>(out var inner)
                .Select(() => new { inner.Row.Id, inner.Row.Name });

            var query = outer.Select(() => new { V = FSql.Scalar<int>(two) });

            var exception = Assert.Throws<InvalidOperationException>(() => query.ToSql());
            Assert.That(exception.Message, Does.Contain("exactly one column"));
            Assert.That(exception.Message, Does.Contain("projects 2"));
        }

        [Test] public void InRejectsASubqueryProjectingMoreThanOneColumn()
        {
            var outer = _database.FluentQuery().From<GuardItem>(out var item);
            var two = outer.Subquery().From<GuardItem>(out var inner)
                .Select(() => new { inner.Row.Id, inner.Row.Name });

            var query = outer.Where(() => item.Row.Id.In(two)).Select(() => new { item.Row.Name });

            var exception = Assert.Throws<InvalidOperationException>(() => query.ToSql());
            Assert.That(exception.Message, Does.Contain("exactly one column"));
        }

        [Test] public void SingleValueSubqueriesSatisfyTheSingleColumnRule()
        {
            var outer = _database.FluentQuery().From<GuardItem>(out var item);
            var one = outer.Subquery().From<GuardItem>(out var inner)
                .Where(() => inner.Row.Id == item.Row.Id)
                .Select(() => inner.Row.Id);

            Assert.DoesNotThrow(() => outer.Select(() => new { V = FSql.Scalar<int>(one) }).ToSql());
        }

        [Test] public void UnionRejectsOrderBySkipAndTakeOnTheQueryBeingUnioned()
        {
            var ordered = _database.FluentQuery().From<GuardItem>(out var a)
                .OrderBy(() => a.Row.Id)
                .Select(() => a.Row.Name);
            Assert.That(Assert.Throws<InvalidOperationException>(
                    () => ordered.UnionAll(q => q.From<GuardItem>(out var b).Select(() => b.Row.Name))).Message,
                Does.Contain("UNION operand"));

            var taken = _database.FluentQuery().From<GuardItem>(out var c).Take(1)
                .Select(() => c.Row.Name);
            Assert.Throws<InvalidOperationException>(
                () => taken.UnionAll(q => q.From<GuardItem>(out var d).Select(() => d.Row.Name)));

            var skipped = _database.FluentQuery().From<GuardItem>(out var e).Skip(1)
                .Select(() => e.Row.Name);
            Assert.Throws<InvalidOperationException>(
                () => skipped.UnionAll(q => q.From<GuardItem>(out var f).Select(() => f.Row.Name)));
        }

        [Test] public void UnionRejectsOrderBySkipAndTakeInsideAnOperand()
        {
            var query = _database.FluentQuery().From<GuardItem>(out var a).Select(() => a.Row.Name);

            Assert.That(Assert.Throws<InvalidOperationException>(() => query.UnionAll(q =>
                    q.From<GuardItem>(out var b).OrderBy(() => b.Row.Id).Select(() => b.Row.Name))).Message,
                Does.Contain("UNION operand"));

            Assert.Throws<InvalidOperationException>(() => query.UnionAll(q =>
                q.From<GuardItem>(out var c).Take(2).Select(() => c.Row.Name)));
        }

        [Test] public void ExpressionFormsRejectATableBelongingToAnotherQuery()
        {
            _database.FluentQuery().From<GuardItem>(out var foreignTable);

            Assert.That(Assert.Throws<InvalidOperationException>(() => _database.FluentQuery()
                    .From<GuardItem>(out var a)
                    .OrderBy(() => foreignTable.Row.Id)
                    .Select(() => a.Row.Name).ToSql()).Message,
                Does.Contain("not available to this query"));

            Assert.Throws<InvalidOperationException>(() => _database.FluentQuery()
                .From<GuardItem>(out var b)
                .GroupBy(() => foreignTable.Row.Id)
                .Select(() => b.Row.Name).ToSql());

            Assert.Throws<InvalidOperationException>(() => _database.FluentQuery()
                .From<GuardItem>(out var c)
                .Where(() => foreignTable.Row.Id == 1)
                .Select(() => c.Row.Name).ToSql());

            Assert.Throws<InvalidOperationException>(() => _database.FluentQuery()
                .From<GuardItem>(out var d)
                .GroupBy(() => d.Row.Id)
                .Having(() => foreignTable.Row.Id > 1)
                .Select(() => d.Row.Name).ToSql());

            Assert.Throws<InvalidOperationException>(() => _database.FluentQuery()
                .From<GuardItem>(out var e)
                .SelectScalar(() => foreignTable.Row.Name).ToSql());
        }

        [Test] public void TableFormsRejectATableBelongingToAnotherQuery()
        {
            _database.FluentQuery().From<GuardItem>(out var foreignTable);

            Assert.Throws<InvalidOperationException>(() => _database.FluentQuery()
                .From<GuardItem>(out var a)
                .OrderBy(foreignTable, x => x.Id)
                .Select(() => a.Row.Name).ToSql());

            Assert.Throws<InvalidOperationException>(() => _database.FluentQuery()
                .From<GuardItem>(out var b)
                .GroupBy(foreignTable, x => x.Id)
                .Select(() => b.Row.Name).ToSql());
        }

        [Test] public void TakeAndSkipValidateTheirArguments()
        {
            var stage = _database.FluentQuery().From<GuardItem>(out var item);

            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => stage.Take(0)).Message,
                Does.Contain("Take must be greater than zero"));
            Assert.Throws<ArgumentOutOfRangeException>(() => stage.Take(-1));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => stage.Skip(-1)).Message,
                Does.Contain("Skip cannot be negative"));
            Assert.DoesNotThrow(() => stage.Skip(0));
        }

        [Test] public void NullExpressionsAreRejectedRatherThanIgnored()
        {
            var stage = _database.FluentQuery().From<GuardItem>(out var item);

            Assert.Throws<ArgumentNullException>(() => stage.Where((System.Linq.Expressions.Expression<Func<bool>>)null));
            Assert.Throws<ArgumentNullException>(() => stage.OrderBy((System.Linq.Expressions.Expression<Func<int>>)null));
            Assert.Throws<ArgumentNullException>(() => stage.GroupBy((System.Linq.Expressions.Expression<Func<int>>)null));
            Assert.Throws<ArgumentNullException>(() => stage.SelectScalar((System.Linq.Expressions.Expression<Func<int>>)null));
            Assert.Throws<ArgumentNullException>(() => stage.Select((System.Linq.Expressions.Expression<Func<int>>)null));
        }

        [Test] public void ACteProjectingASingleValueIsRejectedWhereItIsDeclared()
        {
            // A CTE is addressed through a TableReference<T>, so T needs mapped columns. A
            // single-value body leaves the CTE's one column unnamed and unreferenceable, which
            // used to surface much later as "a query must end with Select".
            var query = _database.FluentQuery();

            var exception = Assert.Throws<InvalidOperationException>(() => query.With(
                sub => sub.From<GuardItem>(out var item).Select(() => item.Row.Name), out var t));
            Assert.That(exception.Message, Does.Contain("A CTE must project a type with mapped columns"));
            Assert.That(exception.Message, Does.Contain("String"));
        }

        [Test] public void AnOuterApplyProjectingASingleValueIsRejectedWhereItIsDeclared()
        {
            var stage = _database.FluentQuery().From<GuardItem>(out var item);

            var exception = Assert.Throws<InvalidOperationException>(() => stage.OuterApply<string>(out var t,
                sub => sub.From<GuardItem>(out var inner).Select(() => inner.Row.Name)));
            Assert.That(exception.Message, Does.Contain("An OUTER APPLY must project a type with mapped columns"));
        }

        [Test] public void CtesAndAppliesProjectingAShapeWithColumnsAreAccepted()
        {
            var query = _database.FluentQuery();
            Assert.DoesNotThrow(() => query.With(
                sub => sub.From<GuardItem>(out var item).Select(() => new { item.Row.Id, item.Row.Name }), out var t));

            var stage = _database.FluentQuery().From<GuardItem>(out var outerItem);
            Assert.DoesNotThrow(() => stage.OuterApply(out var applied,
                sub => sub.From<GuardItem>(out var inner).Select(inner)));
        }

        [Test] public void SubqueryBeforeAnyFromIsRejected()
        {
            // A subquery correlates with the query it is built from, and before a FROM there is
            // nothing there to correlate with. Nothing can be written against one either: the
            // reference a correlated body reads is handed out by the FROM that has not run yet.
            var query = _database.FluentQuery();
            Assert.Throws<InvalidOperationException>(() => query.Subquery());
        }

        [Test] public void ADeclaredTableReferenceIsAddedOnce()
        {
            var query = _database.FluentQuery().From<GuardItem>(out var item);
            var declared = query.Table<GuardNote>();

            query.Subquery().From(declared).Where(() => declared.Row.ItemId == item.Row.Id).SelectScalar(() => 1);

            Assert.Throws<InvalidOperationException>(() => query.Subquery().From(declared));
            Assert.Throws<InvalidOperationException>(() => query.InnerJoin(declared, () => declared.Row.ItemId == item.Row.Id));
        }

        [Test] public void ADeclaredTableReferenceFromAnotherStatementIsRejected()
        {
            var other = _database.FluentQuery().From<GuardItem>(out _);
            var stranger = other.Table<GuardNote>();

            var query = _database.FluentQuery().From<GuardItem>(out var item);

            Assert.Throws<InvalidOperationException>(() => query.Subquery().From(stranger));
            Assert.Throws<InvalidOperationException>(() => query.InnerJoin(stranger, () => stranger.Row.ItemId == item.Row.Id));
        }

        [Test] public void ADeclaredTableReferenceIsNotInScopeUntilItIsAdded()
        {
            var query = _database.FluentQuery().From<GuardItem>(out var item);
            var declared = query.Table<GuardNote>();

            Assert.Throws<InvalidOperationException>(() => query
                .Where(() => declared.Row.ItemId == item.Row.Id)
                .Select(() => item.Row.Name)
                .ToSql());
        }

        [TableName("guarditems")]
        public class GuardItem
        {
            [Column("id")] public int Id { get; set; }
            [Column("name")] public string Name { get; set; }
        }

        // Declared with Table<T> and never executed, so the table behind it only has to map.
        [TableName("guardnotes")]
        public class GuardNote
        {
            [Column("id")] public int Id { get; set; }
            [Column("item_id")] public int ItemId { get; set; }
        }
    }
}
