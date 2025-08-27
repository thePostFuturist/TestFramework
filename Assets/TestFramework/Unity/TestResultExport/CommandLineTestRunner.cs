using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using TestStatus = UnityEditor.TestTools.TestRunner.Api.TestStatus;

namespace TestFramework.Unity.TestResultExport
{
    /// <summary>
    /// Command-line test runner with XML export support.
    /// Usage: Unity.exe -batchmode -runTests -testPlatform EditMode -testResultFile results.xml
    /// </summary>
    public static class CommandLineTestRunner
    {
        private static TestResultXMLExporter _exporter;
        private static bool _testsCompleted;
        private static bool _testsFailed;
        private static string _outputPath;
        
        [InitializeOnLoadMethod]
        public static void Initialize()
        {
            if (!Application.isBatchMode)
                return;
            
            var args = Environment.GetCommandLineArgs();
            
            if (!args.Contains("-runTests"))
                return;
            
            ParseCommandLineArgs(args);
            EditorApplication.delayCall += RunTests;
        }
        
        private static void ParseCommandLineArgs(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-testResultFile":
                        if (i + 1 < args.Length)
                        {
                            _outputPath = args[i + 1];
                            Debug.Log($"[TEST-CLI] Output path set to: {_outputPath}");
                        }
                        break;
                        
                    case "-testPlatform":
                        if (i + 1 < args.Length)
                        {
                            var platform = args[i + 1];
                            Debug.Log($"[TEST-CLI] Test platform: {platform}");
                        }
                        break;
                        
                    case "-testFilter":
                        if (i + 1 < args.Length)
                        {
                            var filter = args[i + 1];
                            Debug.Log($"[TEST-CLI] Test filter: {filter}");
                        }
                        break;
                }
            }
            
            if (string.IsNullOrEmpty(_outputPath))
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                _outputPath = Path.Combine(Application.dataPath, "..", "TestResults", $"TestResults_{timestamp}.xml");
                Debug.Log($"[TEST-CLI] Using default output path: {_outputPath}");
            }
        }
        
        private static void RunTests()
        {
            try
            {
                _exporter = new TestResultXMLExporter(_outputPath, true);
                
                var api = ScriptableObject.CreateInstance<TestRunnerApi>();
                
                api.RegisterCallbacks(_exporter);
                api.RegisterCallbacks(new CommandLineCallbacks());
                
                var filter = CreateFilterFromCommandLine();
                var executionSettings = new ExecutionSettings(filter);
                api.Execute(executionSettings);
                
                Debug.Log("[TEST-CLI] Test execution started");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TEST-CLI-ERROR] Failed to run tests: {ex.Message}\n{ex.StackTrace}");
                EditorApplication.Exit(1);
            }
        }
        
        private static Filter CreateFilterFromCommandLine()
        {
            var filter = new Filter();
            var args = Environment.GetCommandLineArgs();
            
            var platformIndex = Array.IndexOf(args, "-testPlatform");
            if (platformIndex >= 0 && platformIndex + 1 < args.Length)
            {
                var platform = args[platformIndex + 1];
                switch (platform.ToLower())
                {
                    case "editmode":
                        filter.testMode = TestMode.EditMode;
                        break;
                    case "playmode":
                        filter.testMode = TestMode.PlayMode;
                        break;
                    default:
                        filter.testMode = TestMode.EditMode | TestMode.PlayMode;
                        break;
                }
            }
            else
            {
                filter.testMode = TestMode.EditMode | TestMode.PlayMode;
            }
            
            var filterIndex = Array.IndexOf(args, "-testFilter");
            if (filterIndex >= 0 && filterIndex + 1 < args.Length)
            {
                var testFilter = args[filterIndex + 1];
                filter.testNames = new[] { testFilter };
                Debug.Log($"[TEST-CLI] Filtering tests by: {testFilter}");
            }
            
            var categoryIndex = Array.IndexOf(args, "-testCategories");
            if (categoryIndex >= 0 && categoryIndex + 1 < args.Length)
            {
                var categories = args[categoryIndex + 1].Split(',');
                filter.categoryNames = categories;
                Debug.Log($"[TEST-CLI] Filtering by categories: {string.Join(", ", categories)}");
            }
            
            return filter;
        }
        
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
                
                Debug.Log($"[TEST-CLI] Starting test run with {_totalTests} tests");
            }
            
            public void RunFinished(ITestResultAdaptor result)
            {
                var duration = DateTime.Now - _startTime;
                
                Debug.Log($"[TEST-CLI] Test run completed in {duration.TotalSeconds:F2} seconds");
                Debug.Log($"[TEST-CLI] Results: {_completedTests}/{_totalTests} tests, {_failedTests} failed");
                
                var exitCode = _failedTests > 0 ? 1 : 0;
                
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(exitCode);
                }
            }
            
            public void TestStarted(ITestAdaptor test)
            {
                Debug.Log($"[TEST-CLI] Running: {test.FullName}");
            }
            
            public void TestFinished(ITestResultAdaptor result)
            {
                _completedTests++;
                
                switch (result.TestStatus)
                {
                    case TestStatus.Passed:
                        Debug.Log($"[TEST-CLI] ✓ PASSED: {result.Test.FullName} ({result.Duration:F3}s)");
                        break;
                        
                    case TestStatus.Failed:
                        _failedTests++;
                        Debug.LogError($"[TEST-CLI] ✗ FAILED: {result.Test.FullName}");
                        if (!string.IsNullOrEmpty(result.Message))
                        {
                            Debug.LogError($"[TEST-CLI]   Message: {result.Message}");
                        }
                        if (!string.IsNullOrEmpty(result.StackTrace))
                        {
                            Debug.LogError($"[TEST-CLI]   Stack: {result.StackTrace}");
                        }
                        break;
                        
                    case TestStatus.Skipped:
                        Debug.LogWarning($"[TEST-CLI] - SKIPPED: {result.Test.FullName}");
                        break;
                        
                    case TestStatus.Inconclusive:
                        Debug.LogWarning($"[TEST-CLI] ? INCONCLUSIVE: {result.Test.FullName}");
                        break;
                }
                
                var progress = (_completedTests * 100.0f / _totalTests);
                if (_completedTests % 10 == 0 || _completedTests == _totalTests)
                {
                    Debug.Log($"[TEST-CLI] Progress: {_completedTests}/{_totalTests} ({progress:F1}%)");
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
    }
}