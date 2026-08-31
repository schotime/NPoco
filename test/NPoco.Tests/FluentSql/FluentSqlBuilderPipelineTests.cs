using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using NPoco.FluentSqlBuilder;
using NUnit.Framework;

namespace NPoco.Tests.FluentSqlTests
{
    /// <summary>
    /// The fluent builder's projection path materializes rows itself. These tests pin it to the
    /// same pipeline semantics as a plain Fetch: connections, transactions, interceptors,
    /// exception reporting and IOnLoaded.
    ///
    /// The database is built from a connection string rather than a DbConnection on purpose.
    /// Passing a connection in sets KeepConnectionAlive, which hides every connection lifetime
    /// problem these tests exist to catch.
    /// </summary>
    [TestFixture]
    public class FluentSqlBuilderPipelineTests
    {
        private string _file;
        private string _connectionString;

        [SetUp]
        public void SetUp()
        {
            PipelineUser.LoadedCount = 0;
            PipelineDto.LoadedCount = 0;
            _file = Path.Combine(Path.GetTempPath(), "npoco-pipeline-" + Guid.NewGuid().ToString("N") + ".db");
            _connectionString = "Data Source=" + _file;
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText =
                    "create table pipelineusers(userid integer primary key, name text, age integer);" +
                    "insert into pipelineusers values(1,'one',11),(2,'two',22),(3,'three',33);";
                command.ExecuteNonQuery();
            }
        }

        [TearDown]
        public void TearDown()
        {
            SqliteConnection.ClearAllPools();
            try { File.Delete(_file); } catch (IOException) { }
        }

        private Database CreateDatabase() => new Database(_connectionString, DatabaseType.SQLite, SqliteFactory.Instance);

        [Test]
        public void QueryStreamsAProjectionAndClosesTheConnection()
        {
            using (var database = CreateDatabase())
            {
                var names = new List<string>();
                foreach (var row in database.FluentQuery()
                    .From<PipelineUser>(out var user)
                    .OrderBy(user, x => x.UserId)
                    .Select(() => new { user.Row.Name })
                    .Query())
                {
                    names.Add(row.Name);
                }

                Assert.That(names, Is.EqualTo(new[] { "one", "two", "three" }));
                Assert.That(database.Connection, Is.Null, "streaming left the connection open");
            }
        }

        [Test]
        public void QueryStreamsAnEntityProjection()
        {
            using (var database = CreateDatabase())
            {
                var rows = database.FluentQuery()
                    .From<PipelineUser>(out var user)
                    .OrderBy(user, x => x.UserId)
                    .Select(user)
                    .Query()
                    .ToList();

                Assert.That(rows.Select(x => x.Name), Is.EqualTo(new[] { "one", "two", "three" }));
            }
        }

        [Test]
        public void QueryStopsEarlyWithoutReadingEveryRow()
        {
            using (var database = CreateDatabase())
            {
                var first = database.FluentQuery()
                    .From<PipelineUser>(out var user)
                    .OrderBy(user, x => x.UserId)
                    .Select(() => new { user.Row.Name })
                    .Query()
                    .Take(1)
                    .ToList();

                Assert.That(first.Single().Name, Is.EqualTo("one"));
                // Take disposes the enumerator, which releases the connection.
                Assert.That(database.Connection, Is.Null);
            }
        }

        [Test]
        public async Task QueryAsyncStreamsAProjection()
        {
            using (var database = CreateDatabase())
            {
                var names = new List<string>();
                await foreach (var row in database.FluentQuery()
                    .From<PipelineUser>(out var user)
                    .OrderBy(user, x => x.UserId)
                    .Select(() => new { user.Row.Name })
                    .QueryAsync())
                {
                    names.Add(row.Name);
                }

                Assert.That(names, Is.EqualTo(new[] { "one", "two", "three" }));
                Assert.That(database.Connection, Is.Null);
            }
        }

