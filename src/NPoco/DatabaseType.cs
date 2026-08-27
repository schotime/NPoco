using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using NPoco.DatabaseTypes;
using NPoco.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using System.Text;
using System.Threading;

namespace NPoco
{
    /// <summary>
    /// Base class for DatabaseType handlers - provides default/common handling for different database engines
    /// </summary>
    public abstract partial class DatabaseType : IDatabaseType
    {
        // Helper Properties
        public static DatabaseType SqlServer2012 { get { return DynamicDatabaseType.MakeSqlServerType("SqlServer2012DatabaseType"); } }
        public static DatabaseType SqlServer2008 { get { return DynamicDatabaseType.MakeSqlServerType("SqlServer2008DatabaseType"); } }
        public static DatabaseType SqlServer2005 { get { return DynamicDatabaseType.MakeSqlServerType("SqlServerDatabaseType"); } }
        public static DatabaseType PostgreSQL { get { return Singleton<PostgreSQLDatabaseType>.Instance; } }
        public static DatabaseType Oracle { get { return Singleton<OracleDatabaseType>.Instance; } }
        public static DatabaseType OracleManaged { get { return Singleton<OracleManagedDatabaseType>.Instance; } }
        public static DatabaseType MySQL { get { return Singleton<MySqlDatabaseType>.Instance; } }
        public static DatabaseType SQLite { get { return Singleton<SQLiteDatabaseType>.Instance; } }
        public static DatabaseType SQLCe { get { return DynamicDatabaseType.MakeSqlServerType("SqlServerCEDatabaseType"); } }
        public static DatabaseType Firebird { get { return Singleton<FirebirdDatabaseType>.Instance; } }

        readonly Dictionary<Type, DbType> typeMap;

        public DatabaseType()
        {
            typeMap = new Dictionary<Type, DbType>();
            typeMap[typeof(byte)] = DbType.Byte;
            typeMap[typeof(sbyte)] = DbType.SByte;
            typeMap[typeof(short)] = DbType.Int16;
            typeMap[typeof(ushort)] = DbType.UInt16;
            typeMap[typeof(int)] = DbType.Int32;
            typeMap[typeof(uint)] = DbType.UInt32;
            typeMap[typeof(long)] = DbType.Int64;
            typeMap[typeof(ulong)] = DbType.UInt64;
            typeMap[typeof(float)] = DbType.Single;
            typeMap[typeof(double)] = DbType.Double;
            typeMap[typeof(decimal)] = DbType.Decimal;
            typeMap[typeof(bool)] = DbType.Boolean;
            typeMap[typeof(string)] = DbType.String;
            typeMap[typeof(char)] = DbType.StringFixedLength;
            typeMap[typeof(Guid)] = DbType.Guid;
            typeMap[typeof(DateTime)] = DbType.DateTime;
            typeMap[typeof(DateTimeOffset)] = DbType.DateTimeOffset;
            typeMap[typeof(TimeSpan)] = DbType.Time;
            typeMap[typeof(byte[])] = DbType.Binary;
            typeMap[typeof(byte?)] = DbType.Byte;
            typeMap[typeof(sbyte?)] = DbType.SByte;
            typeMap[typeof(short?)] = DbType.Int16;
            typeMap[typeof(ushort?)] = DbType.UInt16;
            typeMap[typeof(int?)] = DbType.Int32;
            typeMap[typeof(uint?)] = DbType.UInt32;
            typeMap[typeof(long?)] = DbType.Int64;
            typeMap[typeof(ulong?)] = DbType.UInt64;
            typeMap[typeof(float?)] = DbType.Single;
            typeMap[typeof(double?)] = DbType.Double;
            typeMap[typeof(decimal?)] = DbType.Decimal;
            typeMap[typeof(bool?)] = DbType.Boolean;
            typeMap[typeof(char?)] = DbType.StringFixedLength;
            typeMap[typeof(Guid?)] = DbType.Guid;
            typeMap[typeof(DateTime?)] = DbType.DateTime;
            typeMap[typeof(DateTimeOffset?)] = DbType.DateTimeOffset;
            typeMap[typeof(TimeSpan?)] = DbType.Time;
            typeMap[typeof(Object)] = DbType.Object;
#if NET6_0_OR_GREATER
            typeMap[typeof(DateOnly)] = DbType.Date;
            typeMap[typeof(DateOnly?)] = DbType.Date;
#endif
        }

