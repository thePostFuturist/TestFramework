using UnityEditor;
using UnityEngine;
using UnityEditor.TestTools.TestRunner.Api;
using System.IO;

namespace TestFramework.Unity.TestResultExport.Editor
{
    /// <summary>
    /// Unity Editor menu items for test result export functionality
    /// </summary>
    public static class TestResultExportMenuItem
    {
        private static TestResultXMLExporter _currentExporter;
        
        [MenuItem("TestFramework/Test Export/Enable XML Export")]
        public static void EnableXMLExport()
        {
            if (_currentExporter != null)
            {
                Debug.LogWarning("[TEST-EXPORT] XML export is already enabled");
                return;
            }
            
            var outputPath = GetDefaultOutputPath();
            _currentExporter = TestResultXMLExporter.RegisterExporter(outputPath);
            Debug.Log($"[TEST-EXPORT] XML export enabled. Results will be saved to: {outputPath}");
            
            EditorPrefs.SetBool("TestFramework.XMLExportEnabled", true);
        }
        
        [MenuItem("TestFramework/Test Export/Disable XML Export")]
        public static void DisableXMLExport()
        {
            if (_currentExporter == null)
            {
                Debug.LogWarning("[TEST-EXPORT] XML export is not currently enabled");
                return;
            }
            
            _currentExporter.UnregisterExporter();
            _currentExporter = null;
            Debug.Log("[TEST-EXPORT] XML export disabled");
            
            EditorPrefs.SetBool("TestFramework.XMLExportEnabled", false);
        }
        
        [MenuItem("TestFramework/Test Export/Run Tests with XML Export")]
        public static void RunTestsWithExport()
        {
            if (_currentExporter == null)
            {
                EnableXMLExport();
            }
            
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            var filter = new Filter()
            {
                testMode = TestMode.EditMode | TestMode.PlayMode
            };
            
            api.Execute(new ExecutionSettings(filter));
            Debug.Log("[TEST-EXPORT] Running all tests with XML export enabled");
        }
        
        [MenuItem("TestFramework/Test Export/Open Test Results Folder")]
        public static void OpenTestResultsFolder()
        {
            var outputDir = Path.Combine(Application.dataPath, "..", "TestResults");
            
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }
            
            EditorUtility.RevealInFinder(outputDir);
        }
        
        [InitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            if (EditorPrefs.GetBool("TestFramework.XMLExportEnabled", false))
            {
                EditorApplication.delayCall += () =>
                {
                    if (_currentExporter == null)
                    {
                        EnableXMLExport();
                    }
                };
            }
        }
        
        [MenuItem("TestFramework/Test Export/Documentation")]
        public static void OpenDocumentation()
        {
            Application.OpenURL("https://docs.unity3d.com/Packages/com.unity.test-framework@latest");
        }
        
        private static string GetDefaultOutputPath()
        {
            var timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var directory = Path.Combine(Application.dataPath, "..", "TestResults");
            
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            return Path.Combine(directory, $"TestResults_{timestamp}.xml");
        }
    }
}