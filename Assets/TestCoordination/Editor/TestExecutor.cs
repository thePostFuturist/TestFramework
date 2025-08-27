using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using UnityEditor.TestTools.TestRunner.Api;

namespace TestCoordination
{
    public class TestExecutor : ICallbacks
    {
        private SQLiteManager _dbManager;
        private TestRequest _currentRequest;
        private Action<TestRequest, bool, string, TestResultSummary> _onComplete;
        private TestResultSummary _currentSummary;
        private Dictionary<string, TestResult> _testResults;
        private float _startTime;
        private TestRunnerApi _testApi;
        
        public TestExecutor(SQLiteManager dbManager)
        {
            _dbManager = dbManager;
            _testResults = new Dictionary<string, TestResult>();
            _testApi = ScriptableObject.CreateInstance<TestRunnerApi>();
        }
        
        public void ExecuteTests(TestRequest request, Filter filter, Action<TestRequest, bool, string, TestResultSummary> onComplete)
        {
            _currentRequest = request;
            _onComplete = onComplete;
            _currentSummary = new TestResultSummary();
            _testResults.Clear();
            _startTime = Time.realtimeSinceStartup;
            
            try
            {
                // Register callbacks
                _testApi.RegisterCallbacks(this);
                
                // Execute tests with the filter
                _testApi.Execute(new ExecutionSettings(filter));
                
                Debug.Log($"[TestExecutor] Started test execution for request {request.Id}");
                _dbManager.LogExecution(request.Id, "INFO", "TestExecutor", "Test execution started");
            }
            catch (Exception e)
            {
                Debug.LogError($"[TestExecutor] Failed to start test execution: {e.Message}");
                _dbManager.LogExecution(request.Id, "ERROR", "TestExecutor", $"Failed to start: {e.Message}");
                
                if (_onComplete != null)
                {
                    _onComplete(_currentRequest, false, e.Message, null);
                }
                
                Cleanup();
            }
        }
        
        public void RunStarted(ITestAdaptor testsToRun)
        {
            Debug.Log($"[TestExecutor] Test run started");
            
            if (_currentRequest != null)
            {
                _dbManager.LogExecution(_currentRequest.Id, "INFO", "TestExecutor", "Test run started");
                
                // Count total tests
                _currentSummary.TotalTests = CountTests(testsToRun);
                Debug.Log($"[TestExecutor] Total tests to run: {_currentSummary.TotalTests}");
            }
        }
        
        public void RunFinished(ITestResultAdaptor result)
        {
            Debug.Log($"[TestExecutor] Test run finished");
            
            if (_currentRequest != null)
            {
                // Calculate duration
                _currentSummary.Duration = Time.realtimeSinceStartup - _startTime;
                
                // Process all test results
                ProcessTestResults(result);
                
                // Log completion
                _dbManager.LogExecution(_currentRequest.Id, "INFO", "TestExecutor", 
                    $"Test run completed: {_currentSummary.PassedTests}/{_currentSummary.TotalTests} passed");
                
                Debug.Log($"[TestExecutor] Test results - Passed: {_currentSummary.PassedTests}, " +
                         $"Failed: {_currentSummary.FailedTests}, Skipped: {_currentSummary.SkippedTests}");
                
                // Save individual test results to database
                SaveTestResultsToDatabase();
                
                // Notify completion
                if (_onComplete != null)
                {
                    _onComplete(_currentRequest, true, null, _currentSummary);
                }
            }
            
            Cleanup();
        }
        
        public void TestStarted(ITestAdaptor test)
        {
            if (test.Method != null)
            {
                Debug.Log($"[TestExecutor] Test started: {test.FullName}");
            }
        }
        
        public void TestFinished(ITestResultAdaptor result)
        {
            if (result.Test.Method != null)
            {
                Debug.Log($"[TestExecutor] Test finished: {result.Test.FullName} - {result.TestStatus}");
                
                // Store test result
                _testResults[result.Test.FullName] = new TestResult
                {
                    Name = result.Test.FullName,
                    ClassName = result.Test.Parent?.Name,
                    MethodName = result.Test.Method.Name,
                    Status = result.TestStatus,
                    Duration = (float)(result.Duration * 1000), // Convert to milliseconds
                    Message = result.Message,
                    StackTrace = result.StackTrace
                };
            }
        }
        
        private void ProcessTestResults(ITestResultAdaptor result)
        {
            // Reset counters
            _currentSummary.PassedTests = 0;
            _currentSummary.FailedTests = 0;
            _currentSummary.SkippedTests = 0;
            
            // Count results recursively
            CountTestResults(result);
        }
        
        private void CountTestResults(ITestResultAdaptor result)
        {
            if (result.Test.Method != null)
            {
                // This is a test method
                switch (result.TestStatus)
                {
                    case TestStatus.Passed:
                        _currentSummary.PassedTests++;
                        break;
                    case TestStatus.Failed:
                        _currentSummary.FailedTests++;
                        break;
                    case TestStatus.Skipped:
                        _currentSummary.SkippedTests++;
                        break;
                }
            }
            
            // Process child results
            if (result.Children != null)
            {
                foreach (var child in result.Children)
                {
                    CountTestResults(child);
                }
            }
        }
        
        private int CountTests(ITestAdaptor test)
        {
            if (test.Method != null)
            {
                return 1; // This is a test method
            }
            
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
        
        private void SaveTestResultsToDatabase()
        {
            foreach (var kvp in _testResults)
            {
                var result = kvp.Value;
                
                string resultString = result.Status switch
                {
                    TestStatus.Passed => "Passed",
                    TestStatus.Failed => "Failed",
                    TestStatus.Skipped => "Skipped",
                    _ => "Inconclusive"
                };
                
                _dbManager.InsertTestResult(
                    _currentRequest.Id,
                    result.Name,
                    result.ClassName,
                    result.MethodName,
                    resultString,
                    result.Duration,
                    result.Message,
                    result.StackTrace
                );
            }
        }
        
        private void Cleanup()
        {
            // Unregister callbacks
            if (_testApi != null)
            {
                _testApi.UnregisterCallbacks(this);
            }
            
            _currentRequest = null;
            _onComplete = null;
            _testResults.Clear();
        }
        
        private class TestResult
        {
            public string Name { get; set; }
            public string ClassName { get; set; }
            public string MethodName { get; set; }
            public TestStatus Status { get; set; }
            public float Duration { get; set; }
            public string Message { get; set; }
            public string StackTrace { get; set; }
        }
    }
}