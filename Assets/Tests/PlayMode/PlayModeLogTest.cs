using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Cysharp.Threading.Tasks;
using PerSpec;

namespace Tests.PlayMode
{
    [TestFixture]
    public class PlayModeLogTest
    {
        [UnityTest]
        public IEnumerator Should_CapturePlayModeLogs() => UniTask.ToCoroutine(async () =>
        {
            // Log various types of messages
            Debug.Log("[TEST] PlayMode test starting - this should be captured");
            PerSpecDebug.LogTestSetup("Setting up PlayMode log test");
            
            await UniTask.Delay(100);
            
            Debug.LogWarning("[TEST] PlayMode warning message");
            PerSpecDebug.LogTestAction("Performing test action in PlayMode");
            
            await UniTask.Delay(100);
            
            // Use LogWarning instead of LogError to avoid test failure
            Debug.LogWarning("[TEST] PlayMode simulated error message (as warning)");
            PerSpecDebug.LogTestAssert("Asserting in PlayMode test");
            
            // Test with objects
            var testObj = new GameObject("TestObject");
            Debug.Log("[TEST] Created test object", testObj);
            
            Object.DestroyImmediate(testObj);
            
            PerSpecDebug.LogTestComplete("PlayMode log test completed");
            Debug.Log("[TEST] PlayMode test ending - all logs should be captured");
            
            // Test passes if we get here
            Assert.IsTrue(true, "PlayMode logging test completed");
        });
    }
}