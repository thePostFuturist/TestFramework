using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace TestFramework.Unity.TestResultExport.Editor
{
    /// <summary>
    /// Custom Editor Window for running tests in the Unity Editor with advanced options
    /// </summary>
    public class EditorTestRunnerWindow : EditorWindow
    {
        #region Fields
        
        private TestMode _selectedMode = TestMode.EditMode;
        private string _filterText = "";
        private string _categoriesText = "";
        private bool _exportXML = true;
        private bool _openResultsAfter = false;
        private bool _clearConsole = true;
        private bool _showAdvancedOptions = false;
        
        private bool _isRunning = false;
        private TestResultXMLExporter _currentExporter;
        private TestRunProgress _progress;
        
        private Vector2 _scrollPosition;
        private GUIStyle _headerStyle;
        private GUIStyle _subHeaderStyle;
        private GUIStyle _boxStyle;
        
        #endregion
        
        #region Window Management
        
        [MenuItem("TestFramework/Run Tests/Open Test Runner Window", false, 0)]
        public static void ShowWindow()
        {
            var window = GetWindow<EditorTestRunnerWindow>("Test Runner");
            window.minSize = new Vector2(400, 500);
            window.Show();
        }
        
        private void OnEnable()
        {
            // Load preferences
            _selectedMode = (TestMode)EditorPrefs.GetInt("TestRunner.Mode", (int)TestMode.EditMode);
            _filterText = EditorPrefs.GetString("TestRunner.Filter", "");
            _categoriesText = EditorPrefs.GetString("TestRunner.Categories", "");
            _exportXML = EditorPrefs.GetBool("TestRunner.ExportXML", true);
            _openResultsAfter = EditorPrefs.GetBool("TestRunner.OpenResults", false);
            _clearConsole = EditorPrefs.GetBool("TestRunner.ClearConsole", true);
        }
        
        private void OnDisable()
        {
            // Save preferences
            EditorPrefs.SetInt("TestRunner.Mode", (int)_selectedMode);
            EditorPrefs.SetString("TestRunner.Filter", _filterText);
            EditorPrefs.SetString("TestRunner.Categories", _categoriesText);
            EditorPrefs.SetBool("TestRunner.ExportXML", _exportXML);
            EditorPrefs.SetBool("TestRunner.OpenResults", _openResultsAfter);
            EditorPrefs.SetBool("TestRunner.ClearConsole", _clearConsole);
        }
        
        #endregion
        
        #region GUI
        
        private void OnGUI()
        {
            InitializeStyles();
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            DrawHeader();
            EditorGUILayout.Space(10);
            
            if (_isRunning)
            {
                DrawRunningStatus();
            }
            else
            {
                DrawTestConfiguration();
                EditorGUILayout.Space(10);
                DrawRunButtons();
            }
            
            EditorGUILayout.Space(10);
            DrawRecentResults();
            
            EditorGUILayout.EndScrollView();
        }
        
        private void InitializeStyles()
        {
            if (_headerStyle == null)
            {
                _headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 16,
                    alignment = TextAnchor.MiddleCenter
                };
            }
            
            if (_subHeaderStyle == null)
            {
                _subHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 12
                };
            }
            
            if (_boxStyle == null)
            {
                _boxStyle = new GUIStyle(GUI.skin.box)
                {
                    padding = new RectOffset(10, 10, 10, 10)
                };
            }
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.LabelField("Unity Test Runner", _headerStyle, GUILayout.Height(25));
            EditorGUILayout.LabelField("Run tests in the current Unity Editor instance", EditorStyles.centeredGreyMiniLabel);
            
            EditorGUILayout.Space(5);
            
            // Status bar
            EditorGUILayout.BeginHorizontal(_boxStyle);
            
            var statusColor = _isRunning ? Color.yellow : Color.green;
            var oldColor = GUI.color;
            GUI.color = statusColor;
            
            EditorGUILayout.LabelField(_isRunning ? "● Running" : "● Ready", GUILayout.Width(80));
            
            GUI.color = oldColor;
            
            if (_progress != null && _isRunning)
            {
                EditorGUILayout.LabelField($"Progress: {_progress.CompletedTests}/{_progress.TotalTests}", GUILayout.Width(120));
                EditorGUILayout.LabelField($"Passed: {_progress.PassedTests} Failed: {_progress.FailedTests}");
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawTestConfiguration()
        {
            EditorGUILayout.LabelField("Test Configuration", _subHeaderStyle);
            
            EditorGUILayout.BeginVertical(_boxStyle);
            
            // Test Mode Selection
            EditorGUILayout.LabelField("Test Mode:", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Toggle(_selectedMode == TestMode.EditMode, "EditMode", EditorStyles.radioButton))
                _selectedMode = TestMode.EditMode;
            
            if (GUILayout.Toggle(_selectedMode == TestMode.PlayMode, "PlayMode", EditorStyles.radioButton))
                _selectedMode = TestMode.PlayMode;
            
            if (GUILayout.Toggle(_selectedMode == (TestMode.EditMode | TestMode.PlayMode), "Both", EditorStyles.radioButton))
                _selectedMode = TestMode.EditMode | TestMode.PlayMode;
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            
            // Filter Options
            EditorGUILayout.LabelField("Filter Options:", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Test Filter:", GUILayout.Width(80));
            _filterText = EditorGUILayout.TextField(_filterText);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.HelpBox("Enter namespace, class, or method name to filter tests (e.g., MyNamespace.MyTestClass)", MessageType.Info);
            
            EditorGUILayout.Space(5);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Categories:", GUILayout.Width(80));
            _categoriesText = EditorGUILayout.TextField(_categoriesText);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.HelpBox("Enter comma-separated categories (e.g., Unit,Integration,Critical)", MessageType.Info);
            
            EditorGUILayout.Space(10);
            
            // Quick Filters
            EditorGUILayout.LabelField("Quick Filters:", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Unit Tests", EditorStyles.miniButton))
            {
                _filterText = "TestFramework.Unity.Tests.Unit";
                _selectedMode = TestMode.EditMode;
            }
            if (GUILayout.Button("Integration", EditorStyles.miniButton))
            {
                _filterText = "TestFramework.Unity.Tests.Integration";
                _selectedMode = TestMode.PlayMode;
            }
            if (GUILayout.Button("DOTS", EditorStyles.miniButton))
            {
                _filterText = "TestFramework.DOTS";
                _selectedMode = TestMode.EditMode;
            }
            if (GUILayout.Button("Clear", EditorStyles.miniButton))
            {
                _filterText = "";
                _categoriesText = "";
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
            
            // Options
            _showAdvancedOptions = EditorGUILayout.Foldout(_showAdvancedOptions, "Advanced Options", true);
            
            if (_showAdvancedOptions)
            {
                EditorGUILayout.BeginVertical(_boxStyle);
                
                _exportXML = EditorGUILayout.Toggle("Export XML Results", _exportXML);
                _openResultsAfter = EditorGUILayout.Toggle("Open Results Folder", _openResultsAfter);
                _clearConsole = EditorGUILayout.Toggle("Clear Console Before Run", _clearConsole);
                
                EditorGUILayout.Space(5);
                
                if (GUILayout.Button("Open Test Results Folder", EditorStyles.miniButton))
                {
                    OpenTestResultsFolder();
                }
                
                EditorGUILayout.EndVertical();
            }
        }
        
        private void DrawRunButtons()
        {
            EditorGUILayout.BeginHorizontal();
            
            GUI.enabled = !_isRunning;
            
            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.green;
            
            if (GUILayout.Button("Run Tests", GUILayout.Height(30)))
            {
                RunTests();
            }
            
            GUI.backgroundColor = oldColor;
            
            if (GUILayout.Button("Run in Test Runner", GUILayout.Height(30), GUILayout.Width(120)))
            {
                // Open Unity's built-in Test Runner
                EditorApplication.ExecuteMenuItem("Window/General/Test Runner");
            }
            
            GUI.enabled = true;
            
            EditorGUILayout.EndHorizontal();
            
            if (_isRunning)
            {
                EditorGUILayout.Space(5);
                
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("Stop Tests", GUILayout.Height(25)))
                {
                    StopTests();
                }
                GUI.backgroundColor = oldColor;
            }
        }
        
        private void DrawRunningStatus()
        {
            EditorGUILayout.LabelField("Test Execution", _subHeaderStyle);
            
            EditorGUILayout.BeginVertical(_boxStyle);
            
            if (_progress != null)
            {
                // Progress bar
                var progress = _progress.TotalTests > 0 ? (float)_progress.CompletedTests / _progress.TotalTests : 0f;
                var progressRect = EditorGUILayout.GetControlRect(GUILayout.Height(20));
                EditorGUI.ProgressBar(progressRect, progress, $"{_progress.CompletedTests}/{_progress.TotalTests} Tests");
                
                EditorGUILayout.Space(10);
                
                // Statistics
                EditorGUILayout.BeginHorizontal();
                
                var oldColor = GUI.color;
                
                GUI.color = Color.green;
                EditorGUILayout.LabelField($"✓ Passed: {_progress.PassedTests}", GUILayout.Width(100));
                
                GUI.color = Color.red;
                EditorGUILayout.LabelField($"✗ Failed: {_progress.FailedTests}", GUILayout.Width(100));
                
                GUI.color = Color.yellow;
                EditorGUILayout.LabelField($"- Skipped: {_progress.SkippedTests}", GUILayout.Width(100));
                
                GUI.color = oldColor;
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(10);
                
                // Current test
                if (!string.IsNullOrEmpty(_progress.CurrentTest))
                {
                    EditorGUILayout.LabelField("Current Test:", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(_progress.CurrentTest, EditorStyles.wordWrappedLabel);
                }
                
                // Duration
                if (_progress.StartTime != default)
                {
                    var duration = DateTime.Now - _progress.StartTime;
                    EditorGUILayout.LabelField($"Duration: {duration:mm\\:ss}");
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawRecentResults()
        {
            EditorGUILayout.LabelField("Recent Results", _subHeaderStyle);
            
            EditorGUILayout.BeginVertical(_boxStyle);
            
            var resultsPath = Path.Combine(Application.dataPath, "..", "TestResults");
            
            if (Directory.Exists(resultsPath))
            {
                var files = Directory.GetFiles(resultsPath, "*.xml")
                    .OrderByDescending(f => File.GetCreationTime(f))
                    .Take(5)
                    .ToArray();
                
                if (files.Length > 0)
                {
                    foreach (var file in files)
                    {
                        EditorGUILayout.BeginHorizontal();
                        
                        var fileName = Path.GetFileName(file);
                        var fileTime = File.GetCreationTime(file);
                        
                        EditorGUILayout.LabelField($"{fileName}", GUILayout.Width(250));
                        EditorGUILayout.LabelField($"{fileTime:yyyy-MM-dd HH:mm}", GUILayout.Width(120));
                        
                        if (GUILayout.Button("Open", EditorStyles.miniButton, GUILayout.Width(50)))
                        {
                            EditorUtility.RevealInFinder(file);
                        }
                        
                        EditorGUILayout.EndHorizontal();
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("No test results found", EditorStyles.centeredGreyMiniLabel);
                }
            }
            else
            {
                EditorGUILayout.LabelField("TestResults folder not found", EditorStyles.centeredGreyMiniLabel);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        #endregion
        
        #region Test Execution
        
        private void RunTests()
        {
            if (_isRunning)
            {
                Debug.LogWarning("[TEST-RUNNER] Tests are already running");
                return;
            }
            
            _isRunning = true;
            _progress = new TestRunProgress();
            
            try
            {
                // Clear console if requested
                if (_clearConsole)
                {
                    var logEntries = Type.GetType("UnityEditor.LogEntries, UnityEditor");
                    var clearMethod = logEntries?.GetMethod("Clear");
                    clearMethod?.Invoke(null, null);
                }
                
                Debug.Log($"[TEST-RUNNER] Starting tests from Editor Window");
                Debug.Log($"[TEST-RUNNER] Mode: {_selectedMode}");
                
                // Clear TestResults directory
                var testResultsDir = Path.Combine(Application.dataPath, "..", "TestResults");
                ClearTestResultsDirectory(testResultsDir);
                Debug.Log($"[TEST-RUNNER] Cleared TestResults directory");
                
                // Setup filter
                string[] testNames = null;
                if (!string.IsNullOrWhiteSpace(_filterText))
                {
                    testNames = _filterText.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToArray();
                    Debug.Log($"[TEST-RUNNER] Filter: {string.Join(", ", testNames)}");
                }
                
                string[] categoryNames = null;
                if (!string.IsNullOrWhiteSpace(_categoriesText))
                {
                    categoryNames = _categoriesText.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToArray();
                    Debug.Log($"[TEST-RUNNER] Categories: {string.Join(", ", categoryNames)}");
                }
                
                // Setup XML export
                if (_exportXML)
                {
                    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    var outputPath = Path.Combine(testResultsDir, $"TestResults_Window_{timestamp}.xml");
                    
                    // Ensure directory exists (recreate after clearing)
                    if (!Directory.Exists(testResultsDir))
                    {
                        Directory.CreateDirectory(testResultsDir);
                    }
                    
                    _currentExporter = new TestResultXMLExporter(outputPath, true);
                    Debug.Log($"[TEST-RUNNER] XML results will be saved to: {outputPath}");
                }
                
                // Create TestRunner API
                var api = ScriptableObject.CreateInstance<TestRunnerApi>();
                
                // Register callbacks
                if (_currentExporter != null)
                {
                    api.RegisterCallbacks(_currentExporter);
                }
                api.RegisterCallbacks(new WindowTestCallbacks(this));
                
                // Create filter
                var filter = new Filter()
                {
                    testMode = _selectedMode,
                    testNames = testNames,
                    categoryNames = categoryNames
                };
                
                // Execute tests
                api.Execute(new ExecutionSettings(filter));
                
                Repaint();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TEST-RUNNER-ERROR] Failed to start tests: {ex.Message}\n{ex.StackTrace}");
                _isRunning = false;
                
                EditorUtility.DisplayDialog("Error", $"Failed to start tests:\n{ex.Message}", "OK");
            }
        }
        
        private void StopTests()
        {
            _isRunning = false;
            
            if (_currentExporter != null)
            {
                _currentExporter.UnregisterExporter();
                _currentExporter = null;
            }
            
            EditorUtility.ClearProgressBar();
            
            Debug.LogWarning("[TEST-RUNNER] Test run stopped by user");
            
            Repaint();
        }
        
        private void OpenTestResultsFolder()
        {
            var resultsPath = Path.Combine(Application.dataPath, "..", "TestResults");
            if (!Directory.Exists(resultsPath))
            {
                Directory.CreateDirectory(resultsPath);
            }
            EditorUtility.RevealInFinder(resultsPath);
        }
        
        private void ClearTestResultsDirectory(string directory)
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
                Debug.LogWarning($"[TEST-RUNNER] Could not clear TestResults directory: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Nested Types
        
        private class TestRunProgress
        {
            public int TotalTests { get; set; }
            public int CompletedTests { get; set; }
            public int PassedTests { get; set; }
            public int FailedTests { get; set; }
            public int SkippedTests { get; set; }
            public string CurrentTest { get; set; }
            public DateTime StartTime { get; set; }
        }
        
        private class WindowTestCallbacks : ICallbacks
        {
            private readonly EditorTestRunnerWindow _window;
            
            public WindowTestCallbacks(EditorTestRunnerWindow window)
            {
                _window = window;
            }
            
            public void RunStarted(ITestAdaptor testsToRun)
            {
                _window._progress.StartTime = DateTime.Now;
                _window._progress.TotalTests = CountTests(testsToRun);
                _window._progress.CompletedTests = 0;
                _window._progress.PassedTests = 0;
                _window._progress.FailedTests = 0;
                _window._progress.SkippedTests = 0;
                
                _window.Repaint();
            }
            
            public void RunFinished(ITestResultAdaptor result)
            {
                var duration = DateTime.Now - _window._progress.StartTime;
                
                _window._isRunning = false;
                
                Debug.Log($"[TEST-RUNNER] Tests completed in {duration.TotalSeconds:F2} seconds");
                Debug.Log($"[TEST-RUNNER] Results: {_window._progress.PassedTests} passed, {_window._progress.FailedTests} failed, {_window._progress.SkippedTests} skipped");
                
                // Clean up exporter
                if (_window._currentExporter != null)
                {
                    _window._currentExporter.UnregisterExporter();
                    _window._currentExporter = null;
                }
                
                // Open results if requested
                if (_window._openResultsAfter)
                {
                    _window.OpenTestResultsFolder();
                }
                
                // Show notification
                var message = $"Tests completed in {duration.TotalSeconds:F2}s\n" +
                            $"Passed: {_window._progress.PassedTests}\n" +
                            $"Failed: {_window._progress.FailedTests}\n" +
                            $"Skipped: {_window._progress.SkippedTests}";
                
                if (_window._progress.FailedTests > 0)
                {
                    EditorUtility.DisplayDialog("Tests Failed", message, "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Tests Passed", message, "OK");
                }
                
                _window.Repaint();
            }
            
            public void TestStarted(ITestAdaptor test)
            {
                if (!test.HasChildren)
                {
                    _window._progress.CurrentTest = test.FullName;
                    _window.Repaint();
                }
            }
            
            public void TestFinished(ITestResultAdaptor result)
            {
                if (!result.HasChildren)
                {
                    _window._progress.CompletedTests++;
                    
                    switch (result.TestStatus)
                    {
                        case TestStatus.Passed:
                            _window._progress.PassedTests++;
                            break;
                        case TestStatus.Failed:
                            _window._progress.FailedTests++;
                            break;
                        case TestStatus.Skipped:
                            _window._progress.SkippedTests++;
                            break;
                    }
                    
                    _window.Repaint();
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
    }
}