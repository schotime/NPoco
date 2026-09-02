using System;
using System.Collections.Generic;
using NPoco;
using NPoco.FluentSql;

namespace NPoco.Tests.FluentSqlTests
{
    /// <summary>
    /// Every example in docs/StronglyTypedSqlBuilder.md, so that documentation which no longer
    /// compiles fails the build instead of misleading someone. Nothing here runs.
    /// </summary>
    internal class DocumentationSamples
    {
        private readonly IDatabase db = null;
        private readonly IAsyncDatabase asyncDb = null;
        private readonly decimal minimum = 0;
        private readonly int? clientId = null;
        private readonly bool includeInactive = false, recent = false;
        private readonly DateTime cutoff = default(DateTime);
        private readonly int minimumOrders = 0;

        public void Overview()
        {
            var rows = db.FluentQuery()
                .From<User>(out var user)
                .InnerJoin<Order>(out var order, x => x.UserId == user.Row.Id)
                .Where(() => user.Row.IsActive && order.Row.Amount >= minimum)
                .OrderBy(() => user.Row.Name)
                .Select(() => new { user.Row.Name, order.Row.Amount })
                .Fetch();
        }

        public async System.Threading.Tasks.Task AsyncDatabaseEntryPoint()
        {
            var result = asyncDb.FluentQuery().From<User>(out var user).Select(user);
            await result.FetchAsync();
            await result.FirstAsync();
            await result.SingleAsync();
        }

        public void References()
        {
            var query = db.FluentQuery().From<User>(out var user);
            var alias = user.Alias;
            var column = user.GetColumn(x => x.Name);
        }

        public void Predicates()
        {
            var query = db.FluentQuery().From<User>(out var user)
                .InnerJoin<Order>(out var order, x => x.UserId == user.Row.Id);

            query.Where(user, x => x.IsActive)
                .Where(() => user.Row.IsActive)
                .Where(() => order.Row.UserId == user.Row.Id)
                .WhereIf(clientId.HasValue, () => user.Row.ClientId == clientId.Value)
                .OrWhereIf(includeInactive, user, x => !x.IsActive);

            var active = query.CreatePredicate(group => group
                .And(() => user.Row.IsActive)
                .OrGroup(nested => nested
                    .And(() => user.Row.Name == "Ada")
                    .Or(() => user.Row.Name == "Bob")));

            query.Where(active).WhereIf(recent, () => user.Row.Created > cutoff);
        }

        public void Joins()
        {
            db.FluentQuery().From<User>(out var user)
                .InnerJoin<Order>(out var order, x => x.UserId == user.Row.Id)
                .LeftJoin<Address>(out var address, x => x.UserId == user.Row.Id)
                .RightJoin<Region>(out var region, x => x.Id == address.Row.RegionId)
                .FullOuterJoin<Audit>(out var audit, x => x.UserId == user.Row.Id)
                .OuterApply(out var latest, apply => apply
                    .From<Order>(out var recent2)
                    .Where(() => recent2.Row.UserId == user.Row.Id)
                    .OrderByDescending(() => recent2.Row.Created)
                    .Take(1)
                    .Select(recent2));
        }

        public void GroupingOrderingPaging()
        {
            db.FluentQuery().From<User>(out var user)
                .GroupBy(() => user.Row.Name)
                .HavingIf(minimumOrders > 0, () => FSql.Count() > minimumOrders)
                .OrderByDescending(() => FSql.Count())
                .ThenBy(() => user.Row.Name)
                .Distinct()
                .Skip(20).Take(10);
        }

        public void Projections()
        {
            var stage = db.FluentQuery().From<User>(out var user)
                .InnerJoin<Order>(out var order, x => x.UserId == user.Row.Id);

            stage.Select(user);
            stage.Select(() => new
            {
                User = user.Row,
                Order = new { order.Row.Id, order.Row.Amount },
                Label = user.Row.Name + "!"
            });
            stage.Select(() => new UserRecord(user.Row.Id, user.Row.Name));
            stage.Select(() => new UserDto { Id = user.Row.Id, Name = user.Row.Name });
            stage.SelectScalar(user, x => x.Name);
            stage.SelectScalar(() => user.Row.Name);
            stage.SelectScalar(() => user.Row.Name + "/" + order.Row.Id);
            stage.Select(() => user.Row.Name);
            stage.Select(() => FSql.Cast<string>(user.Row.Id, "text"));
            stage.Select(sql => new
            {
                Score = sql.Case(user.Row.IsActive, user.Row.Age + 1, 0),
                UniqueNames = sql.CountDistinct(user.Row.Name)
            });
        }

        public void Raw()
        {
            db.FluentQuery().From<Metric>(out var metric)
                .Select(() => new
                {
                    Readings = FSql.Raw<string>(
                        "json_agg(json_build_object('value', {0}, 'at', {1}) ORDER BY {1})",
                        metric.Row.Value,
                        metric.Row.OccurredAt)
                });
        }

