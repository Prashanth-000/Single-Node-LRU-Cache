using Single_Node_Cache.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace Single_Node_Cache.Demo
{
    internal class CacheDemo
    {
        public static void Run()
        {
            var cache = new SimpleCache(3);

            cache.Set("A", 1);
            cache.Set("B", 2);
            cache.Set("C", 3);

            cache.Get("A");
            cache.Set("D", 4);

            cache.Set("token", "ABC", TimeSpan.FromSeconds(3));
            Thread.Sleep(6000);
            cache.Get("token");
        }
    }
}
