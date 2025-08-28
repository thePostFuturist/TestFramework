using System;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;

namespace PerSpec.Runtime.DOTS
{
    /// <summary>
    /// Factory class for creating DOTS test objects programmatically.
    /// Used by tests and the editor window to create consistent test setups.
    /// Implements IDisposable to manage temporary test objects.
    /// </summary>
    public class DOTSTestFactory : System.IDisposable
    {
        private System.Collections.Generic.List<GameObject> temporaryObjects;
        private bool disposed = false;
        
        public DOTSTestFactory()
        {
            temporaryObjects = new System.Collections.Generic.List<GameObject>();
        }
        
        /// <summary>
        /// Load a prefab from the Tests/Prefabs folder
        /// </summary>
        public GameObject LoadPrefab(string prefabName)
        {
            string prefabPath = $"Assets/TestFramework/DOTS/Prefabs/{prefabName}.prefab";
            
            // Try to load prefab from Resources or create new setup
            GameObject prefabInstance = null;
            
#if UNITY_EDITOR
            // In editor, load from AssetDatabase
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                prefabInstance = UnityEngine.Object.Instantiate(prefab);
                Debug.Log($"[DOTSTestFactory] Loaded prefab: {prefabName}");
            }
#endif
            
            // If prefab not found, create programmatically
            if (prefabInstance == null)
            {
                Debug.Log($"[DOTSTestFactory] Creating {prefabName} programmatically");
                prefabInstance = CreateSetupByName(prefabName);
            }
            
            if (prefabInstance != null)
            {
                temporaryObjects.Add(prefabInstance);
            }
            
            return prefabInstance;
        }
        
        /// <summary>
        /// Create a setup by name if prefab is not found
        /// </summary>
        private GameObject CreateSetupByName(string prefabName)
        {
            var config = CreateDefaultConfiguration();
            
            switch (prefabName)
            {
                case "DOTSTestSetup":
                    return CreateBasicTestSetup(config);
                    
                case "DOTSPerformanceTestSetup":
                    config.EnableDiagnostics = true;
                    config.CollectMetrics = true;
                    return CreatePerformanceTestSetup(config);
                    
                case "DOTSUITestSetup":
                    return CreateUITestSetup(config);
                    
                default:
                    Debug.Log($"[DOTSTestFactory] Unknown prefab name: {prefabName}, creating default setup");
                    return CreateBasicTestSetup(config);
            }
        }
        
        /// <summary>
        /// Create default test configuration
        /// </summary>
        public static DOTSTestConfiguration CreateDefaultConfiguration()
        {
            return new DOTSTestConfiguration
            {
                Name = "DefaultTest",
                EnableDiagnostics = true,
                CollectMetrics = false
            };
        }
        
        /// <summary>
        /// Creates a basic test setup with necessary components
        /// Template method - replace with your specific implementation
        /// </summary>
        public static GameObject CreateBasicTestSetup(DOTSTestConfiguration config)
        {
            var testSetup = new GameObject($"DOTSTestSetup_{config.Name}");
            
            // Create test object child 
            var testObject = new GameObject("TestObject");
            testObject.transform.SetParent(testSetup.transform);
            
            // TODO: Add your specific test components here
            // Example:
            // var myComponent = testObject.AddComponent<MyTestComponent>();
            // ConfigureMyComponent(myComponent, config);
            
            // Create Main Camera if needed
            var cameraObject = new GameObject("Main Camera");
            cameraObject.transform.SetParent(testSetup.transform);
            cameraObject.transform.localPosition = new Vector3(0, 1, -10);
            cameraObject.tag = "MainCamera";
            
            var camera = cameraObject.AddComponent<Camera>();
            ConfigureTestCamera(camera);
            
#if UNITY_EDITOR
            // Mark everything as dirty for editor saving
            UnityEditor.EditorUtility.SetDirty(testSetup);
            UnityEditor.EditorUtility.SetDirty(testObject);
#endif
            
            return testSetup;
        }
        
        /// <summary>
        /// Creates a performance test setup
        /// Template method - replace with your specific implementation
        /// </summary>
        public static GameObject CreatePerformanceTestSetup(DOTSTestConfiguration config)
        {
            var testSetup = new GameObject($"DOTSPerformanceTestSetup_{config.Name}");
            
            // Create performance test object
            var perfTestObject = new GameObject("PerformanceTestObject");
            perfTestObject.transform.SetParent(testSetup.transform);
            
            // TODO: Add performance monitoring components
            // Example:
            // var perfMonitor = perfTestObject.AddComponent<PerformanceMonitor>();
            // ConfigurePerformanceMonitor(perfMonitor, config);
            
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(testSetup);
            UnityEditor.EditorUtility.SetDirty(perfTestObject);
#endif
            
            return testSetup;
        }
        
        /// <summary>
        /// Creates a UI integration test setup
        /// Template method - replace with your specific implementation
        /// </summary>
        public static GameObject CreateUITestSetup(DOTSTestConfiguration config)
        {
            var testSetup = new GameObject($"DOTSUITestSetup_{config.Name}");
            
            // Create Canvas
            var canvas = testSetup.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            testSetup.AddComponent<UnityEngine.UI.CanvasScaler>();
            testSetup.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            // TODO: Add UI test components
            // Example:
            // var uiManager = testSetup.AddComponent<UITestManager>();
            // ConfigureUIManager(uiManager, config);
            
            return testSetup;
        }
        
        /// <summary>
        /// Creates DOTS entities for testing
        /// Template method - replace with your specific implementation
        /// </summary>
        public static Entity CreateTestEntity(EntityManager entityManager, DOTSTestConfiguration config)
        {
            var entity = entityManager.CreateEntity();
            
            // TODO: Add your specific components to the entity
            // Example:
            // entityManager.AddComponentData(entity, new MyTestComponent
            // {
            //     Value = config.TestValue
            // });
            
            return entity;
        }
        
        #region Private Helper Methods
        
        private static void ConfigureTestCamera(Camera camera)
        {
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = false;
            camera.fieldOfView = 60;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 1000f;
            camera.depth = -1;
        }
        
        private static void SetPrivateField(object obj, string fieldName, object value)
        {
            var type = obj.GetType();
            var field = type.GetField(fieldName, 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            
            if (field != null)
            {
                field.SetValue(obj, value);
            }
            else
            {
                Debug.LogWarning($"[DOTSTestFactory] Field '{fieldName}' not found on type {type.Name}");
            }
        }
        
        #endregion
        
        #region IDisposable Implementation
        
        public void Dispose()
        {
            if (!disposed)
            {
                // Clean up temporary objects
                if (temporaryObjects != null)
                {
                    foreach (var obj in temporaryObjects)
                    {
                        if (obj != null)
                        {
                            UnityEngine.Object.DestroyImmediate(obj);
                        }
                    }
                    temporaryObjects.Clear();
                }
                
                disposed = true;
            }
        }
        
        #endregion
    }
}