using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine.TestTools;
using UnityEditor.TestTools.TestRunner.Api;
using System.Collections;

namespace PerSpec.Editor.Coordination
{
    [InitializeOnLoad]
    public static class TestCoordinatorEditor
    {
        private static double _lastCheckTime;
        private static double _checkInterval = 1.0; // Check every 1 second
        private static bool _isRunningTests = false;
        private static SQLiteManager _dbManager;
        private static TestExecutor _testExecutor;
        private static int _currentRequestId = -1;
        
        // Background processing support
        private static SynchronizationContext _unitySyncContext;
        private static System.Threading.Timer _backgroundTimer;
        private static bool _useBackgroundPolling = true;
        private static DateTime _lastBackgroundPoll;
        
        static TestCoordinatorEditor()
        {
            Debug.Log("[TestCoordinator] Initializing test coordination system");
            
            // Capture Unity's sync context for thread marshalling
            _unitySyncContext = SynchronizationContext.Current;
            
            _dbManager = new SQLiteManager();
            _testExecutor = new TestExecutor(_dbManager);
            
            EditorApplication.update += OnEditorUpdate;
            
            // Initialize last check time
            _lastCheckTime = EditorApplication.timeSinceStartup;
            
            // Set up background polling if enabled
            if (_useBackgroundPolling)
            {
                SetupBackgroundPolling();
            }
            
            // Force Unity to run in background
            Application.runInBackground = true;
            
            // Update system heartbeat
            _dbManager.UpdateSystemHeartbeat("Unity");
            
            Debug.Log("[TestCoordinator] Test coordination system initialized");
        }
        
        private static void SetupBackgroundPolling()
        {
            _backgroundTimer = new System.Threading.Timer(
                BackgroundPollCallback,
                null,
                TimeSpan.FromSeconds(2), // Initial delay
                TimeSpan.FromSeconds(1)  // Repeat every second
            );
            
            Debug.Log("[TestCoordinator] Background polling enabled");
        }
        
        private static void BackgroundPollCallback(object state)
        {
            // Skip if already running tests
            if (_isRunningTests)
                return;
            
            try
            {
                // Check database from background thread (thread-safe)
                var request = _dbManager.GetNextPendingRequest();
                
                if (request != null)
                {
                    _lastBackgroundPoll = DateTime.Now;
                    Debug.Log($"[TestCoordinator-BG] Found pending test request #{request.Id}");
                    
                    // Marshal back to Unity main thread
                    _unitySyncContext?.Post(_ =>
                    {
                        if (!_isRunningTests && request != null)
                        {
                            CheckForPendingRequests();
                            // Force compilation to ensure Unity processes
                            CompilationPipeline.RequestScriptCompilation();
                        }
                    }, null);
                }
            }
            catch (Exception e)
            {
                // Log but don't crash the background thread
                UnityEngine.Debug.LogError($"[TestCoordinator-BG] Error: {e.Message}");
            }
        }
        
        private static void OnEditorUpdate()
        {
            // Check for new requests periodically using Editor time
            double currentTime = EditorApplication.timeSinceStartup;
            
            if (currentTime - _lastCheckTime >= _checkInterval)
            {
                _lastCheckTime = currentTime;
                
                if (!_isRunningTests)
                {
                    CheckForPendingRequests();
                }
                
                // Update heartbeat every check
                _dbManager.UpdateSystemHeartbeat("Unity");
            }
        }
        
