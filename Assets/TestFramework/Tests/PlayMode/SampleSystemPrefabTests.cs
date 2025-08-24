using System;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using Cysharp.Threading.Tasks;
using TestFramework.Unity.Core;

namespace TestFramework.Tests.PlayMode
{
    /// <summary>
    /// Sample test demonstrating TDD pattern with Unity prefabs
    /// Shows proper setup, teardown, and testing patterns
    /// </summary>
    [TestFixture]
    public class SampleSystemPrefabTests : UniTaskTestBase
    {
        #region Fields
        
        private GameObject sampleSystemInstance;
        private Rigidbody systemRigidbody;
        private BoxCollider systemCollider;
        private Light systemLight;
        private Transform inputHandler;
        private Transform display;
        private Transform[] sensors;
        
        #endregion
        
        #region Setup and Teardown
        
        [SetUp]
        public override void Setup()
        {
            base.Setup();
            
            // Load the prefab from Resources
            var prefab = Resources.Load<GameObject>("TestPrefabs/SampleSystemPrefab");
            Assert.IsNotNull(prefab, 
                "SampleSystemPrefab not found in Resources/TestPrefabs/. " +
                "Run 'TestFramework/Prefabs/Create Sample System Prefab' menu item to create it.");
            
            // Instantiate the prefab for testing
            sampleSystemInstance = UnityEngine.Object.Instantiate(prefab);
            sampleSystemInstance.name = "SampleSystem_TestInstance";
            
            // Cache frequently used components
            CacheComponents();
            
            // Validate critical components exist
            ValidateComponents();
        }
        
        [TearDown]
        public override void TearDown()
        {
            // Clean up test instance
            if (sampleSystemInstance != null)
            {
                UnityEngine.Object.DestroyImmediate(sampleSystemInstance);
                sampleSystemInstance = null;
            }
            
            // Clear cached references
            ClearCachedReferences();
            
            base.TearDown();
        }
        
        #endregion
        
        #region Setup Helpers
        
        private void CacheComponents()
        {
            systemRigidbody = sampleSystemInstance.GetComponent<Rigidbody>();
            systemCollider = sampleSystemInstance.GetComponent<BoxCollider>();
            systemLight = sampleSystemInstance.GetComponent<Light>();
            
            inputHandler = sampleSystemInstance.transform.Find("InputHandler");
            display = sampleSystemInstance.transform.Find("Display");
            
            // Cache sensor references
            sensors = new Transform[3];
            for (int i = 0; i < 3; i++)
            {
                sensors[i] = sampleSystemInstance.transform.Find($"Sensor_{i}");
            }
        }
        
        private void ValidateComponents()
        {
            Assert.IsNotNull(systemRigidbody, "Rigidbody component missing from prefab");
            Assert.IsNotNull(systemCollider, "BoxCollider component missing from prefab");
            Assert.IsNotNull(systemLight, "Light component missing from prefab");
            Assert.IsNotNull(inputHandler, "InputHandler child object missing from prefab");
            Assert.IsNotNull(display, "Display child object missing from prefab");
            
            for (int i = 0; i < sensors.Length; i++)
            {
                Assert.IsNotNull(sensors[i], $"Sensor_{i} child object missing from prefab");
            }
        }
        
        private void ClearCachedReferences()
        {
            systemRigidbody = null;
            systemCollider = null;
            systemLight = null;
            inputHandler = null;
            display = null;
            sensors = null;
        }
        
        #endregion
        
        #region Component Configuration Tests
        
        [UnityTest]
        public IEnumerator Should_Have_Correct_Rigidbody_Configuration() => UniTask.ToCoroutine(async () =>
        {
            // Assert
            Assert.IsFalse(systemRigidbody.useGravity, "Rigidbody should not use gravity");
            Assert.IsTrue(systemRigidbody.isKinematic, "Rigidbody should be kinematic");
            Assert.AreEqual(1f, systemRigidbody.mass, 0.01f, "Rigidbody mass should be 1");
            
            await UniTask.Yield();
        });
        
        [UnityTest]
        public IEnumerator Should_Have_Correct_Collider_Configuration() => UniTask.ToCoroutine(async () =>
        {
            // Assert
            Assert.IsTrue(systemCollider.isTrigger, "Collider should be a trigger");
            Assert.AreEqual(Vector3.one * 2f, systemCollider.size, "Collider size should be 2x2x2");
            
            await UniTask.Yield();
        });
        
