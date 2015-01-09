﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using NPoco.DatabaseTypes;
using NPoco.Expressions;
using NPoco.Linq;

namespace NPoco
{
    /// <summary>
    /// Base class for DatabaseType handlers - provides default/common handling for different database engines
    /// </summary>
    public abstract class DatabaseType
    {
        // Helper Properties
        public static DatabaseType SqlServer2012 { get { return Singleton<SqlServer2012DatabaseType>.Instance; } }
        public static DatabaseType SqlServer2008 { get { return Singleton<SqlServer2008DatabaseType>.Instance; } }
        public static DatabaseType SqlServer2005 { get { return Singleton<SqlServerDatabaseType>.Instance; } }
        public static DatabaseType PostgreSQL { get { return Singleton<PostgreSQLDatabaseType>.Instance; } }
        public static DatabaseType Oracle { get { return Singleton<OracleDatabaseType>.Instance; } }
        public static DatabaseType OracleManaged { get { return Singleton<OracleManagedDatabaseType>.Instance; } }
        public static DatabaseType MySQL { get { return Singleton<MySqlDatabaseType>.Instance; } }
        public static DatabaseType SQLite { get { return Singleton<SQLiteDatabaseType>.Instance; } }
        public static DatabaseType SQLCe { get { return Singleton<SqlServerCEDatabaseType>.Instance; } }
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
        }

        /// <summary>
        /// Configire the specified type to be mapped to a given db-type
        /// </summary>
        protected void AddTypeMap(Type type, DbType dbType)
        {
            typeMap[type] = dbType;
        }

