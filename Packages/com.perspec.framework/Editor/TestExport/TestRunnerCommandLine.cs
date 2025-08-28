using System;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using TestStatus = UnityEditor.TestTools.TestRunner.Api.TestStatus;

namespace PerSpec.Editor.TestExport
{
    /// <summary>
    /// Command line interface for running tests via -executeMethod
    /// This allows running tests in an existing Unity instance without batch mode
    /// </summary>
    public static class TestRunnerCommandLine
    {
        private static bool _testsCompleted = false;
        private static int _exitCode = 0;
        private static TestResultXMLExporter _exporter;
        
        #region Main Entry Points
        
        /// <summary>
        /// Run all tests from command line
        /// Usage: Unity.exe -projectPath "path" -executeMethod TestRunnerCommandLine.RunAllTests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("[TEST-CLI] Running all tests via executeMethod...");
            Debug.Log("[TEST-CLI] Running all tests via executeMethod...");
            
            var filter = new Filter()
            {
                testMode = TestMode.EditMode | TestMode.PlayMode
            };
            
            ExecuteTests(filter, "All Tests");
        }
        
        /// <summary>
        /// Run EditMode tests from command line
        /// Usage: Unity.exe -projectPath "path" -executeMethod TestRunnerCommandLine.RunEditModeTests
        /// </summary>
        public static void RunEditModeTests()
        {
            Console.WriteLine("[TEST-CLI] Running EditMode tests via executeMethod...");
            Debug.Log("[TEST-CLI] Running EditMode tests via executeMethod...");
            
            var filter = new Filter()
            {
                testMode = TestMode.EditMode
            };
            
            ExecuteTests(filter, "EditMode Tests");
        }
        
        /// <summary>
        /// Run PlayMode tests from command line
        /// Usage: Unity.exe -projectPath "path" -executeMethod TestRunnerCommandLine.RunPlayModeTests
        /// </summary>
        public static void RunPlayModeTests()
        {
            Console.WriteLine("[TEST-CLI] Running PlayMode tests via executeMethod...");
            Debug.Log("[TEST-CLI] Running PlayMode tests via executeMethod...");
            
            var filter = new Filter()
            {
                testMode = TestMode.PlayMode
            };
            
            ExecuteTests(filter, "PlayMode Tests");
        }
        
