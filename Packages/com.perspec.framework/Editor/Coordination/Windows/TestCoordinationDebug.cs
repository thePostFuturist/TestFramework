using UnityEngine;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using System;

namespace PerSpec.Editor.Coordination
{
    public static class TestCoordinationDebug
    {
        // Method now accessed via Control Center
        public static void ForceReinitialize()
        {
            Debug.Log("[TestCoordinationDebug] Forcing reinitialization...");
            
            // This will trigger the static constructor again after domain reload
            EditorUtility.RequestScriptReload();
        }
        
        // Method now accessed via Control Center
        public static void TestDatabaseConnection()
        {
            try
            {
                var dbManager = new SQLiteManager();
                Debug.Log("[TestCoordinationDebug] Database connection successful");
                
                var pendingRequests = dbManager.GetAllPendingRequests();
                Debug.Log($"[TestCoordinationDebug] Found {pendingRequests.Count} pending requests");
                
                foreach (var request in pendingRequests)
                {
                    Debug.Log($"  - Request #{request.Id}: {request.RequestType} on {request.TestPlatform} (Status: {request.Status})");
                }
                
                dbManager.UpdateSystemHeartbeat("Unity");
                Debug.Log("[TestCoordinationDebug] Heartbeat updated");
            }
            catch (Exception e)
            {
                Debug.LogError($"[TestCoordinationDebug] Database error: {e.Message}");
                Debug.LogError(e.StackTrace);
            }
        }
        
        // Method now accessed via Control Center
        public static void ManuallyProcessNextRequest()
        {
            try
            {
                var dbManager = new SQLiteManager();
                var nextRequest = dbManager.GetNextPendingRequest();
                
                if (nextRequest != null)
                {
                    Debug.Log($"[TestCoordinationDebug] Processing request #{nextRequest.Id}");
                    
                    // Update to running
                    dbManager.UpdateRequestStatus(nextRequest.Id, "running");
                    
                    // Try to execute
                    var testExecutor = new TestExecutor(dbManager);
                    var filter = new Filter();
                    
                    if (nextRequest.TestPlatform == "EditMode")
                    {
                        filter.testMode = TestMode.EditMode;
                    }
                    else if (nextRequest.TestPlatform == "PlayMode")
                    {
                        filter.testMode = TestMode.PlayMode;
                    }
                    
                    testExecutor.ExecuteTests(nextRequest, filter, (req, success, error, summary) =>
                    {
                        if (success && summary != null)
                        {
                            Debug.Log($"[TestCoordinationDebug] Test completed: {summary.PassedTests}/{summary.TotalTests} passed");
                            dbManager.UpdateRequestResults(req.Id, "completed", 
                                summary.TotalTests, summary.PassedTests, 
                                summary.FailedTests, summary.SkippedTests, summary.Duration);
                        }
                        else
                        {
                            Debug.LogError($"[TestCoordinationDebug] Test failed: {error}");
                            dbManager.UpdateRequestStatus(req.Id, "failed", error);
                        }
                    });
                }
                else
                {
                    Debug.Log("[TestCoordinationDebug] No pending requests found");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[TestCoordinationDebug] Error processing request: {e.Message}");
                Debug.LogError(e.StackTrace);
            }
        }
        
        // Method now accessed via Control Center
        public static void ClearAllPendingRequests()
        {
            try
            {
                var dbManager = new SQLiteManager();
                var pendingRequests = dbManager.GetAllPendingRequests();
                
                foreach (var request in pendingRequests)
                {
                    dbManager.UpdateRequestStatus(request.Id, "cancelled", "Cancelled by debug tool");
                    Debug.Log($"[TestCoordinationDebug] Cancelled request #{request.Id}");
                }
                
                Debug.Log($"[TestCoordinationDebug] Cleared {pendingRequests.Count} pending requests");
            }
            catch (Exception e)
            {
                Debug.LogError($"[TestCoordinationDebug] Error clearing requests: {e.Message}");
            }
        }
    }
}