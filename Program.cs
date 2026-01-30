using Single_Node_Cache.CLI;
using Single_Node_Cache.Core;
using System;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        var cache = new SimpleCache(3);
        var console = new CacheConsole(cache);
        console.Run();
    }
}
