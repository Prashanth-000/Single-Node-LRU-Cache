using Single_Node_Cache.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace Single_Node_Cache.CLI
{
    internal class CacheConsole
    {
        private readonly SimpleCache _cache;
        private const int LeftPanelWidth = 60;
        private const int HeaderHeight = 5;
        private int _inputRow;
        private readonly List<string> _messages = new();
        private const int MaxMessages = 10;

        public CacheConsole(SimpleCache cache)
        {
            _cache = cache;
            _cache.CacheChanged += OnCacheChanged;
        }

        public void Run()
        {
            Console.Clear();
            Console.CursorVisible = false;
            
            try
            {
                InitializeUI();
                DrawStaticUI();
                UpdateCacheDisplay();

                while (true)
                {
                    DrawCommandPrompt();
                    var input = ReadInput();

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

                    case "EX":
                        HandleExpiry(parts);
                        break;

                    case "CLEAR":
                        _messages.Clear();
                        DrawMessagesPanel();
                        break;

                    case "EXIT":
                            Console.Clear();
                            Console.CursorVisible = true;
                            return;

                        default:
                            AddMessage($"Unknown command: {command}", ConsoleColor.Red);
                            break;
                    }

                    UpdateCacheDisplay();
                }
            }
            finally
            {
                Console.Clear();
                Console.CursorVisible = true;
            }
        }

        private void InitializeUI()
        {
            Console.SetWindowSize(Math.Min(120, Console.LargestWindowWidth), 
                                 Math.Min(40, Console.LargestWindowHeight));
            _inputRow = Console.WindowHeight - 4;
        }

        private void DrawStaticUI()
        {
            Console.Clear();
            
            // Draw header
            DrawBox(0, 0, Console.WindowWidth - 1, HeaderHeight, ConsoleColor.Cyan);
            
            // Center the heading
            string line1 = "+---------------------------------------+";
            string line2 = "¦   SIMPLE CACHE - Real-Time View     ¦";
            string line3 = "+---------------------------------------+";
            
            int centerPos1 = (Console.WindowWidth - line1.Length) / 2;
            int centerPos2 = (Console.WindowWidth - line2.Length) / 2;
            int centerPos3 = (Console.WindowWidth - line3.Length) / 2;
            
            Console.SetCursorPosition(Math.Max(0, centerPos1), 1);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(line1);
            Console.SetCursorPosition(Math.Max(0, centerPos2), 2);
            Console.Write(line2);
            Console.SetCursorPosition(Math.Max(0, centerPos3), 3);
            Console.Write(line3);
            Console.ResetColor();

            // Draw cache panel
            DrawBox(0, HeaderHeight, LeftPanelWidth, _inputRow - HeaderHeight, ConsoleColor.Blue);
            Console.SetCursorPosition(2, HeaderHeight);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("+- CACHE QUEUE (LRU Order) -+");
            Console.ResetColor();

            // Draw messages panel
            DrawBox(LeftPanelWidth, HeaderHeight, Console.WindowWidth - LeftPanelWidth - 1, _inputRow - HeaderHeight, ConsoleColor.Green);
            Console.SetCursorPosition(LeftPanelWidth + 2, HeaderHeight);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("+- MESSAGES -+");
            Console.ResetColor();

            // Draw command panel
            DrawBox(0, _inputRow, Console.WindowWidth - 1, Console.WindowHeight - _inputRow - 1, ConsoleColor.Magenta);
            Console.SetCursorPosition(2, _inputRow);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("+- COMMANDS: SET <key> <value> [ttl] | GET <key> | EX <key> | CLEAR | EXIT -+");
            Console.ResetColor();
        }

        private void UpdateCacheDisplay()
        {
            // Save current cursor position
            var cursorLeft = Console.CursorLeft;
            var cursorTop = Console.CursorTop;
            var cursorVisible = Console.CursorVisible;
            
            try
            {
                var state = _cache.GetCacheState();
                
                // Clear cache display area
                for (int i = 0; i < _inputRow - HeaderHeight - 2; i++)
                {
                    Console.SetCursorPosition(1, HeaderHeight + 1 + i);
                    Console.Write(new string(' ', LeftPanelWidth - 2));
                }

                // Draw capacity info
                Console.SetCursorPosition(2, HeaderHeight + 1);
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"Capacity: {state.count}/{state.capacity} ");
                
                // Draw capacity bar
                int barWidth = 30;
                int filled = (int)((double)state.count / state.capacity * barWidth);
                Console.Write("[");
                Console.ForegroundColor = state.count >= state.capacity ? ConsoleColor.Red : ConsoleColor.Green;
                Console.Write(new string('¦', filled));
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write(new string('¦', barWidth - filled));
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("]");
                Console.ResetColor();

                // Draw items
                int row = HeaderHeight + 3;
                for (int i = 0; i < state.items.Count && row < _inputRow - 1; i++)
                {
                    var item = state.items[i];
                    Console.SetCursorPosition(2, row);

                    // Position indicator
                    Console.ForegroundColor = i == 0 ? ConsoleColor.Yellow : ConsoleColor.DarkGray;
                    Console.Write($"[{i + 1}] ");

                    // Key
                    Console.ForegroundColor = item.isExpired ? ConsoleColor.Red : ConsoleColor.White;
                    Console.Write($"{item.key,-15}");

                    // Value
                    Console.ForegroundColor = ConsoleColor.Gray;
                    string valueStr = item.value?.ToString() ?? "null";
                    Console.Write($" ¦ {valueStr.Substring(0, Math.Min(15, valueStr.Length)),-15}");

                    // TTL info
                    if (item.expiryTime.HasValue)
                    {
                        var remaining = item.expiryTime.Value - DateTime.UtcNow;
                        if (remaining.TotalSeconds > 0)
                        {
                            Console.ForegroundColor = remaining.TotalSeconds < 5 ? ConsoleColor.Yellow : ConsoleColor.Green;
                            Console.Write($" ¦ {remaining.TotalSeconds:F1}s");
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write(" ¦ EXPIRED");
                        }
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.Write(" ¦ 8 NO TTL");
                    }

                    Console.ResetColor();
                    row++;
                }
            }
            catch (Exception ex)
            {
                // If display update fails, continue gracefully
                try
                {
                    Console.SetCursorPosition(2, HeaderHeight + 1);
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write($"Display error: {ex.Message}");
                    Console.ResetColor();
                }
                catch { /* Ignore if even error display fails */ }
            }
            finally
            {
                // Restore cursor position
                try
                {
                    Console.SetCursorPosition(cursorLeft, cursorTop);
                    Console.CursorVisible = cursorVisible;
                }
                catch { /* Ignore if cursor position invalid */ }
            }
        }

        private void DrawMessagesPanel()
        {
            // Clear messages area
            for (int i = 0; i < _inputRow - HeaderHeight - 1; i++)
            {
                Console.SetCursorPosition(LeftPanelWidth + 1, HeaderHeight + 1 + i);
                Console.Write(new string(' ', Console.WindowWidth - LeftPanelWidth - 3));
            }

            // Draw messages
            int startIndex = Math.Max(0, _messages.Count - MaxMessages);
            int row = HeaderHeight + 1;
            
            for (int i = startIndex; i < _messages.Count && row < _inputRow - 1; i++)
            {
                Console.SetCursorPosition(LeftPanelWidth + 2, row);
                Console.Write(_messages[i]);
                row++;
            }
        }

        private void AddMessage(string message, ConsoleColor color = ConsoleColor.White)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var colorCode = color switch
            {
                ConsoleColor.Green => "?",
                ConsoleColor.Red => "?",
                ConsoleColor.Yellow => "?",
                _ => "?"
            };

            var oldColor = Console.ForegroundColor;
            var formattedMessage = $"{timestamp} {colorCode} ";
            
            _messages.Add(formattedMessage);
            
            // Store original for redraw
            var currentPos = Console.GetCursorPosition();
            DrawMessagesPanel();
            
            Console.SetCursorPosition(LeftPanelWidth + 2, Math.Min(_inputRow - 2, HeaderHeight + 1 + (_messages.Count - 1)));
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(timestamp);
            Console.Write(" ");
            Console.ForegroundColor = color;
            Console.Write($"{colorCode} {message}");
            Console.ForegroundColor = oldColor;
        }

        private void DrawCommandPrompt()
        {
            Console.SetCursorPosition(2, _inputRow + 1);
            Console.Write(new string(' ', Console.WindowWidth - 4));
            Console.SetCursorPosition(2, _inputRow + 1);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("> ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.CursorVisible = true;
        }

        private string ReadInput()
        {
            var input = Console.ReadLine() ?? "";
            Console.CursorVisible = false;
            return input;
        }

        private void HandleSet(string[] parts)
        {
            if (parts.Length < 3)
            {
                AddMessage("Usage: SET <key> <value> [ttlSeconds]", ConsoleColor.Red);
                return;
            }

            string key = parts[1];
            string value = parts[2];

            if (parts.Length == 4 && int.TryParse(parts[3], out int ttl))
            {
                _cache.Set(key, value, TimeSpan.FromSeconds(ttl));
                AddMessage($"SET {key} = {value} (TTL: {ttl}s)", ConsoleColor.Green);
            }
            else
            {
                _cache.Set(key, value);
                AddMessage($"SET {key} = {value} (No TTL)", ConsoleColor.Green);
            }
        }

        private void HandleGet(string[] parts)
        {
            if (parts.Length != 2)
            {
                AddMessage("Usage: GET <key>", ConsoleColor.Red);
                return;
            }

            var result = _cache.Get(parts[1]);
            if (result != null)
            {
                AddMessage($"GET {parts[1]} = {result}", ConsoleColor.Green);
            }
            else
            {
                AddMessage($"GET {parts[1]} = (null)", ConsoleColor.Yellow);
            }
        }

        private void HandleExpiry(string[] parts)
        {
            if (parts.Length != 2)
            {
                AddMessage("Usage: EX <key>", ConsoleColor.Red);
                return;
            }

            var expiryInfo = _cache.GetExpiry(parts[1]);
            if (!expiryInfo.exists)
            {
                AddMessage($"EX {parts[1]} - Key not found", ConsoleColor.Yellow);
                return;
            }

            if (!expiryInfo.expiryTime.HasValue)
            {
                AddMessage($"EX {parts[1]} - No expiry (8 infinite TTL)", ConsoleColor.Cyan);
            }
            else
            {
                var remaining = expiryInfo.expiryTime.Value - DateTime.UtcNow;
                if (remaining.TotalSeconds > 0)
                {
                    var color = remaining.TotalSeconds < 5 ? ConsoleColor.Yellow : ConsoleColor.Green;
                    AddMessage($"EX {parts[1]} - Expires in {remaining.TotalSeconds:F1}s (at {expiryInfo.expiryTime.Value.ToLocalTime():HH:mm:ss})", color);
                }
                else
                {
                    AddMessage($"EX {parts[1]} - EXPIRED (will be cleaned up soon)", ConsoleColor.Red);
                }
            }
        }

        private void OnCacheChanged()
        {
            UpdateCacheDisplay();
        }

        private void DrawBox(int left, int top, int width, int height, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            
            // Top border
            Console.SetCursorPosition(left, top);
            Console.Write("+" + new string('-', width - 2) + "+");
            
            // Sides
            for (int i = 1; i < height - 1; i++)
            {
                Console.SetCursorPosition(left, top + i);
                Console.Write("¦");
                Console.SetCursorPosition(left + width - 1, top + i);
                Console.Write("¦");
            }
            
            // Bottom border
            Console.SetCursorPosition(left, top + height - 1);
            Console.Write("+" + new string('-', width - 2) + "+");
            
            Console.ResetColor();
        }
    }
}
