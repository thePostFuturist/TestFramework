using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using PerSpec.Editor.Services;
using PerSpec.Editor.Coordination;

namespace PerSpec.Editor.Windows
{
    /// <summary>
    /// Central control panel for all PerSpec features
    /// </summary>
    public class PerSpecControlCenter : EditorWindow
    {
        #region Constants
        
        private const string WINDOW_TITLE = "PerSpec Control Center";
        private static readonly Vector2 MIN_SIZE = new Vector2(600, 500);
        
        #endregion
        
        #region Fields
        
        private int selectedTab = 0;
        private string[] tabNames = new string[]
        {
            "Dashboard",
            "Test Coordinator",
            "Debug Settings",
            "Console Logs",
            "Initialization",
            "About"
        };
        
        private Vector2 scrollPosition;
        private GUIStyle headerStyle;
        private GUIStyle sectionStyle;
        private GUIStyle statusStyle;
        
        #endregion
        
        #region Unity Menu
        
        [MenuItem("Tools/PerSpec/Control Center", priority = -200)]
        public static void ShowWindow()
        {
            var window = GetWindow<PerSpecControlCenter>(false, WINDOW_TITLE);
            window.minSize = MIN_SIZE;
            window.Show();
        }
        
        [MenuItem("Tools/PerSpec/Quick Actions/Run Pending Tests", priority = 1)]
        private static void QuickRunTests()
        {
            if (TestCoordinationService.CheckPendingTests())
                Debug.Log("[PerSpec] Started pending tests");
            else
                Debug.Log("[PerSpec] No pending tests found");
        }
        
        [MenuItem("Tools/PerSpec/Quick Actions/Toggle Debug Logging", priority = 2)]
        private static void QuickToggleDebug()
        {
            DebugService.ToggleDebugLogging();
        }
        
        [MenuItem("Tools/PerSpec/Quick Actions/Open Working Directory", priority = 3)]
        private static void QuickOpenDirectory()
        {
            InitializationService.OpenWorkingDirectory();
        }
        
        #endregion
        
        #region Unity Lifecycle
        
        private void OnEnable()
        {
            titleContent = new GUIContent(WINDOW_TITLE, EditorGUIUtility.IconContent("d_Settings").image);
        }
        
        private void OnGUI()
        {
            InitStyles();
            
            // Header
            DrawHeader();
            
            // Tab bar
            EditorGUILayout.Space(5);
            selectedTab = GUILayout.Toolbar(selectedTab, tabNames, GUILayout.Height(30));
            EditorGUILayout.Space(5);
            
            // Tab content
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            switch (selectedTab)
            {
                case 0: DrawDashboardTab(); break;
                case 1: DrawTestCoordinatorTab(); break;
                case 2: DrawDebugSettingsTab(); break;
                case 3: DrawConsoleLogsTab(); break;
                case 4: DrawInitializationTab(); break;
                case 5: DrawAboutTab(); break;
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        #endregion
        
        #region GUI Drawing - Header
        
        private void InitStyles()
        {
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(EditorStyles.largeLabel)
                {
                    fontSize = 18,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
            }
            
            if (sectionStyle == null)
            {
                sectionStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    padding = new RectOffset(10, 10, 10, 10)
                };
            }
            
            if (statusStyle == null)
            {
                statusStyle = new GUIStyle(EditorStyles.label)
                {
                    richText = true,
                    alignment = TextAnchor.MiddleCenter
                };
            }
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("PerSpec Control Center", headerStyle);
            EditorGUILayout.LabelField("Unity Test Framework with TDD & SQLite Coordination", 
                new GUIStyle(EditorStyles.centeredGreyMiniLabel));
        }
        
        #endregion
        
        #region GUI Drawing - Dashboard Tab
        
        private void DrawDashboardTab()
        {
            // Quick Status
            DrawSection("System Status", () =>
            {
                DrawStatusRow("Initialization", InitializationService.IsInitialized ? "✓ Ready" : "✗ Not initialized", 
                    InitializationService.IsInitialized ? Color.green : Color.red);
                
                DrawStatusRow("Test Coordinator", TestCoordinationService.GetStatusSummary(), 
                    TestCoordinationService.IsRunningTests ? Color.yellow : Color.green);
                
                DrawStatusRow("Debug Logging", DebugService.DebugStatus,
                    DebugService.IsDebugEnabled ? Color.green : Color.gray);
                
                DrawStatusRow("Console Capture", ConsoleService.CaptureStatus,
                    ConsoleService.IsCaptureEnabled ? Color.green : Color.gray);
            });
            
            EditorGUILayout.Space(10);
            
            // Quick Actions
            DrawSection("Quick Actions", () =>
            {
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("Run Tests", GUILayout.Height(40)))
                {
                    TestCoordinationService.CheckPendingTests();
                }
                
                if (GUILayout.Button("Toggle Debug", GUILayout.Height(40)))
                {
                    DebugService.ToggleDebugLogging();
                }
                
                if (GUILayout.Button("Open Directory", GUILayout.Height(40)))
                {
                    InitializationService.OpenWorkingDirectory();
                }
                
                EditorGUILayout.EndHorizontal();
            });
            
            EditorGUILayout.Space(10);
            
            // Statistics
            DrawSection("Statistics", () =>
            {
                EditorGUILayout.LabelField("Database Size:", 
                    $"{InitializationService.DatabaseSize / 1024f:F1} KB");
                EditorGUILayout.LabelField("Console Logs:", 
                    $"{ConsoleService.CapturedLogCount} captured");
                EditorGUILayout.LabelField("Errors:", 
                    $"{ConsoleService.ErrorCount}", 
                    ConsoleService.ErrorCount > 0 ? EditorStyles.boldLabel : EditorStyles.label);
            });
        }
        
