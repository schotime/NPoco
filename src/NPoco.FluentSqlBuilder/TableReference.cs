using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NPoco.Expressions;

namespace NPoco.FluentSqlBuilder
{
    public abstract class TableReference
    {
        protected TableReference(IDatabase database, string alias, Type entityType, bool derived, string sourceName = null)
        {
            Database = database ?? throw new ArgumentNullException(nameof(database));
            Alias = alias ?? throw new ArgumentNullException(nameof(alias));
            EntityType = entityType;
            IsDerived = derived;
            SourceName = sourceName;
            PocoData = database.PocoDataFactory.ForType(entityType);
        }

        internal IDatabase Database { get; }
        public string Alias { get; }
        public Type EntityType { get; }
        public PocoData PocoData { get; }
        internal bool IsDerived { get; }
        internal string SourceName { get; }
        public IDatabaseType DatabaseType => Database.DatabaseType;
        internal string EscapedAlias => DatabaseType.EscapeSqlIdentifier(Alias);
        internal string EscapedTableName => DatabaseType.EscapeTableName(SourceName ?? PocoData.TableInfo.TableName);

        internal abstract string GetColumn(MemberInfo[] members);
        internal abstract PocoColumn ResolveColumn(MemberInfo[] members);
    }

    public sealed class TableReference<T> : TableReference
    {
        internal TableReference(IDatabase database, string alias, bool derived = false, string sourceName = null)
            : base(database, alias, typeof(T), derived, sourceName)
        {
        }

        public T Row => throw new InvalidOperationException("TableReference.Row can only be used inside a fluent SQL expression.");

        public string GetColumn<TProperty>(Expression<Func<T, TProperty>> selector)
        {
            if (selector == null) throw new ArgumentNullException(nameof(selector));
            return GetColumn(MemberChainHelper.GetMembers(selector).ToArray());
        }

        internal override string GetColumn(MemberInfo[] members)
        {
            var column = ResolveColumn(members);
            var name = IsDerived
                ? (string.IsNullOrWhiteSpace(column.ColumnAlias) ? column.MemberInfoKey : column.ColumnAlias)
                : column.ColumnName;
            return EscapedAlias + "." + DatabaseType.EscapeSqlIdentifier(name);
        }

        internal override PocoColumn ResolveColumn(MemberInfo[] members)
        {
            if (members.Length == 0)
                throw new ArgumentException("The expression must select a mapped property.", nameof(members));

            var member = PocoData.GetAllMembers().FirstOrDefault(x =>
                x.PocoColumn != null && x.MemberInfoChain != null &&
                x.MemberInfoChain.Select(y => y.Name).SequenceEqual(members.Select(y => y.Name)));

            if (member?.PocoColumn == null)
                throw new InvalidOperationException($"Property '{string.Join(".", members.Select(x => x.Name))}' on '{typeof(T).Name}' is not mapped to a database column.");

            return member.PocoColumn;
        }
    }

    internal static class TableAliasGenerator
    {
        internal static string Root(Type type)
        {
            var name = type.Name;
            var root = new string(name.Where((c, i) => i == 0 || char.IsUpper(c)).Select(char.ToLowerInvariant).ToArray());
            return root.Length == 0 ? "t" : root;
        }
    }
}