        private static void CheckForPendingRequests()
        {
            try
            {
                var pendingRequest = _dbManager.GetNextPendingRequest();
                
                if (pendingRequest != null)
                {
                    Debug.Log($"[TestCoordinator] Found pending request: {pendingRequest.Id}");
                    ProcessTestRequest(pendingRequest);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[TestCoordinator] Error checking for pending requests: {e.Message}");
            }
        }
        
        private static void ProcessTestRequest(TestRequest request)
        {
            _isRunningTests = true;
            _currentRequestId = request.Id;
            
            try
            {
                // Clean TestResults directory before running new tests
                CleanTestResultsDirectory();
                
                // Update status to running
                _dbManager.UpdateRequestStatus(request.Id, "running");
                _dbManager.LogExecution(request.Id, "INFO", "Unity", $"Starting test execution for request {request.Id}");
                
                // Create test filter based on request
                Filter filter = CreateTestFilter(request);
                
                // Execute tests
                _testExecutor.ExecuteTests(request, filter, OnTestComplete);
                
                Debug.Log($"[TestCoordinator] Executing tests for request {request.Id}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[TestCoordinator] Error processing request {request.Id}: {e.Message}");
                
                _dbManager.UpdateRequestStatus(request.Id, "failed", e.Message);
                _dbManager.LogExecution(request.Id, "ERROR", "Unity", $"Failed to execute tests: {e.Message}");
                
                _isRunningTests = false;
                _currentRequestId = -1;
            }
        }
        
        private static Filter CreateTestFilter(TestRequest request)
        {
            var filter = new Filter();
            
            // Set test mode
            if (request.TestPlatform == "EditMode")
            {
                filter.testMode = TestMode.EditMode;
            }
            else if (request.TestPlatform == "PlayMode")
            {
                filter.testMode = TestMode.PlayMode;
            }
            else // Both
            {
                // For "Both", we'll need to run tests twice
                filter.testMode = TestMode.EditMode | TestMode.PlayMode;
            }
            
            // Apply filters based on request type
            switch (request.RequestType)
            {
                case "all":
                    // No additional filters needed
                    break;
                    
                case "class":
                    if (!string.IsNullOrEmpty(request.TestFilter))
                    {
                        filter.testNames = new[] { request.TestFilter };
                    }
                    break;
                    
                case "method":
                    if (!string.IsNullOrEmpty(request.TestFilter))
                    {
                        filter.testNames = new[] { request.TestFilter };
                    }
                    break;
                    
                case "category":
                    if (!string.IsNullOrEmpty(request.TestFilter))
                    {
                        filter.categoryNames = new[] { request.TestFilter };
                    }
                    break;
            }
            
            return filter;
        }
        
        private static void OnTestComplete(TestRequest request, bool success, string errorMessage, TestResultSummary summary)
        {
            try
            {
                if (success)
                {
                    // Update request with results
                    _dbManager.UpdateRequestResults(
                        request.Id,
                        "completed",
                        summary.TotalTests,
                        summary.PassedTests,
                        summary.FailedTests,
                        summary.SkippedTests,
                        summary.Duration
                    );
                    
                    _dbManager.LogExecution(request.Id, "INFO", "Unity", 
                        $"Tests completed: {summary.PassedTests}/{summary.TotalTests} passed");
                    
                    Debug.Log($"[TestCoordinator] Tests completed for request {request.Id}: " +
                             $"{summary.PassedTests}/{summary.TotalTests} passed");
                }
                else
                {
                    _dbManager.UpdateRequestStatus(request.Id, "failed", errorMessage);
                    _dbManager.LogExecution(request.Id, "ERROR", "Unity", $"Test execution failed: {errorMessage}");
                    
                    Debug.LogError($"[TestCoordinator] Tests failed for request {request.Id}: {errorMessage}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[TestCoordinator] Error updating test results: {e.Message}");
            }
            finally
            {
                _isRunningTests = false;
                _currentRequestId = -1;
            }
        }
        
        [MenuItem("Tools/PerSpec/Test Coordinator", priority = -90)]
        public static void ShowTestCoordinatorWindow()
        {
            Debug.Log("[PerSpec] Test Coordinator is running in background mode.");
            Debug.Log("[PerSpec] Use the Commands and Debug menu items to interact with the coordinator.");
            Debug.Log($"[PerSpec] Current status: {(_isRunningTests ? $"Running test {_currentRequestId}" : "Idle")}");
            
            if (_dbManager != null)
            {
                var status = _dbManager.GetSystemStatus();
                Debug.Log($"[PerSpec] Database Status:\n{status}");
            }
        }
        
        [MenuItem("Tools/PerSpec/Commands/Check Pending Tests", priority = 100)]
        public static void ManualCheckPendingRequests()
        {
            if (!_isRunningTests)
            {
                CheckForPendingRequests();
            }
            else
            {
                Debug.Log($"[TestCoordinator] Currently running test request {_currentRequestId}");
            }
        }
        
        [MenuItem("Tools/PerSpec/Debug/Database Status", priority = 500)]
        public static void ViewDatabaseStatus()
        {
            if (_dbManager != null)
            {
                var status = _dbManager.GetSystemStatus();
                Debug.Log($"[TestCoordinator] Database Status:\n{status}");
            }
        }
        
        [MenuItem("Tools/PerSpec/Commands/Cancel Current Test", priority = 101)]
        public static void CancelCurrentTest()
        {
            if (_isRunningTests && _currentRequestId > 0)
            {
                _dbManager.UpdateRequestStatus(_currentRequestId, "cancelled", "Cancelled by user");
                _isRunningTests = false;
                _currentRequestId = -1;
                Debug.Log($"[TestCoordinator] Cancelled test request {_currentRequestId}");
            }
            else
            {
                Debug.Log("[TestCoordinator] No test currently running");
            }
        }
        
        [MenuItem("Tools/PerSpec/Commands/Toggle Auto-Polling", priority = 102)]
        public static void TogglePolling()
        {
            if (_checkInterval > 0)
            {
                _checkInterval = 0;
                Debug.Log("[TestCoordinator] Polling disabled");
            }
            else
            {
                _checkInterval = 1.0;
                Debug.Log("[TestCoordinator] Polling enabled (1 second interval)");
            }
        }
        
        [MenuItem("Tools/PerSpec/Debug/Polling Status", priority = 501)]
        public static void DebugPollingStatus()
        {
            double currentTime = EditorApplication.timeSinceStartup;
            double timeSinceLastCheck = currentTime - _lastCheckTime;
            
            Debug.Log($"[TestCoordinator] Polling Debug Info:");
            Debug.Log($"  - Polling Enabled: {_checkInterval > 0}");
            Debug.Log($"  - Check Interval: {_checkInterval} seconds");
            Debug.Log($"  - Current Time: {currentTime:F2}");
            Debug.Log($"  - Last Check Time: {_lastCheckTime:F2}");
            Debug.Log($"  - Time Since Last Check: {timeSinceLastCheck:F2} seconds");
            Debug.Log($"  - Is Running Tests: {_isRunningTests}");
            Debug.Log($"  - Current Request ID: {_currentRequestId}");
        }
        
        private static void CleanTestResultsDirectory()
        {
            try
            {
                string projectPath = Directory.GetParent(Application.dataPath).FullName;
                string testResultsPath = Path.Combine(projectPath, "TestResults");
                
                if (Directory.Exists(testResultsPath))
                {
                    // Get all files in the TestResults directory
                    string[] files = Directory.GetFiles(testResultsPath, "*", SearchOption.AllDirectories);
                    
                    foreach (string file in files)
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning($"[TestCoordinator] Failed to delete file {file}: {e.Message}");
                        }
                    }
                    
                    // Get and delete all subdirectories
                    string[] directories = Directory.GetDirectories(testResultsPath, "*", SearchOption.AllDirectories);
                    
                    // Delete directories in reverse order (deepest first)
                    for (int i = directories.Length - 1; i >= 0; i--)
                    {
                        try
                        {
                            Directory.Delete(directories[i], true);
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning($"[TestCoordinator] Failed to delete directory {directories[i]}: {e.Message}");
                        }
                    }
                    
                    Debug.Log($"[TestCoordinator] Cleaned TestResults directory");
                }
                else
                {
                    // Create the directory if it doesn't exist
                    Directory.CreateDirectory(testResultsPath);
                    Debug.Log($"[TestCoordinator] Created TestResults directory");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[TestCoordinator] Error cleaning TestResults directory: {e.Message}");
            }
        }
    }
}