        #endregion
        
        #region GUI Drawing - Test Coordinator Tab
        
        private void DrawTestCoordinatorTab()
        {
            DrawInfoBox(
                "Test Coordinator manages automated test execution through SQLite database coordination. " +
                "Python scripts can submit test requests that Unity will automatically execute."
            );
            
            EditorGUILayout.Space(10);
            
            // Status
            DrawSection("Test Status", () =>
            {
                EditorGUILayout.LabelField("Status:", TestCoordinationService.GetStatusSummary());
                
                if (TestCoordinationService.IsRunningTests)
                {
                    EditorGUILayout.LabelField("Current Request:", 
                        $"#{TestCoordinationService.CurrentRequestId}");
                        
                    if (GUILayout.Button("Cancel Current Test"))
                    {
                        TestCoordinationService.CancelCurrentTest();
                    }
                }
            });
            
            EditorGUILayout.Space(10);
            
            // Controls
            DrawSection("Test Controls", () =>
            {
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("Check Pending Tests", GUILayout.Height(30)))
                {
                    if (TestCoordinationService.CheckPendingTests())
                        ShowNotification(new GUIContent("Started pending tests"));
                    else
                        ShowNotification(new GUIContent("No pending tests"));
                }
                
                if (GUILayout.Button("Force Compilation", GUILayout.Height(30)))
                {
                    TestCoordinationService.ForceScriptCompilation();
                }
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(5);
                
                bool polling = TestCoordinationService.PollingEnabled;
                bool newPolling = EditorGUILayout.Toggle("Auto-Polling Enabled", polling);
                if (newPolling != polling)
                {
                    TestCoordinationService.PollingEnabled = newPolling;
                }
            });
            
            EditorGUILayout.Space(10);
            
            // Database Info
            DrawSection("Database Status", () =>
            {
                EditorGUILayout.TextArea(TestCoordinationService.GetDatabaseStatus(), 
                    GUILayout.Height(100));
            });
        }
        
        #endregion
        
        #region GUI Drawing - Debug Settings Tab
        