        [UnityTest]
        public IEnumerator Should_Have_Correct_Light_Configuration() => UniTask.ToCoroutine(async () =>
        {
            // Assert
            Assert.AreEqual(LightType.Point, systemLight.type, "Light should be Point type");
            Assert.AreEqual(Color.yellow, systemLight.color, "Light should be yellow");
            Assert.AreEqual(2f, systemLight.intensity, 0.01f, "Light intensity should be 2");
            Assert.AreEqual(10f, systemLight.range, 0.01f, "Light range should be 10");
            
            await UniTask.Yield();
        });
        
        #endregion
        
        #region Child Object Tests
        
        [UnityTest]
        public IEnumerator Should_Have_All_Required_Child_Objects() => UniTask.ToCoroutine(async () =>
        {
            // Assert
            Assert.IsNotNull(inputHandler, "Should have InputHandler child");
            Assert.IsNotNull(display, "Should have Display child");
            
            // Verify Display has renderer
            var displayRenderer = display.GetComponent<Renderer>();
            Assert.IsNotNull(displayRenderer, "Display should have Renderer component");
            Assert.IsNotNull(displayRenderer.material, "Display should have material");
            
            await UniTask.Yield();
        });
        
        [UnityTest]
        public IEnumerator Should_Have_Three_Sensor_Objects() => UniTask.ToCoroutine(async () =>
        {
            // Assert
            Assert.AreEqual(3, sensors.Length, "Should have exactly 3 sensors");
            
            for (int i = 0; i < sensors.Length; i++)
            {
                Assert.IsNotNull(sensors[i], $"Sensor_{i} should exist");
                
                var sensorCollider = sensors[i].GetComponent<SphereCollider>();
                Assert.IsNotNull(sensorCollider, $"Sensor_{i} should have SphereCollider");
                Assert.IsTrue(sensorCollider.isTrigger, $"Sensor_{i} collider should be trigger");
                Assert.AreEqual(0.5f, sensorCollider.radius, 0.01f, $"Sensor_{i} radius should be 0.5");
            }
            
            await UniTask.Yield();
        });
        
        [UnityTest]
        public IEnumerator Should_Have_Sensors_Positioned_In_Circle() => UniTask.ToCoroutine(async () =>
        {
            // Verify sensors are positioned in a circle
            for (int i = 0; i < sensors.Length; i++)
            {
                float expectedAngle = i * 120f * Mathf.Deg2Rad;
                Vector3 expectedPosition = new Vector3(
                    Mathf.Cos(expectedAngle) * 3f,
                    0,
                    Mathf.Sin(expectedAngle) * 3f
                );
                
                Vector3 actualPosition = sensors[i].localPosition;
                float distance = Vector3.Distance(expectedPosition, actualPosition);
                
                Assert.Less(distance, 0.01f, 
                    $"Sensor_{i} should be at position {expectedPosition}, but is at {actualPosition}");
            }
            
            await UniTask.Yield();
        });
        
        #endregion
        
        #region Runtime Behavior Tests
        
        [UnityTest]
        public IEnumerator Should_Maintain_Position_When_Moved() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            Vector3 newPosition = new Vector3(5f, 10f, 15f);
            
            // Act
            sampleSystemInstance.transform.position = newPosition;
            await UniTask.Delay(100);
            
            // Assert - Kinematic rigidbody should maintain position
            Assert.AreEqual(newPosition, sampleSystemInstance.transform.position, 
                "Object should maintain its position");
            
