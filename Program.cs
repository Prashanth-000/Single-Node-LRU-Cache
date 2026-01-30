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
        
        // Create cache (capacity: 3 items)
        var cache = new SimpleCache(3);
        
        // Create file-based database simulation using Services/db.txt
        var dbFilePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Services", "db.txt");
        dbFilePath = Path.GetFullPath(dbFilePath);
        var database = new FileDatabase(dbFilePath, readDelayMs: 1000, writeDelayMs: 1500);
        
        // Create cache manager to coordinate cache and database
        var cacheManager = new CacheManager(cache, database);
        
        Console.WriteLine($"Cache capacity: 3 items");
        Console.WriteLine($"Database file: {dbFilePath}");
        Console.WriteLine($"DB Read delay: 1000ms, Write delay: 1500ms");
        Console.WriteLine("=====================================\n");
        
        // Start CLI
        var console = new CacheConsole(cacheManager);
        console.Run();
    }
}

