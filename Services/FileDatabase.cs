using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Single_Node_Cache.Services
{
    internal class FileDatabase
    {
        private readonly string _filePath;
        private readonly int _readDelayMs;
        private readonly int _writeDelayMs;
        private readonly ReaderWriterLockSlim _fileLock = new();

        public FileDatabase(string filePath, int readDelayMs = 1000, int writeDelayMs = 1500)
        {
            _filePath = filePath;
            _readDelayMs = readDelayMs;
            _writeDelayMs = writeDelayMs;
            
            // Create file if it doesn't exist
            if (!File.Exists(_filePath))
            {
                File.WriteAllText(_filePath, string.Empty);
            }
        }

        public string? Get(string key)
        {
            _fileLock.EnterReadLock();
            try
            {
                // Simulate database read delay
                Thread.Sleep(_readDelayMs);
                
                var lines = File.ReadAllLines(_filePath);
                var line = lines.FirstOrDefault(l => l.StartsWith($"{key}="));
                
                if (line != null)
                {
                    var parts = line.Split('=', 2);
                    if (parts.Length == 2)
                    {
                        Console.WriteLine($"[DB READ] {key} (took {_readDelayMs}ms)");
                        return parts[1];
                    }
                }
                
                Console.WriteLine($"[DB MISS] {key}");
                return null;
            }
            finally
            {
                _fileLock.ExitReadLock();
            }
        }

        public void Set(string key, string value)
        {
            _fileLock.EnterWriteLock();
            try
            {
                // Simulate database write delay
                Thread.Sleep(_writeDelayMs);
                
                var lines = File.Exists(_filePath) ? File.ReadAllLines(_filePath).ToList() : new List<string>();
                
                // Remove existing key if present
                lines.RemoveAll(l => l.StartsWith($"{key}="));
                
                // Add new key-value pair
                lines.Add($"{key}={value}");
                
                File.WriteAllLines(_filePath, lines);
                Console.WriteLine($"[DB WRITE] {key} = {value} (took {_writeDelayMs}ms)");
            }
            finally
            {
                _fileLock.ExitWriteLock();
            }
        }

        public bool Delete(string key)
        {
            _fileLock.EnterWriteLock();
            try
            {
                // Simulate database write delay
                Thread.Sleep(_writeDelayMs);
                
                var lines = File.ReadAllLines(_filePath).ToList();
                var initialCount = lines.Count;
                
                lines.RemoveAll(l => l.StartsWith($"{key}="));
                
                if (lines.Count < initialCount)
                {
                    File.WriteAllLines(_filePath, lines);
                    Console.WriteLine($"[DB DELETE] {key} (took {_writeDelayMs}ms)");
                    return true;
                }
                
                return false;
            }
            finally
            {
                _fileLock.ExitWriteLock();
            }
        }

        public Dictionary<string, string> GetAll()
        {
            _fileLock.EnterReadLock();
            try
            {
                var result = new Dictionary<string, string>();
                var lines = File.ReadAllLines(_filePath);
                
                foreach (var line in lines)
                {
                    var parts = line.Split('=', 2);
                    if (parts.Length == 2)
                    {
                        result[parts[0]] = parts[1];
                    }
                }
                
                return result;
            }
            finally
            {
                _fileLock.ExitReadLock();
            }
        }

        public int Count()
        {
            _fileLock.EnterReadLock();
            try
            {
                return File.ReadAllLines(_filePath).Length;
            }
            finally
            {
                _fileLock.ExitReadLock();
            }
        }
    }
}