            await UniTask.Yield();
        });
        
        [UnityTest]
        public IEnumerator Should_Detect_Trigger_Collisions() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var testObject = new GameObject("TestCollider");
            var testCollider = testObject.AddComponent<BoxCollider>();
            testCollider.isTrigger = false;
            
            bool triggerEntered = false;
            var triggerDetector = sampleSystemInstance.AddComponent<TriggerDetector>();
            triggerDetector.OnTriggerEnterCallback = (other) => 
            {
                if (other.gameObject == testObject)
                    triggerEntered = true;
            };
            
            try
            {
                // Act - Move test object into trigger zone
                testObject.transform.position = sampleSystemInstance.transform.position;
                
                // Wait for physics update
                await UniTask.WaitForFixedUpdate();
                await UniTask.Delay(100);
                
                // Assert
                Assert.IsTrue(triggerEntered, "Should detect trigger collision");
            }
            finally
            {
                // Cleanup
                UnityEngine.Object.DestroyImmediate(testObject);
                UnityEngine.Object.DestroyImmediate(triggerDetector);
            }
        });
        
        [UnityTest]
        public IEnumerator Should_Handle_Concurrent_Sensor_Updates() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var sensorTasks = new UniTask[sensors.Length];
            var sensorRotations = new Quaternion[sensors.Length];
            
            // Act - Rotate all sensors concurrently
            for (int i = 0; i < sensors.Length; i++)
            {
                int index = i; // Capture for closure
                sensorTasks[i] = RotateSensorAsync(sensors[index], index * 30f);
            }
            
            await UniTask.WhenAll(sensorTasks);
            
            // Capture final rotations
            for (int i = 0; i < sensors.Length; i++)
            {
                sensorRotations[i] = sensors[i].rotation;
            }
            
            // Assert - All sensors should have different rotations
            for (int i = 0; i < sensors.Length - 1; i++)
            {
                Assert.AreNotEqual(sensorRotations[i], sensorRotations[i + 1], 
                    $"Sensor {i} and {i + 1} should have different rotations");
            }
        });
        
        #endregion
        
        #region Performance Tests
        
        [UnityTest]
        public IEnumerator Should_Initialize_Within_Performance_Budget() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var stopwatch = new System.Diagnostics.Stopwatch();
            
            // Act - Measure instantiation time
            stopwatch.Start();
            var testPrefab = Resources.Load<GameObject>("TestPrefabs/SampleSystemPrefab");
            var testInstance = UnityEngine.Object.Instantiate(testPrefab);
            stopwatch.Stop();
            
            try
            {
                // Assert
                var elapsedMs = stopwatch.ElapsedMilliseconds;
                Debug.Log($"[TestFramework] Prefab instantiation took: {elapsedMs}ms");
                Assert.Less(elapsedMs, 50, "Prefab should instantiate within 50ms");
            }
            finally
            {
                // Cleanup
                UnityEngine.Object.DestroyImmediate(testInstance);
            }
            
            await UniTask.Yield();
        });
        
        [UnityTest]
        public IEnumerator Should_Handle_Multiple_Instances() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var prefab = Resources.Load<GameObject>("TestPrefabs/SampleSystemPrefab");
            var instances = new GameObject[10];
            
            try
            {
                // Act - Create multiple instances
                for (int i = 0; i < instances.Length; i++)
                {
                    instances[i] = UnityEngine.Object.Instantiate(prefab);
                    instances[i].name = $"Instance_{i}";
                    instances[i].transform.position = Vector3.right * i * 3f;
                }
                
                await UniTask.Delay(100);
                
                // Assert - All instances should exist and be at correct positions
                for (int i = 0; i < instances.Length; i++)
                {
                    Assert.IsNotNull(instances[i]);
                    Assert.AreEqual(Vector3.right * i * 3f, instances[i].transform.position);
                }
            }
            finally
            {
                // Cleanup all instances
                foreach (var instance in instances)
                {
                    if (instance != null)
                        UnityEngine.Object.DestroyImmediate(instance);
                }
            }
        });
        
        #endregion
        
        #region Helper Methods
        
        private async UniTask RotateSensorAsync(Transform sensor, float angle)
        {
            var startRotation = sensor.rotation;
            var endRotation = Quaternion.Euler(0, angle, 0);
            
            float elapsed = 0f;
            float duration = 0.5f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                sensor.rotation = Quaternion.Lerp(startRotation, endRotation, t);
                await UniTask.Yield(testCancellationTokenSource.Token);
            }
            
            sensor.rotation = endRotation;
        }
        
        /// <summary>
        /// Helper component for detecting trigger events
        /// </summary>
        private class TriggerDetector : MonoBehaviour
        {
            public System.Action<Collider> OnTriggerEnterCallback;
            
            private void OnTriggerEnter(Collider other)
            {
                OnTriggerEnterCallback?.Invoke(other);
            }
        }
        
        #endregion
    }
}