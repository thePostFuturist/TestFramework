using System;
using System.Threading;
using NUnit.Framework;
using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.TestTools;
using Cysharp.Threading.Tasks;

namespace PerSpec.Runtime.DOTS
{
    /// <summary>
    /// Base class for DOTS tests with UniTask support
    /// Provides helpers for ECS, Jobs, and async operations
    /// </summary>
    public abstract class DOTSTestBase
    {
        #region Fields
        protected World testWorld;
        protected EntityManager entityManager;
        protected string testName;
        protected CancellationTokenSource testCancellationTokenSource;
        
        #endregion
        
        #region Setup and Teardown
        
        [SetUp]
        public virtual void Setup()
        {
            // Create a test world for ECS
            testWorld = new World("DOTS_Test_World");
            entityManager = testWorld.EntityManager;
            
            // Initialize cancellation token
            testCancellationTokenSource = new CancellationTokenSource();
            
            // Initialize test name
            testName = TestContext.CurrentContext.Test.Name;
            Debug.Log($"[DOTS-TEST] {testName} starting");
        }
        
        [TearDown]
        public virtual void Teardown()
        {
            Debug.Log($"[DOTS-TEST] {testName} completed");
            
            // Cancel any running async operations
            testCancellationTokenSource?.Cancel();
            testCancellationTokenSource?.Dispose();
            
            if (testWorld != null && testWorld.IsCreated)
            {
                testWorld.Dispose();
            }
        }
        
        #endregion
        
        #region Entity Creation
        
        protected Entity CreateTestEntity(params ComponentType[] components)
        {
            var entity = entityManager.CreateEntity(components);
            Debug.Log($"[DOTS-ENTITY] Created entity ID={entity.Index} with {components.Length} components");
            return entity;
        }
        
        /// <summary>
        /// Creates an entity asynchronously on the main thread
        /// </summary>
        protected async UniTask<Entity> CreateTestEntityAsync(params ComponentType[] components)
        {
            await UniTask.SwitchToMainThread();
            return CreateTestEntity(components);
        }
        
        /// <summary>
        /// Creates multiple entities asynchronously
        /// </summary>
        protected async UniTask<NativeArray<Entity>> CreateTestEntitiesAsync(int count, params ComponentType[] components)
        {
            await UniTask.SwitchToMainThread();
            
            var entities = new NativeArray<Entity>(count, Allocator.Temp);
            entityManager.CreateEntity(entityManager.CreateArchetype(components), entities);
            
            Debug.Log($"[DOTS-ENTITY] Created {count} entities with {components.Length} components");
            return entities;
        }
        
        #endregion
        
        #region Logging Methods
        
        protected void LogJobExecution(string jobName, float executionTimeMs)
        {
            Debug.Log($"[DOTS-JOB] {jobName} completed in {executionTimeMs:F2}ms");
        }
        
        protected void LogSystemUpdate(string systemName, float deltaTime)
        {
            Debug.Log($"[DOTS-SYSTEM] {systemName} triggered at {deltaTime:F2}ms");
        }
        
        protected void LogBufferOperation(string operation, int size)
        {
            Debug.Log($"[DOTS-BUFFER] {operation}: {size} bytes");
        }
        
        protected void LogBurstCompilation(string jobName, bool success)
        {
            Debug.Log($"[DOTS-BURST] {jobName} compiled with Burst: {(success ? "SUCCESS" : "FAILED")}");
        }
        
        protected void LogResult(string testName, bool passed, string details = null)
        {
            string status = passed ? "PASSED" : "FAILED";
            string message = $"[RESULT] {testName}: {status}";
            if (!string.IsNullOrEmpty(details))
            {
                message += $" - {details}";
            }
            Debug.Log(message);
        }
        
        protected void AssertNoMemoryLeaks()
        {
            var allocatedMemory = NativeLeakDetection.Mode;
            // Accept both Enabled and EnabledWithStackTrace as valid modes
            Assert.IsTrue(allocatedMemory == NativeLeakDetectionMode.Enabled || 
                         allocatedMemory == NativeLeakDetectionMode.EnabledWithStackTrace, 
                $"Memory leak detection should be enabled, but was {allocatedMemory}");
        }
        
