using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace NPoco
{
    /// <summary>
    /// Container for a Memory cache object
    /// </summary>
    /// <remarks>
    /// Better to have one memory cache instance than many so it's memory management can be handled more effectively
    /// http://stackoverflow.com/questions/8463962/using-multiple-instances-of-memorycache
    /// </remarks>
    internal class ManagedCache
    {
#if !POCO_NO_DYNAMIC
        public System.Runtime.Caching.ObjectCache GetCache()
        {
            return ObjectCache;
        }

        static readonly System.Runtime.Caching.ObjectCache ObjectCache = new System.Runtime.Caching.MemoryCache("NPoco");
#endif
    }

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

        readonly Dictionary<TKey, TValue> _map = new Dictionary<TKey, TValue>();
        readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        readonly ManagedCache _managedCache = new ManagedCache();
        
        public int Count
        {
            get
            {
                return _map.Count;
            }
        }

        public TValue Get(TKey key, Func<TValue> factory)
        {
#if !POCO_NO_DYNAMIC
            if (_useManaged)
            {
                var objectCache = _managedCache.GetCache();
                //lazy usage of AddOrGetExisting ref: http://stackoverflow.com/questions/10559279/how-to-deal-with-costly-building-operations-using-memorycache/15894928#15894928
                var newValue = new Lazy<TValue>(factory);
                // the line belows returns existing item or adds the new value if it doesn't exist
                var value = (Lazy<TValue>)objectCache.AddOrGetExisting(key.ToString(), newValue, new System.Runtime.Caching.CacheItemPolicy
                {
                    //sliding expiration of 1 hr, if the same key isn't used in this 
                    // timeframe it will be removed from the cache
                    SlidingExpiration = new TimeSpan(1,0,0)
                });
                return (value ?? newValue).Value; // Lazy<T> handles the locking itself
            }
#endif

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
