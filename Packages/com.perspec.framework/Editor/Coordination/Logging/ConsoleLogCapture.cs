using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Linq;
using UnityEngine;
using UnityEditor;
using SQLite;

namespace PerSpec.Editor.Coordination
{
    /// <summary>
    /// Captures Unity console logs in real-time and stores them in SQLite with intelligent stack trace truncation
    /// </summary>
    [InitializeOnLoad]
    public static class ConsoleLogCapture
    {
        private static readonly string SessionId = Guid.NewGuid().ToString();
        private static SQLiteManager _dbManager;
        private static readonly Queue<ConsoleLogEntry> _logQueue = new Queue<ConsoleLogEntry>();
        private static readonly object _queueLock = new object();
        private static bool _isCapturing = true;
        private static int _maxStackFrames = 10;
        private static int _maxLineLength = 200;
        
        // Patterns to filter out from stack traces
        private static readonly string[] FrameworkPatterns = new[]
        {
            @"^UnityEngine\.",
            @"^UnityEditor\.",
            @"^System\.Reflection",
            @"^System\.Runtime",
            @"^Unity\.Collections",
            @"^Mono\.Runtime",
            @"^\(wrapper",
            @"^UnityEditor\.TestTools",
            @"^NUnit\.Framework",
            @"UnityEngine\.TestRunner",
            @"UnityEngine\.Debug:Log",
            @"UnityEngine\.Logger:Log",
            @"UnityEngine\.DebugLogHandler",
            @"TestCoordination\.ConsoleLogCapture"
        };
        
        // Unity source file patterns to preserve
        private static readonly string[] ImportantPatterns = new[]
        {
            @"Assets/",
            @"Packages/",
            @"\[Test\]",
            @"Test\.cs",
            @"Tests\.cs"
        };
        
        static ConsoleLogCapture()
        {
            try
            {
                _dbManager = new SQLiteManager();
                
                // Subscribe to Unity's thread-safe log handler
                Application.logMessageReceivedThreaded += OnLogMessageReceived;
                
                // Subscribe to editor update for batch processing
                EditorApplication.update += ProcessLogQueue;
                
                // Log session start
                Debug.Log($"[ConsoleLogCapture] Started capture session: {SessionId}");
                
                // Clear old logs on startup (optional, configurable)
                CleanOldLogs(24); // Keep last 24 hours
            }
            catch (Exception e)
            {
                Debug.LogError($"[ConsoleLogCapture] Failed to initialize: {e.Message}");
            }
        }
        
        private static void OnLogMessageReceived(string message, string stackTrace, LogType logType)
        {
            if (!_isCapturing) return;
            
            try
            {
                var entry = new ConsoleLogEntry
                {
                    SessionId = SessionId,
                    LogLevel = ConvertLogType(logType),
                    Message = message,
                    StackTrace = stackTrace,
                    Timestamp = DateTime.Now
                };
                
                // Truncate stack trace intelligently
                TruncateStackTrace(entry);
                
                // Extract source location if available
                ExtractSourceLocation(entry);
                
                // Queue for batch processing
                lock (_queueLock)
                {
                    _logQueue.Enqueue(entry);
                    
                    // Limit queue size to prevent memory issues
                    while (_logQueue.Count > 1000)
                    {
                        _logQueue.Dequeue();
                    }
                }
            }
            catch (Exception e)
            {
                // Don't log errors about logging to avoid recursion
                System.Diagnostics.Debug.WriteLine($"[ConsoleLogCapture] Error capturing log: {e.Message}");
            }
        }
        
        private static void ProcessLogQueue()
        {
            if (_logQueue.Count == 0) return;
            
            List<ConsoleLogEntry> logsToProcess = null;
            
            lock (_queueLock)
            {
                if (_logQueue.Count > 0)
                {
                    logsToProcess = new List<ConsoleLogEntry>(_logQueue);
                    _logQueue.Clear();
                }
            }
            
            if (logsToProcess != null && logsToProcess.Count > 0)
            {
                try
                {
                    foreach (var log in logsToProcess)
                    {
                        _dbManager.SaveConsoleLog(log);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ConsoleLogCapture] Failed to save logs: {e.Message}");
                }
            }
        }
        
