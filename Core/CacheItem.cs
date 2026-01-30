using System;
using System.Collections.Generic;
using System.Text;

namespace Single_Node_Cache.Core
{
    internal class CacheItem
    {
        public object Value { get; }
        public DateTime? ExpiryTime { get; }

        public CacheItem(object value, TimeSpan? ttl)
        {
            Value = value;
            ExpiryTime = ttl == null
                ? null
                : DateTime.UtcNow.Add(ttl.Value);
        }

        public bool IsExpired()
        {
            return ExpiryTime != null && DateTime.UtcNow > ExpiryTime;
        }
    }
}
