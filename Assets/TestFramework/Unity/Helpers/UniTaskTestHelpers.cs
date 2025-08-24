using System;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;

namespace TestFramework.Unity.Helpers
{
    /// <summary>
    /// Helper utilities for UniTask-based testing
    /// Provides common async patterns and utilities
    /// </summary>
    public static class UniTaskTestHelpers
    {
        #region Delay Helpers
        
        /// <summary>
        /// Creates a delayed UniTask with proper timing for tests
        /// </summary>
        /// <param name="milliseconds">Delay in milliseconds</param>
        /// <param name="useUnscaledTime">Whether to use unscaled time</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        public static UniTask DelayMilliseconds(int milliseconds, bool useUnscaledTime = false, CancellationToken cancellationToken = default)
        {
            var delayType = useUnscaledTime ? DelayType.UnscaledDeltaTime : DelayType.DeltaTime;
            
            // In EditMode, force Realtime as DeltaTime doesn't work properly
            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                delayType = DelayType.Realtime;
            }
            #endif
            
            return UniTask.Delay(milliseconds, delayType, PlayerLoopTiming.Update, cancellationToken);
        }
        
        #endregion
        
        #region Batch Operations
        
        /// <summary>
        /// Runs multiple async operations in parallel and waits for all to complete
        /// </summary>
        /// <param name="operations">Array of async operations to run</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        public static async UniTask RunInParallel(params Func<UniTask>[] operations)
        {
            var tasks = new UniTask[operations.Length];
            for (int i = 0; i < operations.Length; i++)
            {
                tasks[i] = operations[i]();
            }
            await UniTask.WhenAll(tasks);
        }
        
        /// <summary>
        /// Runs multiple async operations in parallel and returns all results
        /// </summary>
        /// <typeparam name="T">Return type of operations</typeparam>
        /// <param name="operations">Array of async operations to run</param>
        public static async UniTask<T[]> RunInParallelWithResults<T>(params Func<UniTask<T>>[] operations)
        {
            var tasks = new UniTask<T>[operations.Length];
            for (int i = 0; i < operations.Length; i++)
            {
                tasks[i] = operations[i]();
            }
            return await UniTask.WhenAll(tasks);
        }
        
        /// <summary>
        /// Runs operations sequentially with delay between each
        /// </summary>
        /// <param name="delayMs">Delay in milliseconds between operations</param>
        /// <param name="operations">Operations to run</param>
        public static async UniTask RunSequentiallyWithDelay(int delayMs, params Func<UniTask>[] operations)
        {
            foreach (var operation in operations)
            {
                await operation();
                await DelayMilliseconds(delayMs);
            }
        }
        
        #endregion
        
        #region Retry Logic
        
