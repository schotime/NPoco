using System;
using System.Data;

namespace NPoco.Tests.Common
{
    public abstract class TestDatabase : IDisposable
    {
        public string ConnectionString { get; set; }
        public string ProviderName { get; set; }
        public IDbConnection Connection { get; set; }

        protected static readonly object _syncRoot = new object();

        public virtual void RecreateDataBase()
        {
            Console.WriteLine("Creating database schema... ");
        }

        public abstract void EnsureSharedConnectionConfigured();

        public virtual void CleanupDataBase()
        {
            Console.WriteLine("Deleting database schema... ");
        }

        public virtual void Dispose()
        {
            Console.WriteLine("Disposing connection...     ");

            if (Connection == null) return;

            Connection.Close();
            Connection.Dispose();
        }
    }
}
