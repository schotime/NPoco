/* NPoco 2.0 - PetaPoco v4.0.3.12 - A Tiny ORMish thing for your POCO's.
 * Copyright 2011-2012.  All Rights Reserved.
 * 
 * Apache License 2.0 - http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Originally created by Brad Robinson (@toptensoftware)
 * 
 * Special thanks to Rob Conery (@robconery) for original inspiration (ie:Massive) and for 
 * use of Subsonic's T4 templates, Rob Sullivan (@DataChomp) for hard core DBA advice 
 * and Adam Schroder (@schotime) for lots of suggestions, improvements and Oracle support
 * 
 * #define POCO_NO_DYNAMIC in your project settings on .NET 3.5
 * 
 */

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using NPoco.Expressions;

namespace NPoco
{
    public class Database : IDatabase
    {
        public const bool DefaultEnableAutoSelect = true;

        public Database(IDbConnection connection)
            : this(connection, DatabaseType.Resolve(connection.GetType().Name, null))
        { }

        public Database(IDbConnection connection, DatabaseType dbType)
            : this(connection, dbType, null, DefaultEnableAutoSelect)
        { }

        public Database(IDbConnection connection, DatabaseType dbType, IsolationLevel? isolationLevel)
            : this(connection, dbType, isolationLevel, DefaultEnableAutoSelect)
        { }

        public Database(IDbConnection connection, DatabaseType dbType, IsolationLevel? isolationLevel, bool enableAutoSelect)
        {
            EnableAutoSelect = enableAutoSelect;
            KeepConnectionAlive = true;

            _sharedConnection = connection;
            _connectionString = connection.ConnectionString;
            _dbType = dbType;
            _providerName = _dbType.GetProviderName();
            _factory = DbProviderFactories.GetFactory(_providerName);
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

        public Database(string connectionString, string providerName)
            : this(connectionString, providerName, DefaultEnableAutoSelect)
        { }

        public Database(string connectionString, string providerName, bool enableAutoSelect)
        {
            EnableAutoSelect = enableAutoSelect;
            KeepConnectionAlive = false;

            _connectionString = connectionString;
            _factory = DbProviderFactories.GetFactory(providerName);
            var dbTypeName = (_factory == null ? _sharedConnection.GetType() : _factory.GetType()).Name;
            _dbType = DatabaseType.Resolve(dbTypeName, providerName);
            _providerName = providerName;
            _isolationLevel = _dbType.GetDefaultTransactionIsolationLevel();
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

        public Database(string connectionString, DbProviderFactory provider)
            : this(connectionString, provider, DefaultEnableAutoSelect)
        { }

        public Database(string connectionString, DbProviderFactory provider, bool enableAutoSelect)
        {
            EnableAutoSelect = enableAutoSelect;
            KeepConnectionAlive = false;

            _connectionString = connectionString;
            _factory = provider;
            var dbTypeName = (_factory == null ? _sharedConnection.GetType() : _factory.GetType()).Name;
            _dbType = DatabaseType.Resolve(dbTypeName, null);
            _providerName = _dbType.GetProviderName();
            _isolationLevel = _dbType.GetDefaultTransactionIsolationLevel();
            _paramPrefix = _dbType.GetParameterPrefix(_connectionString);
        }

        public Database(string connectionStringName)
            : this(connectionStringName, DefaultEnableAutoSelect)
        { }

        public Database(string connectionStringName,  bool enableAutoSelect)
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
            var dbTypeName = (_factory == null ? _sharedConnection.GetType() : _factory.GetType()).Name;
            _dbType = DatabaseType.Resolve(dbTypeName, _providerName);
            _isolationLevel = _dbType.GetDefaultTransactionIsolationLevel();
            _paramPrefix = _dbType.GetParameterPrefix(_connectionString);
        }

        private readonly DatabaseType _dbType;
        public DatabaseType DatabaseType { get { return _dbType; } }
        public IsolationLevel IsolationLevel { get { return _isolationLevel; } }

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
        public void OpenSharedConnection()
        {
            OpenSharedConnectionImp(false);
        }

        private void OpenSharedConnectionInternal()
        {
            OpenSharedConnectionImp(true);
        }

        private void OpenSharedConnectionImp(bool isInternal)
        {
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

                //using (var cmd = _sharedConnection.CreateCommand())
                //{
                //    cmd.CommandTimeout = CommandTimeout;
                //    cmd.CommandText = _dbType.GetSQLForTransactionLevel(_isolationLevel);
                //    cmd.ExecuteNonQuery();
                //}
            }

            _sharedConnection = OnConnectionOpened(_sharedConnection);
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
            if (_sharedConnection == null) return;

            OnConnectionClosing(_sharedConnection);

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
        public IDbConnection Connection
        {
            get { return _sharedConnection; }
        }

        public IDbTransaction Transaction
        {
            get { return _transaction; }
        }

        public IDataParameter CreateParameter()
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
        public Transaction GetTransaction()
        {
            return GetTransaction(_isolationLevel);
        }

        public Transaction GetTransaction(IsolationLevel isolationLevel)
        {
            return new Transaction(this, isolationLevel);
        }

        public void SetTransaction(IDbTransaction tran)
        {
            _transaction = tran;
        }

        // Use by derived repo generated by T4 templates
        protected virtual void OnBeginTransaction()
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine("Created new transaction using isolation level of " + _transaction.IsolationLevel + ".");
#endif
        }

