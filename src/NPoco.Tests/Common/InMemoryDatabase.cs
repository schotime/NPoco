using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SQLite;

namespace NPoco.Tests.Common
{
    public class InMemoryDatabase : IDisposable
    {
        public static string ConnectionString = "Data Source=:memory:;Version=3;";

        public IDbConnection Connection { get; protected set; }

        public InMemoryDatabase()
        {
            EnsureConfigured();

            RecreateDataBase();
        }

        private void RecreateDataBase()
        {
            Console.WriteLine("----------------------------");
            Console.WriteLine("Creating database schema...");
            Console.WriteLine("----------------------------");
        }

        private static readonly object _syncRoot = new object();
        private void EnsureConfigured()
        {
            if (Connection == null)
            {
                lock (_syncRoot)
                {
                    if (Connection == null)
                    {
                        Connection = new SQLiteConnection(ConnectionString);
                        Connection.Open();
                    }
                }
            }
        }

        public void Dispose()
        {
            Console.WriteLine("----------------------------");
            Console.WriteLine("Disposing connection...");
            Console.WriteLine("----------------------------");

            if (Connection != null)
            {
                Connection.Close();
                Connection.Dispose();
            }
        }
    }
}
