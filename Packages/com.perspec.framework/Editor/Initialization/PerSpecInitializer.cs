using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace PerSpec.Editor.Initialization
{
    /// <summary>
    /// PerSpec initialization window that appears on first launch and creates working directories
    /// </summary>
    [InitializeOnLoad]
    public class PerSpecInitializer : EditorWindow
    {
        private static string ProjectPerSpecPath => Path.Combine(Directory.GetParent(Application.dataPath).FullName, "PerSpec");
        private static string DatabasePath => Path.Combine(ProjectPerSpecPath, "test_coordination.db");
        private static bool hasShownThisSession = false;
        
        static PerSpecInitializer()
        {
            // Check on Unity startup after a delay
            EditorApplication.delayCall += CheckInitializationOnStartup;
        }
        
        static void CheckInitializationOnStartup()
        {
            if (!Directory.Exists(ProjectPerSpecPath) && !hasShownThisSession)
            {
                hasShownThisSession = true;
                Debug.LogWarning("[PerSpec] Not initialized. Opening setup window...");
                ShowWindow();
            }
            else if (Directory.Exists(ProjectPerSpecPath))
            {
                Debug.Log($"[PerSpec] Initialized at: {ProjectPerSpecPath}");
            }
        }
        
        [MenuItem("Tools/PerSpec/Initialize PerSpec", priority = -100)]
        public static void ShowWindow()
        {
            var window = GetWindow<PerSpecInitializer>("PerSpec Setup");
            window.minSize = new Vector2(450, 350);
            window.maxSize = new Vector2(600, 500);
            window.Show();
        }
        
        [MenuItem("Tools/PerSpec/Documentation", priority = 600)]
        public static void OpenDocumentation()
        {
            Application.OpenURL("https://github.com/yourusername/perspec/wiki");
            Debug.Log("[PerSpec] Opening documentation in browser...");
        }
        
        private void OnGUI()
        {
            // Header
            EditorGUILayout.Space(10);
            GUILayout.Label("PerSpec Testing Framework", EditorStyles.largeLabel);
            GUILayout.Label("Professional Unity TDD with UniTask and SQLite coordination", EditorStyles.miniLabel);
            EditorGUILayout.Space(10);
            
            // Draw separator
            DrawUILine(Color.gray);
            
            // Status check
            bool isInitialized = Directory.Exists(ProjectPerSpecPath);
            
            if (isInitialized)
            {
                ShowInitializedUI();
            }
            else
            {
                ShowNotInitializedUI();
            }
            
            // Footer
            GUILayout.FlexibleSpace();
            DrawUILine(Color.gray);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Open Documentation"))
            {
                Application.OpenURL("https://github.com/yourusername/perspec/wiki");
            }
            if (GUILayout.Button("View on GitHub"))
            {
                Application.OpenURL("https://github.com/yourusername/perspec");
            }
            EditorGUILayout.EndHorizontal();
        }
        
        private void ShowInitializedUI()
        {
            EditorGUILayout.HelpBox("PerSpec is initialized and ready to use!", MessageType.Info);
            
            EditorGUILayout.Space(10);
            
            // Show paths
            EditorGUILayout.LabelField("Working Directory:", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField(ProjectPerSpecPath);
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.LabelField("Database Path:", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField(DatabasePath);
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.Space(10);
            
            // Actions
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Open PerSpec Folder"))
            {
                EditorUtility.RevealInFinder(ProjectPerSpecPath);
            }
            
            if (GUILayout.Button("Open Test Coordinator"))
            {
                EditorApplication.ExecuteMenuItem("Tools/PerSpec/Test Coordinator");
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            
            // Re-initialize option
            EditorGUILayout.HelpBox("Re-initializing will keep your database but recreate the folder structure and scripts.", MessageType.None);
            
            if (GUILayout.Button("Re-Initialize PerSpec"))
            {
                if (EditorUtility.DisplayDialog("Re-Initialize PerSpec", 
                    "This will recreate the folder structure and convenience scripts.\n\nYour database and logs will be preserved.\n\nContinue?", 
                    "Yes", "Cancel"))
                {
                    InitializePerSpec();
                }
            }
        }
        
        private void ShowNotInitializedUI()
        {
            EditorGUILayout.HelpBox("PerSpec needs to be initialized for this project.", MessageType.Warning);
            
            EditorGUILayout.Space(10);
            
            GUILayout.Label("This will create:", EditorStyles.boldLabel);
            
            EditorGUI.indentLevel++;
            GUILayout.Label("• PerSpec/ folder in your project root");
            GUILayout.Label("• SQLite database for test coordination");
            GUILayout.Label("• Convenience scripts for command-line testing");
            GUILayout.Label("• Logs directory for console output");
            EditorGUI.indentLevel--;
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.HelpBox("The PerSpec folder will be created at:\n" + ProjectPerSpecPath, MessageType.None);
            
            EditorGUILayout.Space(20);
            
            // Big initialization button
            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.green;
            
            if (GUILayout.Button("Initialize PerSpec", GUILayout.Height(40)))
            {
                InitializePerSpec();
            }
            
            GUI.backgroundColor = oldColor;
        }
        
        private void InitializePerSpec()
        {
            try
            {
                // Create directories
                Directory.CreateDirectory(ProjectPerSpecPath);
                Directory.CreateDirectory(Path.Combine(ProjectPerSpecPath, "logs"));
                Directory.CreateDirectory(Path.Combine(ProjectPerSpecPath, "scripts"));
                
                Debug.Log($"[PerSpec] Created directories at: {ProjectPerSpecPath}");
                
                // Create convenience scripts
                CreateConvenienceScripts();
                
                // Initialize database via Python if possible
                InitializePythonDatabase();
                
                // Success message
                EditorUtility.DisplayDialog("Success", 
                    "PerSpec has been initialized successfully!\n\n" +
                    "Working directory created at:\n" + ProjectPerSpecPath + "\n\n" +
                    "You can now:\n" +
                    "• Use Tools > PerSpec menu items\n" +
                    "• Run tests from PerSpec/scripts/\n" +
                    "• Monitor logs in PerSpec/logs/", 
                    "OK");
                
                // Refresh Unity
                AssetDatabase.Refresh();
                
                // Update window
                Repaint();
                
                Debug.Log("[PerSpec] Initialization complete!");
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Error", 
                    $"Failed to initialize PerSpec:\n{e.Message}", 
                    "OK");
                Debug.LogError($"[PerSpec] Initialization failed: {e}");
            }
        }
        
        private void CreateConvenienceScripts()
        {
            string scriptsPath = Path.Combine(ProjectPerSpecPath, "scripts");
            string packageScriptsPath = "Packages/com.perspec.framework/ScriptingTools/Coordination/Scripts";
            
            // Create test scripts
            CreateWrapperScript(scriptsPath, "test", "quick_test.py", packageScriptsPath);
            CreateWrapperScript(scriptsPath, "logs", "quick_logs.py", packageScriptsPath);
            CreateWrapperScript(scriptsPath, "refresh", "quick_refresh.py", packageScriptsPath);
            CreateWrapperScript(scriptsPath, "init_db", "db_initializer.py", packageScriptsPath);
            
            Debug.Log($"[PerSpec] Created convenience scripts in: {scriptsPath}");
        }
        
        private void CreateWrapperScript(string targetDir, string scriptName, string pythonScript, string packagePath)
        {
            // Windows batch file
            string batContent = $@"@echo off
REM PerSpec wrapper script for {pythonScript}
python ""%~dp0\..\..\{packagePath}\{pythonScript}"" %*
if %ERRORLEVEL% NEQ 0 (
    echo Error running {pythonScript}
    pause
)";
            File.WriteAllText(Path.Combine(targetDir, $"{scriptName}.bat"), batContent);
            
            // Unix shell script
            string shContent = $@"#!/bin/bash
# PerSpec wrapper script for {pythonScript}
SCRIPT_DIR=""$( cd ""$( dirname ""${{BASH_SOURCE[0]}}"" )"" && pwd )""
python ""$SCRIPT_DIR/../../{packagePath}/{pythonScript}"" ""$@""";
            
            string shPath = Path.Combine(targetDir, $"{scriptName}.sh");
            File.WriteAllText(shPath, shContent);
            
            // Make shell script executable on Unix
            if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.LinuxEditor)
            {
                try
                {
                    var chmod = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "chmod",
                        Arguments = $"+x \"{shPath}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    System.Diagnostics.Process.Start(chmod)?.WaitForExit(1000);
                }
                catch { /* Ignore chmod errors */ }
            }
        }
        
        private void InitializePythonDatabase()
        {
            string initScript = Path.Combine(Application.dataPath, "..", 
                "Packages/com.perspec.framework/ScriptingTools/Coordination/Scripts/db_initializer.py");
            
            if (!File.Exists(initScript))
            {
                Debug.LogWarning($"[PerSpec] Database initializer script not found at: {initScript}");
                return;
            }
            
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = $"\"{initScript}\"",
                    WorkingDirectory = Directory.GetParent(Application.dataPath).FullName,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                
                using (var process = System.Diagnostics.Process.Start(psi))
                {
                    if (process != null)
                    {
                        process.WaitForExit(5000);
                        string output = process.StandardOutput.ReadToEnd();
                        string error = process.StandardError.ReadToEnd();
                        
                        if (!string.IsNullOrEmpty(output))
                            Debug.Log($"[PerSpec] Database init output: {output}");
                        if (!string.IsNullOrEmpty(error))
                            Debug.LogWarning($"[PerSpec] Database init error: {error}");
                    }
                }
                
                Debug.Log("[PerSpec] Database initialization attempted");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PerSpec] Could not run Python database initialization: {e.Message}");
                Debug.Log("[PerSpec] Database will be created on first use");
            }
        }
        
        private static void DrawUILine(Color color, int thickness = 1, int padding = 10)
        {
            Rect rect = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            rect.height = thickness;
            rect.y += padding / 2;
            rect.x -= 2;
            rect.width += 4;
            EditorGUI.DrawRect(rect, color);
        }
    }
}