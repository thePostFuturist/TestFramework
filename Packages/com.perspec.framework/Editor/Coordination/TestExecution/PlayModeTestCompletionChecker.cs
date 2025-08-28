using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;

namespace PerSpec.Editor.Coordination
{
    /// <summary>
    /// Monitors for PlayMode test completion after Unity exits Play mode
    /// Since EditorApplication.update doesn't run during Play mode, we need to check after
    /// </summary>
    [InitializeOnLoad]
    public static class PlayModeTestCompletionChecker
    {
        private static string _testResultsPath;
        
        static PlayModeTestCompletionChecker()
        {
            string projectPath = Directory.GetParent(Application.dataPath).FullName;
            _testResultsPath = Path.Combine(projectPath, "TestResults");
            
            // Subscribe to play mode state changes
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            
            Debug.Log("[PlayModeTestCompletionChecker] Initialized");
        }
        
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            Debug.Log($"[PlayModeTestCompletionChecker] Play mode state changed to: {state}");
            
            // When exiting play mode, check for test results
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                Debug.Log("[PlayModeTestCompletionChecker] Exited Play mode, checking for test results...");
                CheckForCompletedTests();
            }
        }
        
        private static void CheckForCompletedTests()
        {
            try
            {
                var dbManager = new SQLiteManager();
                
                // Get all running PlayMode test requests
                var runningRequests = dbManager.GetRunningRequests()
                    .Where(r => r.TestPlatform == "PlayMode")
                    .ToList();
                
                if (runningRequests.Count == 0)
                {
                    Debug.Log("[PlayModeTestCompletionChecker] No running PlayMode tests to check");
                    return;
                }
                
                Debug.Log($"[PlayModeTestCompletionChecker] Found {runningRequests.Count} running PlayMode test(s)");
                
                // Look for the latest test result file
                var latestResultFile = GetLatestResultFile();
                
                if (!string.IsNullOrEmpty(latestResultFile))
                {
                    Debug.Log($"[PlayModeTestCompletionChecker] Found result file: {latestResultFile}");
                    
                    // Parse the summary file if it exists
                    string summaryPath = latestResultFile.Replace(".xml", ".summary.txt");
                    if (File.Exists(summaryPath))
                    {
                        var summary = ParseSummaryFile(summaryPath);
                        
                        // Update the most recent running request
                        var requestToUpdate = runningRequests.OrderByDescending(r => r.Id).First();
                        
                        Debug.Log($"[PlayModeTestCompletionChecker] Updating request {requestToUpdate.Id} with results");
                        
                        dbManager.UpdateRequestResults(
                            requestToUpdate.Id,
                            "completed",
                            summary.TotalTests,
                            summary.PassedTests,
                            summary.FailedTests,
                            summary.SkippedTests,
                            summary.Duration
                        );
                        
                        dbManager.LogExecution(requestToUpdate.Id, "INFO", "PlayModeTestCompletionChecker", 
                            $"Test completed (detected after Play mode exit): {summary.PassedTests}/{summary.TotalTests} passed");
                        
                        Debug.Log($"[PlayModeTestCompletionChecker] Request {requestToUpdate.Id} marked as completed");
                    }
                }
                else
                {
                    Debug.Log("[PlayModeTestCompletionChecker] No test result files found");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayModeTestCompletionChecker] Error checking for completed tests: {e.Message}");
            }
        }
        
        private static string GetLatestResultFile()
        {
            if (!Directory.Exists(_testResultsPath)) return null;
            
            var xmlFiles = Directory.GetFiles(_testResultsPath, "*.xml")
                .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                .FirstOrDefault();
            
            return xmlFiles;
        }
        
        private static TestResultSummary ParseSummaryFile(string summaryPath)
        {
            var summary = new TestResultSummary();
            var lines = File.ReadAllLines(summaryPath);
            
            foreach (var line in lines)
            {
                if (line.Contains("Total Tests:"))
                {
                    if (int.TryParse(Regex.Match(line, @"\d+").Value, out int totalTests))
                        summary.TotalTests = totalTests;
                }
                else if (line.Contains("Passed:"))
                {
                    if (int.TryParse(Regex.Match(line, @"\d+").Value, out int passedTests))
                        summary.PassedTests = passedTests;
                }
                else if (line.Contains("Failed:"))
                {
                    if (int.TryParse(Regex.Match(line, @"\d+").Value, out int failedTests))
                        summary.FailedTests = failedTests;
                }
                else if (line.Contains("Skipped:"))
                {
                    if (int.TryParse(Regex.Match(line, @"\d+").Value, out int skippedTests))
                        summary.SkippedTests = skippedTests;
                }
                else if (line.Contains("Duration:"))
                {
                    var match = Regex.Match(line, @"[\d.]+");
                    if (match.Success)
                    {
                        if (float.TryParse(match.Value, out float duration))
                            summary.Duration = duration;
                    }
                }
            }
            
            return summary;
        }
        
        // Integrated into main coordinator - no longer needed as separate menu item
        // [MenuItem("Test Coordination/Debug/Check PlayMode Completion Now")]
        public static void ManualCheck()
        {
            CheckForCompletedTests();
        }
    }
}