        [Test]
        public async Task QueryAsyncStreamsInsideATransaction()
        {
            using (var database = CreateDatabase())
            using (var transaction = database.GetTransaction())
            {
                var names = new List<string>();
                await foreach (var row in database.FluentQuery()
                    .From<PipelineUser>(out var user)
                    .Select(() => new { user.Row.Name })
                    .QueryAsync())
                {
                    names.Add(row.Name);
                }

                Assert.That(names.Count, Is.EqualTo(3));
                Assert.That(database.Transaction, Is.Not.Null, "streaming disposed the ambient transaction");
                transaction.Complete();
            }
        }

        [Test]
        public void ProjectionFetchLeavesAnAmbientTransactionUsable()
        {
            using (var database = CreateDatabase())
            using (var transaction = database.GetTransaction())
            {
                var rows = database.FluentQuery()
                    .From<PipelineUser>(out var user)
                    .OrderBy(user, x => x.UserId)
                    .Select(() => new { Id = user.Row.UserId, user.Row.Name })
                    .Fetch();

                Assert.That(rows.Count, Is.EqualTo(3));
                Assert.That(database.Transaction, Is.Not.Null, "the projection fetch disposed the ambient transaction");
                Assert.That(database.Connection, Is.Not.Null, "the projection fetch closed the transaction's connection");

                database.Execute("update pipelineusers set name = 'renamed' where userid = 1");
                transaction.Complete();
            }

            using (var database = CreateDatabase())
                Assert.That(database.ExecuteScalar<string>("select name from pipelineusers where userid = 1"), Is.EqualTo("renamed"));
        }

        [Test]
        public async Task ProjectionFetchAsyncLeavesAnAmbientTransactionUsable()
        {
            using (var database = CreateDatabase())
            using (var transaction = database.GetTransaction())
            {
                var rows = await database.FluentQuery()
                    .From<PipelineUser>(out var user)
                    .Select(() => new { Id = user.Row.UserId })
                    .FetchAsync();

                Assert.That(rows.Count, Is.EqualTo(3));
                Assert.That(database.Transaction, Is.Not.Null, "the projection fetch disposed the ambient transaction");
                Assert.That(database.Connection, Is.Not.Null, "the projection fetch closed the transaction's connection");
                transaction.Complete();
            }
        }

        [Test]
        public void ProjectionFetchRollsBackWithTheAmbientTransaction()
        {
            using (var database = CreateDatabase())
            {
                using (database.GetTransaction())
                {
                    database.Execute("update pipelineusers set name = 'rolled-back' where userid = 1");
                    var rows = database.FluentQuery()
                        .From<PipelineUser>(out var user)
                        .Where(user, x => x.UserId == 1)
                        .Select(() => new { user.Row.Name })
                        .Fetch();

                    // The projection must read through the same transaction, not a fresh connection.
                    Assert.That(rows.Single().Name, Is.EqualTo("rolled-back"));
                    // No Complete: the using block aborts.
                }

                Assert.That(database.ExecuteScalar<string>("select name from pipelineusers where userid = 1"), Is.EqualTo("one"));
            }
        }

        [Test]
        public void ProjectionFetchLeavesAnExplicitlyOpenedConnectionOpen()
        {
            using (var database = CreateDatabase())
            {
                database.OpenSharedConnection();

                database.FluentQuery()
                    .From<PipelineUser>(out var user)
                    .Select(() => new { Id = user.Row.UserId })
                    .Fetch();

                Assert.That(database.Connection, Is.Not.Null, "the projection fetch closed a connection it did not open");
                Assert.That(database.Connection.State, Is.EqualTo(ConnectionState.Open));
            }
        }

        [Test]
        public void ProjectionFetchClosesTheConnectionItOpened()
        {
            using (var database = CreateDatabase())
            {
                database.FluentQuery()
                    .From<PipelineUser>(out var user)
                    .Select(() => new { Id = user.Row.UserId })
                    .Fetch();

                Assert.That(database.Connection, Is.Null);
            }
        }