        internal const string LinqBinary = "System.Data.Linq.Binary";
        public virtual DbType? LookupDbType(Type type, string name)
        {
            DbType dbType;
            var nullUnderlyingType = Nullable.GetUnderlyingType(type);
            if (nullUnderlyingType != null) type = nullUnderlyingType;
            if (type.IsEnum && !typeMap.ContainsKey(type))
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
        /// Called immediately before a command is executed, allowing for modification of the IDbCommand before it's passed to the database provider
        /// </summary>
        /// <param name="cmd"></param>
        public virtual void PreExecute(IDbCommand cmd)
        {
        }

        /// <summary>
        /// Builds an SQL query suitable for performing page based queries to the database
        /// </summary>
        /// <param name="skip">The number of rows that should be skipped by the query</param>
        /// <param name="take">The number of rows that should be retruend by the query</param>
        /// <param name="parts">The original SQL query after being parsed into it's component parts</param>
        /// <param name="args">Arguments to any embedded parameters in the SQL query</param>
        /// <returns>The final SQL query that should be executed.</returns>
        public virtual string BuildPageQuery(long skip, long take, PagingHelper.SQLParts parts, ref object[] args)
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

        public virtual string GetDefaultInsertSql(string tableName, IEnumerable<string> outputColumns, bool selectLastId, string idColumnName)
        {
            var outputClause = GetInsertOutputClause(outputColumns);
            string selectIdSql = string.Empty;
            if (selectLastId)
            {
                selectIdSql = GetSelectIdSql();
            }
            return string.Format("INSERT INTO {0} {1} DEFAULT VALUES {2}", EscapeTableName(tableName), outputClause, selectIdSql);
        }

        public virtual string GetInsertSql(string tableName, IEnumerable<string> columnNames, IEnumerable<string> outputColumns, IEnumerable<string> values, bool selectLastId, string idColumnName)
        {
            var outputClause = GetInsertOutputClause(outputColumns);
            string selectIdSql = string.Empty;
            if (selectLastId)
            {
                selectIdSql = GetSelectIdSql();
            }
            var sql = string.Format("INSERT INTO {0} ({1}){2} VALUES ({3}){4}",
                                   EscapeTableName(tableName),
                                   string.Join(",", columnNames.ToArray()),
                                   outputClause,
                                   string.Join(",", values.ToArray()),
                                   selectIdSql
                                   );

            return sql;
        }

        /// <summary>
        /// Performs an Insert operation
        /// </summary>
        /// <param name="db">The calling Database object</param>
        /// <param name="cmd">The insert command to be executed</param>
        /// <param name="primaryKeyName">The primary key of the table being inserted into</param>
        /// <param name="poco"></param>
        /// <param name="args"></param>
        /// <returns>The ID of the newly inserted record</returns>
        public virtual object ExecuteInsert<T>(Database db, IDbCommand cmd, string primaryKeyName, IEnumerable<string> outputColumns, T poco1, object[] args)
        {         
            return db.ExecuteScalarHelper(cmd);
        }

        public virtual void InsertBulk<T>(IDatabase db, IEnumerable<T> pocos)
        {
            foreach (var poco in pocos)
            {
                db.Insert(poco);
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
                return Singleton<SqlServerCEDatabaseType>.Instance;
            if (typeName.StartsWith("Npgsql") || typeName.StartsWith("PgSql"))
                return Singleton<PostgreSQLDatabaseType>.Instance;
            if (typeName.StartsWith("OracleManaged"))
                return Singleton<OracleDatabaseType>.Instance;
            if (typeName.StartsWith("Oracle"))
                return Singleton<OracleDatabaseType>.Instance;
            if (typeName.StartsWith("SQLite"))
                return Singleton<SQLiteDatabaseType>.Instance;
            if (typeName.StartsWith("SqlConnection"))
                return Singleton<SqlServerDatabaseType>.Instance;
            if (typeName.StartsWith("Fb") || typeName.StartsWith("Firebird"))
                return Singleton<FirebirdDatabaseType>.Instance;

            if (!string.IsNullOrEmpty(providerName))
            {
                // Try again with provider name
                if (providerName.IndexOf("MySql", StringComparison.InvariantCultureIgnoreCase) >= 0)
                    return Singleton<MySqlDatabaseType>.Instance;
                if (providerName.IndexOf("SqlServerCe", StringComparison.InvariantCultureIgnoreCase) >= 0)
                    return Singleton<SqlServerCEDatabaseType>.Instance;
                if (providerName.IndexOf("pgsql", StringComparison.InvariantCultureIgnoreCase) >= 0)
                    return Singleton<PostgreSQLDatabaseType>.Instance;
                if (providerName.IndexOf("Oracle.DataAccess", StringComparison.InvariantCultureIgnoreCase) >= 0)
                    return Singleton<OracleDatabaseType>.Instance;
                if (providerName.IndexOf("Oracle.ManagedDataAccess", StringComparison.InvariantCultureIgnoreCase) >= 0)
                    return Singleton<OracleManagedDatabaseType>.Instance;
                if (providerName.IndexOf("SQLite", StringComparison.InvariantCultureIgnoreCase) >= 0)
                    return Singleton<SQLiteDatabaseType>.Instance;
                if (providerName.IndexOf("Firebird", StringComparison.InvariantCultureIgnoreCase) >= 0)
                    return Singleton<FirebirdDatabaseType>.Instance;
            }

            // Assume SQL Server
            return Singleton<SqlServerDatabaseType>.Instance;
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

        public SqlExpression<T> ExpressionVisitor<T>(IDatabase db)
        {
            return ExpressionVisitor<T>(db, false);
        }

        public virtual SqlExpression<T> ExpressionVisitor<T>(IDatabase db, bool prefixTableName)
        {
            return new DefaultSqlExpression<T>(db, prefixTableName);
        }

        public virtual string GetProviderName()
        {
            return "System.Data.SqlClient";
        }

        #region private methods

        private string GetSelectIdSql()
        {
            return ";\nSELECT @@IDENTITY AS NewID;";
        }

        private string GetInsertOutputClause(IEnumerable<string> outputColumnNames)
        {
            if (outputColumnNames != null && outputColumnNames.Any())
            {
                throw new NotSupportedException("OUTPUT columns are not supported by this provider.");
            }
            return string.Empty;
        }

        private string GetUpdateOutputClause(IEnumerable<string> outputColumnNames)
        {
            if (outputColumnNames != null && outputColumnNames.Any())
            {
                throw new NotSupportedException("OUTPUT columns are not supported by this provider.");
            }
            return string.Empty;
        }

        #endregion


    }
}
