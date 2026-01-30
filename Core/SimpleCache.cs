using Single_Node_Cache.Services;
using System;
using System.Collections.Generic;
using System.Text;
using Single_Node_Cache.CLI;

namespace Single_Node_Cache.Core
{
    internal class SimpleCache
    {
        private readonly int _capacity;
        private readonly Dictionary<string, CacheItem> _store = new();
        private readonly LinkedList<string> _lruList = new();
        private readonly ReaderWriterLockSlim _lock = new();

        private readonly CleanupService _cleanupService;

        public event Action? CacheChanged;
        public event Action<string>? ItemCleaned;     // Event when items are cleaned up
        public event Action<string>? ItemSet;         // Event when item is set
        public event Action<string>? ItemEvicted;     // Event when item is evicted
        public event Action<string>? CacheMiss;       // Event when cache miss occurs
        public event Action<string>? CacheHit;        // Event when cache hit occurs
        public event Action<string>? ItemExpired;     // Event when item expires on access

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
            
            CacheChanged?.Invoke();
        }

        public object? Get(string key)
        {
            bool itemExpired = false;
            
            _lock.EnterUpgradeableReadLock();
            try
            {
                if (!_store.ContainsKey(key))
                {
                    // Notify UI about cache miss
                    CacheMiss?.Invoke(key);
                    return null;
                }

                var item = _store[key];

                if (item.IsExpired())
                {
                    _lock.EnterWriteLock();
                    try { Remove(key); }
                    finally { _lock.ExitWriteLock(); }

                    Console.WriteLine($"[EXPIRED] {key}");
                    itemExpired = true;
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
            
            if (itemExpired)
                CacheChanged?.Invoke();
        }

        private void EvictLeastRecentlyUsed()
        {
            if (_lruList.Last != null)
            {
                var lruKey = _lruList.Last.Value;
                Remove(lruKey);
                Console.WriteLine($"[EVICT] {lruKey}");
            }
        }

        private void Remove(string key)
        {
            _store.Remove(key);
            _lruList.Remove(key);
        }

        private void RemoveExpiredItems()
        {
            bool hasExpired = false;
            
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
                    // Notify UI about cleanup via event
                    ItemCleaned?.Invoke(key);
                }

                hasExpired = expiredKeys.Count > 0;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
            
            if (hasExpired)
                CacheChanged?.Invoke();
        }

        public (int capacity, int count, List<(string key, object value, DateTime? expiryTime, bool isExpired)> items) GetCacheState()
        {
            _lock.EnterReadLock();
            try
            {
                var items = new List<(string key, object value, DateTime? expiryTime, bool isExpired)>();
                
                foreach (var key in _lruList)
                {
                    if (_store.TryGetValue(key, out var item))
                    {
                        items.Add((key, item.Value, item.ExpiryTime, item.IsExpired()));
                    }
                }

                return (_capacity, _store.Count, items);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public (bool exists, DateTime? expiryTime) GetExpiry(string key)
        {
            _lock.EnterReadLock();
            try
            {
                if (_store.TryGetValue(key, out var item))
                {
                    return (true, item.ExpiryTime);
                }
                return (false, null);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }
}