        [Test]
        public void ProjectionFetchReportsExceptionsToInterceptors()
        {
            using (var database = CreateDatabase())
            {
                var interceptor = new RecordingInterceptor();
                database.Interceptors.Add(interceptor);

                var query = database.FluentQuery()
                    .From<PipelineUser>(out var user)
                    .Select(() => new { Id = user.Row.UserId });

                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = "drop table pipelineusers";
                    command.ExecuteNonQuery();
                }

                Assert.Throws<SqliteException>(() => query.Fetch());
                Assert.That(interceptor.Exceptions, Is.GreaterThan(0), "OnException was never called");
            }
        }

        [Test]
        public void ProjectionFetchFiresCommandInterceptorsAndRecordsLastSql()
        {
            using (var database = CreateDatabase())
            {
                var interceptor = new RecordingInterceptor();
                database.Interceptors.Add(interceptor);

                database.FluentQuery()
                    .From<PipelineUser>(out var user)
                    .Select(() => new { Id = user.Row.UserId })
                    .Fetch();

                Assert.That(interceptor.Executing, Is.EqualTo(1));
                Assert.That(interceptor.Executed, Is.EqualTo(1));
                Assert.That(database.LastSQL, Does.Contain("pipelineusers"));
            }
        }

        // A single-value body skips the projection plan entirely and maps through NPoco's ordinary
        // query path, so the pipeline guarantees above have to hold for that branch too.

        [Test]
        public void ScalarQueryStreamsAndClosesTheConnection()
        {
            using (var database = CreateDatabase())
            {
                var names = new List<string>();
                foreach (var name in database.FluentQuery()
                    .From<PipelineUser>(out var user)
                    .OrderBy(() => user.Row.UserId)
                    .Select(() => user.Row.Name)
                    .Query())
                {
                    names.Add(name);
                }

                Assert.That(names, Is.EqualTo(new[] { "one", "two", "three" }));
                Assert.That(database.Connection, Is.Null, "streaming left the connection open");
            }
        }

        [Test]
        public void ScalarFetchLeavesAnAmbientTransactionUsable()
        {
            using (var database = CreateDatabase())
            using (var transaction = database.GetTransaction())
            {
                database.Execute("update pipelineusers set name = 'in-transaction' where userid = 1");

                var name = database.FluentQuery()
                    .From<PipelineUser>(out var user)
                    .Where(() => user.Row.UserId == 1)
                    .Select(() => user.Row.Name)
                    .Single();

                // The read must go through the same transaction, not a fresh connection.
                Assert.That(name, Is.EqualTo("in-transaction"));
                Assert.That(database.Transaction, Is.Not.Null, "the scalar fetch disposed the ambient transaction");
                Assert.That(database.Connection, Is.Not.Null, "the scalar fetch closed the transaction's connection");
                transaction.Complete();
            }
        }

        [Test]
        public void ScalarFetchClosesTheConnectionItOpened()
        {
            using (var database = CreateDatabase())
            {
                database.FluentQuery()
                    .From<PipelineUser>(out var user)
                    .Select(() => user.Row.Name)
                    .Fetch();

                Assert.That(database.Connection, Is.Null);
            }
        }

        [Test]
        public void ScalarFetchFiresCommandInterceptorsAndRecordsLastSql()
        {
            using (var database = CreateDatabase())
            {
                var interceptor = new RecordingInterceptor();
                database.Interceptors.Add(interceptor);

                database.FluentQuery()
                    .From<PipelineUser>(out var user)
                    .Select(() => user.Row.Name)
                    .Fetch();

                Assert.That(interceptor.Executing, Is.EqualTo(1));
                Assert.That(interceptor.Executed, Is.EqualTo(1));
                Assert.That(database.LastSQL, Does.Contain("pipelineusers"));
            }
        }

        [Test]
        public void ScalarFetchReportsExceptionsToInterceptors()
        {
            using (var database = CreateDatabase())
            {
                var interceptor = new RecordingInterceptor();
                database.Interceptors.Add(interceptor);

                var query = database.FluentQuery()
                    .From<PipelineUser>(out var user)
                    .Select(() => user.Row.Name);

                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = "drop table pipelineusers";
                    command.ExecuteNonQuery();
                }

                Assert.Throws<SqliteException>(() => query.Fetch());
                Assert.That(interceptor.Exceptions, Is.GreaterThan(0), "OnException was never called");
            }
        }

        [Test]
        public void ProjectionCallsOnLoadedForEntitiesAndResultObjects()
        {
            using (var database = CreateDatabase())
            {
                var rows = database.FluentQuery()
                    .From<PipelineUser>(out var user)
                    .Select(() => new PipelineDto { Id = user.Row.UserId, User = user.Row })
                    .Fetch();

                Assert.That(rows.Count, Is.EqualTo(3));
                Assert.That(PipelineUser.LoadedCount, Is.EqualTo(3), "OnLoaded was not called on the projected entity");
                Assert.That(PipelineDto.LoadedCount, Is.EqualTo(3), "OnLoaded was not called on the result object");
            }
        }

        [Test]
        public void ProjectionMapsThroughRegisteredConverters()
        {
            using (var database = CreateDatabase())
            {
                var rows = database.FluentQuery()
                    .From<PipelineUser>(out var user)
                    .OrderBy(user, x => x.UserId)
                    .Select(() => new { Band = (AgeBand)user.Row.Age, user.Row.Name })
                    .Fetch();

                Assert.That(rows.Select(x => x.Band), Is.EqualTo(new[] { AgeBand.Eleven, AgeBand.TwentyTwo, AgeBand.ThirtyThree }));
            }
        }

        [Test]
        public void ProjectionReturnsNullForAnAllNullNestedObject()
        {
            using (var database = CreateDatabase())
            {
                database.Execute("insert into pipelineusers values(4, null, null)");

                var rows = database.FluentQuery()
                    .From<PipelineUser>(out var user)
                    .Where(user, x => x.UserId == 4)
                    .Select(() => new { Id = user.Row.UserId, Nested = new NestedNames { Name = user.Row.Name } })
                    .Fetch();

                Assert.That(rows.Single().Nested, Is.Null);
            }
        }

        [Test]
        public void ProjectionSingleAndFirstStreamThroughThePipeline()
        {
            using (var database = CreateDatabase())
            {
                var first = database.FluentQuery()
                    .From<PipelineUser>(out var user)
                    .OrderBy(user, x => x.UserId)
                    .Select(() => new { user.Row.Name })
                    .First();

                Assert.That(first.Name, Is.EqualTo("one"));
                Assert.That(database.Connection, Is.Null, "First left the connection open");
            }
        }

        [Test]
        public void EntityFetchLeavesAnAmbientTransactionUsable()
        {
            using (var database = CreateDatabase())
            using (var transaction = database.GetTransaction())
            {
                var rows = database.FluentQuery().From<PipelineUser>(out var user).Select(user).Fetch();

                Assert.That(rows.Count, Is.EqualTo(3));
                Assert.That(database.Transaction, Is.Not.Null);
                Assert.That(PipelineUser.LoadedCount, Is.EqualTo(3));
                transaction.Complete();
            }
        }

        [Test]
        public void ProjectionOverACteRunsAsACompleteStatement()
        {
            using (var database = CreateDatabase())
            {
                var rows = database.FluentQuery()
                    .With(q => q.From<PipelineUser>(out var inner)
                                          .Where(inner, x => x.Age > 20)
                                          .Select(inner), out var adults)
                    .From(adults)
                    .OrderBy(adults, x => x.UserId)
                    .Select(() => new { adults.Row.Name })
                    .Fetch();

                Assert.That(rows.Select(x => x.Name), Is.EqualTo(new[] { "two", "three" }));
            }
        }

        [Test]
        public void ProjectionBuildsResultsThroughAConstructor()
        {
            using (var database = CreateDatabase())
            {
                var rows = database.FluentQuery()
                    .From<PipelineUser>(out var user)
                    .OrderBy(user, x => x.UserId)
                    .Select(() => new ConstructedProjection(user.Row.UserId, user.Row.Name))
                    .Fetch();

                Assert.That(rows.Select(x => x.Id), Is.EqualTo(new[] { 1, 2, 3 }));
                Assert.That(rows.Select(x => x.Name), Is.EqualTo(new[] { "one", "two", "three" }));
            }
        }

        [Test]
        public void ProjectionUsesTheTypeDefaultForANullScalar()
        {
            using (var database = CreateDatabase())
            {
                database.Execute("insert into pipelineusers values(4, null, null)");

                var rows = database.FluentQuery()
                    .From<PipelineUser>(out var user)
                    .Where(user, x => x.UserId == 4)
                    .Select(() => new { NotNullable = user.Row.Age.Value, Nullable = user.Row.Age, user.Row.Name })
                    .Fetch();

                var row = rows.Single();
                Assert.That(row.NotNullable, Is.EqualTo(0));
                Assert.That(row.Nullable, Is.Null);
                Assert.That(row.Name, Is.Null);
            }
        }

        public class ConstructedProjection
        {
            public ConstructedProjection(int id, string name)
            {
                Id = id;
                Name = name;
            }

            public int Id { get; }
            public string Name { get; }
        }

        [Test]
        public void OneColumnCanFeedSeveralDestinations()
        {
            using (var database = CreateDatabase())
            {
                var row = database.FluentQuery()
                    .From<PipelineUser>(out var user)
                    .Where(user, x => x.UserId == 1)
                    .Select(() => new DuplicatedColumns
                    {
                        Primary = user.Row.Name,
                        Copy = user.Row.Name,
                        Nested = new NestedNames { Name = user.Row.Name }
                    })
                    .Fetch()
                    .Single();

                // Nothing stops a source column being read more than once; only the destinations
                // have to differ, and the language now enforces that.
                Assert.That(row.Primary, Is.EqualTo("one"));
                Assert.That(row.Copy, Is.EqualTo("one"));
                Assert.That(row.Nested.Name, Is.EqualTo("one"));
            }
        }

        public class DuplicatedColumns
        {
            public string Primary { get; set; }
            public string Copy { get; set; }
            public NestedNames Nested { get; set; }
        }

        public enum AgeBand
        {
            Eleven = 11,
            TwentyTwo = 22,
            ThirtyThree = 33
        }

        public class NestedNames
        {
            public string Name { get; set; }
        }

        [TableName("pipelineusers"), PrimaryKey("userid")]
        public class PipelineUser : IOnLoaded
        {
            public static int LoadedCount;

            [Column("userid")] public int UserId { get; set; }
            [Column("name")] public string Name { get; set; }
            [Column("age")] public int? Age { get; set; }

            public void OnLoaded() => LoadedCount++;
        }

        public class PipelineDto : IOnLoaded
        {
            public static int LoadedCount;

            public int Id { get; set; }
            public PipelineUser User { get; set; }

            public void OnLoaded() => LoadedCount++;
        }

        public class RecordingInterceptor : IExecutingInterceptor, IExceptionInterceptor
        {
            public int Executing;
            public int Executed;
            public int Exceptions;

            public void OnExecutingCommand(IDatabase database, DbCommand cmd) => Executing++;
            public void OnExecutedCommand(IDatabase database, DbCommand cmd) => Executed++;
            public void OnException(IDatabase database, Exception exception) => Exceptions++;
        }
    }
}
