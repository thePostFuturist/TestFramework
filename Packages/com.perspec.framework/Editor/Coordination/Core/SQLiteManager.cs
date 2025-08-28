using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using SQLite;

namespace PerSpec.Editor.Coordination
{
    [Table("test_requests")]
    public class TestRequest
    {
        [PrimaryKey, AutoIncrement, Column("id")]
        public int Id { get; set; }
        
        [Column("request_type")]
        public string RequestType { get; set; }
        
        [Column("test_filter")]
        public string TestFilter { get; set; }
        
        [Column("test_platform")]
        public string TestPlatform { get; set; }
        
        [Column("status")]
        public string Status { get; set; }
        
        [Column("priority")]
        public int Priority { get; set; }
        
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        
        [Column("started_at")]
        public DateTime? StartedAt { get; set; }
        
        [Column("completed_at")]
        public DateTime? CompletedAt { get; set; }
        
        [Column("error_message")]
        public string ErrorMessage { get; set; }
        
        [Column("total_tests")]
        public int TotalTests { get; set; }
        
        [Column("passed_tests")]
        public int PassedTests { get; set; }
        
        [Column("failed_tests")]
        public int FailedTests { get; set; }
        
        [Column("skipped_tests")]
        public int SkippedTests { get; set; }
        
        [Column("duration_seconds")]
        public float DurationSeconds { get; set; }
    }
    
    [Table("test_results")]
    public class TestResultRecord
    {
        [PrimaryKey, AutoIncrement, Column("id")]
        public int Id { get; set; }
        
        [Column("request_id")]
        public int RequestId { get; set; }
        
        [Column("test_name")]
        public string TestName { get; set; }
        
        [Column("test_class")]
        public string TestClass { get; set; }
        
        [Column("test_method")]
        public string TestMethod { get; set; }
        
        [Column("result")]
        public string Result { get; set; }
        
        [Column("duration_ms")]
        public float DurationMs { get; set; }
        
        [Column("error_message")]
        public string ErrorMessage { get; set; }
        
        [Column("stack_trace")]
        public string StackTrace { get; set; }
        
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
    
    [Table("execution_log")]
    public class ExecutionLog
    {
        [PrimaryKey, AutoIncrement, Column("id")]
        public int Id { get; set; }
        
        [Column("request_id")]
        public int? RequestId { get; set; }
        
        [Column("log_level")]
        public string LogLevel { get; set; }
        
        [Column("source")]
        public string Source { get; set; }
        
        [Column("message")]
        public string Message { get; set; }
        
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
    
    [Table("system_status")]
    public class SystemStatus
    {
        [PrimaryKey, AutoIncrement, Column("id")]
        public int Id { get; set; }
        
        [Column("component")]
        public string Component { get; set; }
        
        [Column("status")]
        public string Status { get; set; }
        
        [Column("last_heartbeat")]
        public DateTime LastHeartbeat { get; set; }
        
        [Column("message")]
        public string Message { get; set; }
        
        [Column("metadata")]
        public string Metadata { get; set; }
    }
    
    public class TestResultSummary
    {
        public int TotalTests { get; set; }
        public int PassedTests { get; set; }
        public int FailedTests { get; set; }
        public int SkippedTests { get; set; }
        public float Duration { get; set; }
    }
    
    [Table("asset_refresh_requests")]
    public class AssetRefreshRequest
    {
        [PrimaryKey, AutoIncrement, Column("id")]
        public int Id { get; set; }
        
        [Column("refresh_type")]
        public string RefreshType { get; set; }
        
        [Column("paths")]
        public string Paths { get; set; }
        
        [Column("import_options")]
        public string ImportOptions { get; set; }
        
        [Column("status")]
        public string Status { get; set; }
        
        [Column("priority")]
        public int Priority { get; set; }
        
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        
        [Column("started_at")]
        public DateTime? StartedAt { get; set; }
        
        [Column("completed_at")]
        public DateTime? CompletedAt { get; set; }
        
        [Column("duration_seconds")]
        public float DurationSeconds { get; set; }
        