        /// <summary>
        /// Configire the specified type to be mapped to a given db-type
        /// </summary>
        public void AddTypeMap(Type type, DbType dbType)
        {
            typeMap[type] = dbType;
        }

        internal const string LinqBinary = "System.Data.Linq.Binary";
        public virtual DbType? LookupDbType(Type type, string name)
        {
            DbType dbType;
            var nullUnderlyingType = Nullable.GetUnderlyingType(type);
            if (nullUnderlyingType != null) type = nullUnderlyingType;
            if (type.GetTypeInfo().IsEnum && !typeMap.ContainsKey(type))
            {
                type = Enum.GetUnderlyingType(type);
            }
            if (typeMap.TryGetValue(type, out dbType))
            {
                return dbType;
            }
            if (type.FullName == LinqBinary)
            {
                return DbType.Binary;
            }

            return null;
        }

        /// <summary>
        /// Returns the prefix used to delimit parameters in SQL query strings.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public virtual string GetParameterPrefix(string connectionString)
        {
            return "@";
        }

        /// <summary>
        /// Converts a supplied C# object value into a value suitable for passing to the database
        /// </summary>
        /// <param name="value">The value to convert</param>
        /// <returns>The converted value</returns>
        public virtual object MapParameterValue(object value)
        {
            // Cast bools to integer
            if (value is bool)
            {
                return ((bool)value) ? 1 : 0;
            }

            // Leave it
            return value;
        }

        /// <summary>
        /// Called immediately before a command is executed, allowing for modification of the DbCommand before it's passed to the database provider
        /// </summary>
        /// <param name="cmd"></param>
        public virtual void PreExecute(DbCommand cmd)
        {
        }

        /// <summary>
        /// Sets a DbParameter's Value/DbType/Size for a given CLR value. Override in a DatabaseType subclass
        /// to customize provider-specific parameter handling (e.g. mapping DateTime to a provider-specific
        /// enum based on DateTimeKind) instead of special-casing it here.
        /// </summary>
        public virtual void SetParameterValue(DbParameter p, object value)
        {
            if (value == null)
            {
                p.Value = DBNull.Value;
                return;
            }

            // Give the database type first crack at converting to DB required type
            value = MapParameterValue(value);

            var dbtypeSet = false;
            var t = value.GetType();
            var underlyingT = Nullable.GetUnderlyingType(t);
            if (t.GetTypeInfo().IsEnum || (underlyingT != null && underlyingT.GetTypeInfo().IsEnum))        // PostgreSQL .NET driver wont cast enum to int
            {
                p.Value = (int)value;
            }
            else if (t == typeof(Guid))
            {
                p.Value = value;
                p.DbType = DbType.Guid;
                p.Size = 40;
                dbtypeSet = true;
            }
            else if (t == typeof(string))
            {
                var strValue = value as string;
                if (strValue == null)
                {
                    p.Size = 0;
                    p.Value = DBNull.Value;
                }
                else
                {
                    // out of memory exception occurs if trying to save more than 4000 characters to SQL Server CE NText column. Set before attempting to set Size, or Size will always max out at 4000
                    if (strValue.Length + 1 > 4000 && p.GetType().Name == "SqlCeParameter")
                    {
                        ReflectionCache.GetSetter(p.GetType(), "SqlDbType")(p, SqlDbType.NText);
                    }

                    p.Size = Math.Max(strValue.Length + 1, 4000); // Help query plan caching by using common size
                    p.Value = value;
                }
            }
            else if (t == typeof(AnsiString))
            {
                var ansistrValue = value as AnsiString;
                if (ansistrValue?.Value == null)
                {
                    p.Size = 0;
                    p.Value = DBNull.Value;
                    p.DbType = DbType.AnsiString;
                }
                else
                {
                    // Thanks @DataChomp for pointing out the SQL Server indexing performance hit of using wrong string type on varchar
                    p.Size = Math.Max(ansistrValue.Value.Length + 1, 4000);
                    p.Value = ansistrValue.Value;
                    p.DbType = DbType.AnsiString;
                }
                dbtypeSet = true;
            }
            else if (value.GetType().Name == "SqlGeography") //SqlGeography is a CLR Type
            {
                ReflectionCache.GetSetter(p.GetType(), "UdtTypeName")(p, "geography"); //geography is the equivalent SQL Server Type
                p.Value = value;
            }
            else if (value.GetType().Name == "SqlGeometry") //SqlGeometry is a CLR Type
            {
                ReflectionCache.GetSetter(p.GetType(), "UdtTypeName")(p, "geometry"); //geography is the equivalent SQL Server Type
                p.Value = value;
            }
            else
            {
                p.Value = value;
            }

            if (!dbtypeSet)
            {
                var dbTypeLookup = LookupDbType(p.Value.GetTheType(), p.ParameterName);
                if (dbTypeLookup.HasValue)
                {
                    p.DbType = dbTypeLookup.Value;
                }
            }
        }

