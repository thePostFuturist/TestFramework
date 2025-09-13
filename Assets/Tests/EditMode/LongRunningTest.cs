using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using PerSpec;

namespace Tests.EditMode
{
    [TestFixture]
    public class LongRunningTest
    {
        [UnityTest]
        public IEnumerator Should_Run_For_30_Seconds() => UniTask.ToCoroutine(async () =>
        {
            PerSpecDebug.LogTestSetup("Starting 30-second test");
            
            var startTime = Time.realtimeSinceStartup;
            const float targetDuration = 30f; // 30 seconds
            
            // Log progress every 5 seconds
            float lastLogTime = startTime;
            const float logInterval = 5f;
            
            while (Time.realtimeSinceStartup - startTime < targetDuration)
            {
                float elapsed = Time.realtimeSinceStartup - startTime;
                
                // Log progress at intervals
                if (elapsed - lastLogTime >= logInterval)
                {
                    lastLogTime = elapsed;
                    float progress = (elapsed / targetDuration) * 100f;
                    PerSpecDebug.LogTestAction($"Test progress: {progress:F1}% ({elapsed:F1}/{targetDuration:F1} seconds)");
                }
                
                // Wait a bit before checking again
                await UniTask.Delay(100);
            }
            
            float finalDuration = Time.realtimeSinceStartup - startTime;
            PerSpecDebug.LogTestComplete($"30-second test completed in {finalDuration:F1} seconds");
            
            // Assert that we ran for at least 30 seconds
            Assert.GreaterOrEqual(finalDuration, targetDuration, "Test should run for at least 30 seconds");
        });
        
        [UnityTest]
        public IEnumerator Should_Run_For_10_Seconds() => UniTask.ToCoroutine(async () =>
        {
            PerSpecDebug.LogTestSetup("Starting 10-second test");
            
            var startTime = Time.realtimeSinceStartup;
            const float targetDuration = 10f; // 10 seconds
            
            // Simple 10-second delay
            await UniTask.Delay((int)(targetDuration * 1000));
            
            float finalDuration = Time.realtimeSinceStartup - startTime;
            PerSpecDebug.LogTestComplete($"10-second test completed in {finalDuration:F1} seconds");
            
            Assert.GreaterOrEqual(finalDuration, targetDuration, "Test should run for at least 10 seconds");
        });
        
        [UnityTest]
        public IEnumerator Should_Complete_Quickly() => UniTask.ToCoroutine(async () =>
        {
            PerSpecDebug.LogTestSetup("Starting quick test");
            
            // Very quick test for comparison
            await UniTask.Delay(100);
            
            PerSpecDebug.LogTestComplete("Quick test completed");
            Assert.Pass("Quick test passed");
        });
    }
}