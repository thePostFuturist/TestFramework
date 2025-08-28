using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;
using Newtonsoft.Json;

namespace PerSpec.Editor.Coordination
{
    /// <summary>
    /// Polls database for asset refresh requests and executes them
    /// Enhanced with background processing support
    /// </summary>
    [InitializeOnLoad]
    public static class AssetRefreshCoordinator
    {
        private static double _lastCheckTime;
        private static double _checkInterval = 1.0; // Check every 1 second
        private static bool _isRefreshing = false;
        private static SQLiteManager _dbManager;
        private static int _currentRequestId = -1;
        private static DateTime _refreshStartTime;
        private static bool _pollingEnabled = true;
        private static SynchronizationContext _unitySyncContext;
        
        // Background processing support
        private static System.Threading.Timer _fallbackTimer;
        private static bool _useBackgroundFallback = true;
        private static DateTime _lastBackgroundCheck;
        
        static AssetRefreshCoordinator()
        {
            Debug.Log("[AssetRefreshCoordinator] Initializing asset refresh coordination");
            
            // Capture Unity's sync context for thread marshalling
            _unitySyncContext = SynchronizationContext.Current;
            
            _dbManager = new SQLiteManager();
            EditorApplication.update += OnEditorUpdate;
            _lastCheckTime = EditorApplication.timeSinceStartup;
            
            // Set up background fallback timer if enabled
            if (_useBackgroundFallback)
            {
                SetupBackgroundFallback();
            }
            
            // Force Unity to run in background
            Application.runInBackground = true;
            
            Debug.Log("[AssetRefreshCoordinator] Asset refresh coordination initialized");
        }
        
        private static void SetupBackgroundFallback()
        {
            // Create a timer that runs even when Unity loses focus
            _fallbackTimer = new System.Threading.Timer(
                BackgroundCheck,
                null,
                TimeSpan.FromSeconds(2), // Initial delay
                TimeSpan.FromSeconds(1)  // Repeat interval
            );
            
            Debug.Log("[AssetRefreshCoordinator] Background fallback timer enabled");
        }
        
        private static void BackgroundCheck(object state)
        {
            // Skip if already refreshing
            if (_isRefreshing)
                return;
            
            try
            {
                // Check database from background thread (thread-safe)
                var request = _dbManager.GetNextPendingRefreshRequest();
                
                if (request != null)
                {
                    _lastBackgroundCheck = DateTime.Now;
                    Debug.Log($"[AssetRefreshCoordinator-BG] Found pending request #{request.Id}");
                    
                    // Marshal back to Unity main thread
                    _unitySyncContext?.Post(_ =>
                    {
                        if (!_isRefreshing && request != null)
                        {
                            ProcessRefreshRequest(request);
                            // Force compilation to ensure Unity processes
                            CompilationPipeline.RequestScriptCompilation();
                        }
                    }, null);
                }
            }
            catch (Exception e)
            {
                // Log but don't crash the background thread
                UnityEngine.Debug.LogError($"[AssetRefreshCoordinator-BG] Error: {e.Message}");
            }
        }
        
        private static void OnEditorUpdate()
        {
            if (!_pollingEnabled) return;
            
            double currentTime = EditorApplication.timeSinceStartup;
            
            if (currentTime - _lastCheckTime >= _checkInterval)
            {
                _lastCheckTime = currentTime;
                
                if (!_isRefreshing)
                {
                    CheckForPendingRefreshRequests();
                }
            }
        }
        