        [Column("result_message")]
        public string ResultMessage { get; set; }
        
        [Column("error_message")]
        public string ErrorMessage { get; set; }
    }
    
    public class SQLiteManager
    {
        private readonly string _dbPath;
        private readonly SQLiteConnection _connection;
        
        public SQLiteManager()
        {
            // Get path to PerSpec folder in project root
            string projectPath = Directory.GetParent(Application.dataPath).FullName;
            string perspecPath = Path.Combine(projectPath, "PerSpec");
            _dbPath = Path.Combine(perspecPath, "test_coordination.db");
            
            // Check if PerSpec is initialized
            if (!Directory.Exists(perspecPath))
            {
                Debug.LogError($"[SQLiteManager] PerSpec not initialized. Please run Tools > PerSpec > Initialize PerSpec");
                throw new DirectoryNotFoundException($"PerSpec directory not found at: {perspecPath}. Please initialize PerSpec first.");
            }
            
            // Database will be created by Python scripts if it doesn't exist yet
            if (!File.Exists(_dbPath))
            {
                Debug.LogWarning($"[SQLiteManager] Database not found at: {_dbPath}. It will be created on first use.");
                // Don't throw here - let Python scripts create it
            }
            
            try
            {
                _connection = new SQLiteConnection(_dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.FullMutex);
                _connection.BusyTimeout = TimeSpan.FromSeconds(5);
                Debug.Log($"[SQLiteManager] Connected to database at: {_dbPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SQLiteManager] Failed to connect to database: {e.Message}");
                throw;
            }
        }
        
        public TestRequest GetNextPendingRequest()
        {
            try
            {
                var query = _connection.Table<TestRequest>()
                    .Where(r => r.Status == "pending")
                    .OrderByDescending(r => r.Priority)
                    .ThenBy(r => r.CreatedAt)
                    .FirstOrDefault();
                
                return query;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SQLiteManager] Error getting pending request: {e.Message}");
                return null;
            }
        }
        
        public void UpdateRequestStatus(int requestId, string status, string errorMessage = null)
        {
            try
            {
                var request = _connection.Table<TestRequest>().FirstOrDefault(r => r.Id == requestId);
                
                if (request != null)
                {
                    request.Status = status;
                    
                    if (status == "running")
                    {
                        request.StartedAt = DateTime.Now;
                    }
                    else if (status == "completed" || status == "failed" || status == "cancelled")
                    {
                        request.CompletedAt = DateTime.Now;
                        request.ErrorMessage = errorMessage;
                    }
                    
                    _connection.Update(request);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SQLiteManager] Error updating request status: {e.Message}");
            }
        }
        
        public void UpdateRequestResults(int requestId, string status, int totalTests, int passedTests, 
                                        int failedTests, int skippedTests, float duration)
        {
            try
            {
                var request = _connection.Table<TestRequest>().FirstOrDefault(r => r.Id == requestId);
                
                if (request != null)
                {
                    request.Status = status;
                    request.CompletedAt = DateTime.Now;
                    request.TotalTests = totalTests;
                    request.PassedTests = passedTests;
                    request.FailedTests = failedTests;
                    request.SkippedTests = skippedTests;
                    request.DurationSeconds = duration;
                    
                    _connection.Update(request);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SQLiteManager] Error updating request results: {e.Message}");
            }
        }
        
        public void InsertTestResult(int requestId, string testName, string testClass, string testMethod,
                                    string result, float durationMs, string errorMessage = null, string stackTrace = null)
        {
            try
            {
                var testResult = new TestResultRecord
                {
                    RequestId = requestId,
                    TestName = testName,
                    TestClass = testClass,
                    TestMethod = testMethod,
                    Result = result,
                    DurationMs = durationMs,
                    ErrorMessage = errorMessage,
                    StackTrace = stackTrace,
                    CreatedAt = DateTime.Now
                };
                
                _connection.Insert(testResult);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SQLiteManager] Error inserting test result: {e.Message}");
            }
        }
        