        private void DrawDebugSettingsTab()
        {
            DrawInfoBox(
                "Debug Settings control whether PerSpec debug logging is compiled into builds. " +
                "When disabled, all PerSpecDebug.Log() calls are completely stripped with zero runtime overhead."
            );
            
            EditorGUILayout.Space(10);
            
            // Current Status
            DrawSection("Debug Status", () =>
            {
                string status = DebugService.IsDebugEnabled 
                    ? "● ENABLED - Debug logs will be included" 
                    : "● DISABLED - Debug logs are stripped";
                
                Color color = DebugService.IsDebugEnabled ? Color.green : Color.gray;
                GUI.color = color;
                EditorGUILayout.LabelField(status, statusStyle);
                GUI.color = Color.white;
            });
            
            EditorGUILayout.Space(10);
            
            // Controls
            DrawSection("Debug Controls", () =>
            {
                EditorGUILayout.BeginHorizontal();
                
                GUI.backgroundColor = DebugService.IsDebugEnabled ? Color.red : Color.green;
                string buttonText = DebugService.IsDebugEnabled 
                    ? "Disable Debug Logging" 
                    : "Enable Debug Logging";
                    
                if (GUILayout.Button(buttonText, GUILayout.Height(40)))
                {
                    DebugService.ToggleDebugLogging();
                }
                GUI.backgroundColor = Color.white;
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(10);
                
                if (GUILayout.Button("Test Log Levels"))
                {
                    DebugService.TestLogLevels();
                }
            });
            
            EditorGUILayout.Space(10);
            
            // Usage Example
            DrawSection("Usage Example", () =>
            {
                string code = @"using PerSpec;

// These calls are stripped when disabled:
PerSpecDebug.Log(""[TEST] Starting test"");
PerSpecDebug.LogError(""[ERROR] Test failed"");
PerSpecDebug.LogTestSetup(""Creating prefab"");
PerSpecDebug.LogTestComplete(""Test passed"");";
                
                EditorGUILayout.TextArea(code, GUILayout.Height(100));
            });
        }
        
        #endregion
        
        #region GUI Drawing - Console Logs Tab
        
        private void DrawConsoleLogsTab()
        {
            DrawInfoBox(
                "Console Log Capture saves Unity console output to SQLite database for analysis. " +
                "Captured logs can be retrieved via Python scripts for debugging and reporting."
            );
            
            EditorGUILayout.Space(10);
            
            // Status
            DrawSection("Capture Status", () =>
            {
                EditorGUILayout.LabelField("Status:", ConsoleService.CaptureStatus);
                EditorGUILayout.LabelField("Session:", ConsoleService.SessionId);
                
                EditorGUILayout.Space(5);
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Total: {ConsoleService.CapturedLogCount}");
                EditorGUILayout.LabelField($"Errors: {ConsoleService.ErrorCount}");
                EditorGUILayout.LabelField($"Warnings: {ConsoleService.WarningCount}");
                EditorGUILayout.EndHorizontal();
            });
            
            EditorGUILayout.Space(10);
            
            // Controls
            DrawSection("Console Controls", () =>
            {
                EditorGUILayout.BeginHorizontal();
                
                GUI.backgroundColor = ConsoleService.IsCaptureEnabled ? Color.red : Color.green;
                string buttonText = ConsoleService.IsCaptureEnabled 
                    ? "Stop Capture" 
                    : "Start Capture";
                    
                if (GUILayout.Button(buttonText, GUILayout.Height(40)))
                {
                    ConsoleService.ToggleCapture();
                }
                GUI.backgroundColor = Color.white;
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(5);
                
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("Clear Session"))
                {
                    ConsoleService.ClearSession();
                }
                
                if (GUILayout.Button("Export Logs"))
                {
                    ConsoleService.ExportLogs();
                }
                
                if (GUILayout.Button("Test Logs"))
                {
                    ConsoleService.TestLogLevels();
                }
                
                EditorGUILayout.EndHorizontal();
            });
            
            EditorGUILayout.Space(10);
            
