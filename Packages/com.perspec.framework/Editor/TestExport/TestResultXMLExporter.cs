using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEditor;
using TestStatus = UnityEditor.TestTools.TestRunner.Api.TestStatus;

namespace PerSpec.Editor.TestExport
{
    /// <summary>
    /// Exports Unity test results to NUnit-compatible XML format.
    /// Implements ICallbacks to hook into Unity's test execution pipeline.
    /// </summary>
    public class TestResultXMLExporter : ICallbacks
    {
        #region Fields
        
        private readonly string _outputPath;
        private readonly bool _autoExport;
        private readonly Dictionary<string, TestResult> _testResults;
        private DateTime _runStartTime;
        private DateTime _runEndTime;
        private int _totalTests;
        private int _passedTests;
        private int _failedTests;
        private int _skippedTests;
        private int _inconclusiveTests;
        
        #endregion
        
        #region Properties
        
        public string OutputPath => _outputPath;
        public bool AutoExport => _autoExport;
        public int TotalTests => _totalTests;
        public int PassedTests => _passedTests;
        public int FailedTests => _failedTests;
        
        #endregion
        
        #region Constructor
        
        public TestResultXMLExporter(string outputPath = null, bool autoExport = true)
        {
            _outputPath = outputPath ?? GetDefaultOutputPath();
            _autoExport = autoExport;
            _testResults = new Dictionary<string, TestResult>();
            
            var directory = Path.GetDirectoryName(_outputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
        
        #endregion
        
        #region ICallbacks Implementation
        
        public void RunStarted(ITestAdaptor testsToRun)
        {
            _runStartTime = DateTime.Now;
            _totalTests = CountTests(testsToRun);
            _passedTests = 0;
            _failedTests = 0;
            _skippedTests = 0;
            _inconclusiveTests = 0;
            _testResults.Clear();
            
            Debug.Log($"[TEST-EXPORT] Test run started at {_runStartTime:yyyy-MM-dd HH:mm:ss}");
            Debug.Log($"[TEST-EXPORT] Total tests to run: {_totalTests}");
        }
        
        public void RunFinished(ITestResultAdaptor result)
        {
            _runEndTime = DateTime.Now;
            ProcessTestResults(result);
            
            Debug.Log($"[TEST-EXPORT] Test run finished at {_runEndTime:yyyy-MM-dd HH:mm:ss}");
            Debug.Log($"[TEST-EXPORT] Results - Passed: {_passedTests}, Failed: {_failedTests}, Skipped: {_skippedTests}");
            
            if (_autoExport)
            {
                ExportToXML(result);
            }
        }
        
        public void TestStarted(ITestAdaptor test)
        {
            Debug.Log($"[TEST-EXPORT] Test started: {test.FullName}");
        }
        
        public void TestFinished(ITestResultAdaptor result)
        {
            var testResult = new TestResult
            {
                FullName = result.Test.FullName,
                Name = result.Test.Name,
                ClassName = GetClassName(result.Test),
                TestStatus = result.TestStatus,
                Duration = result.Duration,
                StartTime = result.StartTime,
                EndTime = result.EndTime,
                Message = result.Message,
                StackTrace = result.StackTrace,
                Output = result.Output,
                HasChildren = result.HasChildren
            };
            
            _testResults[result.Test.FullName] = testResult;
            
            switch (result.TestStatus)
            {
                case TestStatus.Passed:
                    _passedTests++;
                    break;
                case TestStatus.Failed:
                    _failedTests++;
                    Debug.LogError($"[TEST-EXPORT] Test failed: {result.Test.FullName}\n{result.Message}");
                    break;
                case TestStatus.Skipped:
                    _skippedTests++;
                    break;
                case TestStatus.Inconclusive:
                    _inconclusiveTests++;
                    break;
            }
        }
        
        #endregion
        
        #region Public Methods
        
        public void ExportToXML(ITestResultAdaptor rootResult = null)
        {
            try
            {
                var doc = CreateNUnitXMLDocument(rootResult);
                
                var settings = new XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = "  ",
                    NewLineChars = "\r\n",
                    NewLineHandling = NewLineHandling.Replace,
                    Encoding = Encoding.UTF8
                };
                
                using (var writer = XmlWriter.Create(_outputPath, settings))
                {
                    doc.Save(writer);
                }
                
                Debug.Log($"[TEST-EXPORT] Test results exported to: {_outputPath}");
                
                var summaryPath = Path.ChangeExtension(_outputPath, ".summary.txt");
                CreateSummaryFile(summaryPath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TEST-EXPORT-ERROR] Failed to export XML: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }
        
        public static TestResultXMLExporter RegisterExporter(string outputPath = null)
        {
            var exporter = new TestResultXMLExporter(outputPath);
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(exporter);
            
            Debug.Log($"[TEST-EXPORT] XML exporter registered. Output path: {exporter.OutputPath}");
            return exporter;
        }
        
        public void UnregisterExporter()
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.UnregisterCallbacks(this);
            
            Debug.Log("[TEST-EXPORT] XML exporter unregistered");
        }
        
        #endregion
        
        #region Private Methods
        
        private XDocument CreateNUnitXMLDocument(ITestResultAdaptor rootResult)
        {
            var doc = new XDocument(new XDeclaration("1.0", "utf-8", "no"));
            
            var testRun = new XElement("test-run",
                new XAttribute("id", "2"),
                new XAttribute("testcasecount", _totalTests),
                new XAttribute("result", GetOverallResult()),
                new XAttribute("total", _totalTests),
                new XAttribute("passed", _passedTests),
                new XAttribute("failed", _failedTests),
                new XAttribute("inconclusive", _inconclusiveTests),
                new XAttribute("skipped", _skippedTests),
                new XAttribute("asserts", "0"),
                new XAttribute("engine-version", Application.unityVersion),
                new XAttribute("clr-version", Environment.Version.ToString()),
                new XAttribute("start-time", _runStartTime.ToString("yyyy-MM-dd HH:mm:ss")),
                new XAttribute("end-time", _runEndTime.ToString("yyyy-MM-dd HH:mm:ss")),
                new XAttribute("duration", (_runEndTime - _runStartTime).TotalSeconds.ToString("F3"))
            );
            
            var environment = new XElement("environment",
                new XAttribute("framework-version", Application.unityVersion),
                new XAttribute("os-version", SystemInfo.operatingSystem),
                new XAttribute("platform", Application.platform.ToString()),
                new XAttribute("cwd", Application.dataPath),
                new XAttribute("machine-name", SystemInfo.deviceName),
                new XAttribute("user", Environment.UserName),
                new XAttribute("user-domain", Environment.UserDomainName)
            );
            testRun.Add(environment);
            
            if (rootResult != null)
            {
                var testSuite = CreateTestSuiteElement(rootResult);
                testRun.Add(testSuite);
            }
            else if (_testResults.Count > 0)
            {
                var testSuite = CreateTestSuiteFromResults();
                testRun.Add(testSuite);
            }
            
            doc.Add(testRun);
            return doc;
        }
        
        private XElement CreateTestSuiteElement(ITestResultAdaptor result)
        {
            var testCaseCount = GetTestCaseCount(result.Test);
            
            var element = new XElement("test-suite",
                new XAttribute("type", GetTestType(result)),
                new XAttribute("id", result.Test.Id),
                new XAttribute("name", result.Test.Name),
                new XAttribute("fullname", result.Test.FullName),
                new XAttribute("testcasecount", testCaseCount),
                new XAttribute("result", result.TestStatus.ToString()),
                new XAttribute("start-time", result.StartTime.ToString("yyyy-MM-dd HH:mm:ss")),
                new XAttribute("end-time", result.EndTime.ToString("yyyy-MM-dd HH:mm:ss")),
                new XAttribute("duration", result.Duration.ToString("F3")),
                new XAttribute("total", testCaseCount),
                new XAttribute("passed", result.PassCount),
                new XAttribute("failed", result.FailCount),
                new XAttribute("inconclusive", result.InconclusiveCount),
                new XAttribute("skipped", result.SkipCount),
                new XAttribute("asserts", result.AssertCount)
            );
            
            if (result.TestStatus == TestStatus.Failed && !string.IsNullOrEmpty(result.Message))
            {
                var failure = new XElement("failure",
                    new XElement("message", new XCData(result.Message ?? "")),
                    new XElement("stack-trace", new XCData(result.StackTrace ?? ""))
                );
                element.Add(failure);
            }
            
            if (!string.IsNullOrEmpty(result.Output))
            {
                element.Add(new XElement("output", new XCData(result.Output)));
            }
            
            if (result.HasChildren && result.Children != null)
            {
                foreach (var child in result.Children)
                {
                    if (child.HasChildren)
                    {
                        element.Add(CreateTestSuiteElement(child));
                    }
                    else
                    {
                        element.Add(CreateTestCaseElement(child));
                    }
                }
            }
            
            return element;
        }
        
        private XElement CreateTestCaseElement(ITestResultAdaptor result)
        {
            var element = new XElement("test-case",
                new XAttribute("id", result.Test.Id),
                new XAttribute("name", result.Test.Name),
                new XAttribute("fullname", result.Test.FullName),
                new XAttribute("methodname", result.Test.Name),
                new XAttribute("classname", GetClassName(result.Test)),
                new XAttribute("result", result.TestStatus.ToString()),
                new XAttribute("start-time", result.StartTime.ToString("yyyy-MM-dd HH:mm:ss")),
                new XAttribute("end-time", result.EndTime.ToString("yyyy-MM-dd HH:mm:ss")),
                new XAttribute("duration", result.Duration.ToString("F3")),
                new XAttribute("asserts", result.AssertCount)
            );
            
            if (result.TestStatus == TestStatus.Failed)
            {
                var failure = new XElement("failure",
                    new XElement("message", new XCData(result.Message ?? "")),
                    new XElement("stack-trace", new XCData(result.StackTrace ?? ""))
                );
                element.Add(failure);
            }
            
            if (result.TestStatus == TestStatus.Skipped && !string.IsNullOrEmpty(result.Message))
            {
                element.Add(new XElement("reason",
                    new XElement("message", new XCData(result.Message))
                ));
            }
            
            if (!string.IsNullOrEmpty(result.Output))
            {
                element.Add(new XElement("output", new XCData(result.Output)));
            }
            
            return element;
        }
        
        private XElement CreateTestSuiteFromResults()
        {
            var suite = new XElement("test-suite",
                new XAttribute("type", "Assembly"),
                new XAttribute("name", "TestResults"),
                new XAttribute("fullname", "TestResults"),
                new XAttribute("testcasecount", _totalTests),
                new XAttribute("result", GetOverallResult()),
                new XAttribute("total", _totalTests),
                new XAttribute("passed", _passedTests),
                new XAttribute("failed", _failedTests),
                new XAttribute("inconclusive", _inconclusiveTests),
                new XAttribute("skipped", _skippedTests)
            );
            
            var testsByClass = _testResults.Values.GroupBy(t => t.ClassName);
            
            foreach (var classGroup in testsByClass)
            {
                var classSuite = new XElement("test-suite",
                    new XAttribute("type", "TestFixture"),
                    new XAttribute("name", classGroup.Key),
                    new XAttribute("fullname", classGroup.Key),
                    new XAttribute("testcasecount", classGroup.Count())
                );
                
                foreach (var test in classGroup)
                {
                    var testCase = new XElement("test-case",
                        new XAttribute("name", test.Name),
                        new XAttribute("fullname", test.FullName),
                        new XAttribute("result", test.TestStatus.ToString()),
                        new XAttribute("duration", test.Duration.ToString("F3"))
                    );
                    
                    if (test.TestStatus == TestStatus.Failed && !string.IsNullOrEmpty(test.Message))
                    {
                        var failure = new XElement("failure",
                            new XElement("message", new XCData(test.Message)),
                            new XElement("stack-trace", new XCData(test.StackTrace ?? ""))
                        );
                        testCase.Add(failure);
                    }
                    
                    if (!string.IsNullOrEmpty(test.Output))
                    {
                        testCase.Add(new XElement("output", new XCData(test.Output)));
                    }
                    
                    classSuite.Add(testCase);
                }
                
                suite.Add(classSuite);
            }
            
            return suite;
        }
        
        private void CreateSummaryFile(string summaryPath)
        {
            var summary = new StringBuilder();
            summary.AppendLine("========================================");
            summary.AppendLine("Unity Test Run Summary");
            summary.AppendLine("========================================");
            summary.AppendLine($"Start Time: {_runStartTime:yyyy-MM-dd HH:mm:ss}");
            summary.AppendLine($"End Time: {_runEndTime:yyyy-MM-dd HH:mm:ss}");
            summary.AppendLine($"Duration: {(_runEndTime - _runStartTime).TotalSeconds:F2} seconds");
            summary.AppendLine();
            summary.AppendLine("Results:");
            summary.AppendLine($"  Total Tests: {_totalTests}");
            summary.AppendLine($"  Passed: {_passedTests}");
            summary.AppendLine($"  Failed: {_failedTests}");
            summary.AppendLine($"  Skipped: {_skippedTests}");
            summary.AppendLine($"  Inconclusive: {_inconclusiveTests}");
            summary.AppendLine($"  Pass Rate: {(_totalTests > 0 ? (_passedTests * 100.0 / _totalTests) : 0):F1}%");
            summary.AppendLine();
            
            if (_failedTests > 0)
            {
                summary.AppendLine("Failed Tests:");
                foreach (var test in _testResults.Values.Where(t => t.TestStatus == TestStatus.Failed))
                {
                    summary.AppendLine($"  - {test.FullName}");
                    if (!string.IsNullOrEmpty(test.Message))
                    {
                        summary.AppendLine($"    {test.Message}");
                    }
                }
            }
            
            File.WriteAllText(summaryPath, summary.ToString());
            Debug.Log($"[TEST-EXPORT] Summary saved to: {summaryPath}");
        }
        
        private void ProcessTestResults(ITestResultAdaptor result)
        {
            if (!result.HasChildren)
            {
                switch (result.TestStatus)
                {
                    case TestStatus.Passed:
                        _passedTests++;
                        break;
                    case TestStatus.Failed:
                        _failedTests++;
                        break;
                    case TestStatus.Skipped:
                        _skippedTests++;
                        break;
                    case TestStatus.Inconclusive:
                        _inconclusiveTests++;
                        break;
                }
            }
            else if (result.Children != null)
            {
                foreach (var child in result.Children)
                {
                    ProcessTestResults(child);
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
        
        private string GetClassName(ITestAdaptor test)
        {
            // Try to get TypeInfo if available (may not exist in all Unity versions)
            try
            {
                var typeInfo = test.GetType().GetProperty("TypeInfo")?.GetValue(test);
                if (typeInfo != null)
                {
                    var fullNameProp = typeInfo.GetType().GetProperty("FullName");
                    var fullName = fullNameProp?.GetValue(typeInfo) as string;
                    if (!string.IsNullOrEmpty(fullName))
                        return fullName;
                }
            }
            catch { }
            
            // Fallback to parsing the full name
            var testFullName = test.FullName;
            var lastDot = testFullName.LastIndexOf('.');
            if (lastDot > 0)
            {
                var secondLastDot = testFullName.LastIndexOf('.', lastDot - 1);
                if (secondLastDot >= 0)
                {
                    return testFullName.Substring(0, lastDot);
                }
            }
            
            return "Unknown";
        }
        
        private int GetTestCaseCount(ITestAdaptor test)
        {
            // Try to get TestCaseCount property if it exists
            try
            {
                var prop = test.GetType().GetProperty("TestCaseCount");
                if (prop != null)
                {
                    return (int)prop.GetValue(test);
                }
            }
            catch { }
            
            // Fallback - count children recursively
            return CountTests(test);
        }
        
        private string GetTestType(ITestResultAdaptor result)
        {
            // Check if IsSuite property exists
            try
            {
                var isSuiteProp = result.Test.GetType().GetProperty("IsSuite");
                if (isSuiteProp != null)
                {
                    var isSuite = (bool)isSuiteProp.GetValue(result.Test);
                    if (isSuite)
                    {
                        if (result.Test.FullName.Contains("Assembly"))
                            return "Assembly";
                        if (result.Test.FullName.Contains("Namespace"))
                            return "Namespace";
                        return "TestFixture";
                    }
                }
            }
            catch { }
            
            // Fallback logic
            if (result.HasChildren)
            {
                if (result.Test.FullName.Contains("Assembly"))
                    return "Assembly";
                if (result.Test.FullName.Contains("Namespace"))
                    return "Namespace";
                return "TestFixture";
            }
            return "TestCase";
        }
        
        private string GetOverallResult()
        {
            if (_failedTests > 0)
                return "Failed";
            if (_inconclusiveTests > 0)
                return "Inconclusive";
            if (_skippedTests == _totalTests)
                return "Skipped";
            return "Passed";
        }
        
        private string GetDefaultOutputPath()
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var directory = Path.Combine(Application.dataPath, "..", "TestResults");
            
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            return Path.Combine(directory, $"TestResults_{timestamp}.xml");
        }
        
        #endregion
        
        #region Nested Types
        
        private class TestResult
        {
            public string FullName { get; set; }
            public string Name { get; set; }
            public string ClassName { get; set; }
            public TestStatus TestStatus { get; set; }
            public double Duration { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
            public string Message { get; set; }
            public string StackTrace { get; set; }
            public string Output { get; set; }
            public bool HasChildren { get; set; }
        }
        
        #endregion
    }
}