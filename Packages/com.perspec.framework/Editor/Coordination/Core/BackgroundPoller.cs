using System;
using System.Threading;
using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.TestTools.TestRunner.Api;

namespace PerSpec.Editor.Coordination
{
    /// <summary>
    /// Background polling system that continues to run even when Unity loses focus
    /// Uses System.Threading.Timer for true background operation
    /// </summary>
    [InitializeOnLoad]
    public static class BackgroundPoller
    {
        private static System.Threading.Timer _backgroundTimer;
        private static SynchronizationContext _unitySyncContext;
        private static SQLiteManager _dbManager;
        private static bool _isEnabled = false;
        private static readonly object _lockObject = new object();
        private static DateTime _lastPollTime;
        private static int _pollInterval = 1000; // 1 second in milliseconds
        
        // Track if we're currently processing to avoid overlapping operations
        private static bool _isProcessing = false;
        
        static BackgroundPoller()
        {
            Debug.Log("[BackgroundPoller] Initializing background polling system");
            
            // Capture Unity's synchronization context for thread marshalling
            _unitySyncContext = SynchronizationContext.Current;
            
            // Initialize database manager
            try
            {
                _dbManager = new SQLiteManager();
            }
            catch (Exception e)
            {
                Debug.LogError($"[BackgroundPoller] Failed to initialize database: {e.Message}");
                return;
            }
            
            // Auto-enable background polling
            EnableBackgroundPolling();
            
            // Subscribe to domain reload to clean up
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        }
        
        private static void OnBeforeAssemblyReload()
        {
            Debug.Log("[BackgroundPoller] Assembly reloading, stopping background timer");
            DisableBackgroundPolling();
        }
        
        public static void EnableBackgroundPolling()
        {
            lock (_lockObject)
            {
                if (_isEnabled)
                {
                    Debug.Log("[BackgroundPoller] Background polling already enabled");
                    return;
                }
                
                _isEnabled = true;
                _lastPollTime = DateTime.Now;
                
                // Create and start the background timer
                _backgroundTimer = new System.Threading.Timer(
                    BackgroundPollCallback,
                    null,
                    0, // Start immediately
                    _pollInterval // Repeat every second
                );
                
                Debug.Log("[BackgroundPoller] Background polling ENABLED");
            }
        }
        
        public static void DisableBackgroundPolling()
        {
            lock (_lockObject)
            {
                if (!_isEnabled)
                {
                    Debug.Log("[BackgroundPoller] Background polling already disabled");
                    return;
                }
                
                _isEnabled = false;
                
                // Dispose of the timer
                _backgroundTimer?.Dispose();
                _backgroundTimer = null;
                
                Debug.Log("[BackgroundPoller] Background polling DISABLED");
            }
        }
        
        private static void BackgroundPollCallback(object state)
        {
            // Skip if already processing or disabled
            if (!_isEnabled || _isProcessing)
            {
                return;
            }
            
            try
            {
                _isProcessing = true;
                
                // Database operations are thread-safe with SQLite WAL mode
                bool hasTestRequests = CheckForPendingTestRequests();
                bool hasRefreshRequests = CheckForPendingRefreshRequests();
                
                if (hasTestRequests || hasRefreshRequests)
                {
                    Debug.Log($"[BackgroundPoller-Thread] Found pending requests - Test: {hasTestRequests}, Refresh: {hasRefreshRequests}");
                    
                    // Marshal the processing back to Unity's main thread
                    _unitySyncContext?.Post(_ =>
                    {
                        try
                        {
                            Debug.Log("[BackgroundPoller-MainThread] Processing pending requests on main thread");
                            
                            if (hasTestRequests)
                            {
                                // Trigger test processing
                                ProcessPendingTestRequest();
                            }
                            
                            if (hasRefreshRequests)
                            {
                                // Trigger refresh processing
                                ProcessPendingRefreshRequest();
                            }
                            
                            // Force script compilation to ensure Unity processes everything
                            if (hasTestRequests || hasRefreshRequests)
                            {
                                Debug.Log("[BackgroundPoller-MainThread] Requesting script compilation");
                                CompilationPipeline.RequestScriptCompilation();
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"[BackgroundPoller-MainThread] Error processing requests: {e.Message}");
                        }
                    }, null);
                }
            }
            catch (Exception e)
            {
                // Log errors but don't crash the background thread
                // Note: Debug.Log might not work from background thread
                UnityEngine.Debug.LogError($"[BackgroundPoller-Thread] Error in background poll: {e.Message}");
            }
            finally
            {
                _isProcessing = false;
            }
        }
        