        protected virtual void OnAbortTransaction()
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine("Rolled back a transaction");
#endif
        }

        protected virtual void OnCompleteTransaction()
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine("Committed the transaction");
#endif
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
            if (_transaction != null) return;

            OpenSharedConnectionInternal();
            _transaction = _sharedConnection.BeginTransaction(isolationLevel);
            OnBeginTransaction();
        }

        // Abort the entire outer most transaction scope
        public void AbortTransaction()
        {
            if (_transaction == null) 
                return;

            if (TransactionIsOk())
                _transaction.Rollback();

            if (_transaction != null)
                _transaction.Dispose();
            
            _transaction = null;

            // You cannot continue to use a connection after a transaction has been rolled back
            if (_sharedConnection != null)
            {
                _sharedConnection.Close();
                _sharedConnection.Open();
            }

            OnAbortTransaction();
            CloseSharedConnectionInternal();
        }

        // Complete the transaction
        public void CompleteTransaction()
        {
            if (_transaction == null) 
                return;

            if (TransactionIsOk())
                _transaction.Commit();

            if (_transaction != null)
                _transaction.Dispose();
            
            _transaction = null;

            OnCompleteTransaction();
            CloseSharedConnectionInternal();
        }

        private bool TransactionIsOk()
        {
            return _sharedConnection != null
                && _transaction != null
                && _transaction.Connection != null
                && _transaction.Connection.State == ConnectionState.Open;
        }

        // Add a parameter to a DB command
        void AddParam(IDbCommand cmd, object value, string parameterPrefix)
        {
            // Convert value to from poco type to db type
            if (Mapper != null && value != null)
            {
                var fn = Mapper.GetParameterConverter(value.GetType());
                if (fn != null)
                    value = fn(value);
            }

            // Support passed in parameters
            var idbParam = value as IDbDataParameter;
            if (idbParam != null)
            {
                idbParam.ParameterName = string.Format("{0}{1}", parameterPrefix, cmd.Parameters.Count);
                cmd.Parameters.Add(idbParam);
                return;
            }
            var p = cmd.CreateParameter();
            p.ParameterName = string.Format("{0}{1}", parameterPrefix, cmd.Parameters.Count);

            if (value == null)
            {
                p.Value = DBNull.Value;
            }
            else
            {
                // Give the database type first crack at converting to DB required type
                value = _dbType.MapParameterValue(value);

                var t = value.GetType();
                if (t.IsEnum)		// PostgreSQL .NET driver wont cast enum to int
                {
                    p.Value = (int)value;
                }
                else if (t == typeof(Guid))
                {
                    p.Value = value.ToString();
                    p.DbType = DbType.String;
                    p.Size = 40;
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
            }

            cmd.Parameters.Add(p);
        }

        // Create a command
        IDbCommand CreateCommand(IDbConnection connection, string sql, params object[] args)
        {
            // Perform parameter prefix replacements
            if (_paramPrefix != "@")
                sql = ParameterHelper.rxParamsPrefix.Replace(sql, m => _paramPrefix + m.Value.Substring(1));
            sql = sql.Replace("@@", "@");		   // <- double @@ escapes a single @

            // Create the command and add parameters
            IDbCommand cmd = connection.CreateCommand();
            cmd.Connection = connection;
            cmd.CommandText = sql;
            cmd.Transaction = _transaction;

            foreach (var item in args)
            {
                AddParam(cmd, item, _paramPrefix);
            }

            // Notify the DB type
            _dbType.PreExecute(cmd);

            return cmd;
        }

        // Override this to log/capture exceptions
        protected virtual void OnException(Exception x)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine("***** EXCEPTION *****" + Environment.NewLine + Environment.NewLine + x.Message + Environment.NewLine + x.StackTrace);
            System.Diagnostics.Debug.WriteLine("***** LAST COMMAND *****" + Environment.NewLine + Environment.NewLine + LastCommand);
            System.Diagnostics.Debug.WriteLine("***** CONN INFO *****" + Environment.NewLine + Environment.NewLine + "Provider: " + _providerName + Environment.NewLine + "Connection String: " + _connectionString + Environment.NewLine + "DB Type: " + _dbType);
#endif
        }

        // Override this to log commands, or modify command before execution
        protected virtual IDbConnection OnConnectionOpened(IDbConnection conn)
        {
            return conn;
        }

        protected virtual void OnConnectionClosing(IDbConnection conn)
        {

        }

        protected virtual void OnExecutingCommand(IDbCommand cmd)
        {

        }

        protected virtual bool OnInserting(InsertContext insertContext)
        {
            return true;
        }

        protected virtual bool OnUpdating(UpdateContext updateContext)
        {
            return true;
        }

        protected virtual bool OnDeleting(DeleteContext deleteContext)
        {
            return true;
        }

        protected virtual void OnExecutedCommand(IDbCommand cmd)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine(LastCommand);
