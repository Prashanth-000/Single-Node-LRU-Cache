using Single_Node_Cache.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace Single_Node_Cache.Core
{
    internal class SimpleCache
    {
        private readonly int _capacity;
        private readonly Dictionary<string, CacheItem> _store = new();
        private readonly LinkedList<string> _lruList = new();
        private readonly ReaderWriterLockSlim _lock = new();

        private readonly CleanupService _cleanupService;

        public SimpleCache(int capacity)
        {
            _capacity = capacity;
            _cleanupService = new CleanupService(RemoveExpiredItems);
            _cleanupService.Start();
        }

        public void Set(string key, object value, TimeSpan? ttl = null)
        {
            _lock.EnterWriteLock();
            try
            {
                if (_store.ContainsKey(key))
                    Remove(key);

                if (_store.Count >= _capacity)
                    EvictLeastRecentlyUsed();

                _store[key] = new CacheItem(value, ttl);
                _lruList.AddFirst(key);

                Console.WriteLine($"[SET] {key}");
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public object Get(string key)
        {
            _lock.EnterUpgradeableReadLock();
            try
            {
                if (!_store.ContainsKey(key))
                {
                    Console.WriteLine($"[MISS] {key}");
                    return null;
                }

                var item = _store[key];

                if (item.IsExpired())
                {
                    _lock.EnterWriteLock();
                    try { Remove(key); }
                    finally { _lock.ExitWriteLock(); }

                    Console.WriteLine($"[EXPIRED] {key}");
                    return null;
                }

                _lock.EnterWriteLock();
                try
                {
                    _lruList.Remove(key);
                    _lruList.AddFirst(key);
                }
                finally { _lock.ExitWriteLock(); }

                Console.WriteLine($"[HIT] {key}");
                return item.Value;
            }
            finally
            {
                _lock.ExitUpgradeableReadLock();
            }
        }

        private void EvictLeastRecentlyUsed()
        {
            var lruKey = _lruList.Last.Value;
            Remove(lruKey);
            Console.WriteLine($"[EVICT] {lruKey}");
        }

        private void Remove(string key)
        {
            _store.Remove(key);
            _lruList.Remove(key);
        }

        private void RemoveExpiredItems()
        {
            _lock.EnterWriteLock();
            try
            {
                var expiredKeys = new List<string>();

                foreach (var pair in _store)
                {
                    if (pair.Value.IsExpired())
                        expiredKeys.Add(pair.Key);
                }

                foreach (var key in expiredKeys)
                {
                    Remove(key);
                    Console.WriteLine($"[CLEANUP] {key}");
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
    }
}