        /// <summary>
        /// Retries an async operation with exponential backoff
        /// </summary>
        /// <typeparam name="T">Return type of the operation</typeparam>
        /// <param name="operation">The operation to retry</param>
        /// <param name="maxRetries">Maximum number of retries</param>
        /// <param name="initialDelayMs">Initial delay in milliseconds</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        public static async UniTask<T> RetryWithBackoff<T>(
            Func<UniTask<T>> operation, 
            int maxRetries = 3, 
            int initialDelayMs = 100,
            CancellationToken cancellationToken = default)
        {
            int delayMs = initialDelayMs;
            
            for (int i = 0; i <= maxRetries; i++)
            {
                try
                {
                    return await operation();
                }
                catch (Exception ex) when (i < maxRetries)
                {
                    Debug.LogWarning($"[UniTaskHelper] Retry {i + 1}/{maxRetries} failed: {ex.Message}");
                    await DelayMilliseconds(delayMs, cancellationToken: cancellationToken);
                    delayMs *= 2; // Exponential backoff
                }
            }
            
            // This will throw the last exception
            return await operation();
        }
        
        #endregion
        
        #region GameObject Helpers
        
        /// <summary>
        /// Creates a test GameObject and destroys it after the operation
        /// </summary>
        /// <param name="name">Name of the GameObject</param>
        /// <param name="operation">Operation to perform with the GameObject</param>
        public static async UniTask WithTestGameObject(string name, Func<GameObject, UniTask> operation)
        {
            GameObject testObject = null;
            try
            {
                await UniTask.SwitchToMainThread();
                testObject = new GameObject(name);
                await operation(testObject);
            }
            finally
            {
                if (testObject != null)
                {
                    UnityEngine.Object.Destroy(testObject);
                }
            }
        }
        
        /// <summary>
        /// Creates a test GameObject with components
        /// </summary>
        /// <param name="name">Name of the GameObject</param>
        /// <param name="components">Components to add</param>
        /// <param name="operation">Operation to perform</param>
        public static async UniTask WithTestGameObject(string name, Type[] components, Func<GameObject, UniTask> operation)
        {
            GameObject testObject = null;
            try
            {
                await UniTask.SwitchToMainThread();
                testObject = new GameObject(name);
                
                foreach (var componentType in components)
                {
                    testObject.AddComponent(componentType);
                }
                
                await operation(testObject);
            }
            finally
            {
                if (testObject != null)
                {
                    UnityEngine.Object.Destroy(testObject);
                }
            }
        }
        
        #endregion
        
        #region Performance Helpers
        
        /// <summary>
        /// Profiles memory allocation during an async operation
        /// </summary>
        /// <param name="operation">Operation to profile</param>
        /// <param name="operationName">Name for logging</param>
        public static async UniTask<long> ProfileMemoryAllocation(Func<UniTask> operation, string operationName = "Operation")
        {
            await UniTask.SwitchToMainThread();
            
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();
            
            var startMemory = System.GC.GetTotalMemory(false);
            await operation();
            var endMemory = System.GC.GetTotalMemory(false);
            
            var allocated = endMemory - startMemory;
            Debug.Log($"[MEMORY] {operationName}: {allocated:N0} bytes allocated");
            
            return allocated;
        }
        
        /// <summary>
        /// Runs an operation multiple times and calculates average execution time
        /// </summary>
        /// <param name="operation">Operation to benchmark</param>
        /// <param name="iterations">Number of iterations</param>
        /// <param name="warmupIterations">Number of warmup iterations</param>
        public static async UniTask<float> BenchmarkAsync(Func<UniTask> operation, int iterations = 10, int warmupIterations = 2)
        {
            // Warmup
            for (int i = 0; i < warmupIterations; i++)
            {
                await operation();
            }
            
            // Actual benchmark
            var times = new List<float>();
            for (int i = 0; i < iterations; i++)
            {
                var startTime = Time.realtimeSinceStartup;
                await operation();
                var elapsed = Time.realtimeSinceStartup - startTime;
                times.Add(elapsed);
            }
            
            // Calculate statistics
            float sum = 0;
            float min = float.MaxValue;
            float max = float.MinValue;
            
            foreach (var time in times)
            {
                sum += time;
                min = Mathf.Min(min, time);
                max = Mathf.Max(max, time);
            }
            
            float average = sum / iterations;
            
            Debug.Log($"[BENCHMARK] Iterations: {iterations}, Avg: {average * 1000:F2}ms, Min: {min * 1000:F2}ms, Max: {max * 1000:F2}ms");
            
            return average;
        }
        
        #endregion
        
        #region Cancellation Helpers
        
        /// <summary>
        /// Creates a linked cancellation token that cancels after a timeout
        /// </summary>
        /// <param name="timeoutMs">Timeout in milliseconds</param>
        /// <param name="parentToken">Parent cancellation token</param>
        public static CancellationTokenSource CreateTimeoutToken(int timeoutMs, CancellationToken parentToken = default)
        {
            var timeoutSource = new CancellationTokenSource(timeoutMs);
            
            if (parentToken != default)
            {
                return CancellationTokenSource.CreateLinkedTokenSource(parentToken, timeoutSource.Token);
            }
            
            return timeoutSource;
        }
        
        /// <summary>
        /// Runs an operation with automatic cancellation on test cleanup
        /// </summary>
        /// <param name="operation">Operation to run</param>
        /// <param name="cleanupToken">Token that will be cancelled on cleanup</param>
        public static async UniTask RunWithCleanup(Func<CancellationToken, UniTask> operation, CancellationTokenSource cleanupToken)
        {
            try
            {
                await operation(cleanupToken.Token);
            }
            finally
            {
                cleanupToken?.Cancel();
                cleanupToken?.Dispose();
            }
        }
        
        #endregion
        
        #region Stream Helpers
        
        /// <summary>
        /// Creates a test stream that produces values at intervals
        /// </summary>
        /// <typeparam name="T">Type of values to produce</typeparam>
        /// <param name="valueFactory">Factory function for values</param>
        /// <param name="intervalMs">Interval between values in milliseconds</param>
        /// <param name="count">Number of values to produce</param>
        public static IUniTaskAsyncEnumerable<T> CreateTestStream<T>(Func<int, T> valueFactory, int intervalMs, int count)
        {
            return UniTaskAsyncEnumerable.Create<T>(async (writer, cancellationToken) =>
            {
                for (int i = 0; i < count; i++)
                {
                    await writer.YieldAsync(valueFactory(i));
                    await DelayMilliseconds(intervalMs, cancellationToken: cancellationToken);
                }
            });
        }
        
        /// <summary>
        /// Collects all values from an async enumerable into a list
        /// </summary>
        public static async UniTask<List<T>> CollectStream<T>(IUniTaskAsyncEnumerable<T> stream, CancellationToken cancellationToken = default)
        {
            var results = new List<T>();
            
            await foreach (var item in stream.WithCancellation(cancellationToken))
            {
                results.Add(item);
            }
            
            return results;
        }
        
        #endregion
    }
}