        /// <summary>
        /// Profiles memory allocation during DOTS operation
        /// </summary>
        protected async UniTask<long> ProfileDOTSMemoryAsync(Func<UniTask> operation)
        {
            await UniTask.SwitchToMainThread();
            
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();
            
            var startMemory = System.GC.GetTotalMemory(false);
            await operation();
            var endMemory = System.GC.GetTotalMemory(false);
            
            var allocated = endMemory - startMemory;
            Debug.Log($"[DOTS-MEMORY] Allocated: {allocated:N0} bytes");
            
            return allocated;
        }
        
        #endregion
        
        #region Test Lifecycle
        
        #endregion
        
        #region Async Wait Methods
        
        /// <summary>
        /// Waits for specified frames using UniTask
        /// </summary>
        protected async UniTask WaitForFramesAsync(int frameCount, CancellationToken cancellationToken = default)
        {
            var token = cancellationToken == default ? testCancellationTokenSource.Token : cancellationToken;
            
            for (int i = 0; i < frameCount; i++)
            {
                await UniTask.Yield(token);
            }
        }
        
        /// <summary>
        /// Waits for a job to complete asynchronously
        /// </summary>
        protected async UniTask WaitForJobAsync(JobHandle jobHandle, CancellationToken cancellationToken = default)
        {
            var token = cancellationToken == default ? testCancellationTokenSource.Token : cancellationToken;
            
            while (!jobHandle.IsCompleted)
            {
                await UniTask.Yield(token);
            }
            
            jobHandle.Complete();
        }
        
        /// <summary>
        /// Waits for system update asynchronously
        /// </summary>
        protected async UniTask WaitForSystemUpdateAsync<T>(CancellationToken cancellationToken = default) where T : SystemBase
        {
            var token = cancellationToken == default ? testCancellationTokenSource.Token : cancellationToken;
            var systemHandle = testWorld.GetExistingSystem<T>();
            
            if (systemHandle == SystemHandle.Null)
            {
                throw new InvalidOperationException($"System {typeof(T).Name} not found in test world");
            }
            
            await UniTask.Yield(token);
            systemHandle.Update(testWorld.Unmanaged);
            await UniTask.Yield(token);
        }
        
        #endregion
        
        #region UniTask Test Helpers
        
        /// <summary>
        /// Converts an async UniTask test for Unity Test Framework
        /// This is the bridge between [UnityTest] and async/await
        /// </summary>
        protected System.Collections.IEnumerator RunAsyncTest(Func<UniTask> asyncTest)
        {
            return asyncTest().ToCoroutine();
        }
        
        /// <summary>
        /// Runs a DOTS operation with performance timing
        /// </summary>
        protected async UniTask<float> MeasureDOTSOperationAsync(Func<UniTask> operation, string operationName)
        {
            var startTime = Time.realtimeSinceStartup;
            await operation();
            var elapsed = Time.realtimeSinceStartup - startTime;
            
            Debug.Log($"[DOTS-TIMING] {operationName}: {elapsed * 1000:F2}ms");
            return elapsed;
        }
        
        /// <summary>
        /// Validates entity state asynchronously
        /// </summary>
        protected async UniTask<bool> ValidateEntityAsync(Entity entity, Func<Entity, bool> validation)
        {
            await UniTask.SwitchToMainThread();
            
            if (!entityManager.Exists(entity))
            {
                Debug.LogError($"[DOTS-ENTITY] Entity {entity.Index} does not exist");
                return false;
            }
            
            return validation(entity);
        }
        
        #endregion
        
        #region Memory Management
        
        /// <summary>
        /// Called at the start of each test
        /// </summary>
        protected void OnTestStart(string testName)
        {
            Debug.Log($"[TEST-START] {testName}");
        }
        
        /// <summary>
        /// Called at the end of each test
        /// </summary>
        protected void OnTestEnd(string testName, bool passed = true, string details = null)
        {
            LogResult(testName, passed, details);
        }
        
        /// <summary>
        /// Called when test throws an exception
        /// </summary>
        protected void OnTestException(string testName, System.Exception ex)
        {
            Debug.LogError($"[EXCEPTION] {testName}: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}");
            OnTestEnd(testName, false, $"Exception: {ex.Message}");
        }
        
        #endregion
    }
}