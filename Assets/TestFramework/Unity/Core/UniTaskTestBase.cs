using System;
using System.Threading;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Cysharp.Threading.Tasks;

namespace TestFramework.Unity.Core
{
    /// <summary>
    /// Base class for tests using UniTask for async operations
    /// Provides zero-allocation async/await testing support
    /// </summary>
    public abstract class UniTaskTestBase
    {
        #region Fields
        
        protected CancellationTokenSource testCancellationTokenSource;
        protected string currentTestName;
        
        #endregion
        
        #region Setup and Teardown
        
        [SetUp]
        public virtual void Setup()
        {
            testCancellationTokenSource = new CancellationTokenSource();
            currentTestName = TestContext.CurrentContext.Test.Name;
            Debug.Log($"[UniTaskTest] Starting: {currentTestName}");
        }
        
        [TearDown]
        public virtual void TearDown()
        {
            testCancellationTokenSource?.Cancel();
            testCancellationTokenSource?.Dispose();
            Debug.Log($"[UniTaskTest] Completed: {currentTestName}");
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Converts an async UniTask method to a coroutine for Unity Test Framework
        /// </summary>
        /// <param name="asyncTest">The async test method to run</param>
        /// <returns>IEnumerator for Unity Test Framework compatibility</returns>
        protected IEnumerator RunAsyncTest(Func<UniTask> asyncTest)
        {
            return UniTask.ToCoroutine(asyncTest);
        }
        
        /// <summary>
        /// Waits for a specified number of frames asynchronously
        /// </summary>
        /// <param name="frameCount">Number of frames to wait</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        protected async UniTask WaitForFramesAsync(int frameCount, CancellationToken cancellationToken = default)
        {
            for (int i = 0; i < frameCount; i++)
            {
                await UniTask.Yield(cancellationToken);
            }
        }
        
        /// <summary>
        /// Waits until a condition is met or timeout occurs
        /// </summary>
        /// <param name="predicate">Condition to check</param>
        /// <param name="timeoutMs">Timeout in milliseconds</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        protected async UniTask WaitUntilAsync(Func<bool> predicate, int timeoutMs = 5000, CancellationToken cancellationToken = default)
        {
            var timeoutToken = new CancellationTokenSource(timeoutMs).Token;
            var combinedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutToken).Token;
            
            while (!predicate())
            {
                await UniTask.Yield(combinedToken);
            }
        }
        
        /// <summary>
        /// Runs an async operation with timeout
        /// </summary>
        /// <typeparam name="T">Return type of the operation</typeparam>
        /// <param name="operation">The async operation to run</param>
        /// <param name="timeoutMs">Timeout in milliseconds</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        protected async UniTask<T> RunWithTimeoutAsync<T>(Func<CancellationToken, UniTask<T>> operation, int timeoutMs = 5000, CancellationToken cancellationToken = default)
        {
            var timeoutToken = new CancellationTokenSource(timeoutMs).Token;
            var combinedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutToken).Token;
            
            try
            {
                return await operation(combinedToken);
            }
            catch (OperationCanceledException) when (timeoutToken.IsCancellationRequested)
            {
                throw new TimeoutException($"Operation timed out after {timeoutMs}ms");
            }
        }
        
        /// <summary>
        /// Measures the execution time of an async operation
        /// </summary>
        /// <param name="operation">The async operation to measure</param>
        /// <param name="operationName">Name for logging purposes</param>
        protected async UniTask<float> MeasureExecutionTimeAsync(Func<UniTask> operation, string operationName = "Operation")
        {
            var startTime = Time.realtimeSinceStartup;
            await operation();
            var elapsed = Time.realtimeSinceStartup - startTime;
            
            Debug.Log($"[TIMING] {operationName}: {elapsed * 1000:F2}ms");
            return elapsed;
        }
        
        /// <summary>
        /// Switches to main thread for Unity API access
        /// </summary>
        protected async UniTask SwitchToMainThreadAsync()
        {
            await UniTask.SwitchToMainThread();
        }
        
        /// <summary>
        /// Switches to thread pool for CPU-intensive work
        /// </summary>
        protected async UniTask SwitchToThreadPoolAsync()
        {
            await UniTask.SwitchToThreadPool();
        }
        
        #endregion
        
        #region Assertion Helpers
        
        /// <summary>
        /// Asserts that an async operation completes within a timeout
        /// </summary>
        protected async UniTask AssertCompletesWithinAsync(Func<UniTask> operation, int timeoutMs)
        {
            try
            {
                await RunWithTimeoutAsync(async token =>
                {
                    await operation();
                    return true;
                }, timeoutMs, testCancellationTokenSource.Token);
            }
            catch (TimeoutException)
            {
                Assert.Fail($"Operation did not complete within {timeoutMs}ms");
            }
        }
        
        /// <summary>
        /// Asserts that an async operation throws a specific exception
        /// </summary>
        protected async UniTask AssertThrowsAsync<TException>(Func<UniTask> operation) where TException : Exception
        {
            try
            {
                await operation();
                Assert.Fail($"Expected exception of type {typeof(TException).Name} was not thrown");
            }
            catch (TException)
            {
                // Expected exception was thrown
            }
            catch (Exception ex)
            {
                Assert.Fail($"Expected exception of type {typeof(TException).Name} but got {ex.GetType().Name}");
            }
        }
        
        #endregion
        
        #region Logging Helpers
        
        /// <summary>
        /// Logs test progress with timestamp
        /// </summary>
        protected void LogProgress(string message)
        {
            Debug.Log($"[UniTaskTest-{currentTestName}] {DateTime.Now:HH:mm:ss.fff} - {message}");
        }
        
        /// <summary>
        /// Logs test result
        /// </summary>
        protected void LogResult(bool passed, string details = null)
        {
            var status = passed ? "PASSED" : "FAILED";
            var message = $"[RESULT] {currentTestName}: {status}";
            if (!string.IsNullOrEmpty(details))
            {
                message += $" - {details}";
            }
            Debug.Log(message);
        }
        
        #endregion
    }
}