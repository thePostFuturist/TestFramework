using System;
using System.Collections;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Cysharp.Threading.Tasks;
using PerSpec;

namespace Tests.EditMode
{
    /// <summary>
    /// Example async tests demonstrating proper async operation handling
    /// with graceful cancellation support in Unity tests
    /// </summary>
    public class AsyncCancellationTests
    {
        private CancellationTokenSource testCancellationTokenSource;
        
        [SetUp]
        public void Setup()
        {
            testCancellationTokenSource = new CancellationTokenSource();
            Debug.Log("[TEST] Test setup - cancellation token created");
        }
        
        [TearDown]
        public void Teardown()
        {
            Debug.Log("[TEST] " + "Test teardown - cancelling operations");
            testCancellationTokenSource?.Cancel();
            testCancellationTokenSource?.Dispose();
        }
        
        #region Basic Async Tests
        
        [UnityTest]
        public IEnumerator Should_HandleGracefulShutdown_WhenOperationsAreRunning() => UniTask.ToCoroutine(async () =>
        {
            Debug.Log("[TEST-SETUP] " + "Testing graceful shutdown with running operations");
            
            // Start a long-running operation
            var operationTask = RunLongOperationAsync();
            
            // Wait a bit to ensure operation is running
            await UniTask.Delay(100);
            
            // The operation should complete normally
            Debug.Log("[TEST] " + "Operation started, waiting for completion");
            
            await operationTask;
            
            Debug.Log("[TEST-COMPLETE] " + "Graceful shutdown test completed");
        });
        
        private async UniTask RunLongOperationAsync()
        {
            Debug.Log("[TEST] " + "Long operation starting");
            
            for (int i = 0; i < 5; i++)
            {
                testCancellationTokenSource.Token.ThrowIfCancellationRequested();
                await UniTask.Delay(100, cancellationToken: testCancellationTokenSource.Token);
                Debug.Log("[TEST] " + $"Long operation progress: {i + 1}/5");
            }
            
            Debug.Log("[TEST] " + "Long operation completed");
        }
        
        [UnityTest]
        public IEnumerator Should_TrackMultipleOperations_Simultaneously() => UniTask.ToCoroutine(async () =>
        {
            Debug.Log("[TEST-SETUP] " + "Testing multiple simultaneous operations");
            
            // Start multiple operations
            var task1 = WaitForFramesAsync(3);
            var task2 = WaitForFramesAsync(5);
            var task3 = UniTask.Delay(50, cancellationToken: testCancellationTokenSource.Token);
            
            // Wait for all to complete
            await UniTask.WhenAll(task1, task2, task3);
            
            Debug.Log("[TEST-COMPLETE] " + "All operations completed successfully");
        });
        
        private async UniTask WaitForFramesAsync(int frameCount)
        {
            for (int i = 0; i < frameCount; i++)
            {
                await UniTask.Yield(testCancellationTokenSource.Token);
            }
        }
        
        #endregion
        
        #region Timeout and Cancellation Tests
        
        [UnityTest]
        public IEnumerator Should_RespectCancellationToken() => UniTask.ToCoroutine(async () =>
        {
            Debug.Log("[TEST-SETUP] " + "Testing cancellation token respect");
            
            using (var cts = new CancellationTokenSource())
            {
                // Start operation
                var task = DelayWithCancellation(1000, cts.Token);
                
                // Cancel after short delay
                _ = UniTask.Create(async () =>
                {
                    await UniTask.Delay(100);
                    cts.Cancel();
                    Debug.Log("[TEST] " + "Cancellation requested");
                });
                
                try
                {
                    await task;
                    Assert.Fail("Operation should have been cancelled");
                }
                catch (OperationCanceledException)
                {
                    Debug.Log("[TEST] " + "Operation cancelled as expected");
                }
            }
            
            Debug.Log("[TEST-COMPLETE] " + "Cancellation token test completed");
        });
        
        private async UniTask DelayWithCancellation(int milliseconds, CancellationToken token)
        {
            await UniTask.Delay(milliseconds, cancellationToken: token);
        }
        
        [UnityTest]
        public IEnumerator Should_CompleteQuickOperations_BeforeTimeout() => UniTask.ToCoroutine(async () =>
        {
            Debug.Log("[TEST-SETUP] " + "Testing quick operation completion");
            
            // Multiple quick operations
            await UniTask.Delay(10, cancellationToken: testCancellationTokenSource.Token);
            Debug.Log("[TEST] " + "Quick operation 1 completed");
            
            await UniTask.Yield(testCancellationTokenSource.Token);
            Debug.Log("[TEST] " + "Quick operation 2 completed");
            
            await UniTask.DelayFrame(1, cancellationToken: testCancellationTokenSource.Token);
            Debug.Log("[TEST] " + "Quick operation 3 completed");
            
            Debug.Log("[TEST-COMPLETE] " + "All quick operations completed");
        });
        
        [UnityTest]
        public IEnumerator Should_HandleTimeoutCorrectly() => UniTask.ToCoroutine(async () =>
        {
            Debug.Log("[TEST-SETUP] " + "Testing timeout handling");
            
            try
            {
                // Create a timeout token
                using (var timeoutCts = new CancellationTokenSource(500))
                {
                    // Try a long operation with timeout
                    await UniTask.Delay(2000, cancellationToken: timeoutCts.Token);
                    Assert.Fail("Should have timed out");
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[TEST] " + "Operation timed out as expected");
            }
            
            Debug.Log("[TEST-COMPLETE] " + "Timeout handled correctly");
        });
        
        #endregion
        
        #region Performance Measurement Tests
        
        [UnityTest]
        public IEnumerator Should_MeasureOperationPerformance() => UniTask.ToCoroutine(async () =>
        {
            Debug.Log("[TEST-SETUP] " + "Testing performance measurement");
            
            var startTime = Time.realtimeSinceStartup;
            
            // Perform some operations
            for (int i = 0; i < 10; i++)
            {
                await UniTask.Yield();
            }
            
            var elapsed = Time.realtimeSinceStartup - startTime;
            
            Assert.Greater(elapsed, 0f, "Operation should take measurable time");
            Debug.Log("[TEST-COMPLETE] " + $"Performance measured: {elapsed * 1000:F2}ms");
        });
        
        [UnityTest]
        public IEnumerator Should_HandleConcurrentOperations() => UniTask.ToCoroutine(async () =>
        {
            Debug.Log("[TEST-SETUP] " + "Testing concurrent operations");
            
            var completedCount = 0;
            
            // Start multiple concurrent operations
            var tasks = new UniTask[5];
            for (int i = 0; i < tasks.Length; i++)
            {
                int index = i;
                tasks[i] = UniTask.Create(async () =>
                {
                    await UniTask.Delay(100 * (index + 1), cancellationToken: testCancellationTokenSource.Token);
                    Interlocked.Increment(ref completedCount);
                    Debug.Log("[TEST] " + $"Task {index} completed");
                });
            }
            
            // Wait for all
            await UniTask.WhenAll(tasks);
            
            Assert.AreEqual(5, completedCount, "All tasks should complete");
            Debug.Log("[TEST-COMPLETE] " + "All concurrent operations completed");
        });
        
        #endregion
    }
}