        public void LogExecution(int? requestId, string logLevel, string source, string message)
        {
            try
            {
                var log = new ExecutionLog
                {
                    RequestId = requestId,
                    LogLevel = logLevel,
                    Source = source,
                    Message = message,
                    CreatedAt = DateTime.Now
                };
                
                _connection.Insert(log);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SQLiteManager] Failed to write log: {e.Message}");
            }
        }
        
        public void UpdateSystemHeartbeat(string component)
        {
            try
            {
                var status = _connection.Table<SystemStatus>()
                    .FirstOrDefault(s => s.Component == component);
                
                if (status != null)
                {
                    status.Status = "online";
                    status.LastHeartbeat = DateTime.Now;
                    status.Message = "Active";
                    _connection.Update(status);
                }
                else
                {
                    _connection.Insert(new SystemStatus
                    {
                        Component = component,
                        Status = "online",
                        LastHeartbeat = DateTime.Now,
                        Message = "Active"
                    });
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SQLiteManager] Failed to update heartbeat: {e.Message}");
            }
        }
        
        public string GetSystemStatus()
        {
            var status = new System.Text.StringBuilder();
            
            try
            {
                // Get pending requests count
                int pendingCount = _connection.Table<TestRequest>()
                    .Where(r => r.Status == "pending")
                    .Count();
                status.AppendLine($"Pending Requests: {pendingCount}");
                
                // Get running requests count
                int runningCount = _connection.Table<TestRequest>()
                    .Where(r => r.Status == "running")
                    .Count();
                status.AppendLine($"Running Requests: {runningCount}");
                
                // Get system components status
                var components = _connection.Table<SystemStatus>()
                    .OrderBy(s => s.Component)
                    .ToList();
                
                status.AppendLine("\nComponent Status:");
                foreach (var comp in components)
                {
                    status.AppendLine($"  {comp.Component}: {comp.Status} (Last: {comp.LastHeartbeat})");
                }
            }
            catch (Exception e)
            {
                status.AppendLine($"Error getting status: {e.Message}");
            }
            
            return status.ToString();
        }
        
        public List<TestRequest> GetAllPendingRequests()
        {
            try
            {
                return _connection.Table<TestRequest>()
                    .Where(r => r.Status == "pending")
                    .OrderByDescending(r => r.Priority)
                    .ThenBy(r => r.CreatedAt)
                    .ToList();
            }
            catch (Exception e)
            {
                Debug.LogError($"[SQLiteManager] Error getting pending requests: {e.Message}");
                return new List<TestRequest>();
            }
        }
        
        public List<TestRequest> GetRunningRequests()
        {
            try
            {
                return _connection.Table<TestRequest>()
                    .Where(r => r.Status == "running")
                    .ToList();
            }
            catch (Exception e)
            {
                Debug.LogError($"[SQLiteManager] Error getting running requests: {e.Message}");
                return new List<TestRequest>();
            }
        }
        
