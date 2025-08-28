using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using UnityEditor.TestTools.TestRunner.Api;

namespace PerSpec.Editor.Coordination
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
        
        // File monitoring fields
        private string _testResultsPath;
        private string _initialResultSnapshot;
        private EditorApplication.CallbackFunction _fileMonitorCallback;
        private double _monitorStartTime;
        private double _lastFileCheckTime;
        private const double FILE_CHECK_INTERVAL = 2.0; // Check every 2 seconds
        private const double MAX_WAIT_TIME = 300.0; // 5 minute timeout
        private bool _isMonitoring;
        private bool _hasCompletedViaCallback;
        
        public TestExecutor(SQLiteManager dbManager)
        {
            _dbManager = dbManager;
            _testResults = new Dictionary<string, TestResult>();
            _testApi = ScriptableObject.CreateInstance<TestRunnerApi>();
            
            // Initialize test results path
            string projectPath = Directory.GetParent(Application.dataPath).FullName;
            _testResultsPath = Path.Combine(projectPath, "TestResults");
        }
        
        public void ExecuteTests(TestRequest request, Filter filter, Action<TestRequest, bool, string, TestResultSummary> onComplete)
        {
            _currentRequest = request;
            _onComplete = onComplete;
            _currentSummary = new TestResultSummary();
            _testResults.Clear();
            _startTime = Time.realtimeSinceStartup;
            _hasCompletedViaCallback = false;
            
            try
            {
                // Start file monitoring before test execution
                StartFileMonitoring();
                
                // Register callbacks
                _testApi.RegisterCallbacks(this);
                
                // Create execution settings with synchronous run for PlayMode to avoid issues
                var settings = new ExecutionSettings(filter);
                
                // For PlayMode tests, we rely heavily on file monitoring due to Unity Test Framework limitations
                if (filter.testMode == TestMode.PlayMode)
                {
                    Debug.Log($"[TestExecutor] PlayMode test detected, relying on file monitoring for completion");
                    _dbManager.LogExecution(request.Id, "INFO", "TestExecutor", "PlayMode test - using file monitoring");
                }
                
                try
                {
                    // Execute tests with the filter
                    _testApi.Execute(settings);
                    
                    Debug.Log($"[TestExecutor] Started test execution for request {request.Id} with file monitoring");
                    _dbManager.LogExecution(request.Id, "INFO", "TestExecutor", "Test execution started with file monitoring");
                }
                catch (NullReferenceException nre)
                {
                    // Known issue with PlayMode tests - rely on file monitoring
                    Debug.LogWarning($"[TestExecutor] PlayMode test execution error (expected): {nre.Message}");
                    Debug.Log($"[TestExecutor] Continuing with file monitoring for request {request.Id}");
                    _dbManager.LogExecution(request.Id, "WARNING", "TestExecutor", 
                        "PlayMode execution error - continuing with file monitoring");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[TestExecutor] Failed to start test execution: {e.Message}");
                _dbManager.LogExecution(request.Id, "ERROR", "TestExecutor", $"Failed to start: {e.Message}");
                
                // Don't immediately fail for PlayMode - let file monitoring try
                if (filter.testMode != TestMode.PlayMode)
                {
                    if (_onComplete != null)
                    {
                        _onComplete(_currentRequest, false, e.Message, null);
                    }
                    Cleanup();
                }
                else
                {
                    Debug.Log($"[TestExecutor] PlayMode error - continuing with file monitoring");
                }
            }
        }
        
        public void RunStarted(ITestAdaptor testsToRun)
        {
            try
            {
                Debug.Log($"[TestExecutor] Test run started via callback");
                
                if (_currentRequest != null)
                {
                    _dbManager.LogExecution(_currentRequest.Id, "INFO", "TestExecutor", "Test run started via callback");
                    
                    // Count total tests
                    _currentSummary.TotalTests = CountTests(testsToRun);
                    Debug.Log($"[TestExecutor] Total tests to run: {_currentSummary.TotalTests}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[TestExecutor] Error in RunStarted: {e.Message}");
            }
        }
        
        public void RunFinished(ITestResultAdaptor result)
        {
            try
            {
                Debug.Log($"[TestExecutor] Test run finished via callback");
                
                // Prevent duplicate completion
                if (_hasCompletedViaCallback)
                {
                    Debug.Log($"[TestExecutor] Test already completed, skipping callback processing");
                    return;
                }
                
                if (_currentRequest != null)
                {
                    // Mark as completed via callback
                    _hasCompletedViaCallback = true;
                    
                    // Stop file monitoring since callback worked
                    StopFileMonitoring();
                    
                    // Calculate duration
                    _currentSummary.Duration = Time.realtimeSinceStartup - _startTime;
                    
                    // Process all test results
                    ProcessTestResults(result);
                    
                    // Log completion
                    _dbManager.LogExecution(_currentRequest.Id, "INFO", "TestExecutor", 
                        $"Test run completed via callback: {_currentSummary.PassedTests}/{_currentSummary.TotalTests} passed");
                    
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
            catch (Exception e)
            {
                Debug.LogError($"[TestExecutor] Error in RunFinished: {e.Message}");
                
                // Even if callback fails, try to complete via file monitoring
                if (!_hasCompletedViaCallback)
                {
                    Debug.Log($"[TestExecutor] Callback failed, relying on file monitoring");
                }
            }
        }
        
        public void TestStarted(ITestAdaptor test)
        {
            try
            {
                if (test.Method != null)
                {
                    Debug.Log($"[TestExecutor] Test started: {test.FullName}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[TestExecutor] Error in TestStarted: {e.Message}");
            }
        }
        
        public void TestFinished(ITestResultAdaptor result)
        {
            try
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
            catch (Exception e)
            {
                Debug.LogError($"[TestExecutor] Error in TestFinished: {e.Message}");
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
            // Stop file monitoring
            StopFileMonitoring();
            
            // Unregister callbacks
            if (_testApi != null)
            {
                _testApi.UnregisterCallbacks(this);
            }
            
            _currentRequest = null;
            _onComplete = null;
            _testResults.Clear();
            _hasCompletedViaCallback = false;
        }
        
        #region File Monitoring Methods
        
        private void StartFileMonitoring()
        {
            if (_isMonitoring) 
            {
                Debug.Log($"[TestExecutor-FM] Already monitoring, skipping start");
                return;
            }
            
            _isMonitoring = true;
            _hasCompletedViaCallback = false;
            _monitorStartTime = EditorApplication.timeSinceStartup;
            _lastFileCheckTime = _monitorStartTime;
            
            // Take snapshot of current files
            _initialResultSnapshot = GetLatestResultFile();
            Debug.Log($"[TestExecutor-FM] Initial snapshot: {_initialResultSnapshot ?? "NULL"}");
            
            // Set up monitoring callback
            _fileMonitorCallback = MonitorResultFiles;
            EditorApplication.update += _fileMonitorCallback;
            
            Debug.Log($"[TestExecutor-FM] Started file monitoring for request {_currentRequest?.Id}");
            Debug.Log($"[TestExecutor-FM] Monitor start time: {_monitorStartTime:F2}");
            Debug.Log($"[TestExecutor-FM] EditorApplication.update callback registered: {_fileMonitorCallback != null}");
        }
        
        private void StopFileMonitoring()
        {
            if (!_isMonitoring) return;
            
            _isMonitoring = false;
            
            if (_fileMonitorCallback != null)
            {
                EditorApplication.update -= _fileMonitorCallback;
                _fileMonitorCallback = null;
            }
            
            Debug.Log($"[TestExecutor] Stopped file monitoring");
        }
        
        private void MonitorResultFiles()
        {
            if (!_isMonitoring || _hasCompletedViaCallback) 
            {
                Debug.Log($"[TestExecutor-FM] Monitor skipped - isMonitoring: {_isMonitoring}, hasCompleted: {_hasCompletedViaCallback}");
                return;
            }
            
            double currentTime = EditorApplication.timeSinceStartup;
            
            // Check for timeout
            if (currentTime - _monitorStartTime > MAX_WAIT_TIME)
            {
                Debug.LogError($"[TestExecutor-FM] Test execution timed out after {MAX_WAIT_TIME} seconds");
                HandleTestTimeout();
                return;
            }
            
            // Check for new files periodically
            if (currentTime - _lastFileCheckTime >= FILE_CHECK_INTERVAL)
            {
                Debug.Log($"[TestExecutor-FM] File check triggered at {currentTime:F2} (interval: {FILE_CHECK_INTERVAL}s)");
                _lastFileCheckTime = currentTime;
                CheckForNewResultFiles();
            }
        }
        
        private void CheckForNewResultFiles()
        {
            Debug.Log($"[TestExecutor-FM] Checking for new files in: {_testResultsPath}");
            
            if (!Directory.Exists(_testResultsPath)) 
            {
                Debug.LogWarning($"[TestExecutor-FM] TestResults directory doesn't exist: {_testResultsPath}");
                return;
            }
            
            string latestFile = GetLatestResultFile();
            Debug.Log($"[TestExecutor-FM] Latest file: {latestFile ?? "NULL"}");
            Debug.Log($"[TestExecutor-FM] Initial snapshot: {_initialResultSnapshot ?? "NULL"}");
            
            // Check if a new file appeared
            if (!string.IsNullOrEmpty(latestFile) && latestFile != _initialResultSnapshot)
            {
                Debug.Log($"[TestExecutor-FM] NEW FILE DETECTED: {latestFile}");
                
                // Wait a bit for file to be fully written
                EditorApplication.delayCall += () =>
                {
                    Debug.Log($"[TestExecutor-FM] DelayCall triggered - monitoring: {_isMonitoring}, completed: {_hasCompletedViaCallback}");
                    if (_isMonitoring && !_hasCompletedViaCallback)
                    {
                        ParseResultsFromFile(latestFile);
                    }
                };
            }
            else
            {
                Debug.Log($"[TestExecutor-FM] No new file detected");
            }
        }
        
        private string GetLatestResultFile()
        {
            if (!Directory.Exists(_testResultsPath)) return null;
            
            var xmlFiles = Directory.GetFiles(_testResultsPath, "*.xml")
                .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                .FirstOrDefault();
            
            return xmlFiles;
        }
        
        private void ParseResultsFromFile(string xmlPath)
        {
            try
            {
                // Look for corresponding summary file
                string summaryPath = xmlPath.Replace(".xml", ".summary.txt");
                
                if (File.Exists(summaryPath))
                {
                    ParseSummaryFile(summaryPath);
                }
                else if (File.Exists(xmlPath))
                {
                    ParseXmlFile(xmlPath);
                }
                
                // Mark as completed via file monitoring
                Debug.Log($"[TestExecutor] Test results parsed from file for request {_currentRequest?.Id}");
                
                if (_currentRequest != null && _onComplete != null && !_hasCompletedViaCallback)
                {
                    _hasCompletedViaCallback = true;
                    _onComplete(_currentRequest, true, null, _currentSummary);
                    StopFileMonitoring();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[TestExecutor] Error parsing result file: {e.Message}");
            }
        }
        
        private void ParseSummaryFile(string summaryPath)
        {
            var lines = File.ReadAllLines(summaryPath);
            
            foreach (var line in lines)
            {
                if (line.Contains("Total Tests:"))
                {
                    if (int.TryParse(Regex.Match(line, @"\d+").Value, out int totalTests))
                        _currentSummary.TotalTests = totalTests;
                }
                else if (line.Contains("Passed:"))
                {
                    if (int.TryParse(Regex.Match(line, @"\d+").Value, out int passedTests))
                        _currentSummary.PassedTests = passedTests;
                }
                else if (line.Contains("Failed:"))
                {
                    if (int.TryParse(Regex.Match(line, @"\d+").Value, out int failedTests))
                        _currentSummary.FailedTests = failedTests;
                }
                else if (line.Contains("Skipped:"))
                {
                    if (int.TryParse(Regex.Match(line, @"\d+").Value, out int skippedTests))
                        _currentSummary.SkippedTests = skippedTests;
                }
                else if (line.Contains("Duration:"))
                {
                    var match = Regex.Match(line, @"[\d.]+");
                    if (match.Success)
                    {
                        if (float.TryParse(match.Value, out float duration))
                            _currentSummary.Duration = duration;
                    }
                }
            }
            
            Debug.Log($"[TestExecutor] Parsed summary - Total: {_currentSummary.TotalTests}, " +
                     $"Passed: {_currentSummary.PassedTests}, Failed: {_currentSummary.FailedTests}");
        }
        
        private void ParseXmlFile(string xmlPath)
        {
            try
            {
                var doc = XDocument.Load(xmlPath);
                var testRun = doc.Root;
                
                if (testRun != null)
                {
                    _currentSummary.TotalTests = int.Parse(testRun.Attribute("total")?.Value ?? "0");
                    _currentSummary.PassedTests = int.Parse(testRun.Attribute("passed")?.Value ?? "0");
                    _currentSummary.FailedTests = int.Parse(testRun.Attribute("failed")?.Value ?? "0");
                    _currentSummary.SkippedTests = int.Parse(testRun.Attribute("skipped")?.Value ?? "0");
                    
                    var duration = testRun.Attribute("duration")?.Value;
                    if (!string.IsNullOrEmpty(duration))
                    {
                        if (float.TryParse(duration, out float durationValue))
                            _currentSummary.Duration = durationValue;
                    }
                }
                
                Debug.Log($"[TestExecutor] Parsed XML - Total: {_currentSummary.TotalTests}, " +
                         $"Passed: {_currentSummary.PassedTests}, Failed: {_currentSummary.FailedTests}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[TestExecutor] Error parsing XML: {e.Message}");
            }
        }
        
        private void HandleTestTimeout()
        {
            StopFileMonitoring();
            
            if (_currentRequest != null && _onComplete != null && !_hasCompletedViaCallback)
            {
                _hasCompletedViaCallback = true;
                _dbManager.LogExecution(_currentRequest.Id, "ERROR", "TestExecutor", 
                    $"Test execution timed out after {MAX_WAIT_TIME} seconds");
                _onComplete(_currentRequest, false, $"Test execution timed out after {MAX_WAIT_TIME} seconds", null);
            }
        }
        
        #endregion
        
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