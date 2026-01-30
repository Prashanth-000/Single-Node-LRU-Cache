using Single_Node_Cache.CLI;
using Single_Node_Cache.Core;
using Single_Node_Cache.Services;
using System;
using System.Collections.Generic;
using System.IO;

class Program
{
    static void Main()
    {
        Console.WriteLine("=== Initializing Cache System ===");
        
        var cache = new SimpleCache(3);
        
        var dbFilePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Services", "db.txt");
        dbFilePath = Path.GetFullPath(dbFilePath);
        var database = new FileDatabase(dbFilePath, readDelayMs: 600, writeDelayMs: 1000);
        
        var cacheManager = new CacheManager(cache, database);
        
        Console.WriteLine($"Cache capacity: 3 items");
        Console.WriteLine($"Database file: {dbFilePath}");
        Console.WriteLine($"DB Read delay: 600ms, Write delay: 1000ms");
        Console.WriteLine("=====================================\n");
        
        var console = new CacheConsole(cacheManager);
        console.Run();
    }
}