        public void Subqueries()
        {
            var outer = db.FluentQuery().From<User>(out var user);

            var orderCount = outer.Subquery()
                .From<Order>(out var order)
                .Where(() => order.Row.UserId == user.Row.Id)
                .Select(() => FSql.Count());

            var rows = outer
                .Where(() => FSql.Exists(orderCount))
                .Select(() => new { user.Row.Name, Orders = FSql.Scalar(orderCount) })
                .Fetch();

            outer.Select(() => new { Orders = FSql.Scalar<long>(orderCount) });
        }

        public void InlineSubqueries()
        {
            var query = db.FluentQuery();
            var order = query.Table<Order>();
            var address = query.Table<Address>();
            var region = query.Table<Region>();

            var users = query.From<User>(out var user);

            var rows = users
                .Select(() => new
                {
                    user.Row.Name,
                    Orders = FSql.Scalar<int>(query.Subquery().From(order)
                        .Where(() => order.Row.UserId == user.Row.Id)
                        .SelectScalar(() => FSql.Count()))
                })
                .Fetch();

            users.Select(() => FSql.Scalar<int>(query.Subquery().From(address)
                .InnerJoin(region, () => region.Row.Id == address.Row.RegionId)
                .Where(() => address.Row.UserId == user.Row.Id)
                .SelectScalar(() => FSql.Count())));
        }

        public void JoinedSubquery()
        {
            db.FluentQuery()
                .From<User>(out var user)
                .LeftJoin<UserTotal>(out var totals,
                    sub => sub.From<Order>(out var order)
                        .GroupBy(() => order.Row.UserId)
                        .Select(() => new UserTotal { UserId = order.Row.UserId, Total = FSql.Sum(order.Row.Amount) }),
                    t => t.UserId == user.Row.Id)
                .Select(() => new { user.Row.Name, totals.Row.Total });
        }

        public void CteJoinedBack()
        {
            var query = db.FluentQuery();

            var rows = query
                .With(out var totals, sub => sub
                    .From<Order>(out var order)
                    .GroupBy(() => order.Row.UserId)
                    .Select(() => new UserTotal { UserId = order.Row.UserId, Total = FSql.Sum(order.Row.Amount) }))
                .From<User>(out var user)
                .LeftJoin(totals, () => totals.Row.UserId == user.Row.Id)
                .Select(() => new { user.Row.Name, totals.Row.Total })
                .Fetch();
        }

        public void Ctes()
        {
            var query = db.FluentQuery()
                .With(out var active, cte => cte
                    .From<User>(out var candidate)
                    .Where(() => candidate.Row.IsActive)
                    .Select(candidate))
                .From(active)
                .Where(() => active.Row.Age >= 18)
                .Select(active);

            var activeUsers = db.FluentQuery()
                .From<User>(out var user)
                .Where(() => user.Row.IsActive)
                .Select(user);

            var second = db.FluentQuery()
                .With(out var reference, activeUsers)
                .From(reference)
                .Select(reference);
        }

        public void Unions()
        {
            var names = db.FluentQuery()
                .From<User>(out var minor)
                .Where(() => minor.Row.Age < 18)
                .SelectScalar(() => minor.Row.Name)
                .UnionAll(query => query
                    .From<User>(out var senior)
                    .Where(() => senior.Row.Age >= 65)
                    .SelectScalar(() => senior.Row.Name));
        }

        public void Reuse()
        {
            var stage = db.FluentQuery()
                .From<User>(out var user)
                .Where(() => user.Row.IsActive)
                .OrderBy(() => user.Row.Name);

            var names = stage.Select(() => user.Row.Name).Fetch();
            var count = stage.Select(() => FSql.Count()).Single();
            var page = stage.Take(10).Select(user).Fetch();
        }

        public void Diagnostics()
        {
            var result = db.FluentQuery().From<User>(out var user).Select(user);
            Sql sql = result.ToSql();
            string debug = result.ToDebugSql();
            Sql explain = result.Explain();
        }

        [TableName("Users")] public class User
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public bool IsActive { get; set; }
            public int Age { get; set; }
            public int? ClientId { get; set; }
            public DateTime Created { get; set; }
            public int Score { get; set; }
        }
        [TableName("Orders")] public class Order
        {
            public int Id { get; set; }
            public int UserId { get; set; }
            public decimal Amount { get; set; }
            public DateTime Created { get; set; }
        }
        public class Address { public int Id { get; set; } public int UserId { get; set; } public int RegionId { get; set; } }
        public class Region { public int Id { get; set; } }
        public class Audit { public int Id { get; set; } public int UserId { get; set; } }
        public class Metric { public decimal Value { get; set; } public DateTime OccurredAt { get; set; } }
        public class UserDto { public int Id { get; set; } public string Name { get; set; } }
        public class UserTotal { public int UserId { get; set; } public decimal Total { get; set; } }
        public class UserRecord
        {
            public UserRecord(int id, string name) { Id = id; Name = name; }
            public int Id { get; }
            public string Name { get; }
        }
    }
}
