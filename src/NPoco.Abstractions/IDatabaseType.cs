using NPoco.Expressions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace NPoco
{
    public interface IDatabaseType
    {
        void AddTypeMap(Type type, DbType dbType);
        string BuildPageQuery(long skip, long take, SQLParts parts, ref object[] args);
        string EscapeSqlIdentifier(string str);
        string EscapeTableName(string tableName);
        object ExecuteInsert<T>(IDatabase db, DbCommand cmd, string primaryKeyName, bool useOutputClause, T poco, object[] args);
        Task<object> ExecuteInsertAsync<T>(IDatabase db, DbCommand cmd, string primaryKeyName, bool useOutputClause, T poco, object[] args, CancellationToken cancellationToken = default);
        Task<int> ExecuteNonQueryAsync(IDatabase database, DbCommand cmd, CancellationToken cancellationToken = default);
        Task<DbDataReader> ExecuteReaderAsync(IDatabase database, DbCommand cmd, CancellationToken cancellationToken = default);
        Task<object> ExecuteScalarAsync(IDatabase database, DbCommand cmd, CancellationToken cancellationToken = default);
        ISqlExpression<T> ExpressionVisitor<T>(IDatabase db, PocoData pocoData);
        ISqlExpression<T> ExpressionVisitor<T>(IDatabase db, PocoData pocoData, bool prefixTableName);
        string FormatCommand(DbCommand cmd);
        string FormatCommand(string sql, object[] args);
        string GetAutoIncrementExpression(TableInfo ti);
        string GetDefaultInsertSql(string tableName, string primaryKeyName, bool useOutputClause, string[] names, string[] parameters);
        IsolationLevel GetDefaultTransactionIsolationLevel();
        string GetExistsSql();
        string GetInsertOutputClause(string primaryKeyName, bool useOutputClause);
        string GetParameterPrefix(string connectionString);
        string GetProviderName();
        string GetSQLForTransactionLevel(IsolationLevel isolationLevel);
        void InsertBulk<T>(IDatabase db, IEnumerable<T> pocos, InsertBulkOptions options);
        Task InsertBulkAsync<T>(IDatabase db, IEnumerable<T> pocos, InsertBulkOptions options, CancellationToken cancellationToken = default);
        DbType? LookupDbType(Type type, string name);
        object MapParameterValue(object value);
        void PreExecute(DbCommand cmd);
        object ProcessDefaultMappings(PocoColumn pocoColumn, object value);
        bool UseColumnAliases();

        bool EscapeTableColumAliasNames { get; set; }
    }
}