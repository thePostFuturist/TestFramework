using System.Collections;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Cysharp.Threading.Tasks;

namespace Tests.PlayMode
{
    [TestFixture]
    public class ThreadedLogTest
    {
        [UnityTest]
        public IEnumerator Should_HandleThreadedLogs() => UniTask.ToCoroutine(async () =>
        {
            Debug.Log("[THREAD-TEST] Starting threaded log test");
            
            // Create a background thread that will log messages
            bool threadCompleted = false;
            Thread backgroundThread = new Thread(() =>
            {
                try
                {
                    Debug.Log("[THREAD-TEST] Log from background thread 1");
                    Thread.Sleep(50);
                    Debug.LogWarning("[THREAD-TEST] Warning from background thread");
                    Thread.Sleep(50);
                    Debug.Log("[THREAD-TEST] Log from background thread 2");
                    threadCompleted = true;
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[THREAD-TEST] Thread exception: {ex.Message}");
                }
            });
            
            backgroundThread.Start();
            
            // Log from main thread while background thread is running
            Debug.Log("[THREAD-TEST] Main thread log 1");
            await UniTask.Delay(100);
            Debug.Log("[THREAD-TEST] Main thread log 2");
            
            // Wait for background thread to complete
            int maxWait = 50; // 5 seconds max
            while (!threadCompleted && maxWait > 0)
            {
                await UniTask.Delay(100);
                maxWait--;
            }
            
            Debug.Log($"[THREAD-TEST] Thread completed: {threadCompleted}");
            Debug.Log("[THREAD-TEST] Threaded log test completed successfully");
            
            Assert.IsTrue(threadCompleted, "Background thread should have completed");
        });
    }
}