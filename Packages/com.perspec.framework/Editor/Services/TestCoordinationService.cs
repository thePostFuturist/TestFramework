using System;
using UnityEngine;
using UnityEditor;
using PerSpec.Editor.Coordination;

namespace PerSpec.Editor.Services
{
    /// <summary>
    /// Service for managing test coordination and execution
    /// </summary>
    public static class TestCoordinationService
    {
        #region Fields
        
        private static SQLiteManager _dbManager;
        private static TestExecutor _testExecutor;
        private static bool _isRunningTests = false;
        private static int _currentRequestId = -1;
        private static bool _pollingEnabled = true;
        
        #endregion
        
        #region Properties
        
        public static bool IsRunningTests => _isRunningTests;
        public static int CurrentRequestId => _currentRequestId;
        public static bool PollingEnabled 
        { 
            get => _pollingEnabled;
            set
            {
                _pollingEnabled = value;
                if (value)
                    BackgroundPoller.EnableBackgroundPolling();
                else
                    BackgroundPoller.DisableBackgroundPolling();
            }
        }
        
        public static bool IsDatabaseConnected => _dbManager != null;
        
        #endregion
        
        #region Initialization
        
        static TestCoordinationService()
        {
            Initialize();
        }
        
        public static void Initialize()
        {
            try
            {
                _dbManager = new SQLiteManager();
                _testExecutor = new TestExecutor(_dbManager);
                Debug.Log("[TestCoordination] Service initialized");
            }
            catch (Exception e)
            {
                Debug.LogError($"[TestCoordination] Failed to initialize: {e.Message}");
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Check for pending test requests
        /// </summary>
        public static bool CheckPendingTests()
        {
            if (!IsDatabaseConnected || _isRunningTests)
                return false;
                
            try
            {
                var request = _dbManager.GetNextPendingRequest();
                if (request != null)
                {
                    ProcessTestRequest(request);
                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[TestCoordination] Error checking pending tests: {e.Message}");
            }
            
            return false;
        }
        
        /// <summary>
        /// Cancel the current test
        /// </summary>
        public static bool CancelCurrentTest()
        {
            if (_isRunningTests && _currentRequestId > 0)
            {
                _dbManager.UpdateRequestStatus(_currentRequestId, "cancelled", "Cancelled by user");
                _isRunningTests = false;
                _currentRequestId = -1;
                Debug.Log($"[TestCoordination] Cancelled test request {_currentRequestId}");
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Get database status
        /// </summary>
        public static string GetDatabaseStatus()
        {
            if (!IsDatabaseConnected)
                return "Database not connected";
                
            try
            {
                return _dbManager.GetSystemStatus();
            }
            catch (Exception e)
            {
                return $"Error: {e.Message}";
            }
        }
        
        /// <summary>
        /// Get current status summary
        /// </summary>
        public static string GetStatusSummary()
        {
            if (_isRunningTests)
                return $"Running test #{_currentRequestId}";
            
            return PollingEnabled ? "Idle (polling enabled)" : "Idle (polling disabled)";
        }
        
        /// <summary>
        /// Get pending test count
        /// </summary>
        public static int GetPendingTestCount()
        {
            if (!IsDatabaseConnected)
                return 0;
                
            try
            {
                // This would need to be added to SQLiteManager
                return 0; // Placeholder
            }
            catch
            {
                return 0;
            }
        }
        
        /// <summary>
        /// Force script compilation
        /// </summary>
        public static void ForceScriptCompilation()
        {
            UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
            Debug.Log("[TestCoordination] Script compilation requested");
        }
        
        #endregion
        
        #region Private Methods
        
        private static void ProcessTestRequest(TestRequest request)
        {
            _isRunningTests = true;
            _currentRequestId = request.Id;
            
            try
            {
                // Update status
                _dbManager.UpdateRequestStatus(request.Id, "running");
                
                // Create filter
                var filter = CreateTestFilter(request);
                
                // Execute tests
                _testExecutor.ExecuteTests(request, filter, OnTestComplete);
                
                Debug.Log($"[TestCoordination] Executing tests for request {request.Id}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[TestCoordination] Error processing request: {e.Message}");
                _dbManager.UpdateRequestStatus(request.Id, "failed", e.Message);
                _isRunningTests = false;
                _currentRequestId = -1;
            }
        }
        
        private static UnityEditor.TestTools.TestRunner.Api.Filter CreateTestFilter(TestRequest request)
        {
            var filter = new UnityEditor.TestTools.TestRunner.Api.Filter();
            
            // Set test mode
            if (request.TestPlatform == "EditMode")
                filter.testMode = UnityEditor.TestTools.TestRunner.Api.TestMode.EditMode;
            else if (request.TestPlatform == "PlayMode")
                filter.testMode = UnityEditor.TestTools.TestRunner.Api.TestMode.PlayMode;
            
            // Apply filters
            if (!string.IsNullOrEmpty(request.TestFilter))
            {
                filter.testNames = new[] { request.TestFilter };
            }
            
            return filter;
        }
        
        private static void OnTestComplete(TestRequest request, bool success, string error, TestResultSummary summary)
        {
            if (success && summary != null)
            {
                _dbManager.UpdateRequestResults(
                    request.Id,
                    "completed",
                    summary.TotalTests,
                    summary.PassedTests,
                    summary.FailedTests,
                    summary.SkippedTests,
                    summary.Duration
                );
            }
            else
            {
                _dbManager.UpdateRequestStatus(request.Id, "failed", error);
            }
            
            _isRunningTests = false;
            _currentRequestId = -1;
        }
        
        #endregion
    }
}