        public TestRequest GetRequestById(int id)
        {
            try
            {
                return _connection.Table<TestRequest>()
                    .FirstOrDefault(r => r.Id == id);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SQLiteManager] Error getting request by id: {e.Message}");
                return null;
            }
        }
        
        // Asset Refresh Methods
        public AssetRefreshRequest GetNextPendingRefreshRequest()
        {
            try
            {
                var query = _connection.Table<AssetRefreshRequest>()
                    .Where(r => r.Status == "pending")
                    .OrderByDescending(r => r.Priority)
                    .ThenBy(r => r.CreatedAt)
                    .FirstOrDefault();
                
                return query;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SQLiteManager] Error getting pending refresh request: {e.Message}");
                return null;
            }
        }
        
        public void UpdateRefreshRequestStatus(int requestId, string status, string resultMessage = null, string errorMessage = null)
        {
            try
            {
                var request = _connection.Table<AssetRefreshRequest>().FirstOrDefault(r => r.Id == requestId);
                
                if (request != null)
                {
                    request.Status = status;
                    
                    if (status == "running")
                    {
                        request.StartedAt = DateTime.Now;
                    }
                    else if (status == "completed" || status == "failed" || status == "cancelled")
                    {
                        request.CompletedAt = DateTime.Now;
                        if (request.StartedAt.HasValue)
                        {
                            request.DurationSeconds = (float)(DateTime.Now - request.StartedAt.Value).TotalSeconds;
                        }
                        request.ResultMessage = resultMessage;
                        request.ErrorMessage = errorMessage;
                    }
                    
                    _connection.Update(request);
                    Debug.Log($"[SQLiteManager] Updated refresh request {requestId} status to {status}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SQLiteManager] Error updating refresh request status: {e.Message}");
            }
        }
        
        public List<AssetRefreshRequest> GetPendingRefreshRequests()
        {
            try
            {
                return _connection.Table<AssetRefreshRequest>()
                    .Where(r => r.Status == "pending")
                    .OrderByDescending(r => r.Priority)
                    .ThenBy(r => r.CreatedAt)
                    .ToList();
            }
            catch (Exception e)
            {
                Debug.LogError($"[SQLiteManager] Error getting pending refresh requests: {e.Message}");
                return new List<AssetRefreshRequest>();
            }
        }
        
        public List<AssetRefreshRequest> GetRunningRefreshRequests()
        {
            try
            {
                return _connection.Table<AssetRefreshRequest>()
                    .Where(r => r.Status == "running")
                    .ToList();
            }
            catch (Exception e)
            {
                Debug.LogError($"[SQLiteManager] Error getting running refresh requests: {e.Message}");
                return new List<AssetRefreshRequest>();
            }
        }
        
        // Console Log Methods
        public void SaveConsoleLog(ConsoleLogEntry logEntry)
        {
            try
            {
                _connection.Insert(logEntry);
            }
            catch (Exception e)
            {
                // Don't use Debug.LogError here to avoid recursion
                System.Diagnostics.Debug.WriteLine($"[SQLiteManager] Error saving console log: {e.Message}");
            }
        }
        
        public void DeleteSessionLogs(string sessionId)
        {
            try
            {
                _connection.Execute("DELETE FROM console_logs WHERE session_id = ?", sessionId);
                Debug.Log($"[SQLiteManager] Deleted logs for session: {sessionId}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SQLiteManager] Error deleting session logs: {e.Message}");
            }
        }
        
        public void DeleteOldConsoleLogs(DateTime cutoffTime)
        {
            try
            {
                _connection.Execute("DELETE FROM console_logs WHERE timestamp < ?", cutoffTime);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SQLiteManager] Error deleting old console logs: {e.Message}");
            }
        }
        
        public List<ConsoleLogEntry> GetConsoleLogs(string sessionId = null, string logLevel = null, int limit = 100)
        {
            try
            {
                var query = _connection.Table<ConsoleLogEntry>();
                
                if (!string.IsNullOrEmpty(sessionId))
                {
                    query = query.Where(l => l.SessionId == sessionId);
                }
                
                if (!string.IsNullOrEmpty(logLevel))
                {
                    query = query.Where(l => l.LogLevel == logLevel);
                }
                
                return query.OrderByDescending(l => l.Timestamp)
                    .Take(limit)
                    .ToList();
            }
            catch (Exception e)
            {
                Debug.LogError($"[SQLiteManager] Error getting console logs: {e.Message}");
                return new List<ConsoleLogEntry>();
            }
        }
        
        public int GetConsoleLogCount(string sessionId = null, string logLevel = null)
        {
            try
            {
                var query = _connection.Table<ConsoleLogEntry>();
                
                if (!string.IsNullOrEmpty(sessionId))
                {
                    query = query.Where(l => l.SessionId == sessionId);
                }
                
                if (!string.IsNullOrEmpty(logLevel))
                {
                    query = query.Where(l => l.LogLevel == logLevel);
                }
                
                return query.Count();
            }
            catch (Exception e)
            {
                Debug.LogError($"[SQLiteManager] Error getting console log count: {e.Message}");
                return 0;
            }
        }
        
        ~SQLiteManager()
        {
            try
            {
                _connection?.Close();
                _connection?.Dispose();
            }
            catch
            {
                // Ignore disposal errors
            }
        }
    }
}