        /// <summary>
        /// Run tests with custom filter from command line arguments
        /// Usage: Unity.exe -projectPath "path" -executeMethod TestRunnerCommandLine.RunFilteredTests -testFilter "namespace.class" -testCategories "Unit,Integration"
        /// </summary>
        public static void RunFilteredTests()
        {
            var args = Environment.GetCommandLineArgs();
            string testFilter = null;
            string testCategories = null;
            TestMode testMode = TestMode.EditMode;
            
            // Parse command line arguments
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-testFilter":
                        if (i + 1 < args.Length)
                            testFilter = args[i + 1];
                        break;
                        
                    case "-testCategories":
                        if (i + 1 < args.Length)
                            testCategories = args[i + 1];
                        break;
                        
                    case "-testPlatform":
                        if (i + 1 < args.Length)
                        {
                            var platform = args[i + 1];
                            if (platform.Equals("PlayMode", StringComparison.OrdinalIgnoreCase))
                                testMode = TestMode.PlayMode;
                            else if (platform.Equals("Both", StringComparison.OrdinalIgnoreCase))
                                testMode = TestMode.EditMode | TestMode.PlayMode;
                        }
                        break;
                }
            }
            
            Console.WriteLine($"[TEST-CLI] Running filtered tests: Filter={testFilter}, Categories={testCategories}, Mode={testMode}");
            Debug.Log($"[TEST-CLI] Running filtered tests: Filter={testFilter}, Categories={testCategories}, Mode={testMode}");
            
            var filter = new Filter()
            {
                testMode = testMode
            };
            
            if (!string.IsNullOrEmpty(testFilter))
            {
                filter.testNames = testFilter.Split(',');
            }
            
            if (!string.IsNullOrEmpty(testCategories))
            {
                filter.categoryNames = testCategories.Split(',');
            }
            
            ExecuteTests(filter, "Filtered Tests");
        }
        
        #endregion
        
        #region Predefined Test Suites
        
        public static void RunUnitTests()
        {
            Console.WriteLine("[TEST-CLI] Running Unit tests...");
            Debug.Log("[TEST-CLI] Running Unit tests...");
            
            var filter = new Filter()
            {
                testMode = TestMode.EditMode,
                testNames = new[] { "TestFramework.Unity.Tests.Unit" }
            };
            
            ExecuteTests(filter, "Unit Tests");
        }
        
        public static void RunIntegrationTests()
        {
            Console.WriteLine("[TEST-CLI] Running Integration tests...");
            Debug.Log("[TEST-CLI] Running Integration tests...");
            
            var filter = new Filter()
            {
                testMode = TestMode.PlayMode,
                testNames = new[] { "TestFramework.Unity.Tests.Integration" }
            };
            
            ExecuteTests(filter, "Integration Tests");
        }
        
        public static void RunCriticalTests()
        {
            Console.WriteLine("[TEST-CLI] Running Critical tests...");
            Debug.Log("[TEST-CLI] Running Critical tests...");
            
            var filter = new Filter()
            {
                testMode = TestMode.EditMode | TestMode.PlayMode,
                categoryNames = new[] { "Critical", "Smoke", "Regression" }
            };
            
            ExecuteTests(filter, "Critical Tests");
        }
        
        #endregion
        
        #region Test Execution
        
        private static void ExecuteTests(Filter filter, string description)
        {
            try
            {
                _testsCompleted = false;
                _exitCode = 0;
                
                // Clear TestResults directory
                var testResultsDir = Path.Combine(Application.dataPath, "..", "TestResults");
                ClearTestResultsDirectory(testResultsDir);
                
                // Setup output path
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var outputPath = GetOutputPath(timestamp);
                
                Console.WriteLine($"[TEST-CLI] Cleared TestResults directory");
                Console.WriteLine($"[TEST-CLI] XML results will be saved to: {outputPath}");
                Debug.Log($"[TEST-CLI] Cleared TestResults directory");
                Debug.Log($"[TEST-CLI] XML results will be saved to: {outputPath}");
                
                // Create exporter
                _exporter = new TestResultXMLExporter(outputPath, true);
                
                // Create API instance
                var api = ScriptableObject.CreateInstance<TestRunnerApi>();
                
                // Register callbacks
                api.RegisterCallbacks(_exporter);
                api.RegisterCallbacks(new CommandLineCallbacks());
                
                // Execute tests
                var settings = new ExecutionSettings(filter);
                api.Execute(settings);
                
                // Wait for completion
                EditorApplication.update += WaitForCompletion;
                
                // Keep Unity alive while tests run
                while (!_testsCompleted)
                {
                    System.Threading.Thread.Sleep(100);
                    
                    // Process Unity events
                    if (EditorApplication.timeSinceStartup > 0)
                    {
                        EditorApplication.QueuePlayerLoopUpdate();
                    }
                }
                
                // Write exit code file for external scripts
                WriteExitCode(_exitCode);
                
                // Exit if running from command line
                if (IsCommandLineExecution())
                {
                    Console.WriteLine($"[TEST-CLI] Exiting with code: {_exitCode}");
                    EditorApplication.Exit(_exitCode);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TEST-CLI-ERROR] Failed to execute tests: {ex.Message}");
                Debug.LogError($"[TEST-CLI-ERROR] Failed to execute tests: {ex.Message}\n{ex.StackTrace}");
                
                WriteExitCode(1);
                
                if (IsCommandLineExecution())
                {
                    EditorApplication.Exit(1);
                }
            }
        }
        
        private static void WaitForCompletion()
        {
            if (_testsCompleted)
            {
                EditorApplication.update -= WaitForCompletion;
                
                if (_exporter != null)
                {
                    _exporter.UnregisterExporter();
                    _exporter = null;
                }
            }
        }
        
        private static string GetOutputPath(string timestamp)
        {
            // Check for command line override
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-testResultFile")
                {
                    return args[i + 1];
                }
            }
            
            // Default path
            var directory = Path.Combine(Application.dataPath, "..", "TestResults");
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            return Path.Combine(directory, $"TestResults_CLI_{timestamp}.xml");
        }
        
        private static void WriteExitCode(int code)
        {
            try
            {
                var exitCodeFile = Path.Combine(Application.dataPath, "..", "TestResults", "exit_code.txt");
                File.WriteAllText(exitCodeFile, code.ToString());
                
                Console.WriteLine($"[TEST-CLI] Exit code written to: {exitCodeFile}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TEST-CLI-ERROR] Failed to write exit code: {ex.Message}");
            }
        }
        
        private static bool IsCommandLineExecution()
        {
            var args = Environment.GetCommandLineArgs();
            foreach (var arg in args)
            {
                if (arg.Contains("-executeMethod"))
                    return true;
            }
            return false;
        }
        
        #endregion
        
        #region Callbacks
        
        private class CommandLineCallbacks : ICallbacks
        {
            private int _totalTests;
            private int _completedTests;
            private int _failedTests;
            private DateTime _startTime;
            
            public void RunStarted(ITestAdaptor testsToRun)
            {
                _startTime = DateTime.Now;
                _totalTests = CountTests(testsToRun);
                _completedTests = 0;
                _failedTests = 0;
                
                Console.WriteLine($"[TEST-CLI] Test run started with {_totalTests} tests");
                Debug.Log($"[TEST-CLI] Test run started with {_totalTests} tests");
            }
            
            public void RunFinished(ITestResultAdaptor result)
            {
                var duration = DateTime.Now - _startTime;
                
                Console.WriteLine($"[TEST-CLI] Test run completed in {duration.TotalSeconds:F2} seconds");
                Console.WriteLine($"[TEST-CLI] Results: {_completedTests} completed, {_failedTests} failed");
                
                Debug.Log($"[TEST-CLI] Test run completed in {duration.TotalSeconds:F2} seconds");
                Debug.Log($"[TEST-CLI] Results: {_completedTests} completed, {_failedTests} failed");
                
                // Set exit code based on results
                _exitCode = _failedTests > 0 ? 1 : 0;
                _testsCompleted = true;
            }
            
            public void TestStarted(ITestAdaptor test)
            {
                if (!test.HasChildren)
                {
                    Console.WriteLine($"[TEST-CLI] Running: {test.FullName}");
                    Debug.Log($"[TEST-CLI] Running: {test.FullName}");
                }
            }
            
            public void TestFinished(ITestResultAdaptor result)
            {
                if (!result.HasChildren)
                {
                    _completedTests++;
                    
                    var status = result.TestStatus switch
                    {
                        TestStatus.Passed => "PASSED",
                        TestStatus.Failed => "FAILED",
                        TestStatus.Skipped => "SKIPPED",
                        _ => "UNKNOWN"
                    };
                    
                    Console.WriteLine($"[TEST-CLI] [{status}] {result.Test.FullName} ({result.Duration:F3}s)");
                    
                    if (result.TestStatus == TestStatus.Failed)
                    {
                        _failedTests++;
                        
                        if (!string.IsNullOrEmpty(result.Message))
                        {
                            Console.WriteLine($"[TEST-CLI]   Message: {result.Message}");
                            Debug.LogError($"[TEST-CLI] Failed: {result.Test.FullName}\n{result.Message}");
                        }
                    }
                    else
                    {
                        Debug.Log($"[TEST-CLI] [{status}] {result.Test.FullName} ({result.Duration:F3}s)");
                    }
                    
                    // Show progress
                    if (_completedTests % 10 == 0 || _completedTests == _totalTests)
                    {
                        var progress = (_completedTests * 100.0f / _totalTests);
                        Console.WriteLine($"[TEST-CLI] Progress: {_completedTests}/{_totalTests} ({progress:F1}%)");
                    }
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
        
        #region Helper Methods
        
        private static void ClearTestResultsDirectory(string directory)
        {
            try
            {
                if (Directory.Exists(directory))
                {
                    // Delete all files
                    foreach (var file in Directory.GetFiles(directory))
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch { }
                    }
                    
                    // Delete all subdirectories
                    foreach (var subdir in Directory.GetDirectories(directory))
                    {
                        try
                        {
                            Directory.Delete(subdir, true);
                        }
                        catch { }
                    }
                }
                else
                {
                    Directory.CreateDirectory(directory);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TEST-CLI] Warning: Could not clear TestResults directory: {ex.Message}");
            }
        }
        
        #endregion
    }
}