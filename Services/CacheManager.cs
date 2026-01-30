using Single_Node_Cache.Core;
using System;

namespace Single_Node_Cache.Services
{
    internal class CacheManager
    {
        private readonly SimpleCache _cache;
        private readonly FileDatabase _database;

        public event Action? CacheChanged;

        public CacheManager(SimpleCache cache, FileDatabase database)
        {
            _cache = cache;
            _database = database;
            
            _cache.CacheChanged += () => CacheChanged?.Invoke();
        }

        public void Set(string key, object value, TimeSpan? ttl = null)
        {
            _database.Set(key, value.ToString() ?? string.Empty);
            _cache.Set(key, value, ttl);
        }

        public object? Get(string key)
        {
            var cachedValue = _cache.Get(key);
            
            if (cachedValue != null)
            {
                return cachedValue;
            }
            
            Console.WriteLine($"[CACHE MISS] Fetching '{key}' from database...");
            var dbValue = _database.Get(key);
            
            if (dbValue != null)
            {
                _cache.Set(key, dbValue);
                Console.WriteLine($"[CACHE WARMED] '{key}' added to cache");
            }
            
            return dbValue;
        }
        public bool Delete(string key)
        {
            var dbDeleted = _database.Delete(key);
            _cache.Set(key, null, TimeSpan.FromMilliseconds(1));
            
            return dbDeleted;
        }

        public (int capacity, int count, System.Collections.Generic.List<(string key, object value, DateTime? expiryTime, bool isExpired)> items) GetCacheState()
        {
            return _cache.GetCacheState();
        }

        public (bool exists, DateTime? expiryTime) GetExpiry(string key)
        {
            return _cache.GetExpiry(key);
        }

        public int GetDatabaseCount()
        {
            return _database.Count();
        }

        public System.Collections.Generic.Dictionary<string, string> GetAllFromDatabase()
        {
            return _database.GetAll();
        }
    }
}
