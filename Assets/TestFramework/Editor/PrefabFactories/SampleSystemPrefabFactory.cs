using UnityEngine;
using UnityEditor;
using System.IO;

namespace TestFramework.Editor.PrefabFactories
{
    /// <summary>
    /// Factory for creating sample test prefabs programmatically
    /// Demonstrates TDD pattern for Unity prefab creation
    /// </summary>
    public static class SampleSystemPrefabFactory
    {
        #region Constants
        
        private const string PREFAB_PATH = "Assets/Resources/TestPrefabs/SampleSystemPrefab.prefab";
        private const string PREFAB_NAME = "SampleSystem";
        
        #endregion
        
        #region Menu Items
        
        [MenuItem("TestFramework/Prefabs/Create Sample System Prefab")]
        public static void CreateSampleSystemPrefab()
        {
            // Ensure directory exists
            var directory = Path.GetDirectoryName(PREFAB_PATH);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                AssetDatabase.Refresh();
            }
            
            // Create root GameObject
            GameObject prefabRoot = new GameObject(PREFAB_NAME);
            
            try
            {
                // Add and configure components
                SetupMainComponents(prefabRoot);
                SetupChildObjects(prefabRoot);
                ConfigureConnections(prefabRoot);
                
                // Save as prefab
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PREFAB_PATH);
                Debug.Log($"[TestFramework] Created prefab at: {PREFAB_PATH}");
                
                // Select the created prefab in Project window
                var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
                Selection.activeObject = prefabAsset;
                EditorGUIUtility.PingObject(prefabAsset);
            }
            finally
            {
                // Always clean up the temporary scene object
                Object.DestroyImmediate(prefabRoot);
            }
        }
        
        [MenuItem("TestFramework/Prefabs/Recreate Sample System Prefab (Force)")]
        public static void RecreateSampleSystemPrefab()
        {
            // Delete existing prefab if it exists
            if (File.Exists(PREFAB_PATH))
            {
                AssetDatabase.DeleteAsset(PREFAB_PATH);
                Debug.Log($"[TestFramework] Deleted existing prefab at: {PREFAB_PATH}");
            }
            
            // Create new prefab
            CreateSampleSystemPrefab();
        }
        
        #endregion
        
        #region Setup Methods
        
        private static void SetupMainComponents(GameObject root)
        {
            // Add Rigidbody component
            var rb = root.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true;
            rb.mass = 1f;
            
            // Add Collider
            var collider = root.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = Vector3.one * 2f;
            
            // Add sample custom component (would be your actual component)
            // For demo purposes, we'll add a Light component
            var light = root.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = Color.yellow;
            light.intensity = 2f;
            light.range = 10f;
            
            // Add Transform configuration
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;
        }
        
        private static void SetupChildObjects(GameObject root)
        {
            // Create input handler child
            var inputHandler = new GameObject("InputHandler");
            inputHandler.transform.SetParent(root.transform);
            inputHandler.transform.localPosition = Vector3.zero;
            
            // Create display child with renderer
            var display = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            display.name = "Display";
            display.transform.SetParent(root.transform);
            display.transform.localPosition = Vector3.up * 2f;
            display.transform.localScale = Vector3.one * 0.5f;
            
            // Configure display material
            var renderer = display.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Standard"));
                renderer.material.color = Color.cyan;
            }
            
            // Create sensor children
            for (int i = 0; i < 3; i++)
            {
                var sensor = new GameObject($"Sensor_{i}");
                sensor.transform.SetParent(root.transform);
                
                // Position sensors in a circle
                float angle = i * 120f * Mathf.Deg2Rad;
                sensor.transform.localPosition = new Vector3(
                    Mathf.Cos(angle) * 3f,
                    0,
                    Mathf.Sin(angle) * 3f
                );
                
                // Add a small collider to each sensor
                var sensorCollider = sensor.AddComponent<SphereCollider>();
                sensorCollider.radius = 0.5f;
                sensorCollider.isTrigger = true;
            }
        }
        
        private static void ConfigureConnections(GameObject root)
        {
            // This simulates setting up references between components
            // In a real scenario, you would use FindVars pattern here
            
            // Example: Set layer for all children
            SetLayerRecursively(root, LayerMask.NameToLayer("Default"));
            
            // Example: Add tags
            root.tag = "Untagged";
        }
        
        #endregion
        
        #region Validation
        
        [MenuItem("TestFramework/Prefabs/Validate Sample System Prefab")]
        public static void ValidatePrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            if (prefab == null)
            {
                Debug.LogError($"[TestFramework] Prefab not found at: {PREFAB_PATH}");
                Debug.LogError("[TestFramework] Run 'TestFramework/Prefabs/Create Sample System Prefab' to create it");
                return;
            }
            
            bool isValid = true;
            
            // Validate main components
            if (!prefab.GetComponent<Rigidbody>())
            {
                Debug.LogError("[TestFramework] Missing Rigidbody component");
                isValid = false;
            }
            
            if (!prefab.GetComponent<BoxCollider>())
            {
                Debug.LogError("[TestFramework] Missing BoxCollider component");
                isValid = false;
            }
            
            if (!prefab.GetComponent<Light>())
            {
                Debug.LogError("[TestFramework] Missing Light component");
                isValid = false;
            }
            
            // Validate children
            var inputHandler = prefab.transform.Find("InputHandler");
            if (inputHandler == null)
            {
                Debug.LogError("[TestFramework] Missing InputHandler child object");
                isValid = false;
            }
            
            var display = prefab.transform.Find("Display");
            if (display == null)
            {
                Debug.LogError("[TestFramework] Missing Display child object");
                isValid = false;
            }
            
            // Validate sensors
            for (int i = 0; i < 3; i++)
            {
                var sensor = prefab.transform.Find($"Sensor_{i}");
                if (sensor == null)
                {
                    Debug.LogError($"[TestFramework] Missing Sensor_{i} child object");
                    isValid = false;
                }
                else if (!sensor.GetComponent<SphereCollider>())
                {
                    Debug.LogError($"[TestFramework] Sensor_{i} missing SphereCollider");
                    isValid = false;
                }
            }
            
            if (isValid)
            {
                Debug.Log($"[TestFramework] Prefab validation PASSED for: {PREFAB_PATH}");
            }
            else
            {
                Debug.LogError($"[TestFramework] Prefab validation FAILED for: {PREFAB_PATH}");
            }
        }
        
        #endregion
        
        #region Helper Methods
        
        private static void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }
        
        #endregion
        
        #region Batch Operations
        
        [MenuItem("TestFramework/Prefabs/Create All Test Prefabs")]
        public static void CreateAllTestPrefabs()
        {
            Debug.Log("[TestFramework] Creating all test prefabs...");
            
            CreateSampleSystemPrefab();
            // Add other prefab creation methods here as needed
            
            Debug.Log("[TestFramework] All test prefabs created successfully");
            AssetDatabase.Refresh();
        }
        
        [MenuItem("TestFramework/Prefabs/Validate All Test Prefabs")]
        public static void ValidateAllTestPrefabs()
        {
            Debug.Log("[TestFramework] Validating all test prefabs...");
            
            ValidatePrefab();
            // Add other validation methods here as needed
            
            Debug.Log("[TestFramework] Validation complete");
        }
        
        #endregion
    }
}