        /// <summary>
        /// Builds an SQL query suitable for performing page based queries to the database
        /// </summary>
        /// <param name="skip">The number of rows that should be skipped by the query</param>
        /// <param name="take">The number of rows that should be retruend by the query</param>
        /// <param name="parts">The original SQL query after being parsed into it's component parts</param>
        /// <param name="args">Arguments to any embedded parameters in the SQL query</param>
        /// <returns>The final SQL query that should be executed.</returns>
        public virtual string BuildPageQuery(long skip, long take, SQLParts parts, ref object[] args)
        {
            var sql = string.Format("{0}\nLIMIT @{1} OFFSET @{2}", parts.sql, args.Length, args.Length + 1);
            args = args.Concat(new object[] { take, skip }).ToArray();
            return sql;
        }

        public virtual bool UseColumnAliases()
        {
            return false;
        }

        /// <summary>
        /// Returns an SQL Statement that can check for the existance of a row in the database.
        /// </summary>
        /// <returns></returns>
        public virtual string GetExistsSql()
        {
            return "SELECT COUNT(*) FROM {0} WHERE {1}";
        }

        /// <summary>
        /// Escape a tablename into a suitable format for the associated database provider.
        /// </summary>
        /// <param name="tableName">The name of the table (as specified by the client program, or as attributes on the associated POCO class.</param>
        /// <returns>The escaped table name</returns>
        public virtual string EscapeTableName(string tableName)
        {
            // Assume table names with "dot" are already escaped
            return tableName.IndexOf('.') >= 0 ? tableName : EscapeSqlIdentifier(tableName);
        }

        /// <summary>
        /// Escape and arbitary SQL identifier into a format suitable for the associated database provider
        /// </summary>
        /// <param name="str">The SQL identifier to be escaped</param>
        /// <returns>The escaped identifier</returns>
        public virtual string EscapeSqlIdentifier(string str)
        {
            return string.Format("[{0}]", str);
        }

        /// <summary>
        /// Return an SQL expression that can be used to populate the primary key column of an auto-increment column.
        /// </summary>
        /// <param name="ti">Table info describing the table</param>
        /// <returns>An SQL expressions</returns>
        /// <remarks>See the Oracle database type for an example of how this method is used.</remarks>
        public virtual string GetAutoIncrementExpression(TableInfo ti)
        {
            return null;
        }

        /// <summary>
        /// Returns an SQL expression that can be used to specify the return value of auto incremented columns.
        /// </summary>
        /// <param name="primaryKeyName">The primary key of the row being inserted.</param>
        /// <returns>An expression describing how to return the new primary key value</returns>
        /// <remarks>See the SQLServer database provider for an example of how this method is used.</remarks>
        public virtual string GetInsertOutputClause(string primaryKeyName, bool useOutputClause)
        {
            return string.Empty;
        }

