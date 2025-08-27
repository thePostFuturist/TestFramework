using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TestFramework.Unity.TestResultExport.Editor
{
    /// <summary>
    /// Helper class to handle running tests when Unity is already open
    /// Monitors for trigger files to start test execution
    /// </summary>
    [InitializeOnLoad]
    public static class UnityInstanceHelper
    {
        private static readonly string TriggerFilePath;
        private static FileSystemWatcher _watcher;
        private static double _lastCheckTime;
        
        static UnityInstanceHelper()
        {
            TriggerFilePath = Path.Combine(Application.dataPath, "TestFramework", "Unity", "TestResultExport", "Editor", "run_tests_trigger.txt");
            
            // Check for trigger file on startup
            EditorApplication.delayCall += CheckForTriggerFile;
            
            // Set up file watcher for trigger file
            SetupFileWatcher();
            
            // Periodic check as backup
            EditorApplication.update += PeriodicCheck;
        }
        
        private static void SetupFileWatcher()
        {
            try
            {
                var directory = Path.GetDirectoryName(TriggerFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                _watcher = new FileSystemWatcher(directory)
                {
                    Filter = "run_tests_trigger.txt",
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime
                };
                
                _watcher.Created += OnTriggerFileChanged;
                _watcher.Changed += OnTriggerFileChanged;
                _watcher.EnableRaisingEvents = true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TEST-HELPER] Could not set up file watcher: {ex.Message}");
            }
        }
        
        private static void OnTriggerFileChanged(object sender, FileSystemEventArgs e)
        {
            EditorApplication.delayCall += CheckForTriggerFile;
        }
        
        private static void PeriodicCheck()
        {
            // Check every 5 seconds as a backup
            if (EditorApplication.timeSinceStartup - _lastCheckTime > 5.0)
            {
                _lastCheckTime = EditorApplication.timeSinceStartup;
                CheckForTriggerFile();
            }
        }
        
        private static void CheckForTriggerFile()
        {
            if (!File.Exists(TriggerFilePath))
                return;
            
            try
            {
                var lines = File.ReadAllLines(TriggerFilePath);
                string testSuite = "all";
                
                foreach (var line in lines)
                {
                    if (line.StartsWith("test_suite="))
                    {
                        testSuite = line.Substring("test_suite=".Length).Trim();
                        break;
                    }
                }
                
                // Delete trigger file
                File.Delete(TriggerFilePath);
                
                // Suppress dialog popups when triggered by file
                EditorPrefs.SetBool("TestRunner.SuppressDialog", true);
                
                // Run tests based on trigger
                Debug.Log($"[TEST-HELPER] Trigger file detected. Running {testSuite} tests automatically...");
                
                switch (testSuite.ToLower())
                {
                    case "all":
                        TestRunnerEditorCommands.RunAllTestsInEditor();
                        break;
                    case "edit":
                    case "editmode":
                        TestRunnerEditorCommands.RunEditModeTests();
                        break;
                    case "play":
                    case "playmode":
                        TestRunnerEditorCommands.RunPlayModeTests();
                        break;
                    case "unit":
                        TestRunnerEditorCommands.RunUnitTests();
                        break;
                    case "integration":
                        TestRunnerEditorCommands.RunIntegrationTests();
                        break;
                    case "critical":
                        TestRunnerEditorCommands.RunCriticalTests();
                        break;
                    default:
                        Debug.LogWarning($"[TEST-HELPER] Unknown test suite: {testSuite}");
                        break;
                }
                
                // Re-enable dialogs after a delay
                EditorApplication.delayCall += () =>
                {
                    EditorPrefs.SetBool("TestRunner.SuppressDialog", false);
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TEST-HELPER] Error processing trigger file: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Show a notification to remind user how to run tests
        /// </summary>
        [MenuItem("TestFramework/Run Tests/Show Test Instructions", false, 200)]
        public static void ShowTestInstructions()
        {
            var message = @"How to Run Tests in Unity Editor:

1. QUICK RUN (Keyboard Shortcut):
   Press Ctrl+Shift+Alt+T to run all tests

2. MENU COMMANDS:
   TestFramework → Run Tests → [Select Test Type]

3. TEST WINDOW:
   TestFramework → Run Tests → Open Test Runner Window
   Configure filters and click 'Run Tests'

4. UNITY TEST RUNNER:
   Window → General → Test Runner
   (XML export will work if enabled)

5. COMMAND LINE (with open Unity):
   Run: run-tests-in-open-unity.bat
   This creates a trigger file that Unity detects

Results are saved to: TestResults folder
XML format compatible with CI/CD systems";
            
            EditorUtility.DisplayDialog("Test Runner Instructions", message, "OK");
        }
        
        /// <summary>
        /// Quick status check
        /// </summary>
        [MenuItem("TestFramework/Run Tests/Check Test System Status", false, 201)]
        public static void CheckTestSystemStatus()
        {
            var xmlExportEnabled = EditorPrefs.GetBool("TestFramework.XMLExportEnabled", false);
            var resultsPath = Path.Combine(Application.dataPath, "..", "TestResults");
            var resultFileCount = Directory.Exists(resultsPath) ? Directory.GetFiles(resultsPath, "*.xml").Length : 0;
            
            var status = $@"Test System Status:

XML Export: {(xmlExportEnabled ? "ENABLED ✓" : "DISABLED ✗")}
Results Folder: {resultsPath}
Previous Results: {resultFileCount} XML files found
Trigger File Path: {TriggerFilePath}
File Watcher: {(_watcher != null && _watcher.EnableRaisingEvents ? "ACTIVE ✓" : "INACTIVE ✗")}

Unity Version: {Application.unityVersion}
Project: {Application.productName}

To enable XML export:
TestFramework → Test Export → Enable XML Export";
            
            EditorUtility.DisplayDialog("Test System Status", status, "OK");
        }
    }
}