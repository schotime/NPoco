using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading;

namespace NPoco
{
    internal class Cache<TKey, TValue>
    {
        private readonly bool _useManaged;

        private Cache(bool useManaged)
        {
            _useManaged = useManaged;
        }

        /// <summary>
        /// Creates a cache that uses static storage
        /// </summary>
        /// <returns></returns>
        public static Cache<TKey, TValue> CreateStaticCache()
        {
            return new Cache<TKey, TValue>(false);
        }

        public static Cache<TKey, TValue> CreateManagedCache()
        {
            return new Cache<TKey, TValue>(true);
        }

        Dictionary<TKey, TValue> _map = new Dictionary<TKey, TValue>();
        ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        ObjectCache _objectCache = new MemoryCache("NPoco");

        public int Count
        {
            get
            {
                return _map.Count;
            }
        }

        public TValue Get(TKey key, Func<TValue> factory)
        {
            if (_useManaged)
            {
                //lazy usage of AddOrGetExisting ref: http://stackoverflow.com/questions/10559279/how-to-deal-with-costly-building-operations-using-memorycache/15894928#15894928
                var newValue = new Lazy<TValue>(factory);
                // the line belows returns existing item or adds the new value if it doesn't exist
                var value = (Lazy<TValue>)_objectCache.AddOrGetExisting(key.ToString(), newValue, new CacheItemPolicy
                {
                    //sliding expiration of 1 hr, if the same key isn't used in this 
                    // timeframe it will be removed from the cache
                    SlidingExpiration = new TimeSpan(1,0,0)
                });
                return (value ?? newValue).Value; // Lazy<T> handles the locking itself
            }

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
