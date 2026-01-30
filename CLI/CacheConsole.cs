using Single_Node_Cache.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace Single_Node_Cache.CLI
{
    internal class CacheConsole
    {
        private readonly SimpleCache _cache;

        public CacheConsole(SimpleCache cache)
        {
            _cache = cache;
        }

        public void Run()
        {
            PrintHelp();

            while (true)
            {
                Console.Write("> ");
                var input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                    continue;

                var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var command = parts[0].ToUpper();

                switch (command)
                {
                    case "SET":
                        HandleSet(parts);
                        break;

                    case "GET":
                        HandleGet(parts);
                        break;

                    case "EXIT":
                        return;

                    default:
                        Console.WriteLine("Unknown command");
                        break;
                }
            }
        }

        private void HandleSet(string[] parts)
        {
            if (parts.Length < 3)
            {
                Console.WriteLine("Usage: SET <key> <value> [ttlSeconds]");
                return;
            }

            string key = parts[1];
            string value = parts[2];

            if (parts.Length == 4 && int.TryParse(parts[3], out int ttl))
                _cache.Set(key, value, TimeSpan.FromSeconds(ttl));
            else
                _cache.Set(key, value);

            Console.WriteLine("[OK]");
        }

        private void HandleGet(string[] parts)
        {
            if (parts.Length != 2)
            {
                Console.WriteLine("Usage: GET <key>");
                return;
            }

            var result = _cache.Get(parts[1]);
            Console.WriteLine(result ?? "(null)");
        }

        private void PrintHelp()
        {
            Console.WriteLine("=== SimpleCache CLI ===");
            Console.WriteLine("SET <key> <value> [ttlSeconds]");
            Console.WriteLine("GET <key>");
            Console.WriteLine("EXIT");
        }
    }
}