        /// <summary>
        /// Performs an Insert operation
        /// </summary>
        /// <param name="db">The calling Database object</param>
        /// <param name="cmd">The insert command to be executed</param>
        /// <param name="primaryKeyName">The primary key of the table being inserted into</param>
        /// <param name="useOutputClause"></param>
        /// <param name="poco"></param>
        /// <param name="args"></param>
        /// <returns>The ID of the newly inserted record</returns>
        public virtual object ExecuteInsert<T>(IDatabase db, DbCommand cmd, string primaryKeyName, bool useOutputClause, T poco, object[] args)
        {
            cmd.CommandText += ";\nSELECT @@IDENTITY AS NewID;";
            return ((IDatabaseHelpers)db).ExecuteScalarHelper(cmd);
        }

        public virtual async Task<object> ExecuteInsertAsync<T>(IDatabase db, DbCommand cmd, string primaryKeyName, bool useOutputClause, T poco, object[] args, CancellationToken cancellationToken = default)
        {
            cmd.CommandText += ";\nSELECT @@IDENTITY AS NewID;";
            return await ((IDatabaseHelpers)db).ExecuteScalarHelperAsync(cmd, cancellationToken).ConfigureAwait(false);
        }

        public virtual void InsertBulk<T>(IDatabase db, IEnumerable<T> pocos, InsertBulkOptions options)
        {
            foreach (var poco in pocos)
            {
                db.Insert(poco);
            }
        }

