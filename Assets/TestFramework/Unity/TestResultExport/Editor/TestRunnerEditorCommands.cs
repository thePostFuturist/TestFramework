using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace TestFramework.Unity.TestResultExport.Editor
{
    /// <summary>
    /// Editor commands to run tests in the existing Unity instance (not batch mode)
    /// These commands use the TestRunner API to execute tests programmatically
    /// </summary>
    public static class TestRunnerEditorCommands
    {
        private static TestResultXMLExporter _exporter;
        private static bool _isRunning = false;
        
        #region Menu Items - Quick Run
        
        [MenuItem("TestFramework/Run Tests/Run All Tests (Editor Instance) %#&t", false, 1)]
        public static void RunAllTestsInEditor()
        {
            if (_isRunning)
            {
                Debug.LogWarning("[TEST-RUNNER] Tests are already running. Please wait for completion.");
                return;
            }
            
            // Pass null to run ALL tests (empty array might filter everything)
            RunTestsWithFilter(TestMode.EditMode | TestMode.PlayMode, null, null, "All Tests");
        }
        
        [MenuItem("TestFramework/Run Tests/Run EditMode Tests", false, 2)]
        public static void RunEditModeTests()
        {
            if (_isRunning)
            {
                Debug.LogWarning("[TEST-RUNNER] Tests are already running. Please wait for completion.");
                return;
            }
            
            RunTestsWithFilter(TestMode.EditMode, null, null, "EditMode Tests");
        }
        
        [MenuItem("TestFramework/Run Tests/Run PlayMode Tests", false, 3)]
        public static void RunPlayModeTests()
        {
            if (_isRunning)
            {
                Debug.LogWarning("[TEST-RUNNER] Tests are already running. Please wait for completion.");
                return;
            }
            
            // Force PlayMode tests to run
            RunTestsWithFilter(TestMode.PlayMode, null, null, "PlayMode Tests");
        }
        
        [MenuItem("TestFramework/Run Tests/Debug - List All Tests", false, 50)]
        public static void DebugListAllTests()
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new TestDiscoveryCallbacks());
            
            // Try to discover all tests
            var filter = new Filter()
            {
                testMode = TestMode.EditMode | TestMode.PlayMode
            };
            
            api.Execute(new ExecutionSettings(filter) { runSynchronously = false });
        }
        
        #endregion
        
        #region Menu Items - Filtered Run
        
        [MenuItem("TestFramework/Run Tests/Run Unit Tests", false, 20)]
        public static void RunUnitTests()
        {
            RunTestsWithFilter(TestMode.EditMode, new[] { "TestFramework.Unity.Tests.Unit" }, null, "Unit Tests");
        }
        
        [MenuItem("TestFramework/Run Tests/Run Integration Tests", false, 21)]
        public static void RunIntegrationTests()
        {
            RunTestsWithFilter(TestMode.PlayMode, new[] { "TestFramework.Unity.Tests.Integration" }, null, "Integration Tests");
        }
        
        [MenuItem("TestFramework/Run Tests/Run Critical Tests", false, 22)]
        public static void RunCriticalTests()
        {
            RunTestsWithFilter(TestMode.EditMode | TestMode.PlayMode, null, new[] { "Critical", "Smoke" }, "Critical Tests");
        }
        
        #endregion
        
        #region Core Test Execution
        
        /// <summary>
        /// Run tests with specified filter in the current Unity Editor instance
        /// </summary>
        public static void RunTestsWithFilter(TestMode testMode, string[] testNames = null, string[] categoryNames = null, string description = "Tests")
        {
            try
            {
                _isRunning = true;
                
                // Clear console
                var logEntries = Type.GetType("UnityEditor.LogEntries, UnityEditor");
                var clearMethod = logEntries?.GetMethod("Clear");
                clearMethod?.Invoke(null, null);
                
                Debug.Log($"[TEST-RUNNER] ========================================");
                Debug.Log($"[TEST-RUNNER] Starting {description} in Editor");
                Debug.Log($"[TEST-RUNNER] Mode: {testMode}");
                if (testNames != null && testNames.Length > 0)
                    Debug.Log($"[TEST-RUNNER] Filter: {string.Join(", ", testNames)}");
                if (categoryNames != null && categoryNames.Length > 0)
                    Debug.Log($"[TEST-RUNNER] Categories: {string.Join(", ", categoryNames)}");
                Debug.Log($"[TEST-RUNNER] ========================================");
                
                // Clear TestResults directory
                var testResultsDir = Path.Combine(Application.dataPath, "..", "TestResults");
                ClearTestResultsDirectory(testResultsDir);
                
                // Setup XML export
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var outputPath = Path.Combine(testResultsDir, $"TestResults_Editor_{timestamp}.xml");
                
                // Ensure directory exists (recreate after clearing)
                if (!Directory.Exists(testResultsDir))
                {
                    Directory.CreateDirectory(testResultsDir);
                }
                
                // Register exporter
                _exporter = new TestResultXMLExporter(outputPath, true);
                
                // Create TestRunner API instance
                var api = ScriptableObject.CreateInstance<TestRunnerApi>();
                
                // Register callbacks
                api.RegisterCallbacks(_exporter);
                api.RegisterCallbacks(new EditorTestCallbacks(description));
                
                // Create filter - only set properties if they have values
                var filter = new Filter()
                {
                    testMode = testMode
                };
                
                // Only set testNames if we have specific filters
                if (testNames != null && testNames.Length > 0)
                {
                    filter.testNames = testNames;
                }
                
                // Only set categoryNames if we have specific categories
                if (categoryNames != null && categoryNames.Length > 0)
                {
                    filter.categoryNames = categoryNames;
                }
                
                // Execute tests
                var executionSettings = new ExecutionSettings(filter);
                
                Debug.Log($"[TEST-RUNNER] Executing tests...");
                Debug.Log($"[TEST-RUNNER] Filter - TestMode: {filter.testMode}");
                if (filter.testNames != null)
                    Debug.Log($"[TEST-RUNNER] Filter - TestNames: {string.Join(", ", filter.testNames)}");
                if (filter.categoryNames != null)
                    Debug.Log($"[TEST-RUNNER] Filter - Categories: {string.Join(", ", filter.categoryNames)}");
                Debug.Log($"[TEST-RUNNER] Results will be saved to: {outputPath}");
                
                // Run tests
                api.Execute(executionSettings);
                
                // Note: The actual test execution happens asynchronously
                // Results will be reported through the callbacks
                
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TEST-RUNNER-ERROR] Failed to start tests: {ex.Message}\n{ex.StackTrace}");
                _isRunning = false;
            }
        }
        
        #endregion
        
        #region Command Line Entry Points
        
        /// <summary>
        /// Entry point for command line execution via -executeMethod
        /// Usage: Unity.exe -projectPath "path" -executeMethod TestRunnerEditorCommands.RunAllTestsFromCommandLine
        /// </summary>
        public static void RunAllTestsFromCommandLine()
        {
            Debug.Log("[TEST-RUNNER] Running all tests from command line...");
            RunAllTestsInEditor();
            
            // For command line execution, we might want to wait or exit after completion
            EditorApplication.update += WaitForTestCompletion;
        }
        
        public static void RunEditModeTestsFromCommandLine()
        {
            Debug.Log("[TEST-RUNNER] Running EditMode tests from command line...");
            RunEditModeTests();
            EditorApplication.update += WaitForTestCompletion;
        }
        
        public static void RunPlayModeTestsFromCommandLine()
        {
            Debug.Log("[TEST-RUNNER] Running PlayMode tests from command line...");
            RunPlayModeTests();
            EditorApplication.update += WaitForTestCompletion;
        }
        
        private static void WaitForTestCompletion()
        {
            if (!_isRunning)
            {
                EditorApplication.update -= WaitForTestCompletion;
                
                // If running from command line, we might want to exit
                var args = Environment.GetCommandLineArgs();
                foreach (var arg in args)
                {
                    if (arg.Contains("-executeMethod"))
                    {
                        Debug.Log("[TEST-RUNNER] Command line execution complete. Exiting in 5 seconds...");
                        EditorApplication.delayCall += () =>
                        {
                            System.Threading.Thread.Sleep(5000);
                            EditorApplication.Exit(0);
                        };
                        break;
                    }
                }
            }
        }
        
        #endregion
        
        #region Test Callbacks
        
        /// <summary>
        /// Custom callbacks for editor test execution
        /// </summary>
        private class EditorTestCallbacks : ICallbacks
        {
            private readonly string _description;
            private int _totalTests;
            private int _completedTests;
            private int _passedTests;
            private int _failedTests;
            private int _skippedTests;
            private DateTime _startTime;
            
            public EditorTestCallbacks(string description)
            {
                _description = description;
            }
            
            public void RunStarted(ITestAdaptor testsToRun)
            {
                _startTime = DateTime.Now;
                _totalTests = CountTests(testsToRun);
                _completedTests = 0;
                _passedTests = 0;
                _failedTests = 0;
                _skippedTests = 0;
                
                Debug.Log($"[TEST-RUNNER] {_description} started");
                Debug.Log($"[TEST-RUNNER] Total tests to run: {_totalTests}");
                
                // Show progress bar
                EditorUtility.DisplayProgressBar("Running Tests", $"Starting {_description}...", 0f);
            }
            
            public void RunFinished(ITestResultAdaptor result)
            {
                var duration = DateTime.Now - _startTime;
                
                // Clear progress bar
                EditorUtility.ClearProgressBar();
                
                Debug.Log($"[TEST-RUNNER] ========================================");
                Debug.Log($"[TEST-RUNNER] {_description} completed");
                Debug.Log($"[TEST-RUNNER] Duration: {duration.TotalSeconds:F2} seconds");
                Debug.Log($"[TEST-RUNNER] Results: {_passedTests} passed, {_failedTests} failed, {_skippedTests} skipped");
                Debug.Log($"[TEST-RUNNER] ========================================");
                
                // Show notification only if not triggered by file (check for suppression flag)
                bool suppressDialog = EditorPrefs.GetBool("TestRunner.SuppressDialog", false);
                
                if (!suppressDialog)
                {
                    if (_failedTests > 0)
                    {
                        EditorUtility.DisplayDialog(
                            "Tests Failed",
                            $"{_description} completed with failures:\n\n" +
                            $"Passed: {_passedTests}\n" +
                            $"Failed: {_failedTests}\n" +
                            $"Skipped: {_skippedTests}\n" +
                            $"Duration: {duration.TotalSeconds:F2}s",
                            "OK"
                        );
                    }
                    else
                    {
                        EditorUtility.DisplayDialog(
                            "Tests Passed",
                            $"All {_description} passed successfully!\n\n" +
                            $"Passed: {_passedTests}\n" +
                            $"Duration: {duration.TotalSeconds:F2}s",
                            "OK"
                        );
                    }
                }
                
                // Clean up exporter
                if (_exporter != null)
                {
                    _exporter.UnregisterExporter();
                    _exporter = null;
                }
                
                _isRunning = false;
            }
            
            public void TestStarted(ITestAdaptor test)
            {
                if (!test.HasChildren)
                {
                    Debug.Log($"[TEST] Running: {test.FullName}");
                    
                    // Update progress bar
                    var progress = (float)_completedTests / _totalTests;
                    EditorUtility.DisplayProgressBar("Running Tests", $"Test {_completedTests + 1}/{_totalTests}: {test.Name}", progress);
                }
            }
            
            public void TestFinished(ITestResultAdaptor result)
            {
                if (!result.HasChildren)
                {
                    _completedTests++;
                    
                    var statusSymbol = "";
                    var statusColor = "";
                    
                    switch (result.TestStatus)
                    {
                        case TestStatus.Passed:
                            _passedTests++;
                            statusSymbol = "✓";
                            statusColor = "green";
                            Debug.Log($"<color={statusColor}>[TEST] {statusSymbol} PASSED: {result.Test.FullName} ({result.Duration:F3}s)</color>");
                            break;
                            
                        case TestStatus.Failed:
                            _failedTests++;
                            statusSymbol = "✗";
                            statusColor = "red";
                            Debug.LogError($"[TEST] {statusSymbol} FAILED: {result.Test.FullName}");
                            if (!string.IsNullOrEmpty(result.Message))
                            {
                                Debug.LogError($"[TEST]   Message: {result.Message}");
                            }
                            if (!string.IsNullOrEmpty(result.StackTrace))
                            {
                                Debug.LogError($"[TEST]   Stack: {result.StackTrace}");
                            }
                            break;
                            
                        case TestStatus.Skipped:
                            _skippedTests++;
                            statusSymbol = "-";
                            statusColor = "yellow";
                            Debug.LogWarning($"[TEST] {statusSymbol} SKIPPED: {result.Test.FullName}");
                            if (!string.IsNullOrEmpty(result.Message))
                            {
                                Debug.LogWarning($"[TEST]   Reason: {result.Message}");
                            }
                            break;
                            
                        case TestStatus.Inconclusive:
                            statusSymbol = "?";
                            statusColor = "yellow";
                            Debug.LogWarning($"[TEST] {statusSymbol} INCONCLUSIVE: {result.Test.FullName}");
                            break;
                    }
                    
                    // Update progress
                    var progress = (float)_completedTests / _totalTests;
                    EditorUtility.DisplayProgressBar("Running Tests", $"Test {_completedTests}/{_totalTests}: {result.Test.Name}", progress);
                }
            }
            
            private int CountTests(ITestAdaptor test)
            {
                if (!test.HasChildren)
                    return 1;
                
                int count = 0;
                if (test.Children != null)
                {
                    foreach (var child in test.Children)
                    {
                        count += CountTests(child);
                    }
                }
                return count;
            }
        }
        
        #endregion
        
        #region Utility
        
        /// <summary>
        /// Clear all files from TestResults directory
        /// </summary>
        private static void ClearTestResultsDirectory(string directory)
        {
            try
            {
                if (Directory.Exists(directory))
                {
                    // Get all files in the directory
                    var files = Directory.GetFiles(directory);
                    var fileCount = files.Length;
                    
                    // Delete all files
                    foreach (var file in files)
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"[TEST-RUNNER] Could not delete file {Path.GetFileName(file)}: {ex.Message}");
                        }
                    }
                    
                    // Delete all subdirectories
                    var subdirs = Directory.GetDirectories(directory);
                    foreach (var subdir in subdirs)
                    {
                        try
                        {
                            Directory.Delete(subdir, true);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"[TEST-RUNNER] Could not delete directory {Path.GetFileName(subdir)}: {ex.Message}");
                        }
                    }
                    
                    if (fileCount > 0)
                    {
                        Debug.Log($"[TEST-RUNNER] Cleared {fileCount} files from TestResults directory");
                    }
                }
                else
                {
                    // Create the directory if it doesn't exist
                    Directory.CreateDirectory(directory);
                    Debug.Log($"[TEST-RUNNER] Created TestResults directory");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TEST-RUNNER] Error clearing TestResults directory: {ex.Message}");
            }
        }
        
        [MenuItem("TestFramework/Run Tests/Stop Current Test Run", false, 100)]
        public static void StopTestRun()
        {
            if (_isRunning)
            {
                EditorUtility.ClearProgressBar();
                _isRunning = false;
                
                if (_exporter != null)
                {
                    _exporter.UnregisterExporter();
                    _exporter = null;
                }
                
                Debug.LogWarning("[TEST-RUNNER] Test run stopped by user");
            }
            else
            {
                Debug.Log("[TEST-RUNNER] No tests are currently running");
            }
        }
        
        [MenuItem("TestFramework/Run Tests/Open Test Results Folder", false, 101)]
        public static void OpenResultsFolder()
        {
            var resultsPath = Path.Combine(Application.dataPath, "..", "TestResults");
            if (!Directory.Exists(resultsPath))
            {
                Directory.CreateDirectory(resultsPath);
            }
            EditorUtility.RevealInFinder(resultsPath);
        }
        
        [MenuItem("TestFramework/Run Tests/Settings/Show Result Popups", false, 150)]
        public static void ToggleResultPopups()
        {
            bool current = !EditorPrefs.GetBool("TestRunner.SuppressDialog", false);
            EditorPrefs.SetBool("TestRunner.SuppressDialog", !current);
            Debug.Log($"[TEST-RUNNER] Result popup dialogs: {(current ? "DISABLED" : "ENABLED")}");
        }
        
        [MenuItem("TestFramework/Run Tests/Settings/Show Result Popups", true)]
        public static bool ToggleResultPopupsValidate()
        {
            bool suppressed = EditorPrefs.GetBool("TestRunner.SuppressDialog", false);
            Menu.SetChecked("TestFramework/Run Tests/Settings/Show Result Popups", !suppressed);
            return true;
        }
        
        #endregion
        
        #region Debug Classes
        
        /// <summary>
        /// Debug callback to discover and list all available tests
        /// </summary>
        private class TestDiscoveryCallbacks : ICallbacks
        {
            private int _testCount = 0;
            
            public void RunStarted(ITestAdaptor testsToRun)
            {
                Debug.Log("[TEST-DEBUG] ========== Test Discovery Started ==========");
                LogTestHierarchy(testsToRun, 0);
                Debug.Log($"[TEST-DEBUG] Total tests found: {_testCount}");
                Debug.Log("[TEST-DEBUG] ========================================");
            }
            
            private void LogTestHierarchy(ITestAdaptor test, int depth)
            {
                var indent = new string(' ', depth * 2);
                
                if (test.HasChildren)
                {
                    Debug.Log($"{indent}[Suite] {test.FullName} (Children: {test.Children?.Count() ?? 0})");
                    
                    if (test.Children != null)
                    {
                        foreach (var child in test.Children)
                        {
                            LogTestHierarchy(child, depth + 1);
                        }
                    }
                }
                else
                {
                    _testCount++;
                    Debug.Log($"{indent}[Test {_testCount}] {test.FullName}");
                }
            }
            
            public void RunFinished(ITestResultAdaptor result)
            {
                // Not needed for discovery
            }
            
            public void TestStarted(ITestAdaptor test)
            {
                // Not needed for discovery
            }
            
            public void TestFinished(ITestResultAdaptor result)
            {
                // Not needed for discovery
            }
        }
        
        #endregion
    }
}