        private static bool CheckForPendingTestRequests()
        {
            try
            {
                // Direct database check - thread safe
                var request = _dbManager.GetNextPendingRequest();
                return request != null;
            }
            catch
            {
                return false;
            }
        }
        
        private static bool CheckForPendingRefreshRequests()
        {
            try
            {
                // Direct database check - thread safe
                var request = _dbManager.GetNextPendingRefreshRequest();
                return request != null;
            }
            catch
            {
                return false;
            }
        }
        
        private static void ProcessPendingTestRequest()
        {
            try
            {
                // Get the request again on main thread
                var request = _dbManager.GetNextPendingRequest();
                if (request != null)
                {
                    Debug.Log($"[BackgroundPoller] Processing test request #{request.Id}");
                    
                    // Update status to running
                    _dbManager.UpdateRequestStatus(request.Id, "running");
                    
                    // Create test filter based on request
                    var filter = CreateTestFilter(request);
                    
                    // Use TestExecutor to run the tests
                    var executor = new TestExecutor(_dbManager);
                    executor.ExecuteTests(request, filter, (req, success, error, summary) =>
                    {
                        if (success)
                        {
                            Debug.Log($"[BackgroundPoller] Test request #{req.Id} completed successfully");
                        }
                        else
                        {
                            Debug.LogError($"[BackgroundPoller] Test request #{req.Id} failed: {error}");
                        }
                    });
                    
                    _dbManager.LogExecution(request.Id, "INFO", "BackgroundPoller", 
                        "Test request triggered via background polling");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[BackgroundPoller] Error processing test request: {e.Message}");
            }
        }
        
        private static Filter CreateTestFilter(TestRequest request)
        {
            var filter = new Filter();
            
            // Set test mode based on platform
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
                // Default to EditMode for "Both" - may need to run twice
                filter.testMode = TestMode.EditMode;
            }
            
            // Set filter based on request type
            if (request.RequestType == "all")
            {
                // No additional filtering needed for all tests
            }
            else if (request.RequestType == "class" && !string.IsNullOrEmpty(request.TestFilter))
            {
                filter.testNames = new[] { request.TestFilter };
            }
            else if (request.RequestType == "method" && !string.IsNullOrEmpty(request.TestFilter))
            {
                filter.testNames = new[] { request.TestFilter };
            }
            else if (request.RequestType == "category" && !string.IsNullOrEmpty(request.TestFilter))
            {
                filter.categoryNames = new[] { request.TestFilter };
            }
            
            return filter;
        }
        
        private static void ProcessPendingRefreshRequest()
        {
            try
            {
                // Get the request again on main thread
                var request = _dbManager.GetNextPendingRefreshRequest();
                if (request != null)
                {
                    Debug.Log($"[BackgroundPoller] Processing refresh request #{request.Id}");
                    
                    // Update status to running
                    _dbManager.UpdateRefreshRequestStatus(request.Id, "running");
                    
                    // Execute the refresh
                    AssetDatabase.Refresh();
                    
                    // Mark as completed
                    _dbManager.UpdateRefreshRequestStatus(request.Id, "completed", 
                        "Refresh triggered via background polling");
                    
                    _dbManager.LogExecution(request.Id, "INFO", "BackgroundPoller", 
                        "Refresh request triggered via background polling");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[BackgroundPoller] Error processing refresh request: {e.Message}");
            }
        }
        
        // Menu items for manual control
        // Background polling controls are now integrated into the main coordinator
        // These items are kept for backward compatibility but hidden from menu
        [MenuItem("Tools/PerSpec/Internal/Enable Polling", false, 999)]
        public static void MenuEnablePolling()
        {
            EnableBackgroundPolling();
        }
        
        [MenuItem("Tools/PerSpec/Internal/Disable Polling", false, 999)]
        public static void MenuDisablePolling()
        {
            DisableBackgroundPolling();
        }
        
        // Status is now shown in Debug/Polling Status
        [MenuItem("Tools/PerSpec/Internal/Polling Status Info", false, 999)]
        public static void ShowPollingStatus()
        {
            Debug.Log($"[BackgroundPoller] Status: {(_isEnabled ? "ENABLED" : "DISABLED")}");
            if (_isEnabled)
            {
                Debug.Log($"  Last poll: {_lastPollTime:HH:mm:ss}");
                Debug.Log($"  Poll interval: {_pollInterval}ms");
                Debug.Log($"  Is processing: {_isProcessing}");
            }
        }
        
        [MenuItem("Tools/PerSpec/Commands/Force Script Compilation", priority = 103)]
        public static void ForceScriptCompilation()
        {
            Debug.Log("[BackgroundPoller] Forcing script compilation");
            CompilationPipeline.RequestScriptCompilation();
        }
    }
}