        private static void TruncateStackTrace(ConsoleLogEntry entry)
        {
            if (string.IsNullOrEmpty(entry.StackTrace))
            {
                entry.IsTruncated = false;
                return;
            }
            
            var lines = entry.StackTrace.Split('\n');
            var truncatedLines = new List<string>();
            var userCodeFound = false;
            var frameworkLinesSkipped = 0;
            
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                // Skip empty lines
                if (string.IsNullOrWhiteSpace(trimmedLine))
                    continue;
                
                // Check if this is a framework line
                bool isFramework = FrameworkPatterns.Any(pattern => 
                    Regex.IsMatch(trimmedLine, pattern, RegexOptions.IgnoreCase));
                
                // Check if this is important user code
                bool isImportant = ImportantPatterns.Any(pattern => 
                    trimmedLine.Contains(pattern, StringComparison.OrdinalIgnoreCase));
                
                if (isImportant || !isFramework)
                {
                    userCodeFound = true;
                    
                    // Add skipped count if we skipped framework lines
                    if (frameworkLinesSkipped > 0)
                    {
                        truncatedLines.Add($"  ... [{frameworkLinesSkipped} framework calls omitted] ...");
                        frameworkLinesSkipped = 0;
                    }
                    
                    // Truncate long lines
                    string processedLine = trimmedLine;
                    if (trimmedLine.Length > _maxLineLength)
                    {
                        processedLine = trimmedLine.Substring(0, _maxLineLength) + "...";
                    }
                    
                    // Convert absolute paths to relative
                    processedLine = ConvertToRelativePath(processedLine);
                    
                    truncatedLines.Add(processedLine);
                    
                    // Stop after max frames of user code
                    if (truncatedLines.Count >= _maxStackFrames)
                    {
                        entry.IsTruncated = true;
                        truncatedLines.Add($"  ... [{lines.Length - Array.IndexOf(lines, line) - 1} more frames] ...");
                        break;
                    }
                }
                else if (userCodeFound)
                {
                    // Count framework lines after user code
                    frameworkLinesSkipped++;
                }
                else if (truncatedLines.Count == 0 && Array.IndexOf(lines, line) < 3)
                {
                    // Keep first few lines even if framework (for context)
                    truncatedLines.Add(ConvertToRelativePath(trimmedLine));
                }
            }
            
            // If no user code found, keep first few framework lines
            if (!userCodeFound && truncatedLines.Count == 0 && lines.Length > 0)
            {
                for (int i = 0; i < Math.Min(3, lines.Length); i++)
                {
                    truncatedLines.Add(ConvertToRelativePath(lines[i].Trim()));
                }
                
                if (lines.Length > 3)
                {
                    truncatedLines.Add($"  ... [{lines.Length - 3} more framework frames] ...");
                }
            }
            
            entry.TruncatedStack = string.Join("\n", truncatedLines);
            entry.FrameCount = lines.Length;
        }
        
        private static string ConvertToRelativePath(string line)
        {
            // Convert absolute paths to relative for readability
            var pattern = @"([A-Z]:[\\/].*?[\\/])(Assets[\\/].+?\.cs)";
            var match = Regex.Match(line, pattern);
            
            if (match.Success)
            {
                return line.Replace(match.Groups[1].Value, "");
            }
            
            // Handle Unity package paths
            pattern = @"(.*?[\\/]Library[\\/]PackageCache[\\/])(.+?\.cs)";
            match = Regex.Match(line, pattern);
            
            if (match.Success)
            {
                return line.Replace(match.Groups[1].Value, "Packages/");
            }
            
            return line;
        }
        