            // Session Info
            DrawSection("Session Information", () =>
            {
                EditorGUILayout.TextArea(ConsoleService.GetSessionInfo(), GUILayout.Height(80));
            });
        }
        
        #endregion
        
        #region GUI Drawing - Initialization Tab
        
        private void DrawInitializationTab()
        {
            DrawInfoBox(
                "PerSpec requires a working directory in your project root for SQLite database and scripts. " +
                "This directory is created at: ProjectRoot/PerSpec/"
            );
            
            EditorGUILayout.Space(10);
            
            // Status
            DrawSection("Initialization Status", () =>
            {
                bool initialized = InitializationService.IsInitialized;
                
                string status = initialized 
                    ? "✓ PerSpec is initialized and ready" 
                    : "✗ PerSpec is not initialized";
                    
                Color color = initialized ? Color.green : Color.red;
                GUI.color = color;
                EditorGUILayout.LabelField(status, statusStyle);
                GUI.color = Color.white;
                
                if (initialized)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("Working Directory:", 
                        InitializationService.ProjectPerSpecPath);
                    EditorGUILayout.LabelField("Database:", 
                        InitializationService.DatabasePath);
                    EditorGUILayout.LabelField("Status:", 
                        InitializationService.GetStatusSummary());
                }
            });
            
            EditorGUILayout.Space(10);
            
            // Controls
            DrawSection("Initialization Controls", () =>
            {
                if (!InitializationService.IsInitialized)
                {
                    GUI.backgroundColor = Color.green;
                    if (GUILayout.Button("Initialize PerSpec", GUILayout.Height(40)))
                    {
                        if (InitializationService.Initialize())
                        {
                            ShowNotification(new GUIContent("PerSpec initialized successfully"));
                        }
                    }
                    GUI.backgroundColor = Color.white;
                }
                else
                {
                    EditorGUILayout.BeginHorizontal();
                    
                    if (GUILayout.Button("Open Directory", GUILayout.Height(30)))
                    {
                        InitializationService.OpenWorkingDirectory();
                    }
                    
                    GUI.backgroundColor = Color.red;
                    if (GUILayout.Button("Reset", GUILayout.Height(30)))
                    {
                        if (EditorUtility.DisplayDialog("Reset PerSpec",
                            "This will delete the PerSpec working directory and all data. Continue?",
                            "Reset", "Cancel"))
                        {
                            InitializationService.Reset();
                        }
                    }
                    GUI.backgroundColor = Color.white;
                    
                    EditorGUILayout.EndHorizontal();
                }
            });
        }
        
        #endregion
        
        #region GUI Drawing - About Tab
        
        private void DrawAboutTab()
        {
            DrawSection("About PerSpec", () =>
            {
                EditorGUILayout.LabelField("Version:", "1.0.0");
                EditorGUILayout.LabelField("Unity:", Application.unityVersion);
                
                EditorGUILayout.Space(10);
                
                EditorGUILayout.LabelField("PerSpec is a Test-Driven Development framework for Unity", 
                    EditorStyles.wordWrappedLabel);
                EditorGUILayout.LabelField("that uses SQLite coordination for automated testing.", 
                    EditorStyles.wordWrappedLabel);
                    
                EditorGUILayout.Space(10);
                
                if (GUILayout.Button("View Documentation"))
                {
                    Application.OpenURL("https://github.com/yourusername/perspec/wiki");
                }
                
                if (GUILayout.Button("Report Issue"))
                {
                    Application.OpenURL("https://github.com/yourusername/perspec/issues");
                }
            });
            
            EditorGUILayout.Space(10);
            
            DrawSection("4-Step TDD Workflow", () =>
            {
                string workflow = @"1. Write code and tests with TDD
2. Refresh Unity: 
   python ScriptingTools/Coordination/Scripts/quick_refresh.py full --wait
3. Check for errors:
   python ScriptingTools/Coordination/Scripts/quick_logs.py errors
4. Run tests:
   python ScriptingTools/Coordination/Scripts/quick_test.py all -p edit --wait";
   
                EditorGUILayout.TextArea(workflow, GUILayout.Height(120));
            });
        }
        
        #endregion
        
        #region Helper Methods
        
        private void DrawSection(string title, Action content)
        {
            EditorGUILayout.BeginVertical(sectionStyle);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            content?.Invoke();
            EditorGUILayout.EndVertical();
        }
        
        private void DrawInfoBox(string message)
        {
            EditorGUILayout.HelpBox(message, MessageType.Info);
        }
        
        private void DrawStatusRow(string label, string status, Color color)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(120));
            
            var oldColor = GUI.color;
            GUI.color = color;
            EditorGUILayout.LabelField(status);
            GUI.color = oldColor;
            
            EditorGUILayout.EndHorizontal();
        }
        
        #endregion
    }
}