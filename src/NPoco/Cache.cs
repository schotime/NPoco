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

        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private readonly ReaderWriterLockSlim _slimLock = new ReaderWriterLockSlim();
        private readonly Dictionary<TKey, TValue> _map = new Dictionary<TKey, TValue>();
        private readonly Dictionary<TKey, AntiDupLockSlim> _lockDict = new Dictionary<TKey, AntiDupLockSlim>();
        class AntiDupLockSlim : ReaderWriterLockSlim { public int UseCount; }


        public int Count => _map.Count;

        // test 
        // private readonly static Cache<int, int> cache = new Cache<int, int>();
        //private static List<int> Build(int count)
        //{
        //    List<int> list = new List<int>();
        //    for (int i = 0; i < 100; i++)
        //    {
        //        for (int j = 0; j < count; j++)
        //        {
        //            list.Add(i);
        //        }
        //    }
        //    return list;
        //}
        // main test method :
        //var list=Build(8);
        //var stopwatch = Stopwatch.StartNew();
        //Parallel.ForEach(list, new ParallelOptions() { MaxDegreeOfParallelism = 8 }, (j) => {
        //    cache.Get(j, () => {
        //        Thread.Sleep(1);
        //        return j;
        //    });
        //});
        //stopwatch.Stop();
        //Console.Write(" " + stopwatch.ElapsedMilliseconds.ToString().PadRight(4));


        public TValue Get(TKey key, Func<TValue> factory)
        {
            if (object.Equals(key, null)) { return factory(); }

            TValue val;
            _lock.EnterReadLock();
            try
            {
                if (_map.TryGetValue(key, out val))
                    return val;
            }
            finally { _lock.ExitReadLock(); }

            AntiDupLockSlim slim;
            _slimLock.EnterUpgradeableReadLock();
            try
            {
                _lock.EnterReadLock();
                try
                {
                    if (_map.TryGetValue(key, out val))
                        return val;
                }
                finally { _lock.ExitReadLock(); }

                _slimLock.EnterWriteLock();
                try
                {
                    if (_lockDict.TryGetValue(key, out slim) == false)
                    {
                        slim = new AntiDupLockSlim();
                        _lockDict[key] = slim;
                    }
                    slim.UseCount++;
                }
                finally { _slimLock.ExitWriteLock(); }
            }
            finally { _slimLock.ExitUpgradeableReadLock(); }


            slim.EnterWriteLock();
            try
            {
                _lock.EnterReadLock();
                try
                {
                    if (_map.TryGetValue(key, out val))
                        return val;
                }
                finally { _lock.ExitReadLock(); }

                val = factory();
                _lock.EnterWriteLock();
                try
                {
                    _map[key] = val;
                }
                finally { _lock.ExitWriteLock(); }
                return val;
            }
            finally
            {
                slim.ExitWriteLock();
                _slimLock.EnterWriteLock();
                try
                {
                    slim.UseCount--;
                    if (slim.UseCount == 0)
                    {
                        _lockDict.Remove(key);
                        slim.Dispose();
                    }
                }
                finally { _slimLock.ExitWriteLock(); }
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
