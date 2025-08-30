using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Cysharp.Threading.Tasks;
using PerSpec;

namespace Tests.PlayMode
{
    public class SimplePerSpecTest
    {
        [UnityTest]
        public IEnumerator Should_Pass_Basic_Test() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            PerSpecDebug.LogTestSetup("Setting up basic test");
            int expected = 5;
            int actual = 2 + 3;
            
            // Act
            PerSpecDebug.LogTestAction("Performing calculation");
            await UniTask.Delay(100); // Simulate async work
            
            // Assert
            Assert.AreEqual(expected, actual, "Basic math should work");
            PerSpecDebug.LogTestComplete("Basic test passed!");
        });
        
        [UnityTest]
        public IEnumerator Should_Create_GameObject() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            PerSpecDebug.LogTestSetup("Creating test GameObject");
            GameObject testObject = null;
            
            try
            {
                // Act
                testObject = new GameObject("TestObject");
                testObject.transform.position = Vector3.one;
                
                PerSpecDebug.LogTestAction("Waiting for frame");
                await UniTask.Yield();
                
                // Assert
                Assert.IsNotNull(testObject, "GameObject should be created");
                Assert.AreEqual("TestObject", testObject.name, "Name should match");
                Assert.AreEqual(Vector3.one, testObject.transform.position, "Position should be set");
                
                PerSpecDebug.LogTestComplete("GameObject test passed!");
            }
            finally
            {
                // Cleanup
                if (testObject != null)
                {
                    Object.DestroyImmediate(testObject);
                    PerSpecDebug.Log("Cleaned up test object");
                }
            }
        });
        
        [UnityTest]
        public IEnumerator Should_Handle_Async_Operations() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            PerSpecDebug.LogTestSetup("Testing async operations");
            bool operationCompleted = false;
            
            // Act
            PerSpecDebug.LogTestAction("Starting async operation");
            await UniTask.RunOnThreadPool(() =>
            {
                // Simulate heavy computation
                System.Threading.Thread.Sleep(50);
                operationCompleted = true;
            });
            
            // Switch back to main thread for Unity operations
            await UniTask.SwitchToMainThread();
            
            // Assert
            Assert.IsTrue(operationCompleted, "Async operation should complete");
            PerSpecDebug.LogTestComplete("Async test passed!");
        });
        
        [Test]
        public void Should_Pass_Simple_Unit_Test()
        {
            // Arrange
            PerSpecDebug.LogTestSetup("Simple unit test");
            string text = "PerSpec";
            
            // Act
            string result = text.ToUpper();
            
            // Assert
            Assert.AreEqual("PERSPEC", result, "String should be uppercase");
            PerSpecDebug.LogTestComplete("Unit test passed!");
        }
    }
}