        public virtual async Task InsertBulkAsync<T>(IDatabase db, IEnumerable<T> pocos, InsertBulkOptions options, CancellationToken cancellationToken = default)
        {
            foreach (var poco in pocos)
            {
                await db.InsertAsync(poco, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Look at the type and provider name being used and instantiate a suitable DatabaseType instance.
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="providerName"></param>
        /// <returns></returns>
        public static DatabaseType Resolve(string typeName, string providerName)
        {
            // Try using type name first (more reliable)
            if (typeName.StartsWith("MySql"))
                return Singleton<MySqlDatabaseType>.Instance;
            if (typeName.StartsWith("SqlCe"))
                return DynamicDatabaseType.MakeSqlServerType("SqlServerCEDatabaseType");
            if (typeName.StartsWith("Npgsql") || typeName.StartsWith("PgSql"))
                return Singleton<PostgreSQLDatabaseType>.Instance;
            if (typeName.StartsWith("OracleManaged"))
                return Singleton<OracleDatabaseType>.Instance;
            if (typeName.StartsWith("Oracle"))
                return Singleton<OracleDatabaseType>.Instance;
            if (typeName.StartsWith("SQLite", StringComparison.OrdinalIgnoreCase))
                return Singleton<SQLiteDatabaseType>.Instance;
            if (typeName.StartsWith("SqlConnection"))
                return DynamicDatabaseType.MakeSqlServerType("SqlServerDatabaseType");
            if (typeName.StartsWith("Fb") || typeName.StartsWith("Firebird"))
                return Singleton<FirebirdDatabaseType>.Instance;

            if (!string.IsNullOrEmpty(providerName))
            {
                // Try again with provider name
                if (providerName.IndexOf("MySql", StringComparison.OrdinalIgnoreCase) >= 0)
                    return Singleton<MySqlDatabaseType>.Instance;
                if (providerName.IndexOf("SqlServerCe", StringComparison.OrdinalIgnoreCase) >= 0)
                    return DynamicDatabaseType.MakeSqlServerType("SqlServerCEDatabaseType");
                if (providerName.IndexOf("pgsql", StringComparison.OrdinalIgnoreCase) >= 0)
                    return Singleton<PostgreSQLDatabaseType>.Instance;
                if (providerName.IndexOf("Oracle.DataAccess", StringComparison.OrdinalIgnoreCase) >= 0)
                    return Singleton<OracleDatabaseType>.Instance;
                if (providerName.IndexOf("Oracle.ManagedDataAccess", StringComparison.OrdinalIgnoreCase) >= 0)
                    return Singleton<OracleManagedDatabaseType>.Instance;
                if (providerName.IndexOf("SQLite", StringComparison.OrdinalIgnoreCase) >= 0)
                    return Singleton<SQLiteDatabaseType>.Instance;
                if (providerName.IndexOf("Firebird", StringComparison.OrdinalIgnoreCase) >= 0)
                    return Singleton<FirebirdDatabaseType>.Instance;
            }

            // Assume SQL Server
            return DynamicDatabaseType.MakeSqlServerType("SqlServerDatabaseType");
        }

        public virtual string GetDefaultInsertSql(string tableName, string primaryKeyName, bool useOutputClause, string[] names, string[] parameters)
        {
            return string.Format("INSERT INTO {0} DEFAULT VALUES", EscapeTableName(tableName));
        }

        public virtual IsolationLevel GetDefaultTransactionIsolationLevel()
        {
            return IsolationLevel.ReadCommitted;
        }

        public virtual string GetSQLForTransactionLevel(IsolationLevel isolationLevel)
        {
            switch (isolationLevel)
            {
                case IsolationLevel.ReadCommitted:
                    return "SET TRANSACTION ISOLATION LEVEL READ COMMITTED;";

                case IsolationLevel.ReadUncommitted:
                    return "SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;";

                case IsolationLevel.RepeatableRead:
                    return "SET TRANSACTION ISOLATION LEVEL REPEATABLE READ;";

                case IsolationLevel.Serializable:
                    return "SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;";

                case IsolationLevel.Snapshot:
                    return "SET TRANSACTION ISOLATION LEVEL SNAPSHOT;";

                default:
                    return "SET TRANSACTION ISOLATION LEVEL READ COMMITTED;";
            }
        }

        public ISqlExpression<T> ExpressionVisitor<T>(IDatabase db, PocoData pocoData)
        {
            return ExpressionVisitor<T>(db, pocoData, false);
        }

        public virtual ISqlExpression<T> ExpressionVisitor<T>(IDatabase db, PocoData pocoData, bool prefixTableName)
        {
            return new DefaultSqlExpression<T>(db, pocoData, prefixTableName);
        }

        public virtual string GetProviderName()
        {
            return "Microsoft.Data.SqlClient";
        }

        public virtual Task<int> ExecuteNonQueryAsync(IDatabase database, DbCommand cmd, CancellationToken cancellationToken = default)
        {
            return cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        public virtual Task<object> ExecuteScalarAsync(IDatabase database, DbCommand cmd, CancellationToken cancellationToken = default)
        {
            return cmd.ExecuteScalarAsync(cancellationToken);
        }

        public virtual Task<DbDataReader> ExecuteReaderAsync(IDatabase database, DbCommand cmd, CancellationToken cancellationToken = default)
        {
            return cmd.ExecuteReaderAsync(cancellationToken);
        }

        public virtual object ProcessDefaultMappings(PocoColumn pocoColumn, object value)
        {
            return value;
        }

        public class FormattedParameter
        {
            public Type Type { get; set; }
            public object Value { get; set; }
            public DbParameter Parameter { get; set; }
        }

        public virtual string FormatCommand(DbCommand cmd)
        {
            return FormatCommand(cmd.CommandText, cmd.Parameters.Cast<object>().ToArray());
        }

        public virtual string FormatCommand(string sql, object[] args)
        {
            if (sql == null)
                return "";

            var sb = new StringBuilder();
            sb.Append(sql);
            if (args != null && args.Length > 0)
            {
                sb.Append("\n");
                for (int i = 0; i < args.Length; i++)
                {
                    string type; 
                    string value;

                    if (args[i] is DbParameter dbParameter)
                    {
                        type = $"{dbParameter.GetType().Name}, {dbParameter.DbType.ToString()}";
                        value = dbParameter.Value?.ToString();
                    }
                    else
                    {
                        type = args[i].GetTheType()?.Name;
                        value = args[i]?.ToString();
                    }

                    sb.AppendFormat("\t -> {0}{1} [{2}] = \"{3}\"\n", GetParameterPrefix(string.Empty), i, type, value);
                }
                sb.Remove(sb.Length - 1, 1);
            }
            return sb.ToString();
        }

    }
}