#endif
        }

        // Execute a non-query command
        public int Execute(string sql, params object[] args)
        {
            return Execute(new Sql(sql, args));
        }

        public int Execute(Sql Sql)
        {
            var sql = Sql.SQL;
            var args = Sql.Arguments;

            try
            {
                OpenSharedConnectionInternal();
                using (var cmd = CreateCommand(_sharedConnection, sql, args))
                {
                    var result = ExecuteNonQueryHelper(cmd);
                    return result;
                }
            }
            catch (Exception x)
            {
                OnException(x);
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
            var sql = Sql.SQL;
            var args = Sql.Arguments;

            try
            {
                OpenSharedConnectionInternal();
                using (var cmd = CreateCommand(_sharedConnection, sql, args))
                {
                    object val = ExecuteScalarHelper(cmd);

                    if (val == null || val == DBNull.Value)
                        return default(T);

                    Type t = typeof (T);
                    Type u = Nullable.GetUnderlyingType(t);

                    return (T) Convert.ChangeType(val, u ?? t);
                }
            }
            catch (Exception x)
            {
                OnException(x);
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

        public List<T> FetchWhere<T>(Expression<Func<T, bool>> expression)
        {
            var ev = _dbType.ExpressionVisitor<T>(this);
            var sql = ev.Where(expression).Context.ToWhereStatement();
            return Fetch<T>(sql, ev.Context.Params.ToArray());
        }

        public List<T> FetchBy<T>(Func<SqlExpression<T>, SqlExpression<T>> expression)
        {
            var ev = _dbType.ExpressionVisitor<T>(this);
            var sql = expression(ev).Context.ToSelectStatement();
            return Fetch<T>(sql, ev.Context.Params.ToArray());
        }

        public void BuildPageQueries<T>(long skip, long take, string sql, ref object[] args, out string sqlCount, out string sqlPage)
        {
            // Add auto select clause
            if (EnableAutoSelect)
                sql = AutoSelectHelper.AddSelectClause<T>(this, sql);

            // Split the SQL
            PagingHelper.SQLParts parts;
            if (!PagingHelper.SplitSQL(sql, out parts)) throw new Exception("Unable to parse SQL statement for paged query");

            sqlPage = _dbType.BuildPageQuery(skip, take, parts, ref args);
            sqlCount = parts.sqlCount;
        }

        // Fetch a page	
        public Page<T> Page<T>(long page, long itemsPerPage, string sql, params object[] args)
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
            result.Items = Fetch<T>(sqlPage, args);

            // Done
            return result;
        }

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
                    converter1 = PocoData.GetConverter(Mapper, null, typeof(TKey), key.GetType()) ?? (x => x);
                    converter2 = PocoData.GetConverter(Mapper, null, typeof(TValue), value.GetType()) ?? (x => x);
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

        private IEnumerable<T> Query<T>(T instance, Sql Sql)
        {
            var sql = Sql.SQL;
            var args = Sql.Arguments;

            if (EnableAutoSelect) sql = AutoSelectHelper.AddSelectClause<T>(this, sql);

            try
            {
                OpenSharedConnectionInternal();
                using (var cmd = CreateCommand(_sharedConnection, sql, args))
                {
                    IDataReader r;
                    var pd = PocoData.ForType(typeof (T), PocoDataFactory);
                    try
                    {
                        r = ExecuteReaderHelper(cmd);
                    }
                    catch (Exception x)
                    {
                        OnException(x);
                        throw;
                    }

                    using (r)
                    {
                        var factory = pd.GetFactory(cmd.CommandText, _sharedConnection.ConnectionString, 0, r.FieldCount, r, instance) as Func<IDataReader, T, T>;
                        while (true)
                        {
                            T poco;
                            try
                            {
                                if (!r.Read()) yield break;
                                poco = factory(r, instance);
                            }
                            catch (Exception x)
                            {
                                OnException(x);
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

        // Multi Fetch
        public List<TRet> Fetch<T1, T2, TRet>(Func<T1, T2, TRet> cb, string sql, params object[] args) { return Query(cb, sql, args).ToList(); }
        public List<TRet> Fetch<T1, T2, T3, TRet>(Func<T1, T2, T3, TRet> cb, string sql, params object[] args) { return Query(cb, sql, args).ToList(); }
        public List<TRet> Fetch<T1, T2, T3, T4, TRet>(Func<T1, T2, T3, T4, TRet> cb, string sql, params object[] args) { return Query(cb, sql, args).ToList(); }

        // Multi Query
        public IEnumerable<TRet> Query<T1, T2, TRet>(Func<T1, T2, TRet> cb, string sql, params object[] args) { return Query<TRet>(new[] { typeof(T1), typeof(T2) }, cb, new Sql(sql, args)); }
        public IEnumerable<TRet> Query<T1, T2, T3, TRet>(Func<T1, T2, T3, TRet> cb, string sql, params object[] args) { return Query<TRet>(new[] { typeof(T1), typeof(T2), typeof(T3) }, cb, new Sql(sql, args)); }
        public IEnumerable<TRet> Query<T1, T2, T3, T4, TRet>(Func<T1, T2, T3, T4, TRet> cb, string sql, params object[] args) { return Query<TRet>(new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) }, cb, new Sql(sql, args)); }

        // Multi Fetch (SQL builder)
        public List<TRet> Fetch<T1, T2, TRet>(Func<T1, T2, TRet> cb, Sql sql) { return Query(cb, sql).ToList(); }
        public List<TRet> Fetch<T1, T2, T3, TRet>(Func<T1, T2, T3, TRet> cb, Sql sql) { return Query(cb, sql).ToList(); }
        public List<TRet> Fetch<T1, T2, T3, T4, TRet>(Func<T1, T2, T3, T4, TRet> cb, Sql sql) { return Query(cb, sql).ToList(); }

        // Multi Query (SQL builder)
        public IEnumerable<TRet> Query<T1, T2, TRet>(Func<T1, T2, TRet> cb, Sql sql) { return Query<TRet>(new[] { typeof(T1), typeof(T2) }, cb, sql); }
        public IEnumerable<TRet> Query<T1, T2, T3, TRet>(Func<T1, T2, T3, TRet> cb, Sql sql) { return Query<TRet>(new[] { typeof(T1), typeof(T2), typeof(T3) }, cb, sql); }
        public IEnumerable<TRet> Query<T1, T2, T3, T4, TRet>(Func<T1, T2, T3, T4, TRet> cb, Sql sql) { return Query<TRet>(new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) }, cb, sql); }

        // Multi Fetch (Simple)
        public List<T1> Fetch<T1, T2>(string sql, params object[] args) { return Query<T1, T2>(sql, args).ToList(); }
        public List<T1> Fetch<T1, T2, T3>(string sql, params object[] args) { return Query<T1, T2, T3>(sql, args).ToList(); }
        public List<T1> Fetch<T1, T2, T3, T4>(string sql, params object[] args) { return Query<T1, T2, T3, T4>(sql, args).ToList(); }

        // Multi Query (Simple)
        public IEnumerable<T1> Query<T1, T2>(string sql, params object[] args) { return Query<T1>(new[] { typeof(T1), typeof(T2) }, null, new Sql(sql, args)); }
        public IEnumerable<T1> Query<T1, T2, T3>(string sql, params object[] args) { return Query<T1>(new[] { typeof(T1), typeof(T2), typeof(T3) }, null, new Sql(sql, args)); }
        public IEnumerable<T1> Query<T1, T2, T3, T4>(string sql, params object[] args) { return Query<T1>(new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) }, null, new Sql(sql, args)); }

        // Multi Fetch (Simple) (SQL builder)
        public List<T1> Fetch<T1, T2>(Sql sql) { return Query<T1, T2>(sql).ToList(); }
        public List<T1> Fetch<T1, T2, T3>(Sql sql) { return Query<T1, T2, T3>(sql).ToList(); }
        public List<T1> Fetch<T1, T2, T3, T4>(Sql sql) { return Query<T1, T2, T3, T4>(sql).ToList(); }

        // Multi Query (Simple) (SQL builder)
        public IEnumerable<T1> Query<T1, T2>(Sql sql) { return Query<T1>(new[] { typeof(T1), typeof(T2) }, null, sql); }
        public IEnumerable<T1> Query<T1, T2, T3>(Sql sql) { return Query<T1>(new[] { typeof(T1), typeof(T2), typeof(T3) }, null, sql); }
        public IEnumerable<T1> Query<T1, T2, T3, T4>(Sql sql) { return Query<T1>(new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) }, null, sql); }


        // Actual implementation of the multi-poco query
        public IEnumerable<TRet> Query<TRet>(Type[] types, Delegate cb, Sql sql)
        {
            try
            {
                OpenSharedConnectionInternal();
                using (var cmd = CreateCommand(_sharedConnection, sql.SQL, sql.Arguments))
                {
                    IDataReader r;
                    try
                    {
                        r = ExecuteReaderHelper(cmd);
                    }
                    catch (Exception x)
                    {
                        OnException(x);
                        throw;
                    }
                    var factory = MultiPocoFactory.GetMultiPocoFactory<TRet>(this, types, sql.SQL, _sharedConnection.ConnectionString, r);
                    if (cb == null) cb = MultiPocoFactory.GetAutoMapper(types.ToArray());
                    var bNeedTerminator = false;
                    using (r)
                    {
                        while (true)
                        {
                            TRet poco;
                            try
                            {
                                if (!r.Read()) break;
                                poco = factory(r, cb);
                            }
                            catch (Exception x)
                            {
                                OnException(x);
                                throw;
                            }

                            if (poco != null)
                            {
                                yield return poco;
                            }
                            else
                            {
                                bNeedTerminator = true;
                            }
                        }
                        if (bNeedTerminator)
                        {
                            var poco = (TRet) cb.DynamicInvoke(new object[types.Length]);
                            if (poco != null)
                            {
                                yield return poco;
                            }
                            else
                            {
                                yield break;
                            }
                        }
                    }
                }
            }
            finally
            {
                CloseSharedConnectionInternal();
            }
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
                    IDataReader r;
                    try
                    {
                        r = ExecuteReaderHelper(cmd);
                    }
                    catch (Exception x)
                    {
                        OnException(x);
                        throw;
                    }

                    using (r)
                    {
                        var typeIndex = 1;
                        var list1 = new List<T1>();
                        var list2 = new List<T2>();
                        var list3 = new List<T3>();
                        var list4 = new List<T4>();
                        do
                        {
                            if (typeIndex > types.Length)
                                break;

                            var pd = PocoData.ForType(types[typeIndex - 1], PocoDataFactory);
                            var factory = pd.GetFactory(cmd.CommandText, _sharedConnection.ConnectionString, 0, r.FieldCount, r, null);

                            while (true)
                            {
                                try
                                {
                                    if (!r.Read())
                                        break;

                                    switch (typeIndex)
                                    {
                                        case 1:
                                            list1.Add(((Func<IDataReader, T1, T1>) factory)(r, default(T1)));
                                            break;
                                        case 2:
                                            list2.Add(((Func<IDataReader, T2, T2>) factory)(r, default(T2)));
                                            break;
                                        case 3:
                                            list3.Add(((Func<IDataReader, T3, T3>) factory)(r, default(T3)));
                                            break;
                                        case 4:
                                            list4.Add(((Func<IDataReader, T4, T4>) factory)(r, default(T4)));
                                            break;
                                    }
                                }
                                catch (Exception x)
                                {
                                    OnException(x);
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

        public bool Exists<T>(object primaryKey)
        {
            var index = 0;
            var primaryKeyValuePairs = GetPrimaryKeyValues(PocoData.ForType(typeof(T), PocoDataFactory).TableInfo.PrimaryKey, primaryKey);
            return FirstOrDefault<T>(string.Format("WHERE {0}", BuildPrimaryKeySql(primaryKeyValuePairs, ref index)), primaryKeyValuePairs.Select(x => x.Value).ToArray()) != null;
        }
        public T SingleById<T>(object primaryKey)
        {
            var index = 0;
            var primaryKeyValuePairs = GetPrimaryKeyValues(PocoData.ForType(typeof(T), PocoDataFactory).TableInfo.PrimaryKey, primaryKey);
            return Single<T>(string.Format("WHERE {0}", BuildPrimaryKeySql(primaryKeyValuePairs, ref index)), primaryKeyValuePairs.Select(x => x.Value).ToArray());
        }
        public T SingleOrDefaultById<T>(object primaryKey)
        {
            var index = 0;
            var primaryKeyValuePairs = GetPrimaryKeyValues(PocoData.ForType(typeof(T), PocoDataFactory).TableInfo.PrimaryKey, primaryKey);
            return SingleOrDefault<T>(string.Format("WHERE {0}", BuildPrimaryKeySql(primaryKeyValuePairs, ref index)), primaryKeyValuePairs.Select(x => x.Value).ToArray());
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
            var pd = PocoData.ForType(poco.GetType(), PocoDataFactory);
            return Insert(pd.TableInfo.TableName, pd.TableInfo.PrimaryKey, pd.TableInfo.AutoIncrement, poco);
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
            if (!OnInserting(new InsertContext(poco, tableName, autoIncrement, primaryKeyName))) return 0;

            try
            {
                OpenSharedConnectionInternal();

                var pd = PocoData.ForObject(poco, primaryKeyName, PocoDataFactory);
                var names = new List<string>();
                var values = new List<string>();
                var rawvalues = new List<object>();
                var index = 0;
                var versionName = "";

                foreach (var i in pd.Columns)
                {
                    // Don't insert result columns
                    if (i.Value.ResultColumn)
                        continue;

                    // Don't insert the primary key (except under oracle where we need bring in the next sequence value)
                    if (autoIncrement && primaryKeyName != null && string.Compare(i.Key, primaryKeyName, true) == 0)
                    {
                        // Setup auto increment expression
                        string autoIncExpression = _dbType.GetAutoIncrementExpression(pd.TableInfo);
                        if (autoIncExpression != null)
                        {
                            names.Add(i.Key);
                            values.Add(autoIncExpression);
                        }
                        continue;
                    }

                    names.Add(_dbType.EscapeSqlIdentifier(i.Key));
                    values.Add(string.Format("{0}{1}", _paramPrefix, index++));

                    object val = i.Value.GetValue(poco);
                    if (Mapper != null)
                    {
                        var converter = Mapper.GetToDbConverter(i.Value.ColumnType, i.Value.MemberInfo.GetMemberInfoType());
                        if (converter != null)
                            val = converter(val);
                    }

                    if (i.Value.VersionColumn)
                    {
                        val = (long)val > 0 ? val : 1;
                        versionName = i.Key;
                    }

                    rawvalues.Add(val);
                }

                var sql = string.Empty;
                var outputClause = String.Empty;
                if (autoIncrement)
                {
                    outputClause = _dbType.GetInsertOutputClause(primaryKeyName);
                }

                if (names.Count != 0)
                {
                    sql = string.Format("INSERT INTO {0} ({1}){2} VALUES ({3})",
                                        _dbType.EscapeTableName(tableName),
                                        string.Join(",", names.ToArray()),
                                        outputClause,
                                        string.Join(",", values.ToArray()));
                }
                else
                {
                    sql = _dbType.GetDefaultInsertSql(tableName, names.ToArray(), values.ToArray());
                }

                using (var cmd = CreateCommand(_sharedConnection, sql, rawvalues.ToArray()))
                {
                    // Assign the Version column
                    if (!string.IsNullOrEmpty(versionName))
                    {
                        PocoColumn pc;
                        if (pd.Columns.TryGetValue(versionName, out pc))
                        {
                            pc.SetValue(poco, pc.ChangeType(1));
                        }
                    }

                    if (!autoIncrement)
                    {
                        ExecuteNonQueryHelper(cmd);

                        PocoColumn pkColumn;
                        if (primaryKeyName != null && pd.Columns.TryGetValue(primaryKeyName, out pkColumn))
                            return pkColumn.GetValue(poco);
                        else
                            return null;
                    }

                    object id = _dbType.ExecuteInsert(this, cmd, primaryKeyName, poco, rawvalues.ToArray());

                    // Assign the ID back to the primary key property
                    if (primaryKeyName != null && id != null && id.GetType().IsValueType)
                    {
                        PocoColumn pc;
                        if (pd.Columns.TryGetValue(primaryKeyName, out pc))
                        {
                            pc.SetValue(poco, pc.ChangeType(id));
                        }
                    }

                    return id;
                }
            }
            catch (Exception x)
            {
                OnException(x);
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
                OnException(x);
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

        // Update a record with values from a poco.  primary key value can be either supplied or read from the poco
        public virtual int Update(string tableName, string primaryKeyName, object poco, object primaryKeyValue, IEnumerable<string> columns)
        {
            if (!OnUpdating(new UpdateContext(poco, tableName, primaryKeyName, primaryKeyValue, columns))) return 0;

            if (columns != null && !columns.Any()) return 0;

            var sb = new StringBuilder();
            var index = 0;
            var rawvalues = new List<object>();
            var pd = PocoData.ForObject(poco, primaryKeyName, PocoDataFactory);
            string versionName = null;
            object versionValue = null;

            var primaryKeyValuePairs = GetPrimaryKeyValues(primaryKeyName, primaryKeyValue);

            foreach (var i in pd.Columns)
            {
                // Don't update the primary key, but grab the value if we don't have it
                if (primaryKeyValue == null && primaryKeyValuePairs.ContainsKey(i.Key))
                {
                    primaryKeyValuePairs[i.Key] = i.Value.GetValue(poco);
                    continue;
                }

                // Dont update result only columns
                if (i.Value.ResultColumn)
                    continue;

                if (!i.Value.VersionColumn && columns != null && !columns.Contains(i.Value.ColumnName, StringComparer.OrdinalIgnoreCase))
                    continue;

                object value = i.Value.GetValue(poco);
                if (Mapper != null)
                {
                    var converter = Mapper.GetToDbConverter(i.Value.ColumnType, i.Value.MemberInfo.GetMemberInfoType());
                    if (converter != null)
                        value = converter(value);
                }

                if (i.Value.VersionColumn)
                {
                    versionName = i.Key;
                    versionValue = value;
                    value = Convert.ToInt64(value) + 1;
                }

                // Build the sql
                if (index > 0)
                    sb.Append(", ");
                sb.AppendFormat("{0} = @{1}", _dbType.EscapeSqlIdentifier(i.Key), index++);

                rawvalues.Add(value);
            }

            if (columns != null && columns.Any() && sb.Length == 0)
                throw new ArgumentException("There were no columns in the columns list that matched your table", "columns");

            var sql = string.Format("UPDATE {0} SET {1} WHERE {2}", _dbType.EscapeTableName(tableName), sb, BuildPrimaryKeySql(primaryKeyValuePairs, ref index));

            rawvalues.AddRange(primaryKeyValuePairs.Select(keyValue => keyValue.Value));

            if (!string.IsNullOrEmpty(versionName))
            {
                sql += string.Format(" AND {0} = @{1}", _dbType.EscapeSqlIdentifier(versionName), index++);
                rawvalues.Add(versionValue);
            }

            var result = Execute(sql, rawvalues.ToArray());

            if (result == 0 && !string.IsNullOrEmpty(versionName) && VersionException == VersionExceptionHandling.Exception)
            {
                throw new DBConcurrencyException(string.Format("A Concurrency update occurred in table '{0}' for primary key value(s) = '{1}' and version = '{2}'", tableName, string.Join(",", primaryKeyValuePairs.Values.Select(x => x.ToString()).ToArray()), versionValue));
            }

            // Set Version
            if (!string.IsNullOrEmpty(versionName))
            {
                PocoColumn pc;
                if (pd.Columns.TryGetValue(versionName, out pc))
                {
                    pc.SetValue(poco, Convert.ChangeType(Convert.ToInt64(versionValue) + 1, pc.MemberInfo.GetMemberInfoType()));
                }
            }

            return result;
        }

        private string BuildPrimaryKeySql(Dictionary<string, object> primaryKeyValuePair, ref int index)
        {
            var tempIndex = index;
            index += primaryKeyValuePair.Count;
            return string.Join(" AND ", primaryKeyValuePair.Select((x, i) => x.Value == null || x.Value == DBNull.Value ? string.Format("{0} IS NULL", _dbType.EscapeSqlIdentifier(x.Key)) : string.Format("{0} = @{1}", _dbType.EscapeSqlIdentifier(x.Key), tempIndex + i)).ToArray());
        }

        private Dictionary<string, object> GetPrimaryKeyValues(string primaryKeyName, object primaryKeyValue)
        {
            Dictionary<string, object> primaryKeyValues;

            var multiplePrimaryKeysNames = primaryKeyName.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();
            if (primaryKeyValue != null)
            {
                if (multiplePrimaryKeysNames.Length == 1)
                {
                    primaryKeyValues = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) { { primaryKeyName, primaryKeyValue } };
                }
                else
                {
                    var dict = primaryKeyValue as Dictionary<string, object>;
                    if (dict != null)
                        return dict;
                    
                    primaryKeyValues = multiplePrimaryKeysNames.ToDictionary(x => x, x => primaryKeyValue.GetType().GetProperties().Single(y => string.Equals(x, y.Name, StringComparison.OrdinalIgnoreCase)).GetValue(primaryKeyValue, null), StringComparer.OrdinalIgnoreCase);
                }
            }
            else
            {
                primaryKeyValues = multiplePrimaryKeysNames.ToDictionary(x => x, x => (object)null, StringComparer.OrdinalIgnoreCase);
            }
            return primaryKeyValues;
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
            var pd = PocoData.ForType(poco.GetType(), PocoDataFactory);
            return Update(pd.TableInfo.TableName, pd.TableInfo.PrimaryKey, poco, primaryKeyValue, columns);
        }

        public int Update<T>(string sql, params object[] args)
        {
            var pd = PocoData.ForType(typeof(T), PocoDataFactory);
            return Execute(string.Format("UPDATE {0} {1}", _dbType.EscapeTableName(pd.TableInfo.TableName), sql), args);
        }

        public int Update<T>(Sql sql)
        {
            var pd = PocoData.ForType(typeof(T), PocoDataFactory);
            return Execute(new Sql(string.Format("UPDATE {0}", _dbType.EscapeTableName(pd.TableInfo.TableName))).Append(sql));
        }

        public int Delete(string tableName, string primaryKeyName, object poco)
        {
            return Delete(tableName, primaryKeyName, poco, null);
        }

        public virtual int Delete(string tableName, string primaryKeyName, object poco, object primaryKeyValue)
        {
            if (!OnDeleting(new DeleteContext(poco, tableName, primaryKeyName, primaryKeyValue))) return 0;

            var primaryKeyValuePairs = GetPrimaryKeyValues(primaryKeyName, primaryKeyValue);
            // If primary key value not specified, pick it up from the object
            if (primaryKeyValue == null)
            {
                var pd = PocoData.ForObject(poco, primaryKeyName, PocoDataFactory);
                foreach (var i in pd.Columns)
                {
                    if (primaryKeyValuePairs.ContainsKey(i.Key))
                    {
                        primaryKeyValuePairs[i.Key] = i.Value.GetValue(poco);
                    }
                }
            }

            // Do it
            var index = 0;
            var sql = string.Format("DELETE FROM {0} WHERE {1}", _dbType.EscapeTableName(tableName), BuildPrimaryKeySql(primaryKeyValuePairs, ref index));
            return Execute(sql, primaryKeyValuePairs.Select(x => x.Value).ToArray());
        }

        public int Delete(object poco)
        {
            var pd = PocoData.ForType(poco.GetType(), PocoDataFactory);
            return Delete(pd.TableInfo.TableName, pd.TableInfo.PrimaryKey, poco);
        }

        public int Delete<T>(object pocoOrPrimaryKey)
        {
            if (pocoOrPrimaryKey.GetType() == typeof(T))
                return Delete(pocoOrPrimaryKey);
            var pd = PocoData.ForType(typeof(T), PocoDataFactory);
            return Delete(pd.TableInfo.TableName, pd.TableInfo.PrimaryKey, null, pocoOrPrimaryKey);
        }

        public int Delete<T>(string sql, params object[] args)
        {
            var pd = PocoData.ForType(typeof(T), PocoDataFactory);
            return Execute(string.Format("DELETE FROM {0} {1}", _dbType.EscapeTableName(pd.TableInfo.TableName), sql), args);
        }

        public int Delete<T>(Sql sql)
        {
            var pd = PocoData.ForType(typeof(T), PocoDataFactory);
            return Execute(new Sql(string.Format("DELETE FROM {0}", _dbType.EscapeTableName(pd.TableInfo.TableName))).Append(sql));
        }

        // Check if a poco represents a new record
        public bool IsNew<T>(object poco)
        {
            var pd = PocoData.ForType(poco.GetType(), PocoDataFactory);
            object pk;
            PocoColumn pc;
            if (pd.Columns.TryGetValue(pd.TableInfo.PrimaryKey, out pc))
            {
                pk = pc.GetValue(poco);
            }
#if !POCO_NO_DYNAMIC
            else if (poco is System.Dynamic.ExpandoObject)
            {
                return true;
            }
#endif
            else if (pd.TableInfo.PrimaryKey.Contains(","))
            {
                foreach (var compositeKey in pd.TableInfo.PrimaryKey.Split(','))
                {
                    var keyName = compositeKey.Trim();
                    var pi = poco.GetType().GetProperty(keyName);
                    if (pi == null) throw new ArgumentException(string.Format("The object doesn't have a property matching the composite primary key column name '{0}'", compositeKey));
                }

                return !Exists<T>(poco);
            }
            else
            {
                var pi = poco.GetType().GetProperty(pd.TableInfo.PrimaryKey);
                if (pi == null) throw new ArgumentException(string.Format("The object doesn't have a property matching the primary key column name '{0}'", pd.TableInfo.PrimaryKey));
                pk = pi.GetValue(poco, null);
            }

            if (pk == null) return true;
            if (!pd.TableInfo.AutoIncrement) return !Exists<T>(pk);

            var type = pk.GetType();

            if (type.IsValueType)
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
        public void Save<T>(object poco)
        {
            var pd = PocoData.ForType(poco.GetType(), PocoDataFactory);
            if (IsNew<T>(poco))
            {
                Insert(pd.TableInfo.TableName, pd.TableInfo.PrimaryKey, pd.TableInfo.AutoIncrement, poco);
            }
            else
            {
                Update(pd.TableInfo.TableName, pd.TableInfo.PrimaryKey, poco);
            }
        }

        public int CommandTimeout { get; set; }
        public int OneTimeCommandTimeout { get; set; }

        void DoPreExecute(IDbCommand cmd)
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
            OnExecutingCommand(cmd);

            // Save it
            _lastSql = cmd.CommandText;
            _lastArgs = (from IDataParameter parameter in cmd.Parameters select parameter.Value).ToArray();
        }

        public string LastSQL { get { return _lastSql; } }
        public object[] LastArgs { get { return _lastArgs; } }
        public string LastCommand
        {
            get { return FormatCommand(_lastSql, _lastArgs); }
        }

        public string FormatCommand(IDbCommand cmd)
        {
            return FormatCommand(cmd.CommandText, (from IDataParameter parameter in cmd.Parameters select parameter.Value).ToArray());
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
                    sb.AppendFormat("\t -> {0}{1} [{2}] = \"{3}\"\n", _paramPrefix, i, args[i].GetType().Name, args[i]);
                }
                sb.Remove(sb.Length - 1, 1);
            }
            return sb.ToString();
        }

        internal Transaction BaseTransaction { get; set; }

        public IMapper Mapper { get; set; }

        private Func<Type, PocoData> _pocoDataFactory;
        public Func<Type, PocoData> PocoDataFactory
        {
            get
            {
                if (_pocoDataFactory == null)
                    return type => new PocoData(type, Mapper);
                return _pocoDataFactory;
            }
            set { _pocoDataFactory = value; }
        }

        public string ConnectionString { get { return _connectionString; } }

        // Member variables
        private readonly string _connectionString;
        private readonly string _providerName;
        private DbProviderFactory _factory;
        private IDbConnection _sharedConnection;
        private IDbTransaction _transaction;
        private IsolationLevel _isolationLevel;
        private string _lastSql;
        private object[] _lastArgs;
        private string _paramPrefix = "@";
        private VersionExceptionHandling _versionException = VersionExceptionHandling.Ignore;

        internal int ExecuteNonQueryHelper(IDbCommand cmd)
        {
            DoPreExecute(cmd);
            var result = cmd.ExecuteNonQuery();
            OnExecutedCommand(cmd);
            return result;
        }

        internal object ExecuteScalarHelper(IDbCommand cmd)
        {
            DoPreExecute(cmd);
            object r = cmd.ExecuteScalar();
            OnExecutedCommand(cmd);
            return r;
        }

        internal IDataReader ExecuteReaderHelper(IDbCommand cmd)
        {
            DoPreExecute(cmd);
            IDataReader r = cmd.ExecuteReader();
            OnExecutedCommand(cmd);
            return r;
        }
    }
}