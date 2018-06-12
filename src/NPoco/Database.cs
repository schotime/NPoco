/* NPoco 3.0 - A Tiny ORMish thing for your POCO's.
 * Copyright 2011-2015. All Rights Reserved.
 *
 * Apache License 2.0 - http://www.apache.org/licenses/LICENSE-2.0
 *
 * Originally created by Brad Robinson (@toptensoftware)
 *
 * Special thanks to Rob Conery (@robconery) for original inspiration (ie:Massive) and for
 * use of Subsonic's T4 templates, Rob Sullivan (@DataChomp) for hard core DBA advice
 * and Adam Schroder (@schotime) for lots of suggestions, improvements and Oracle support
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using NPoco.Expressions;
using NPoco.Linq;
#if !DNXCORE50
using System.Configuration;
#endif

namespace NPoco
{
    public partial class Database : IDatabase, IDatabaseHelpers
    {
        public const bool DefaultEnableAutoSelect = true;

        public Database(DbConnection connection)
            : this(connection, null, null, DefaultEnableAutoSelect)
        { }

        public Database(DbConnection connection, DatabaseType dbType)
            : this(connection, dbType, null, DefaultEnableAutoSelect)
        { }
        
        public Database(DbConnection connection, DatabaseType dbType, IsolationLevel? isolationLevel)
            : this(connection, dbType, isolationLevel, DefaultEnableAutoSelect)
        { }

        public Database(DbConnection connection, DatabaseType dbType, IsolationLevel? isolationLevel, bool enableAutoSelect)
        {
            EnableAutoSelect = enableAutoSelect;
            KeepConnectionAlive = true;

            _connectionPassedIn = true;
            _sharedConnection = connection;
            _connectionString = connection.ConnectionString;
            _dbType = dbType ?? DatabaseType.Resolve(_sharedConnection.GetType().Name, null);
            _providerName = _dbType.GetProviderName();
            _isolationLevel = isolationLevel.HasValue ? isolationLevel.Value : _dbType.GetDefaultTransactionIsolationLevel();
            _paramPrefix = _dbType.GetParameterPrefix(_connectionString);

            // Cause it is an external connection ensure that the isolation level matches ours
            //using (var cmd = _sharedConnection.CreateCommand())
            //{
            //    cmd.CommandTimeout = CommandTimeout;
            //    cmd.CommandText = _dbType.GetSQLForTransactionLevel(_isolationLevel);
            //    cmd.ExecuteNonQuery();
            //}
        }

#if !DNXCORE50
        public Database(string connectionString, string providerName)
            : this(connectionString, providerName, DefaultEnableAutoSelect)
        { }

        public Database(string connectionString, string providerName, IsolationLevel isolationLevel)
            : this(connectionString, providerName, isolationLevel, DefaultEnableAutoSelect)
        { }

        public Database(string connectionString, string providerName, bool enableAutoSelect)
            : this(connectionString, providerName, null, enableAutoSelect)
        { }

        public Database(string connectionString, string providerName, IsolationLevel? isolationLevel, bool enableAutoSelect)
        {
            EnableAutoSelect = enableAutoSelect;
            KeepConnectionAlive = false;

            _connectionString = connectionString;
            _factory = DbProviderFactories.GetFactory(providerName);
            var dbTypeName = (_factory == null ? _sharedConnection.GetType() : _factory.GetType()).Name;
            _dbType = DatabaseType.Resolve(dbTypeName, providerName);
            _providerName = providerName;
            _isolationLevel = isolationLevel.HasValue ? isolationLevel.Value : _dbType.GetDefaultTransactionIsolationLevel();
            _paramPrefix = _dbType.GetParameterPrefix(_connectionString);
        }

        public Database(string connectionString, DatabaseType dbType)
            : this(connectionString, dbType, null, DefaultEnableAutoSelect)
        { }

        public Database(string connectionString, DatabaseType dbType, IsolationLevel? isolationLevel)
            : this(connectionString, dbType, isolationLevel,  DefaultEnableAutoSelect)
        { }

        public Database(string connectionString, DatabaseType dbType, IsolationLevel? isolationLevel, bool enableAutoSelect)
        {
            EnableAutoSelect = enableAutoSelect;
            KeepConnectionAlive = false;

            _connectionString = connectionString;
            _dbType = dbType;
            _providerName = _dbType.GetProviderName();
            _factory = DbProviderFactories.GetFactory(_dbType.GetProviderName());
            _isolationLevel = isolationLevel.HasValue ? isolationLevel.Value : _dbType.GetDefaultTransactionIsolationLevel();
            _paramPrefix = _dbType.GetParameterPrefix(_connectionString);
        }
#endif

        public Database(string connectionString, DatabaseType databaseType, DbProviderFactory provider)
            : this(connectionString, databaseType, provider, null, DefaultEnableAutoSelect)
        { }

        public Database(string connectionString, DatabaseType databaseType, DbProviderFactory provider, IsolationLevel? isolationLevel = null, bool enableAutoSelect = DefaultEnableAutoSelect)
        {
            EnableAutoSelect = enableAutoSelect;
            KeepConnectionAlive = false;

            _connectionString = connectionString;
            _factory = provider;
            _dbType = databaseType ?? DatabaseType.Resolve(_factory.GetType().Name, null);
            _providerName = _dbType.GetProviderName();
            _isolationLevel = isolationLevel.HasValue ? isolationLevel.Value : _dbType.GetDefaultTransactionIsolationLevel();
            _paramPrefix = _dbType.GetParameterPrefix(_connectionString);
        }

#if !DNXCORE50
        public Database(string connectionStringName)
            : this(connectionStringName, DefaultEnableAutoSelect)
        { }

        public Database(string connectionStringName, IsolationLevel isolationLevel)
            : this(connectionStringName, isolationLevel, DefaultEnableAutoSelect)
        { }

        public Database(string connectionStringName, bool enableAutoSelect)
            : this(connectionStringName, (IsolationLevel?) null, enableAutoSelect)
        { }

        public Database(string connectionStringName, IsolationLevel? isolationLevel,  bool enableAutoSelect)
        {
            EnableAutoSelect = enableAutoSelect;
            KeepConnectionAlive = false;

            // Use first?
            if (connectionStringName == "") connectionStringName = ConfigurationManager.ConnectionStrings[0].Name;

            // Work out connection string and provider name
            var providerName = "System.Data.SqlClient";
            if (ConfigurationManager.ConnectionStrings[connectionStringName] != null)
            {
                if (!string.IsNullOrEmpty(ConfigurationManager.ConnectionStrings[connectionStringName].ProviderName))
                {
                    providerName = ConfigurationManager.ConnectionStrings[connectionStringName].ProviderName;
                }
            }
            else
            {
                throw new InvalidOperationException("Can't find a connection string with the name '" + connectionStringName + "'");
            }

            // Store factory and connection string
            _connectionString = ConfigurationManager.ConnectionStrings[connectionStringName].ConnectionString;
            _providerName = providerName;

            _factory = DbProviderFactories.GetFactory(_providerName);
            _dbType = DatabaseType.Resolve(_factory.GetType().Name, _providerName);
            _isolationLevel = isolationLevel.HasValue ? isolationLevel.Value : _dbType.GetDefaultTransactionIsolationLevel();
            _paramPrefix = _dbType.GetParameterPrefix(_connectionString);
        }
#endif

        private readonly DatabaseType _dbType;
        public DatabaseType DatabaseType { get { return _dbType; } }
        public IsolationLevel IsolationLevel { get { return _isolationLevel; } }

        private IDictionary<string, object> _data;
        public IDictionary<string, object> Data => _data ?? (_data = new Dictionary<string, object>());

        // Automatically close connection
        public void Dispose()
        {
            if (KeepConnectionAlive) return;
            CloseSharedConnection();
        }

        // Set to true to keep the first opened connection alive until this object is disposed
        public bool KeepConnectionAlive { get; set; }

        private bool ShouldCloseConnectionAutomatically { get; set; }

        // Open a connection (can be nested)
        public IDatabase OpenSharedConnection()
        {
            OpenSharedConnectionImp(false);
            return this;
        }

        private void OpenSharedConnectionInternal()
        {
            OpenSharedConnectionImp(true);
        }

        private void OpenSharedConnectionImp(bool isInternal)
        {
            if (_connectionPassedIn && _sharedConnection != null && _sharedConnection.State != ConnectionState.Open)
                throw new Exception("You must explicitly open the connection before executing anything when passing in a DbConnection to Database");

            if (_sharedConnection != null && _sharedConnection.State != ConnectionState.Broken && _sharedConnection.State != ConnectionState.Closed)
                return;

            ShouldCloseConnectionAutomatically = isInternal;

            _sharedConnection = _factory.CreateConnection();
            if (_sharedConnection == null) throw new Exception("SQL Connection failed to configure.");

            _sharedConnection.ConnectionString = _connectionString;

            if (_sharedConnection.State == ConnectionState.Broken)
            {
                _sharedConnection.Close();
            }

            if (_sharedConnection.State == ConnectionState.Closed)
            {
                _sharedConnection.Open();
                _sharedConnection = OnConnectionOpenedInternal(_sharedConnection);

                //using (var cmd = _sharedConnection.CreateCommand())
                //{
                //    cmd.CommandTimeout = CommandTimeout;
                //    cmd.CommandText = _dbType.GetSQLForTransactionLevel(_isolationLevel);
                //    cmd.ExecuteNonQuery();
                //}
            }
        }

        private void CloseSharedConnectionInternal()
        {
            if (ShouldCloseConnectionAutomatically && _transaction == null)
                CloseSharedConnection();
        }

        // Close a previously opened connection
        public void CloseSharedConnection()
        {
            if (KeepConnectionAlive) return;

            if (_transaction != null)
            {
                _transaction.Dispose();
                _transaction = null;
            }

            if (_sharedConnection == null) return;

            OnConnectionClosingInternal(_sharedConnection);

            _sharedConnection.Close();
            _sharedConnection.Dispose();
            _sharedConnection = null;
        }

        public VersionExceptionHandling VersionException
        {
            get { return _versionException; }
            set { _versionException = value; }
        }

        // Access to our shared connection
        public DbConnection Connection
        {
            get { return _sharedConnection; }
        }

        public DbTransaction Transaction
        {
            get { return _transaction; }
        }

        public DbParameter CreateParameter()
        {
            using (var conn = _sharedConnection ?? _factory.CreateConnection())
            {
                if (conn == null) throw new Exception("DB Connection no longer active and failed to reset.");
                using (var comm = conn.CreateCommand())
                {
                    return comm.CreateParameter();
                }
            }
        }

        // Helper to create a transaction scope
        public ITransaction GetTransaction()
        {
            return GetTransaction(_isolationLevel);
        }

        public ITransaction GetTransaction(IsolationLevel isolationLevel)
        {
            return new Transaction(this, isolationLevel);
        }

        public void SetTransaction(DbTransaction tran)
        {
            _transaction = tran;
        }

        private void OnBeginTransactionInternal()
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine("Created new transaction using isolation level of " + _transaction.IsolationLevel + ".");
#endif
            OnBeginTransaction();
            foreach (var interceptor in Interceptors.OfType<ITransactionInterceptor>())
            {
                interceptor.OnBeginTransaction(this);
            }
        }

        protected virtual void OnBeginTransaction()
        {
        }

        private void OnAbortTransactionInternal()
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine("Rolled back a transaction");
#endif
            OnAbortTransaction();
            foreach (var interceptor in Interceptors.OfType<ITransactionInterceptor>())
            {
                interceptor.OnAbortTransaction(this);
            }
        }

        protected virtual void OnAbortTransaction()
        {
        }

        private void OnCompleteTransactionInternal()
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine("Committed the transaction");
#endif
            OnCompleteTransaction();
            foreach (var interceptor in Interceptors.OfType<ITransactionInterceptor>())
            {
                interceptor.OnCompleteTransaction(this);
            }
        }

        protected virtual void OnCompleteTransaction()
        {
        }

        public void BeginTransaction()
        {
            BeginTransaction(_isolationLevel);
        }

        // Start a new transaction, can be nested, every call must be
        //	matched by a call to AbortTransaction or CompleteTransaction
        // Use `using (var scope=db.Transaction) { scope.Complete(); }` to ensure correct semantics
        public void BeginTransaction(IsolationLevel isolationLevel)
        {
            if (_transaction == null)
            {
                TransactionCount = 0;
                OpenSharedConnectionInternal();
                _transaction = _sharedConnection.BeginTransaction(isolationLevel);
                OnBeginTransactionInternal();
            }

            if (_transaction != null)
            {
                TransactionCount++;
            }
        }

        // Abort the entire outer most transaction scope
        public void AbortTransaction()
        {
            TransactionIsAborted = true;
            AbortTransaction(false);
        }

        public void AbortTransaction(bool fromComplete)
        {
            if (_transaction == null)
            {
                TransactionIsAborted = false;
                return;
            }

            if (fromComplete == false)
            {
                TransactionCount--;
                if (TransactionCount >= 1)
                {
                    TransactionIsAborted = true;
                    return;
                }
            }

            if (TransactionIsOk())
                _transaction.Rollback();

            if (_transaction != null)
                _transaction.Dispose();

            _transaction = null;
            TransactionIsAborted = false;

            // You cannot continue to use a connection after a transaction has been rolled back
            if (_sharedConnection != null)
            {
                _sharedConnection.Close();
                _sharedConnection.Open();
            }

            OnAbortTransactionInternal();
            CloseSharedConnectionInternal();
        }

        // Complete the transaction
        public void CompleteTransaction()
        {
            if (_transaction == null)
                return;

            TransactionCount--;
            if (TransactionCount >= 1)
                return;

            if (TransactionIsAborted)
            {
                AbortTransaction(true);
                return;
            }

            if (TransactionIsOk())
                _transaction.Commit();

            if (_transaction != null)
                _transaction.Dispose();

            _transaction = null;

            OnCompleteTransactionInternal();
            CloseSharedConnectionInternal();
        }

        internal bool TransactionIsAborted { get; set; }
        internal int TransactionCount { get; set; }

        private bool TransactionIsOk()
        {
            return _sharedConnection != null
                && _transaction != null
                && _transaction.Connection != null
                && _transaction.Connection.State == ConnectionState.Open;
        }

        // Add a parameter to a DB command
        public virtual void AddParameter(DbCommand cmd, object value)
        {
            // Convert value to from poco type to db type
            if (Mappers != null && value != null)
            {
                value = Mappers.FindAndExecute(x => x.GetParameterConverter(cmd, value.GetType()), value);
            }

            // Support passed in parameters
            var idbParam = value as DbParameter;
            if (idbParam != null)
            {
                idbParam.ParameterName = string.Format("{0}{1}", _paramPrefix, cmd.Parameters.Count);
                cmd.Parameters.Add(idbParam);
                return;
            }

            var p = cmd.CreateParameter();
            p.ParameterName = string.Format("{0}{1}", _paramPrefix, cmd.Parameters.Count);

            SetParameterValue(p, value);
            
            cmd.Parameters.Add(p);
        }

        private void SetParameterValue(DbParameter p, object value)
        {
            if (value == null)
            {
                p.Value = DBNull.Value;
                return;
            }

            // Give the database type first crack at converting to DB required type
            value = _dbType.MapParameterValue(value);

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
                    p.Value = String.Empty;
                }
                else
                {
                    // out of memory exception occurs if trying to save more than 4000 characters to SQL Server CE NText column. Set before attempting to set Size, or Size will always max out at 4000
                    if (strValue.Length + 1 > 4000 && p.GetType().Name == "SqlCeParameter")
                    {
                        p.GetType().GetProperty("SqlDbType").SetValue(p, SqlDbType.NText, null);
                    }

                    p.Size = Math.Max(strValue.Length + 1, 4000); // Help query plan caching by using common size
                    p.Value = value;
                }
            }
            else if (t == typeof(AnsiString))
            {
                var ansistrValue = value as AnsiString;
                if (ansistrValue == null)
                {
                    p.Size = 0;
                    p.Value = String.Empty;
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
                p.GetType().GetProperty("UdtTypeName").SetValue(p, "geography", null); //geography is the equivalent SQL Server Type
                p.Value = value;
            }

            else if (value.GetType().Name == "SqlGeometry") //SqlGeometry is a CLR Type
            {
                p.GetType().GetProperty("UdtTypeName").SetValue(p, "geometry", null); //geography is the equivalent SQL Server Type
                p.Value = value;
            }
            else
            {
                p.Value = value;
            }

            if (!dbtypeSet)
            {
                var dbType = _dbType.LookupDbType(p.Value.GetTheType(), p.ParameterName);
                if (dbType.HasValue)
                {
                    p.DbType = dbType.Value;
                }
            }
        }

        // Create a command
        private DbCommand CreateCommand(DbConnection connection, string sql, params object[] args)
        {
            return CreateCommand(connection, CommandType.Text, sql, args);
        }
        
        public virtual DbCommand CreateCommand(DbConnection connection, CommandType commandType, string sql, params object[] args)
        {
            if (commandType == CommandType.StoredProcedure)
            {
                return CreateStoredProcedureCommand(connection, sql, args);
            }

            // Perform parameter prefix replacements
            if (_paramPrefix != "@")
                sql = ParameterHelper.rxParamsPrefix.Replace(sql, m => _paramPrefix + m.Value.Substring(1));
            sql = sql.Replace("@@", "@");		   // <- double @@ escapes a single @

            // Create the command and add parameters
            DbCommand cmd = connection.CreateCommand();
            cmd.Connection = connection;
            cmd.CommandText = sql;            
            cmd.Transaction = _transaction;

            foreach (var item in args)
            {
                AddParameter(cmd, item);
            }

            // Notify the DB type
            _dbType.PreExecute(cmd);

            return cmd;
        }

        protected virtual void OnException(Exception exception)
        {
        }

        // Override this to log/capture exceptions
        private void OnExceptionInternal(Exception exception)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine("***** EXCEPTION *****" + Environment.NewLine + Environment.NewLine + exception.Message + Environment.NewLine + exception.StackTrace);
            System.Diagnostics.Debug.WriteLine("***** LAST COMMAND *****" + Environment.NewLine + Environment.NewLine + LastCommand);
            System.Diagnostics.Debug.WriteLine("***** CONN INFO *****" + Environment.NewLine + Environment.NewLine + "Provider: " + _providerName + Environment.NewLine + "Connection String: " + _connectionString + Environment.NewLine + "DB Type: " + _dbType);
#endif
            OnException(exception);
            foreach (var interceptor in Interceptors.OfType<IExceptionInterceptor>())
            {
                interceptor.OnException(this, exception);
            }
        }

        protected virtual DbConnection OnConnectionOpened(DbConnection conn)
        {
            return conn;
        }

        private DbConnection OnConnectionOpenedInternal(DbConnection conn)
        {
            var newConnection = OnConnectionOpened(conn);
            foreach (var interceptor in Interceptors.OfType<IConnectionInterceptor>())
            {
                newConnection = interceptor.OnConnectionOpened(this, newConnection);
            }
            return newConnection;
        }

        protected virtual void OnConnectionClosing(DbConnection conn)
        {
        }

        private void OnConnectionClosingInternal(DbConnection conn)
        {
            OnConnectionClosing(conn);
            foreach (var interceptor in Interceptors.OfType<IConnectionInterceptor>())
            {
                interceptor.OnConnectionClosing(this, conn);
            }
        }

        protected virtual void OnExecutingCommand(DbCommand cmd)
        {

        }

        private void OnExecutingCommandInternal(DbCommand cmd)
        {
            OnExecutingCommand(cmd);
            foreach (var interceptor in Interceptors.OfType<IExecutingInterceptor>())
            {
                interceptor.OnExecutingCommand(this, cmd);
            }
        }

        protected virtual void OnExecutedCommand(DbCommand cmd)
        {

        }

        private void OnExecutedCommandInternal(DbCommand cmd)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine(LastCommand);
#endif
            OnExecutedCommand(cmd);
            foreach (var interceptor in Interceptors.OfType<IExecutingInterceptor>())
            {
                interceptor.OnExecutedCommand(this, cmd);
            }
        }

        protected virtual bool OnInserting(InsertContext insertContext)
        {
            return true;
        }

        private bool OnInsertingInternal(InsertContext insertContext)
        {
            var result = OnInserting(insertContext);
            return result && Interceptors.OfType<IDataInterceptor>().All(x => x.OnInserting(this, insertContext));
        }

        protected virtual bool OnUpdating(UpdateContext updateContext)
        {
            return true;
        }

        private bool OnUpdatingInternal(UpdateContext updateContext)
        {
            var result = OnUpdating(updateContext);
            return result && Interceptors.OfType<IDataInterceptor>().All(x => x.OnUpdating(this, updateContext));
        }

        protected virtual bool OnDeleting(DeleteContext deleteContext)
        {
            return true;
        }

        private bool OnDeletingInternal(DeleteContext deleteContext)
        {
            var result = OnDeleting(deleteContext);
            return result && Interceptors.OfType<IDataInterceptor>().All(x => x.OnDeleting(this, deleteContext));
        }
        
        public DbCommand CreateStoredProcedureCommand(DbConnection connection, string name, params object[] args)
        {
            DbCommand cmd = connection.CreateCommand();
            cmd.Connection = connection;
            cmd.CommandText = name;
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Transaction = _transaction;

            if (args.Length == 1)
            {
                var arg = args[0] as DbParameter;
                if (arg != null)
                {
                    cmd.Parameters.Add(arg);
                }
                else
                {
                    var props = args[0].GetType().GetProperties().Select(x => new { x.Name, Value = x.GetValue(args[0], null) }).ToList();
                    foreach(var item in props)
                    {
                        DbParameter param = cmd.CreateParameter();
                        param.ParameterName = item.Name;

                        SetParameterValue(param, item.Value);
                        
                        cmd.Parameters.Add(param);
                    }
                }
            }
            else
            {
                cmd.Parameters.AddRange(args.OfType<DbParameter>().ToArray());
            }

            // Notify the DB type
            _dbType.PreExecute(cmd);

            return cmd;
        }

        // Execute a non-query command
        public int Execute(string sql, params object[] args)
        {
            return Execute(new Sql(sql, args));
        }

        public int Execute(Sql Sql)
        {
            return Execute(Sql.SQL, CommandType.Text, Sql.Arguments);
        }

        public int Execute(string sql, CommandType commandType, params object[] args)
        {
            try
            {
                OpenSharedConnectionInternal();
                using (var cmd = CreateCommand(_sharedConnection, commandType, sql, args))
                {
                    var result = ((IDatabaseHelpers)this).ExecuteNonQueryHelper(cmd);
                    return result;
                }
            }
            catch (Exception x)
            {
                OnExceptionInternal(x);
                throw;
            }
            finally
            {
                CloseSharedConnectionInternal();
            }
        }

        // Execute and cast a scalar property
        public T ExecuteScalar<T>(string sql, params object[] args)
        {
            return ExecuteScalar<T>(new Sql(sql, args));
        }
        
        public T ExecuteScalar<T>(Sql Sql)
        {
            return ExecuteScalar<T>(Sql.SQL, CommandType.Text, Sql.Arguments);
        }

        public T ExecuteScalar<T>(string sql, CommandType commandType, params object[] args)
        {
            try
            {
                OpenSharedConnectionInternal();
                using (var cmd = CreateCommand(_sharedConnection, commandType, sql, args))
                {
                    object val = ExecuteScalarHelper(cmd);

                    if (val == null || val == DBNull.Value)
                        return default(T);

                    Type t = typeof(T);
                    Type u = Nullable.GetUnderlyingType(t);

                    return (T)Convert.ChangeType(val, u ?? t);
                }
            }
            catch (Exception x)
            {
                OnExceptionInternal(x);
                throw;
            }
            finally
            {
                CloseSharedConnectionInternal();
            }
        }

        public bool EnableAutoSelect { get; set; }

        // Return a typed list of pocos
        public List<T> Fetch<T>(string sql, params object[] args)
        {
            return Fetch<T>(new Sql(sql, args));
        }

        public List<T> Fetch<T>(Sql sql)
        {
            return Query<T>(sql).ToList();
        }

        public List<T> Fetch<T>()
        {
            return Fetch<T>("");
        }

        public void BuildPageQueries<T>(long skip, long take, string sql, ref object[] args, out string sqlCount, out string sqlPage)
        {
            // Add auto select clause
            if (EnableAutoSelect)
                sql = AutoSelectHelper.AddSelectClause(this, typeof(T), sql);

            // Split the SQL
            PagingHelper.SQLParts parts;
            if (!PagingHelper.SplitSQL(sql, out parts)) throw new Exception("Unable to parse SQL statement for paged query");

            sqlPage = _dbType.BuildPageQuery(skip, take, parts, ref args);
            sqlCount = parts.sqlCount;
        }

        // Fetch a page
        public Page<T> Page<T>(long page, long itemsPerPage, Sql sql)
        {
            return Page<T>(page, itemsPerPage, sql.SQL, sql.Arguments);
        }

        public List<T> Fetch<T>(long page, long itemsPerPage, string sql, params object[] args)
        {
            return SkipTake<T>((page - 1) * itemsPerPage, itemsPerPage, sql, args);
        }

        public List<T> Fetch<T>(long page, long itemsPerPage, Sql sql)
        {
            return SkipTake<T>((page - 1) * itemsPerPage, itemsPerPage, sql.SQL, sql.Arguments);
        }

        public List<T> SkipTake<T>(long skip, long take, string sql, params object[] args)
        {
            string sqlCount, sqlPage;
            BuildPageQueries<T>(skip, take, sql, ref args, out sqlCount, out sqlPage);
            return Fetch<T>(sqlPage, args);
        }

        public List<T> SkipTake<T>(long skip, long take, Sql sql)
        {
            return SkipTake<T>(skip, take, sql.SQL, sql.Arguments);
        }

        public Dictionary<TKey, TValue> Dictionary<TKey, TValue>(Sql Sql)
        {
            return Dictionary<TKey, TValue>(Sql.SQL, Sql.Arguments);
        }

        public Dictionary<TKey, TValue> Dictionary<TKey, TValue>(string sql, params object[] args)
        {
            var newDict = new Dictionary<TKey, TValue>();
            bool isConverterSet = false;
            Func<object, object> converter1 = x => x, converter2 = x => x;

            foreach (var line in Query<Dictionary<string, object>>(sql, args))
            {
                object key = line.ElementAt(0).Value;
                object value = line.ElementAt(1).Value;

                if (isConverterSet == false)
                {
                    converter1 = MappingHelper.GetConverter(Mappers, null, typeof(TKey), key.GetType()) ?? (x => x);
                    converter2 = (value != null ? MappingHelper.GetConverter(Mappers, null, typeof(TValue), value.GetType()) : null) ?? (x => x);
                    isConverterSet = true;
                }

                var keyConverted = (TKey)Convert.ChangeType(converter1(key), typeof(TKey));

                var valueType = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);
                var valConv = converter2(value);
                var valConverted = valConv != null ? (TValue)Convert.ChangeType(valConv, valueType) : default(TValue);

                if (keyConverted != null)
                {
                    newDict.Add(keyConverted, valConverted);
                }
            }
            return newDict;
        }

        // Return an enumerable collection of pocos
        public IEnumerable<T> Query<T>(string sql, params object[] args)
        {
            return Query<T>(new Sql(sql, args));
        }

        public IEnumerable<T> Query<T>(Sql Sql)
        {
            return Query(default(T), Sql);
        }

        private IEnumerable<T> Read<T>(Type type, object instance, DbDataReader r, DbCommand cmd)
        {
            try
            {
                using (cmd)
                {
                    using (r)
                    {
                        var pd = PocoDataFactory.ForType(type);
                        var factory = new MappingFactory(pd, r);
                        while (true)
                        {
                            T poco;
                            try
                            {
                                if (!r.Read()) yield break;
                                poco = (T)factory.Map(r, instance);
                            }
                            catch (Exception x)
                            {
                                OnExceptionInternal(x);
                                throw;
                            }

                            yield return poco;
                        }
                    }
                }
            }
            finally
            {
                CloseSharedConnectionInternal();
            }
        }

        private IEnumerable<T> ReadOneToMany<T>(T instance, DbDataReader r, DbCommand cmd, Expression<Func<T, IList>> listExpression, Func<T, object[]> idFunc)
        {
            Func<T, IList> listFunc = null;
            PocoMember pocoMember = null;
            PocoMember foreignMember = null;

            try
            {
                using (cmd)
                {
                    using (r)
                    {
                        var pocoData = PocoDataFactory.ForType(typeof(T));
                        if (listExpression != null)
                        {
                            idFunc = idFunc ?? (x => pocoData.GetPrimaryKeyValues(x));
                            listFunc = listExpression.Compile();
                            var key = PocoColumn.GenerateKey(MemberChainHelper.GetMembers(listExpression));
                            pocoMember = pocoData.Members.FirstOrDefault(x => x.Name == key);
                            foreignMember = pocoMember != null ? pocoMember.PocoMemberChildren.FirstOrDefault(x => x.Name == pocoMember.ReferenceMemberName && x.ReferenceType == ReferenceType.Foreign) : null;
                        }

                        var factory = new MappingFactory(pocoData, r);
                        object prevPoco = null;

                        while (true)
                        {
                            T poco;
                            try
                            {
                                if (!r.Read()) break;
                                poco = (T)factory.Map(r, instance);
                            }
                            catch (Exception x)
                            {
                                OnExceptionInternal(x);
                                throw;
                            }

                            if (prevPoco != null)
                            {
                                if (listFunc != null
                                    && pocoMember != null
                                    && idFunc(poco).SequenceEqual(idFunc((T)prevPoco)))
                                {
                                    OneToManyHelper.SetListValue(listFunc, pocoMember, prevPoco, poco);
                                    continue;
                                }

                                OneToManyHelper.SetForeignList(listFunc, foreignMember, prevPoco);
                                yield return (T)prevPoco;
                            }

                            prevPoco = poco;
                        }

                        if (prevPoco != null)
                        {
                            OneToManyHelper.SetForeignList(listFunc, foreignMember, prevPoco);
                            yield return (T)prevPoco;
                        }
                    }
                }
            }
            finally
            {
                CloseSharedConnectionInternal();
            }
        }

        public IQueryProviderWithIncludes<T> Query<T>()
        {
            return new QueryProvider<T>(this);
        }

        private IEnumerable<T> Query<T>(T instance, Sql Sql)
        {
            return QueryImp(instance, null, null, Sql);
        }

        public List<object> Fetch(Type type, string sql, params object[] args)
        {
            return Fetch(type, new Sql(sql, args));
        }

        public List<object> Fetch(Type type, Sql Sql)
        {
            return Query(type, Sql).ToList();
        }

        public IEnumerable<object> Query(Type type, string sql, params object[] args)
        {
            return Query(type, new Sql(sql, args));
        }

        public IEnumerable<object> Query(Type type, Sql Sql)
        {
            var sql = Sql.SQL;
            var args = Sql.Arguments;

            if (EnableAutoSelect) sql = AutoSelectHelper.AddSelectClause(this, type, sql);

            try
            {
                OpenSharedConnectionInternal();
                var cmd = CreateCommand(_sharedConnection, sql, args);
                DbDataReader r;
                try
                {
                    r = ExecuteDataReader(cmd);
                }
                catch
                {
                    cmd.Dispose();
                    throw;
                }

                var read = Read<object>(type, null, r, cmd);
                foreach (var item in read)
                {
                    yield return item;
                }
            }
            finally
            {
                CloseSharedConnectionInternal();
            }
        }

        internal IEnumerable<T> QueryImp<T>(T instance, Expression<Func<T, IList>> listExpression, Func<T, object[]> idFunc, Sql Sql)
        {
            var sql = Sql.SQL;
            var args = Sql.Arguments;

            if (EnableAutoSelect) sql = AutoSelectHelper.AddSelectClause(this, typeof (T), sql);

            try
            {
                OpenSharedConnectionInternal();
                var cmd = CreateCommand(_sharedConnection, sql, args);
                DbDataReader r;
                try
                {
                    r = ExecuteDataReader(cmd);
                }
                catch
                {
                    cmd.Dispose();
                    throw;
                }

                var read = listExpression != null ? ReadOneToMany(instance, r, cmd, listExpression, idFunc) : Read<T>(typeof(T), instance, r, cmd);
                foreach (var item in read)
                {
                    yield return item;
                }

            }
            finally
            {
                CloseSharedConnectionInternal();
            }
        }

        private DbDataReader ExecuteDataReader(DbCommand cmd)
        {
            DbDataReader r;
            try
            {
                r = ExecuteReaderHelper(cmd);
            }
            catch (Exception x)
            {
                OnExceptionInternal(x);
                throw;
            }
            return r;
        }

        public List<T> FetchOneToMany<T>(Expression<Func<T, IList>> many, Sql sql)
        {
            return QueryImp(default(T), many, null, sql).ToList();
        }

        public List<T> FetchOneToMany<T>(Expression<Func<T, IList>> many, string sql, params object[] args)
        {
            return FetchOneToMany(many, new Sql(sql, args));
        }

        public List<T> FetchOneToMany<T>(Expression<Func<T, IList>> many, Func<T, object> idFunc, Sql sql)
        {
            return QueryImp(default(T), many, x => new[] { idFunc(x) }, sql).ToList();
        }

        public List<T> FetchOneToMany<T>(Expression<Func<T, IList>> many, Func<T, object> idFunc, string sql, params object[] args)
        {
            return FetchOneToMany(many, idFunc, new Sql(sql, args));
        }

        public Page<T> Page<T>(long page, long itemsPerPage, string sql, params object[] args)
        {
            return PageImp<T, Page<T>>(page, itemsPerPage, sql, args, (paged, thesql) =>
            {
                paged.Items =  Query<T>(thesql).ToList();
                return paged;
            });
        }

        // Actual implementation of the multi-poco paging
        protected TRet PageImp<T, TRet>(long page, long itemsPerPage, string sql, object[] args, Func<Page<T>, Sql, TRet> executeQueryFunc)
        {
            string sqlCount, sqlPage;

            long offset = (page - 1) * itemsPerPage;

            BuildPageQueries<T>(offset, itemsPerPage, sql, ref args, out sqlCount, out sqlPage);

            // Save the one-time command time out and use it for both queries
            int saveTimeout = OneTimeCommandTimeout;

            // Setup the paged result
            var result = new Page<T>();
            result.CurrentPage = page;
            result.ItemsPerPage = itemsPerPage;
            result.TotalItems = ExecuteScalar<long>(sqlCount, args);
            result.TotalPages = result.TotalItems / itemsPerPage;
            if ((result.TotalItems % itemsPerPage) != 0)
                result.TotalPages++;

            OneTimeCommandTimeout = saveTimeout;

            // Get the records
            return executeQueryFunc(result, new Sql(sqlPage, args));
        }

        public TRet FetchMultiple<T1, T2, TRet>(Func<List<T1>, List<T2>, TRet> cb, string sql, params object[] args) { return FetchMultiple<T1, T2, DontMap, DontMap, TRet>(new[] { typeof(T1), typeof(T2) }, cb, new Sql(sql, args)); }
        public TRet FetchMultiple<T1, T2, T3, TRet>(Func<List<T1>, List<T2>, List<T3>, TRet> cb, string sql, params object[] args) { return FetchMultiple<T1, T2, T3, DontMap, TRet>(new[] { typeof(T1), typeof(T2), typeof(T3) }, cb, new Sql(sql, args)); }
        public TRet FetchMultiple<T1, T2, T3, T4, TRet>(Func<List<T1>, List<T2>, List<T3>, List<T4>, TRet> cb, string sql, params object[] args) { return FetchMultiple<T1, T2, T3, T4, TRet>(new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) }, cb, new Sql(sql, args)); }
        public TRet FetchMultiple<T1, T2, TRet>(Func<List<T1>, List<T2>, TRet> cb, Sql sql) { return FetchMultiple<T1, T2, DontMap, DontMap, TRet>(new[] { typeof(T1), typeof(T2) }, cb, sql); }
        public TRet FetchMultiple<T1, T2, T3, TRet>(Func<List<T1>, List<T2>, List<T3>, TRet> cb, Sql sql) { return FetchMultiple<T1, T2, T3, DontMap, TRet>(new[] { typeof(T1), typeof(T2), typeof(T3) }, cb, sql); }
        public TRet FetchMultiple<T1, T2, T3, T4, TRet>(Func<List<T1>, List<T2>, List<T3>, List<T4>, TRet> cb, Sql sql) { return FetchMultiple<T1, T2, T3, T4, TRet>(new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) }, cb, sql); }

        public Tuple<List<T1>, List<T2>> FetchMultiple<T1, T2>(string sql, params object[] args) { return FetchMultiple<T1, T2, DontMap, DontMap, Tuple<List<T1>, List<T2>>>(new[] { typeof(T1), typeof(T2) }, new Func<List<T1>, List<T2>, Tuple<List<T1>, List<T2>>>((y, z) => new Tuple<List<T1>, List<T2>>(y, z)), new Sql(sql, args)); }
        public Tuple<List<T1>, List<T2>, List<T3>> FetchMultiple<T1, T2, T3>(string sql, params object[] args) { return FetchMultiple<T1, T2, T3, DontMap, Tuple<List<T1>, List<T2>, List<T3>>>(new[] { typeof(T1), typeof(T2), typeof(T3) }, new Func<List<T1>, List<T2>, List<T3>, Tuple<List<T1>, List<T2>, List<T3>>>((x, y, z) => new Tuple<List<T1>, List<T2>, List<T3>>(x, y, z)), new Sql(sql, args)); }
        public Tuple<List<T1>, List<T2>, List<T3>, List<T4>> FetchMultiple<T1, T2, T3, T4>(string sql, params object[] args) { return FetchMultiple<T1, T2, T3, T4, Tuple<List<T1>, List<T2>, List<T3>, List<T4>>>(new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) }, new Func<List<T1>, List<T2>, List<T3>, List<T4>, Tuple<List<T1>, List<T2>, List<T3>, List<T4>>>((w, x, y, z) => new Tuple<List<T1>, List<T2>, List<T3>, List<T4>>(w, x, y, z)), new Sql(sql, args)); }
        public Tuple<List<T1>, List<T2>> FetchMultiple<T1, T2>(Sql sql) { return FetchMultiple<T1, T2, DontMap, DontMap, Tuple<List<T1>, List<T2>>>(new[] { typeof(T1), typeof(T2) }, new Func<List<T1>, List<T2>, Tuple<List<T1>, List<T2>>>((y, z) => new Tuple<List<T1>, List<T2>>(y, z)), sql); }
        public Tuple<List<T1>, List<T2>, List<T3>> FetchMultiple<T1, T2, T3>(Sql sql) { return FetchMultiple<T1, T2, T3, DontMap, Tuple<List<T1>, List<T2>, List<T3>>>(new[] { typeof(T1), typeof(T2), typeof(T3) }, new Func<List<T1>, List<T2>, List<T3>, Tuple<List<T1>, List<T2>, List<T3>>>((x, y, z) => new Tuple<List<T1>, List<T2>, List<T3>>(x, y, z)), sql); }
        public Tuple<List<T1>, List<T2>, List<T3>, List<T4>> FetchMultiple<T1, T2, T3, T4>(Sql sql) { return FetchMultiple<T1, T2, T3, T4, Tuple<List<T1>, List<T2>, List<T3>, List<T4>>>(new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) }, new Func<List<T1>, List<T2>, List<T3>, List<T4>, Tuple<List<T1>, List<T2>, List<T3>, List<T4>>>((w, x, y, z) => new Tuple<List<T1>, List<T2>, List<T3>, List<T4>>(w, x, y, z)), sql); }

        public class DontMap { }

        // Actual implementation of the multi query
        private TRet FetchMultiple<T1, T2, T3, T4, TRet>(Type[] types, object cb, Sql Sql)
        {
            var sql = Sql.SQL;
            var args = Sql.Arguments;

            try
            {
                OpenSharedConnectionInternal();
                using (var cmd = CreateCommand(_sharedConnection, sql, args))
                {
                    var r = ExecuteDataReader(cmd);
                    using (r)
                    {
                        var typeIndex = 1;
                        var list1 = new List<T1>();
                        var list2 = types.Length > 1 ? new List<T2>() : null;
                        var list3 = types.Length > 2 ? new List<T3>() : null;
                        var list4 = types.Length > 3 ? new List<T4>() : null;
                        do
                        {
                            if (typeIndex > types.Length)
                                break;

                            var pd = PocoDataFactory.ForType(types[typeIndex - 1]);
                            var factory = new MappingFactory(pd, r);

                            while (true)
                            {
                                try
                                {
                                    if (!r.Read())
                                        break;

                                    switch (typeIndex)
                                    {
                                        case 1:
                                            list1.Add((T1) factory.Map(r, default(T1)));
                                            break;
                                        case 2:
                                            list2.Add((T2) factory.Map(r, default(T2)));
                                            break;
                                        case 3:
                                            list3.Add((T3) factory.Map(r, default(T3)));
                                            break;
                                        case 4:
                                            list4.Add((T4) factory.Map(r, default(T4)));
                                            break;
                                    }
                                }
                                catch (Exception x)
                                {
                                    OnExceptionInternal(x);
                                    throw;
                                }
                            }

                            typeIndex++;
                        } while (r.NextResult());

                        switch (types.Length)
                        {
                            case 2:
                                return ((Func<List<T1>, List<T2>, TRet>) cb)(list1, list2);
                            case 3:
                                return ((Func<List<T1>, List<T2>, List<T3>, TRet>) cb)(list1, list2, list3);
                            case 4:
                                return ((Func<List<T1>, List<T2>, List<T3>, List<T4>, TRet>) cb)(list1, list2, list3, list4);
                        }

                        return default(TRet);
                    }
                }
            }
            finally
            {
                CloseSharedConnectionInternal();
            }
        }

        private bool PocoExists<T>(T poco)
        {
            var index = 0;
            var pd = PocoDataFactory.ForType(typeof(T));
            var primaryKeyValuePairs = GetPrimaryKeyValues(pd, pd.TableInfo.PrimaryKey, poco, true);
            return ExecuteScalar<int>(string.Format(DatabaseType.GetExistsSql(), DatabaseType.EscapeTableName(pd.TableInfo.TableName), BuildPrimaryKeySql(primaryKeyValuePairs, ref index)), primaryKeyValuePairs.Select(x => x.Value).ToArray()) > 0;
        }

        public bool Exists<T>(object primaryKey)
        {
            var index = 0;
            var pd = PocoDataFactory.ForType(typeof (T));
            var primaryKeyValuePairs = GetPrimaryKeyValues(pd, pd.TableInfo.PrimaryKey, primaryKey, false);
            return ExecuteScalar<int>(string.Format(DatabaseType.GetExistsSql(), DatabaseType.EscapeTableName(pd.TableInfo.TableName), BuildPrimaryKeySql(primaryKeyValuePairs, ref index)), primaryKeyValuePairs.Select(x => x.Value).ToArray()) > 0;
        }

        public T SingleById<T>(object primaryKey)
        {
            var sql = GenerateSingleByIdSql<T>(primaryKey);
            return Single<T>(sql);
        }

        public T SingleOrDefaultById<T>(object primaryKey)
        {
            var sql = GenerateSingleByIdSql<T>(primaryKey);
            return SingleOrDefault<T>(sql);
        }

        private Sql GenerateSingleByIdSql<T>(object primaryKey)
        {
            var index = 0;
            var pd = PocoDataFactory.ForType(typeof (T));
            var primaryKeyValuePairs = GetPrimaryKeyValues(pd, pd.TableInfo.PrimaryKey, primaryKey, primaryKey is T);
            var sql = AutoSelectHelper.AddSelectClause(this, typeof(T), string.Format("WHERE {0}", BuildPrimaryKeySql(primaryKeyValuePairs, ref index)));
            var args = primaryKeyValuePairs.Select(x => x.Value).ToArray();
            return new Sql(true, sql, args);
        }

        public T Single<T>(string sql, params object[] args)
        {
            return Query<T>(sql, args).Single();
        }
        public T SingleInto<T>(T instance, string sql, params object[] args)
        {
            return Query(instance, new Sql(sql, args)).Single();
        }
        public T SingleOrDefault<T>(string sql, params object[] args)
        {
            return Query<T>(sql, args).SingleOrDefault();
        }
        public T SingleOrDefaultInto<T>(T instance, string sql, params object[] args)
        {
            return Query(instance, new Sql(sql, args)).SingleOrDefault();
        }
        public T First<T>(string sql, params object[] args)
        {
            return Query<T>(sql, args).First();
        }
        public T FirstInto<T>(T instance, string sql, params object[] args)
        {
            return Query(instance, new Sql(sql, args)).First();
        }
        public T FirstOrDefault<T>(string sql, params object[] args)
        {
            return Query<T>(sql, args).FirstOrDefault();
        }
        public T FirstOrDefaultInto<T>(T instance, string sql, params object[] args)
        {
            return Query(instance, new Sql(sql, args)).FirstOrDefault();
        }
        public T Single<T>(Sql sql)
        {
            return Query<T>(sql).Single();
        }
        public T SingleInto<T>(T instance, Sql sql)
        {
            return Query(instance, sql).Single();
        }
        public T SingleOrDefault<T>(Sql sql)
        {
            return Query<T>(sql).SingleOrDefault();
        }
        public T SingleOrDefaultInto<T>(T instance, Sql sql)
        {
            return Query(instance, sql).SingleOrDefault();
        }
        public T First<T>(Sql sql)
        {
            return Query<T>(sql).First();
        }
        public T FirstInto<T>(T instance, Sql sql)
        {
            return Query(instance, sql).First();
        }
        public T FirstOrDefault<T>(Sql sql)
        {
            return Query<T>(sql).FirstOrDefault();
        }
        public T FirstOrDefaultInto<T>(T instance, Sql sql)
        {
            return Query(instance, sql).FirstOrDefault();
        }

        // Insert an annotated poco object
        public object Insert<T>(T poco)
        {
            var tableInfo = PocoDataFactory.TableInfoForType(poco.GetType());
            return Insert(tableInfo.TableName, tableInfo.PrimaryKey, tableInfo.AutoIncrement, poco);
        }

        public object Insert<T>(string tableName, string primaryKeyName, T poco)
        {
            return Insert(tableName, primaryKeyName, true, poco);
        }

        // Insert a poco into a table.  If the poco has a property with the same name
        // as the primary key the id of the new record is assigned to it.  Either way,
        // the new id is returned.
        public virtual object Insert<T>(string tableName, string primaryKeyName, bool autoIncrement, T poco)
        {
            var pd = PocoDataFactory.ForObject(poco, primaryKeyName, autoIncrement);
            return InsertImp(pd, tableName, primaryKeyName, autoIncrement, poco);
        }

        private object InsertImp<T>(PocoData pocoData, string tableName, string primaryKeyName, bool autoIncrement, T poco)
        {
            if (!OnInsertingInternal(new InsertContext(poco, tableName, autoIncrement, primaryKeyName)))
                return 0;

            try
            {
                OpenSharedConnectionInternal();

                var preparedInsert = InsertStatements.PrepareInsertSql(this, pocoData, tableName, primaryKeyName, autoIncrement, poco);

                using (var cmd = CreateCommand(_sharedConnection, preparedInsert.Sql, preparedInsert.Rawvalues.ToArray()))
                {
                    // Assign the Version column
                    InsertStatements.AssignVersion(poco, preparedInsert);

                    object id;
                    if (!autoIncrement)
                    {
                        ExecuteNonQueryHelper(cmd);
                        id = InsertStatements.AssignNonIncrementPrimaryKey(primaryKeyName, poco, preparedInsert);
                    }
                    else
                    {
                        id = _dbType.ExecuteInsert(this, cmd, primaryKeyName, preparedInsert.PocoData.TableInfo.UseOutputClause, poco, preparedInsert.Rawvalues.ToArray());
                        InsertStatements.AssignPrimaryKey(primaryKeyName, poco, id, preparedInsert);
                    }

                    return id;
                }
            }
            catch (Exception x)
            {
                OnExceptionInternal(x);
                throw;
            }
            finally
            {
                CloseSharedConnectionInternal();
            }
        }

        public void InsertBatch<T>(IEnumerable<T> pocos, BatchOptions options = null)
        {
            options = options ?? new BatchOptions();

            try
            {
                OpenSharedConnectionInternal();

                var pd = PocoDataFactory.ForType(typeof(T));

                foreach (var batchedPocos in pocos.Chunkify(options.BatchSize))
                {
                    var preparedInserts = batchedPocos.Select(x => InsertStatements.PrepareInsertSql(this, pd, pd.TableInfo.TableName, pd.TableInfo.PrimaryKey,pd.TableInfo.AutoIncrement, x)).ToArray();

                    var sql = new Sql();
                    foreach (var preparedInsertSql in preparedInserts)
                    {
                        sql.Append(preparedInsertSql.Sql + options.StatementSeperator, preparedInsertSql.Rawvalues.ToArray());
                    }

                    using (var cmd = CreateCommand(_sharedConnection, sql.SQL, sql.Arguments))
                    {
                        ExecuteNonQueryHelper(cmd);
                    }
                }
            }
            catch (Exception x)
            {
                OnExceptionInternal(x);
                throw;
            }
            finally
            {
                CloseSharedConnectionInternal();
            }
        }

        public void InsertBulk<T>(IEnumerable<T> pocos)
        {
            try
            {
                OpenSharedConnectionInternal();
                _dbType.InsertBulk(this, pocos);
            }
            catch (Exception x)
            {
                OnExceptionInternal(x);
                throw;
            }
            finally
            {
                CloseSharedConnectionInternal();
            }
        }

        public int Update(string tableName, string primaryKeyName, object poco, object primaryKeyValue)
        {
            return Update(tableName, primaryKeyName, poco, primaryKeyValue, null);
        }

        public virtual int Update(string tableName, string primaryKeyName, object poco, object primaryKeyValue, IEnumerable<string> columns)
        {
            return UpdateImp(tableName, primaryKeyName, poco, primaryKeyValue, columns, (sql, args, next) => next(Execute(sql, args)), 0);
        }

        // Update a record with values from a poco.  primary key value can be either supplied or read from the poco
        private TRet UpdateImp<TRet>(string tableName, string primaryKeyName, object poco, object primaryKeyValue, IEnumerable<string> columns, Func<string, object[], Func<int, int>, TRet> executeFunc, TRet defaultId)
        {
            if (!OnUpdatingInternal(new UpdateContext(poco, tableName, primaryKeyName, primaryKeyValue, columns)))
                return defaultId;

            if (columns != null && !columns.Any())
                return defaultId;

            var sb = new StringBuilder();
            var index = 0;
            var rawvalues = new List<object>();
            var pd = PocoDataFactory.ForObject(poco, primaryKeyName, true);
            string versionName = null;
            object versionValue = null;
            VersionColumnType versionColumnType = VersionColumnType.Number;

            var primaryKeyValuePairs = GetPrimaryKeyValues(pd, primaryKeyName, primaryKeyValue ?? poco, primaryKeyValue == null);

            foreach (var pocoColumn in pd.Columns.Values)
            {
                // Don't update the primary key, but grab the value if we don't have it
                if (primaryKeyValuePairs.ContainsKey(pocoColumn.ColumnName))
                { 
                    if (primaryKeyValue == null)
                         primaryKeyValuePairs[pocoColumn.ColumnName] = this.ProcessMapper(pocoColumn, pocoColumn.GetValue(poco));
                    continue;
                }

                // Dont update result only columns
                if (pocoColumn.ResultColumn
                    || (pocoColumn.ComputedColumn && (pocoColumn.ComputedColumnType == ComputedColumnType.Always || pocoColumn.ComputedColumnType == ComputedColumnType.ComputedOnUpdate)))
                {
                    continue;
                }

                if (!pocoColumn.VersionColumn && columns != null && !columns.Contains(pocoColumn.ColumnName, StringComparer.OrdinalIgnoreCase))
                    continue;

                object value = pocoColumn.GetColumnValue(pd, poco, this.ProcessMapper);

                if (pocoColumn.VersionColumn)
                {
                    versionName = pocoColumn.ColumnName;
                    versionValue = value;
                    if (pocoColumn.VersionColumnType == VersionColumnType.Number)
                    {
                        versionColumnType = VersionColumnType.Number;
                        value = Convert.ToInt64(value) + 1;
                    }
                    else if (pocoColumn.VersionColumnType == VersionColumnType.RowVersion)
                    {
                        versionColumnType = VersionColumnType.RowVersion;
                        continue;
                    }
                }

                // Build the sql
                if (index > 0)
                    sb.Append(", ");
                sb.AppendFormat("{0} = @{1}", _dbType.EscapeSqlIdentifier(pocoColumn.ColumnName), index++);

                rawvalues.Add(value);
            }

            if (columns != null && columns.Any() && sb.Length == 0)
                return defaultId;

            var sql = string.Format("UPDATE {0} SET {1} WHERE {2}", _dbType.EscapeTableName(tableName), sb, BuildPrimaryKeySql(primaryKeyValuePairs, ref index));

            rawvalues.AddRange(primaryKeyValuePairs.Select(keyValue => keyValue.Value));

            if (!string.IsNullOrEmpty(versionName))
            {
                sql += string.Format(" AND {0} = @{1}", _dbType.EscapeSqlIdentifier(versionName), index++);
                rawvalues.Add(versionValue);
            }

            var result = executeFunc(sql, rawvalues.ToArray(), (id) =>
            {
                if (id == 0 && !string.IsNullOrEmpty(versionName) && VersionException == VersionExceptionHandling.Exception)
                {
#if DNXCORE50
                    throw new Exception(string.Format("A Concurrency update occurred in table '{0}' for primary key value(s) = '{1}' and version = '{2}'", tableName, string.Join(",", primaryKeyValuePairs.Values.Select(x => x.ToString()).ToArray()), versionValue));
#else
                    throw new DBConcurrencyException(string.Format("A Concurrency update occurred in table '{0}' for primary key value(s) = '{1}' and version = '{2}'", tableName, string.Join(",", primaryKeyValuePairs.Values.Select(x => x.ToString()).ToArray()), versionValue));
#endif
                }

                // Set Version
                if (!string.IsNullOrEmpty(versionName) && versionColumnType == VersionColumnType.Number)
                {
                    PocoColumn pc;
                    if (pd.Columns.TryGetValue(versionName, out pc))
                    {
                        pc.SetValue(poco, Convert.ChangeType(Convert.ToInt64(versionValue) + 1, pc.MemberInfoData.MemberType));
                    }
                }

                return id;
            });

            return result;
        }

        private string BuildPrimaryKeySql(Dictionary<string, object> primaryKeyValuePair, ref int index)
        {
            var tempIndex = index;
            index += primaryKeyValuePair.Count;
            return string.Join(" AND ", primaryKeyValuePair.Select((x, i) => x.Value == null || x.Value == DBNull.Value ? string.Format("{0} IS NULL", _dbType.EscapeSqlIdentifier(x.Key)) : string.Format("{0} = @{1}", _dbType.EscapeSqlIdentifier(x.Key), tempIndex + i)).ToArray());
        }

        private Dictionary<string, object> GetPrimaryKeyValues(PocoData pocoData, string primaryKeyName, object primaryKeyValueOrPoco, bool isPoco)
        {
            Dictionary<string, object> primaryKeyValues;

            var multiplePrimaryKeysNames = primaryKeyName.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();
            if (isPoco == false)
            {
                if (multiplePrimaryKeysNames.Length == 1)
                {
                    primaryKeyValues = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) { { primaryKeyName, primaryKeyValueOrPoco } };
                }
                else
                {
                    var dict = primaryKeyValueOrPoco as Dictionary<string, object>;
                    primaryKeyValues = dict ?? multiplePrimaryKeysNames.ToDictionary(x => x, x => primaryKeyValueOrPoco.GetType().GetProperties().Single(y => string.Equals(x, y.Name, StringComparison.OrdinalIgnoreCase)).GetValue(primaryKeyValueOrPoco, null), StringComparer.OrdinalIgnoreCase);
                }
            }
            else
            {
                primaryKeyValues = ProcessMapper(pocoData, multiplePrimaryKeysNames.ToDictionary(x => x, x => pocoData.Columns[x].GetValue(primaryKeyValueOrPoco), StringComparer.OrdinalIgnoreCase));
            }

            return primaryKeyValues;
        }

        private Dictionary<string, object> ProcessMapper(PocoData pd, Dictionary<string, object> primaryKeyValuePairs)
        {
            var keys = primaryKeyValuePairs.Keys.ToArray();
            foreach (var primaryKeyValuePair in keys)
            {
                var col = pd.Columns[primaryKeyValuePair];
                primaryKeyValuePairs[primaryKeyValuePair] = this.ProcessMapper(col, primaryKeyValuePairs[primaryKeyValuePair]);
            }
            return primaryKeyValuePairs;
        }

        public IUpdateQueryProvider<T> UpdateMany<T>()
        {
            return new UpdateQueryProvider<T>(this);
        }

        public int Update(string tableName, string primaryKeyName, object poco)
        {
            return Update(tableName, primaryKeyName, poco, null);
        }

        public int Update(string tableName, string primaryKeyName, object poco, IEnumerable<string> columns)
        {
            return Update(tableName, primaryKeyName, poco, null, columns);
        }

        public int Update(object poco, IEnumerable<string> columns)
        {
            return Update(poco, null, columns);
        }

        public int Update<T>(T poco, Expression<Func<T, object>> fields)
        {
            var expression = DatabaseType.ExpressionVisitor<T>(this, PocoDataFactory.ForType(typeof(T)));
            expression = expression.Select(fields);
            var columnNames = ((ISqlExpression) expression).SelectMembers.Select(x => x.PocoColumn.ColumnName);
            var otherNames = ((ISqlExpression) expression).GeneralMembers.Select(x => x.PocoColumn.ColumnName);
            return Update(poco, columnNames.Union(otherNames));
        }

        public int Update(object poco)
        {
            return Update(poco, null, null);
        }

        public int Update(object poco, object primaryKeyValue)
        {
            return Update(poco, primaryKeyValue, null);
        }

        public int Update(object poco, object primaryKeyValue, IEnumerable<string> columns)
        {
            var tableInfo = PocoDataFactory.TableInfoForType(poco.GetType());
            return Update(tableInfo.TableName, tableInfo.PrimaryKey, poco, primaryKeyValue, columns);
        }

        public int Update<T>(string sql, params object[] args)
        {
            var tableInfo = PocoDataFactory.TableInfoForType(typeof(T));
            return Execute(string.Format("UPDATE {0} {1}", _dbType.EscapeTableName(tableInfo.TableName), sql), args);
        }

        public int Update<T>(Sql sql)
        {
            var tableInfo = PocoDataFactory.TableInfoForType(typeof(T));
            return Execute(new Sql(string.Format("UPDATE {0}", _dbType.EscapeTableName(tableInfo.TableName))).Append(sql));
        }

        public IDeleteQueryProvider<T> DeleteMany<T>()
        {
            return new DeleteQueryProvider<T>(this);
        }

        public int Delete(string tableName, string primaryKeyName, object poco)
        {
            return Delete(tableName, primaryKeyName, poco, null);
        }

        public virtual int Delete(string tableName, string primaryKeyName, object poco, object primaryKeyValue)
        {
            return DeleteImp(tableName, primaryKeyName, poco, primaryKeyValue, Execute, 0);
        }

        private TRet DeleteImp<TRet>(string tableName, string primaryKeyName, object poco, object primaryKeyValue, Func<string, object[], TRet> executeFunc, TRet defaultRet)
        {
            if (!OnDeletingInternal(new DeleteContext(poco, tableName, primaryKeyName, primaryKeyValue)))
                return defaultRet;

            var pd = poco != null ? PocoDataFactory.ForObject(poco, primaryKeyName, true) : null;
            var primaryKeyValuePairs = GetPrimaryKeyValues(pd, primaryKeyName, primaryKeyValue ?? poco, primaryKeyValue == null);

            // Do it
            var index = 0;
            var sql = string.Format("DELETE FROM {0} WHERE {1}", _dbType.EscapeTableName(tableName), BuildPrimaryKeySql(primaryKeyValuePairs, ref index));
            return executeFunc(sql, primaryKeyValuePairs.Select(x => x.Value).ToArray());
        }

        public int Delete(object poco)
        {
            var tableInfo = PocoDataFactory.TableInfoForType(poco.GetType());
            return Delete(tableInfo.TableName, tableInfo.PrimaryKey, poco);
        }

        public int Delete<T>(object pocoOrPrimaryKey)
        {
            if (pocoOrPrimaryKey.GetType() == typeof(T))
                return Delete(pocoOrPrimaryKey);
            var tableInfo = PocoDataFactory.TableInfoForType(typeof(T));
            return Delete(tableInfo.TableName, tableInfo.PrimaryKey, null, pocoOrPrimaryKey);
        }

        public int Delete<T>(string sql, params object[] args)
        {
            var tableInfo = PocoDataFactory.TableInfoForType(typeof(T));
            return Execute(string.Format("DELETE FROM {0} {1}", _dbType.EscapeTableName(tableInfo.TableName), sql), args);
        }

        public int Delete<T>(Sql sql)
        {
            var tableInfo = PocoDataFactory.TableInfoForType(typeof(T));
            return Execute(new Sql(string.Format("DELETE FROM {0}", _dbType.EscapeTableName(tableInfo.TableName))).Append(sql));
        }

        /// <summary>Checks if a poco represents a new record.</summary>
        public bool IsNew<T>(T poco)
        {
#if !NET35
            if (poco is System.Dynamic.ExpandoObject || poco is PocoExpando)
            {
                return true;
            }
#endif
            var pd = PocoDataFactory.ForType(poco.GetType());
            object pk;
            PocoColumn pc;

            if (pd.Columns.TryGetValue(pd.TableInfo.PrimaryKey, out pc))
            {
                pk = pc.GetValue(poco);
            }
            else if (pd.TableInfo.PrimaryKey.Contains(","))
            {
                return !PocoExists(poco);
            }
            else
            {
                var pi = poco.GetType().GetProperty(pd.TableInfo.PrimaryKey);
                if (pi == null) throw new ArgumentException(string.Format("The object doesn't have a property matching the primary key column name '{0}'", pd.TableInfo.PrimaryKey));
                pk = pi.GetValue(poco, null);
            }

            if (pk == null)
                return true;

            if (!pd.TableInfo.AutoIncrement)
                return !Exists<T>(pk);

            var type = pk.GetType();

            if (type.GetTypeInfo().IsValueType)
            {
                // Common primary key types
                if (type == typeof(long)) return (long)pk == default(long);
                if (type == typeof(ulong)) return (ulong)pk == default(ulong);
                if (type == typeof(int)) return (int)pk == default(int);
                if (type == typeof(uint)) return (uint)pk == default(uint);
                if (type == typeof(Guid)) return (Guid)pk == default(Guid);

                // Create a default instance and compare
                return pk == Activator.CreateInstance(pk.GetType());
            }

            return false;
        }

        // Insert new record or Update existing record
        public void Save<T>(T poco)
        {
            var tableInfo = PocoDataFactory.TableInfoForType(poco.GetType());
            if (IsNew(poco))
            {
                Insert(tableInfo.TableName, tableInfo.PrimaryKey, tableInfo.AutoIncrement, poco);
            }
            else
            {
                Update(tableInfo.TableName, tableInfo.PrimaryKey, poco);
            }
        }

        public int CommandTimeout { get; set; }
        public int OneTimeCommandTimeout { get; set; }

        void DoPreExecute(DbCommand cmd)
        {
            // Setup command timeout
            if (OneTimeCommandTimeout != 0)
            {
                cmd.CommandTimeout = OneTimeCommandTimeout;
                OneTimeCommandTimeout = 0;
            }
            else if (CommandTimeout != 0)
            {
                cmd.CommandTimeout = CommandTimeout;
            }

            // Call hook
            OnExecutingCommandInternal(cmd);

            // Save it
            _lastSql = cmd.CommandText;
            _lastArgs = (from DbParameter parameter in cmd.Parameters select parameter.Value).ToArray();
        }

        public string LastSQL { get { return _lastSql; } }
        public object[] LastArgs { get { return _lastArgs; } }
        public string LastCommand
        {
            get { return FormatCommand(_lastSql, _lastArgs); }
        }

        private class FormattedParameter
        {
            public Type Type { get; set; }
            public object Value { get; set; }
            public DbParameter Parameter { get; set; }
        }

        public string FormatCommand(DbCommand cmd)
        {
            var parameters = cmd.Parameters.Cast<DbParameter>().Select(parameter => new FormattedParameter()
            {
                Type = parameter.Value.GetTheType(),
                Value = parameter.Value,
                Parameter = parameter
            });
            return FormatCommand(cmd.CommandText, parameters.Cast<object>().ToArray());
        }

        public string FormatCommand(string sql, object[] args)
        {
            var sb = new StringBuilder();
            if (sql == null)
                return "";
            sb.Append(sql);
            if (args != null && args.Length > 0)
            {
                sb.Append("\n");
                for (int i = 0; i < args.Length; i++)
                {
                    var type = args[i] != null ? args[i].GetType().Name : string.Empty;
                    var value = args[i];
                    var formatted = args[i] as FormattedParameter;
                    if (formatted != null)
                    {
                        type = formatted.Type != null ? formatted.Type.Name : string.Format("{0}, {1}", formatted.Parameter.GetType().Name, formatted.Parameter.DbType);
                        value = formatted.Value;
                    }
                    sb.AppendFormat("\t -> {0}{1} [{2}] = \"{3}\"\n", _paramPrefix, i, type, value);
                }
                sb.Remove(sb.Length - 1, 1);
            }
            return sb.ToString();
        }

        private List<IInterceptor> _interceptors;
        public List<IInterceptor> Interceptors
        {
            get { return _interceptors ?? (_interceptors = new List<IInterceptor>()); }
        }

        private MapperCollection _mappers;
        public MapperCollection Mappers
        {
            get { return _mappers ?? (_mappers = new MapperCollection()); }
            set { _mappers = value; }
        }

        private IPocoDataFactory _pocoDataFactory;
        public IPocoDataFactory PocoDataFactory
        {
            get { return _pocoDataFactory ?? (_pocoDataFactory = new PocoDataFactory(Mappers)); }
            set { _pocoDataFactory = value; }
        }

        public string ConnectionString { get { return _connectionString; } }

        // Member variables
        private readonly string _connectionString;
        private readonly string _providerName;
        private DbProviderFactory _factory;
        private DbConnection _sharedConnection;
        private DbTransaction _transaction;
        private IsolationLevel _isolationLevel;
        private string _lastSql;
        private object[] _lastArgs;
        private string _paramPrefix = "@";
        private VersionExceptionHandling _versionException = VersionExceptionHandling.Exception;
        private readonly bool _connectionPassedIn;

        internal int ExecuteNonQueryHelper(DbCommand cmd)
        {
            DoPreExecute(cmd);
            var result = cmd.ExecuteNonQuery();
            OnExecutedCommandInternal(cmd);
            return result;
        }

        internal object ExecuteScalarHelper(DbCommand cmd)
        {
            DoPreExecute(cmd);
            object r = cmd.ExecuteScalar();
            OnExecutedCommandInternal(cmd);
            return r;
        }

        internal DbDataReader ExecuteReaderHelper(DbCommand cmd)
        {
            DoPreExecute(cmd);
            DbDataReader r = cmd.ExecuteReader();
            OnExecutedCommandInternal(cmd);
            return r;
        }

        int IDatabaseHelpers.ExecuteNonQueryHelper(DbCommand cmd) => ExecuteNonQueryHelper(cmd);

        object IDatabaseHelpers.ExecuteScalarHelper(DbCommand cmd) => ExecuteScalarHelper(cmd);

        DbDataReader IDatabaseHelpers.ExecuteReaderHelper(DbCommand cmd) => ExecuteReaderHelper(cmd);

#if !NET35 && !NET40
        System.Threading.Tasks.Task<int> IDatabaseHelpers.ExecuteNonQueryHelperAsync(DbCommand cmd) => ExecuteNonQueryHelperAsync(cmd);

        System.Threading.Tasks.Task<object> IDatabaseHelpers.ExecuteScalarHelperAsync(DbCommand cmd) => ExecuteScalarHelperAsync(cmd);

        System.Threading.Tasks.Task<DbDataReader> IDatabaseHelpers.ExecuteReaderHelperAsync(DbCommand cmd) => ExecuteReaderHelperAsync(cmd);
#endif

        public static bool IsEnum(MemberInfoData memberInfo)
        {
            var underlyingType = Nullable.GetUnderlyingType(memberInfo.MemberType);
            return memberInfo.MemberType.GetTypeInfo().IsEnum || (underlyingType != null && underlyingType.GetTypeInfo().IsEnum);
        }
    }

    internal static class ProcessMapperExtensions
    {
        internal static object ProcessMapper(this IDatabase database, PocoColumn pc, object value)
        {
            var converter = database.Mappers.Find(x => x.GetToDbConverter(pc.ColumnType, pc.MemberInfoData.MemberInfo));
            return converter != null ? converter(value) : ProcessDefaultMappings(database, pc, value);
        }
        
        internal static object ProcessDefaultMappings(IDatabase database, PocoColumn pocoColumn, object value)
        {
            if (pocoColumn.SerializedColumn)
            {
                return DatabaseFactory.ColumnSerializer.Serialize(value);
            }
            if (pocoColumn.ColumnType == typeof(string) && Database.IsEnum(pocoColumn.MemberInfoData) && value != null)
            {
                return value.ToString();
            }

            return database.DatabaseType.ProcessDefaultMappings(pocoColumn, value);
        }
    }
}