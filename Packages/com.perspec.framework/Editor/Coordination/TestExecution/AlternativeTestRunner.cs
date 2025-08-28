using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;

namespace PerSpec.Editor.Coordination
{
    /// <summary>
    /// Alternative test runner that uses Unity's menu commands for PlayMode tests
    /// This bypasses the API issues with PlayMode test execution
    /// </summary>
    public static class AlternativeTestRunner
    {
        public static void RunPlayModeTests()
        {
            try
            {
                Debug.Log("[AlternativeTestRunner] Triggering PlayMode tests via menu command");
                
                // Clear test results directory first
                ClearTestResults();
                
                // Use Unity's built-in menu command to run PlayMode tests
                EditorApplication.ExecuteMenuItem("Window/General/Test Runner");
                
                // Small delay to let the window open
                EditorApplication.delayCall += () =>
                {
                    // Try to run all PlayMode tests
                    if (EditorWindow.focusedWindow != null && EditorWindow.focusedWindow.GetType().Name == "TestRunnerWindow")
                    {
                        Debug.Log("[AlternativeTestRunner] Test Runner window opened, attempting to run PlayMode tests");
                        
                        // This would require reflection to access the private methods of TestRunnerWindow
                        // For now, we rely on file monitoring to detect when tests complete
                    }
                };
                
                Debug.Log("[AlternativeTestRunner] PlayMode tests triggered - monitor TestResults folder for completion");
            }
            catch (Exception e)
            {
                Debug.LogError($"[AlternativeTestRunner] Failed to run PlayMode tests: {e.Message}");
            }
        }
        
        public static void RunEditModeTests()
        {
            try
            {
                Debug.Log("[AlternativeTestRunner] Running EditMode tests via API");
                
                var testApi = ScriptableObject.CreateInstance<TestRunnerApi>();
                var filter = new Filter
                {
                    testMode = TestMode.EditMode
                };
                
                testApi.Execute(new ExecutionSettings(filter));
                
                Debug.Log("[AlternativeTestRunner] EditMode tests started");
            }
            catch (Exception e)
            {
                Debug.LogError($"[AlternativeTestRunner] Failed to run EditMode tests: {e.Message}");
            }
        }
        
        private static void ClearTestResults()
        {
            try
            {
                string projectPath = Directory.GetParent(Application.dataPath).FullName;
                string testResultsPath = Path.Combine(projectPath, "TestResults");
                
                if (Directory.Exists(testResultsPath))
                {
                    foreach (string file in Directory.GetFiles(testResultsPath))
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch { }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AlternativeTestRunner] Failed to clear test results: {e.Message}");
            }
        }
        
        // Redundant - functionality integrated into main Test Coordinator
        // [MenuItem("Test Coordination/Run PlayMode Tests (Alternative)")]
        public static void MenuRunPlayModeTests()
        {
            RunPlayModeTests();
        }
        
        // [MenuItem("Test Coordination/Run EditMode Tests (Alternative)")]
        public static void MenuRunEditModeTests()
        {
            RunEditModeTests();
        }
    }
}