        private static void CheckForPendingRefreshRequests()
        {
            try
            {
                var request = _dbManager.GetNextPendingRefreshRequest();
                
                if (request != null)
                {
                    Debug.Log($"[AssetRefreshCoordinator] Found pending refresh request #{request.Id}");
                    ProcessRefreshRequest(request);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[AssetRefreshCoordinator] Error checking for requests: {e.Message}");
            }
        }
        
        private static void ProcessRefreshRequest(AssetRefreshRequest request)
        {
            try
            {
                _isRefreshing = true;
                _currentRequestId = request.Id;
                _refreshStartTime = DateTime.Now;
                
                // Update status to running
                _dbManager.UpdateRefreshRequestStatus(request.Id, "running");
                _dbManager.LogExecution(request.Id, "INFO", "AssetRefreshCoordinator", 
                    $"Starting asset refresh (Type: {request.RefreshType}, Options: {request.ImportOptions})");
                
                Debug.Log($"[AssetRefreshCoordinator] Starting refresh for request #{request.Id}");
                Debug.Log($"  Type: {request.RefreshType}");
                Debug.Log($"  Options: {request.ImportOptions}");
                
                // Parse import options
                var importOptions = ImportAssetOptions.Default;
                if (request.ImportOptions == "synchronous")
                {
                    importOptions = ImportAssetOptions.ForceSynchronousImport;
                }
                else if (request.ImportOptions == "force_update")
                {
                    importOptions = ImportAssetOptions.ForceUpdate;
                }
                
                // Store request ID for the postprocessor to use
                AssetRefreshPostprocessor.SetCurrentRequestId(request.Id);
                
                if (request.RefreshType == "selective" && !string.IsNullOrEmpty(request.Paths))
                {
                    // Parse paths from JSON
                    try
                    {
                        var paths = JsonConvert.DeserializeObject<List<string>>(request.Paths);
                        if (paths != null && paths.Count > 0)
                        {
                            Debug.Log($"[AssetRefreshCoordinator] Refreshing {paths.Count} specific paths");
                            foreach (var path in paths)
                            {
                                AssetDatabase.ImportAsset(path, importOptions);
                                Debug.Log($"  Importing: {path}");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[AssetRefreshCoordinator] Error parsing paths: {e.Message}");
                        AssetDatabase.Refresh(importOptions);
                    }
                }
                else
                {
                    // Full refresh
                    Debug.Log("[AssetRefreshCoordinator] Starting full asset refresh");
                    AssetDatabase.Refresh(importOptions);
                }
                
                // If using synchronous import, we can mark as complete immediately
                if (importOptions == ImportAssetOptions.ForceSynchronousImport)
                {
                    OnRefreshComplete();
                }
                else
                {
                    // Set a delayed callback as fallback if postprocessor doesn't fire
                    EditorApplication.delayCall += () =>
                    {
                        EditorApplication.delayCall += () =>
                        {
                            // Check if still running after 2 frames
                            if (_isRefreshing && _currentRequestId == request.Id)
                            {
                                Debug.Log("[AssetRefreshCoordinator] No assets changed, marking refresh as complete");
                                OnRefreshComplete();
                            }
                        };
                    };
                }
                
            }
            catch (Exception e)
            {
                Debug.LogError($"[AssetRefreshCoordinator] Error processing refresh request: {e.Message}");
                _dbManager.UpdateRefreshRequestStatus(request.Id, "failed", null, e.Message);
                _dbManager.LogExecution(request.Id, "ERROR", "AssetRefreshCoordinator", 
                    $"Failed to process refresh: {e.Message}");
                _isRefreshing = false;
                _currentRequestId = -1;
            }
        }
        
        public static void OnRefreshComplete()
        {
            if (_currentRequestId < 0) return;
            
            try
            {
                var duration = (DateTime.Now - _refreshStartTime).TotalSeconds;
                var message = $"Asset refresh completed in {duration:F2} seconds";
                
                Debug.Log($"[AssetRefreshCoordinator] Refresh request #{_currentRequestId} completed");
                
                _dbManager.UpdateRefreshRequestStatus(_currentRequestId, "completed", message);
                _dbManager.LogExecution(_currentRequestId, "INFO", "AssetRefreshCoordinator", message);
            }
            catch (Exception e)
            {
                Debug.LogError($"[AssetRefreshCoordinator] Error marking request as complete: {e.Message}");
            }
            finally
            {
                _isRefreshing = false;
                _currentRequestId = -1;
                AssetRefreshPostprocessor.ClearCurrentRequestId();
            }
        }
        
        // Methods now accessed via Control Center
        public static void ManualCheckPendingRequests()
        {
            CheckForPendingRefreshRequests();
        }
        
        // Method now accessed via Control Center
        public static void ViewPendingRequests()
        {
            var requests = _dbManager.GetPendingRefreshRequests();
            
            if (requests.Count == 0)
            {
                Debug.Log("[AssetRefreshCoordinator] No pending refresh requests");
            }
            else
            {
                Debug.Log($"[AssetRefreshCoordinator] Found {requests.Count} pending refresh request(s):");
                foreach (var req in requests)
                {
                    Debug.Log($"  #{req.Id}: {req.RefreshType} (Priority: {req.Priority})");
                }
            }
        }
        
        // Method now accessed via Control Center
        public static void TogglePolling()
        {
            _pollingEnabled = !_pollingEnabled;
            Debug.Log($"[AssetRefreshCoordinator] Polling {(_pollingEnabled ? "enabled" : "disabled")}");
        }
        
        // Method now accessed via Control Center
        public static void ForceRefreshNow()
        {
            Debug.Log("[AssetRefreshCoordinator] Forcing asset refresh");
            AssetDatabase.Refresh();
        }
    }
}