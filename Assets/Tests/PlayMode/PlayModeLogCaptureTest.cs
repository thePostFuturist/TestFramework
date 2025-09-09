using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Cysharp.Threading.Tasks;
using PerSpec;

namespace Tests.PlayMode
{
    [TestFixture]
    public class PlayModeLogCaptureTest
    {
        [UnityTest]
        public IEnumerator Should_Capture_All_Log_Types() => UniTask.ToCoroutine(async () =>
        {
            // Test comprehensive logging to verify capture
            PerSpecDebug.LogTestSetup("Starting comprehensive log capture test");
            
            // Log different types
            Debug.Log("[TEST-CAPTURE] Info log 1");
            Debug.LogWarning("[TEST-CAPTURE] Warning log 1");
            Debug.LogError("[TEST-CAPTURE] Error log 1");
            
            // Log from background thread
            await UniTask.RunOnThreadPool(() =>
            {
                Debug.Log("[TEST-CAPTURE-THREAD] Background thread log");
            });
            
            await UniTask.SwitchToMainThread();
            
            // Rapid burst of logs to test queue
            for (int i = 0; i < 50; i++)
            {
                Debug.Log($"[TEST-CAPTURE-BURST] Rapid log {i}");
            }
            
            // Wait a bit for processing
            await UniTask.Delay(200);
            
            // More logs with different levels
            Debug.Log("[TEST-CAPTURE] Info log 2");
            Debug.LogWarning("[TEST-CAPTURE] Warning log 2");
            Debug.LogError("[TEST-CAPTURE] Error log 2");
            
            // Feature-specific logs
            PerSpecDebug.LogFeatureStart("CAPTURE-TEST", "Testing capture reliability");
            PerSpecDebug.LogFeatureProgress("CAPTURE-TEST", "Processing logs");
            PerSpecDebug.LogFeatureComplete("CAPTURE-TEST", "Capture test finished");
            
            // Test logs with stack traces
            try
            {
                throw new System.Exception("Test exception for stack trace");
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
            
            // Final set of logs
            for (int i = 0; i < 10; i++)
            {
                Debug.Log($"[TEST-CAPTURE-FINAL] Final log {i}");
            }
            
            PerSpecDebug.LogTestComplete("Comprehensive log capture test completed");
            
            // Wait for logs to be processed
            await UniTask.Delay(500);
            
            // Assert test passed (actual verification done via monitor_logs.py)
            Assert.Pass("Log capture test completed - verify with monitor_logs.py");
        });
        
        [UnityTest]
        public IEnumerator Should_Handle_High_Volume_Logging() => UniTask.ToCoroutine(async () =>
        {
            PerSpecDebug.LogTestSetup("Starting high-volume log test");
            
            // Generate a large number of logs quickly
            const int LOG_COUNT = 500;
            
            for (int batch = 0; batch < 5; batch++)
            {
                Debug.Log($"[HIGH-VOLUME] Starting batch {batch}");
                
                // Burst of logs
                for (int i = 0; i < LOG_COUNT / 5; i++)
                {
                    Debug.Log($"[HIGH-VOLUME-{batch}] Log message {i}");
                    
                    // Mix in some warnings and errors
                    if (i % 20 == 0)
                    {
                        Debug.LogWarning($"[HIGH-VOLUME-{batch}] Warning {i}");
                    }
                    if (i % 50 == 0)
                    {
                        Debug.LogError($"[HIGH-VOLUME-{batch}] Error {i}");
                    }
                }
                
                // Small delay between batches
                await UniTask.Delay(100);
            }
            
            PerSpecDebug.LogTestComplete($"High-volume test completed - generated {LOG_COUNT} logs");
            
            // Wait for processing
            await UniTask.Delay(1000);
            
            Assert.Pass($"High-volume test completed - {LOG_COUNT} logs generated");
        });
        
        [UnityTest]
        public IEnumerator Should_Capture_Logs_During_Async_Operations() => UniTask.ToCoroutine(async () =>
        {
            PerSpecDebug.LogTestSetup("Starting async operations log test");
            
            // Test logging during various async contexts
            Debug.Log("[ASYNC-TEST] Main thread log before async");
            
            // Background thread operation
            await UniTask.RunOnThreadPool(async () =>
            {
                Debug.Log("[ASYNC-TEST] Background thread start");
                await UniTask.Delay(50);
                Debug.Log("[ASYNC-TEST] Background thread middle");
                await UniTask.Delay(50);
                Debug.Log("[ASYNC-TEST] Background thread end");
            });
            
            Debug.Log("[ASYNC-TEST] Back on main thread");
            
            // Parallel operations
            var task1 = UniTask.RunOnThreadPool(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    Debug.Log($"[ASYNC-TEST-T1] Thread 1 log {i}");
                    System.Threading.Thread.Sleep(10);
                }
            });
            
            var task2 = UniTask.RunOnThreadPool(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    Debug.Log($"[ASYNC-TEST-T2] Thread 2 log {i}");
                    System.Threading.Thread.Sleep(10);
                }
            });
            
            await UniTask.WhenAll(task1, task2);
            
            await UniTask.SwitchToMainThread();
            Debug.Log("[ASYNC-TEST] All async operations completed");
            
            PerSpecDebug.LogTestComplete("Async operations log test completed");
            
            // Wait for processing
            await UniTask.Delay(500);
            
            Assert.Pass("Async operations log test completed");
        });
    }
}