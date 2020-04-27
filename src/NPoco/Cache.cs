using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace NPoco
{
    public class Cache<TKey, TValue>
    {
        /// <summary>
        /// Creates a cache that uses static storage
        /// </summary>
        /// <returns></returns>
        public static Cache<TKey, TValue> CreateStaticCache()
        {
            return new Cache<TKey, TValue>();
        }

        readonly Dictionary<TKey, TValue> _map = new Dictionary<TKey, TValue>();
        readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        
        public int Count => _map.Count;

        public TValue Get(TKey key, Func<TValue> factory)
        {
            // Check cache
            _lock.EnterReadLock();
            TValue val;
            try
            {
                if (_map.TryGetValue(key, out val))
                    return val;
            }
            finally
            {
                _lock.ExitReadLock();
            }

            // Cache it
            _lock.EnterWriteLock();
            try
            {
                // Check again
                if (_map.TryGetValue(key, out val))
                    return val;

                // Create it
                val = factory();

                // Store it
                _map.Add(key, val);

                // Done
                return val;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public bool AddIfNotExists(TKey key, TValue value)
        {
            // Cache it
            _lock.EnterWriteLock();
            try
            {
                // Check again
                TValue val;
                if (_map.TryGetValue(key, out val))
                    return true;

                // Create it
                val = value;

                // Store it
                _map.Add(key, val);

                // Done
                return false;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void Flush()
        {
            // Cache it
            _lock.EnterWriteLock();
            try
            {
                _map.Clear();
            }
            finally
            {
                _lock.ExitWriteLock();
            }

        }
    }
}