        private static void ExtractSourceLocation(ConsoleLogEntry entry)
        {
            if (string.IsNullOrEmpty(entry.StackTrace))
                return;
            
            // Extract file and line from stack trace
            // Pattern: (at Assets/Scripts/File.cs:123)
            var pattern = @"\(at (.+?):(\d+)\)";
            var match = Regex.Match(entry.StackTrace, pattern);
            
            if (match.Success)
            {
                entry.SourceFile = match.Groups[1].Value;
                if (int.TryParse(match.Groups[2].Value, out int line))
                {
                    entry.SourceLine = line;
                }
            }
        }
        
        private static string ConvertLogType(LogType logType)
        {
            switch (logType)
            {
                case LogType.Log: return "Info";
                case LogType.Warning: return "Warning";
                case LogType.Error: return "Error";
                case LogType.Exception: return "Exception";
                case LogType.Assert: return "Assert";
                default: return "Info";
            }
        }
        
        private static void CleanOldLogs(int hoursToKeep)
        {
            try
            {
                var cutoffTime = DateTime.Now.AddHours(-hoursToKeep);
                _dbManager.DeleteOldConsoleLogs(cutoffTime);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ConsoleLogCapture] Failed to clean old logs: {e.Message}");
            }
        }
        
        // Menu items
        [MenuItem("Tools/PerSpec/Console Logs/Toggle Capture", priority = 200)]
        public static void ToggleCapture()
        {
            _isCapturing = !_isCapturing;
            Debug.Log($"[ConsoleLogCapture] Capture {(_isCapturing ? "ENABLED" : "DISABLED")}");
        }
        
        [MenuItem("Tools/PerSpec/Console Logs/Clear Session", priority = 201)]
        public static void ClearCurrentSession()
        {
            try
            {
                _dbManager.DeleteSessionLogs(SessionId);
                Debug.Log($"[ConsoleLogCapture] Cleared logs for session: {SessionId}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[ConsoleLogCapture] Failed to clear session: {e.Message}");
            }
        }
        
        [MenuItem("Tools/PerSpec/Console Logs/Show Session Info", priority = 202)]
        public static void ShowSessionInfo()
        {
            Debug.Log($"[ConsoleLogCapture] Session ID: {SessionId}");
            Debug.Log($"  Capture: {(_isCapturing ? "ENABLED" : "DISABLED")}");
            Debug.Log($"  Queue Size: {_logQueue.Count}");
            Debug.Log($"  Max Stack Frames: {_maxStackFrames}");
            Debug.Log($"  Max Line Length: {_maxLineLength}");
        }
        
        [MenuItem("Tools/PerSpec/Debug/Test Log Levels", priority = 510)]
        public static void TestLogLevels()
        {
            Debug.Log("[TEST] This is an info message");
            Debug.LogWarning("[TEST] This is a warning message");
            Debug.LogError("[TEST] This is an error message");
            
            try
            {
                throw new Exception("This is a test exception with stack trace");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
    
    [Table("console_logs")]
    public class ConsoleLogEntry
    {
        [PrimaryKey, AutoIncrement, Column("id")]
        public int Id { get; set; }
        
        [Column("session_id")]
        public string SessionId { get; set; }
        
        [Column("log_level")]
        public string LogLevel { get; set; }
        
        [Column("message")]
        public string Message { get; set; }
        
        [Column("stack_trace")]
        public string StackTrace { get; set; }
        
        [Column("truncated_stack")]
        public string TruncatedStack { get; set; }
        
        [Column("source_file")]
        public string SourceFile { get; set; }
        
        [Column("source_line")]
        public int? SourceLine { get; set; }
        
        [Column("timestamp")]
        public DateTime Timestamp { get; set; }
        
        [Column("frame_count")]
        public int FrameCount { get; set; }
        
        [Column("is_truncated")]
        public bool IsTruncated { get; set; }
        
        [Column("context")]
        public string Context { get; set; }
        
        [Column("request_id")]
        public int? RequestId { get; set; }
    }
}