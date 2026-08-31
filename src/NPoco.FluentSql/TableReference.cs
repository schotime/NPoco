using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NPoco.Expressions;

namespace NPoco.FluentSql
{
    /// <summary>
    /// One occurrence of a table in a query - a mapped table, a CTE or a derived table - together
    /// with the alias it was given. The builder creates these: <c>From</c>, a join, <c>With</c> and
    /// <c>OuterApply</c> each hand one back, and expressions reference columns through it, so the
    /// same table joined twice stays unambiguous.
    /// </summary>
    public abstract class TableReference
    {
        /// <summary>Creates a reference to a table.</summary>
        /// <param name="database">The database whose mapping and dialect this reference uses.</param>
        /// <param name="alias">The alias this occurrence is given in the generated SQL.</param>
        /// <param name="entityType">The POCO type the table's columns map onto.</param>
        /// <param name="derived">Whether the source is a derived table or CTE rather than a mapped table.</param>
        /// <param name="sourceName">The name to select from, when it is not the mapped table's name - a CTE name, say.</param>
        /// <exception cref="ArgumentNullException"><paramref name="database"/> or <paramref name="alias"/> is null.</exception>
        protected TableReference(IDatabase database, string alias, Type entityType, bool derived, string sourceName = null)
        {
            Database = database ?? throw new ArgumentNullException(nameof(database));
            Alias = alias ?? throw new ArgumentNullException(nameof(alias));
            EntityType = entityType;
            IsDerived = derived;
            SourceName = sourceName;
            PocoData = database.PocoDataFactory.ForType(entityType);
        }

        private string _escapedAlias;
        private string _escapedTableName;

        internal IDatabase Database { get; }
        /// <summary>The alias this occurrence of the table carries in the generated SQL.</summary>
        public string Alias { get; }
        /// <summary>The POCO type the table's columns map onto.</summary>
        public Type EntityType { get; }
        /// <summary>NPoco's mapping metadata for <see cref="EntityType"/>.</summary>
        public PocoData PocoData { get; }
        internal bool IsDerived { get; }
        internal string SourceName { get; }
        /// <summary>The dialect of the database being targeted, which decides identifier escaping.</summary>
        public IDatabaseType DatabaseType => Database.DatabaseType;
        // Both are read once per column reference and per FROM/JOIN clause, and neither can change.
        internal string EscapedAlias => _escapedAlias ?? (_escapedAlias = DatabaseType.EscapeSqlIdentifier(Alias));
        internal string EscapedTableName => _escapedTableName
            ?? (_escapedTableName = DatabaseType.EscapeTableName(SourceName ?? PocoData.TableInfo.TableName));

        internal abstract string GetColumn(MemberInfo[] members);
        internal abstract PocoColumn ResolveColumn(MemberInfo[] members);
    }

    /// <summary>
    /// A <see cref="TableReference"/> for a known entity type, which is what makes expressions
    /// against it strongly typed.
    /// </summary>
    /// <typeparam name="T">The POCO type mapped to this table.</typeparam>
    public sealed class TableReference<T> : TableReference
    {
        private readonly Dictionary<string, string> _rendered = new Dictionary<string, string>(StringComparer.Ordinal);
        private Dictionary<string, PocoColumn> _columns;

        internal TableReference(IDatabase database, string alias, bool derived = false, string sourceName = null)
            : base(database, alias, typeof(T), derived, sourceName)
        {
        }

        /// <summary>
        /// Stands in for a row of this table inside a builder expression: reading a member of it -
        /// <c>user.Row.Name</c> - references that column. It is never evaluated, so reading it outside
        /// an expression throws.
        /// </summary>
        /// <exception cref="InvalidOperationException">Always, when actually evaluated.</exception>
        public T Row => throw new InvalidOperationException("TableReference.Row can only be used inside a fluent SQL expression.");

        /// <summary>
        /// Renders the escaped <c>alias.column</c> SQL for a mapped member, for use where a fragment of
        /// SQL is written by hand rather than built from an expression.
        /// </summary>
        /// <typeparam name="TProperty">The type of the selected member.</typeparam>
        /// <param name="selector">Selects the member, including through complex-mapped members.</param>
        /// <returns>The column reference, escaped for the target database.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="selector"/> is null.</exception>
        /// <exception cref="InvalidOperationException">The member is not mapped to a column.</exception>
        public string GetColumn<TProperty>(Expression<Func<T, TProperty>> selector)
        {
            if (selector == null) throw new ArgumentNullException(nameof(selector));
            return GetColumn(MemberChainHelper.GetMembers(selector).ToArray());
        }

        internal override string GetColumn(MemberInfo[] members)
        {
            var path = Path(members);
            string sql;
            if (_rendered.TryGetValue(path, out sql)) return sql;

            var column = ResolveColumn(path, members);
            var name = IsDerived
                ? (string.IsNullOrWhiteSpace(column.ColumnAlias) ? column.MemberInfoKey : column.ColumnAlias)
                : column.ColumnName;
            sql = EscapedAlias + "." + DatabaseType.EscapeSqlIdentifier(name);
            _rendered.Add(path, sql);
            return sql;
        }

        internal override PocoColumn ResolveColumn(MemberInfo[] members) => ResolveColumn(Path(members), members);

        private PocoColumn ResolveColumn(string path, MemberInfo[] members)
        {
            if (members.Length == 0)
                throw new ArgumentException("The expression must select a mapped property.", nameof(members));

            if (_columns == null) _columns = BuildColumnMap();

            PocoColumn column;
            if (!_columns.TryGetValue(path, out column))
                throw new InvalidOperationException($"Property '{path}' on '{typeof(T).Name}' is not mapped to a database column.");

            return column;
        }

        // The same column is usually referenced several times in one query - a predicate, a sort,
        // a projection - and every reference used to walk the whole member tree to find it.
        private Dictionary<string, PocoColumn> BuildColumnMap()
        {
            var map = new Dictionary<string, PocoColumn>(StringComparer.Ordinal);
            foreach (var member in PocoData.GetAllMembers())
            {
                if (member.PocoColumn == null || member.MemberInfoChain == null) continue;
                var path = string.Join(".", member.MemberInfoChain.Select(x => x.Name));
                if (!map.ContainsKey(path)) map.Add(path, member.PocoColumn);
            }
            return map;
        }

        // A single member - the common case by far - is its own path, so a lookup allocates nothing.
        private static string Path(MemberInfo[] members)
            => members.Length == 1 ? members[0].Name : string.Join(".", members.Select(x => x.Name));
    }

    internal static class TableAliasGenerator
    {
        /// <summary>The root used when a type has no name worth abbreviating - an anonymous projection.</summary>
        internal const string GeneratedRoot = "__t";

        internal static string Root(Type type)
        {
            var name = type.Name;
            // A compiler-generated name - an anonymous projection, most often - has nothing worth
            // abbreviating in it, and its punctuation makes a poor alias. It takes the same
            // underscore-prefixed form as a generated CTE name, so everything the builder names
            // for itself reads as the builder's, not the caller's.
            if (name.Length == 0 || name[0] == '<') return GeneratedRoot;

            // Letters only: the initial, then each subsequent capital. Anything else a type name
            // can carry - digits, underscores, backticks, generic arity - has no place in an alias.
            var initials = new char[name.Length];
            var length = 0;
            for (var i = 0; i < name.Length; i++)
            {
                var character = name[i];
                if (!char.IsLetter(character)) continue;
                if (length != 0 && !char.IsUpper(character)) continue;
                initials[length++] = char.ToLowerInvariant(character);
            }
            return length == 0 ? "t" : new string(initials, 0, length);
        }
    }
}
