using System;
using System.Collections;
using NUnit.Framework;
using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.TestTools;
using Cysharp.Threading.Tasks;
using PerSpec.Runtime.DOTS;

namespace Tests.PlayMode
{
    /// <summary>
    /// Simple DOTS PlayMode test to verify async cancellation handling
    /// </summary>
    public class SimpleDOTSTest : DOTSTestBase
    {
        // Enable operation tracking to test the new features
        protected override bool LogOperationLifecycle => true;
        protected override int GracefulShutdownTimeoutMs => 3000;
        
        #region Test Components
        
        public struct TestComponent : IComponentData
        {
            public float value;
            public int counter;
        }
        
        public struct ProcessedTag : IComponentData { }
        
        [BurstCompile]
        public struct SimpleIncrementJob : IJobParallelFor
        {
            public NativeArray<float> values;
            
            public void Execute(int index)
            {
                values[index] = values[index] + 1.0f;
            }
        }
        
        #endregion
        
        #region Simple Tests
        
        [UnityTest]
        public IEnumerator Should_CreateEntity_WithComponents() => RunAsyncTest(async () =>
        {
            Debug.Log("[DOTS-TEST] Starting entity creation test");
            
            // Create an entity with components
            var entity = await CreateTestEntityAsync(
                ComponentType.ReadWrite<TestComponent>(),
                ComponentType.ReadWrite<ProcessedTag>()
            );
            
            // Verify entity exists
            Assert.IsTrue(entityManager.Exists(entity), "Entity should exist");
            Assert.IsTrue(entityManager.HasComponent<TestComponent>(entity), "Should have TestComponent");
            Assert.IsTrue(entityManager.HasComponent<ProcessedTag>(entity), "Should have ProcessedTag");
            
            // Set component data
            entityManager.SetComponentData(entity, new TestComponent 
            { 
                value = 42.0f, 
                counter = 10 
            });
            
            // Wait a frame to ensure data is set
            await WaitForFramesAsync(1);
            
            // Verify data
            var data = entityManager.GetComponentData<TestComponent>(entity);
            Assert.AreEqual(42.0f, data.value, 0.01f, "Value should be 42");
            Assert.AreEqual(10, data.counter, "Counter should be 10");
            
            Debug.Log("[DOTS-TEST] Entity creation test completed successfully");
        });
        
        [UnityTest]
        public IEnumerator Should_ProcessJob_WithTracking() => RunAsyncTest(async () =>
        {
            Debug.Log("[DOTS-TEST] Starting job processing test");
            
            // Create native array for job
            var dataArray = new NativeArray<float>(100, Allocator.TempJob);
            
            try
            {
                // Initialize data
                for (int i = 0; i < dataArray.Length; i++)
                {
                    dataArray[i] = i * 0.5f;
                }
                
                // Create and schedule job
                var job = new SimpleIncrementJob 
                { 
                    values = dataArray 
                };
                var jobHandle = job.Schedule(dataArray.Length, 32);
                
                Debug.Log("[DOTS-TEST] Job scheduled, waiting for completion");
                
                // Wait for job with tracking
                await WaitForJobAsync(jobHandle);
                
                // Verify results
                Assert.AreEqual(1.0f, dataArray[0], 0.01f, "First element should be incremented");
                Assert.AreEqual(50.5f, dataArray[100-1], 0.01f, "Last element should be incremented");
                
                Debug.Log("[DOTS-TEST] Job completed and verified");
            }
            finally
            {
                dataArray.Dispose();
            }
        });
        
        [UnityTest]
        public IEnumerator Should_CreateMultipleEntities_Async() => RunAsyncTest(async () =>
        {
            Debug.Log("[DOTS-TEST] Starting batch entity creation test");
            
            // Create multiple entities
            var entities = await CreateTestEntitiesAsync(50, 
                ComponentType.ReadWrite<TestComponent>()
            );
            
            try
            {
                Assert.AreEqual(50, entities.Length, "Should create 50 entities");
                
                // Set data on all entities  
                for (int i = 0; i < entities.Length; i++)
                {
                    entityManager.SetComponentData(entities[i], new TestComponent
                    {
                        value = i * 2.0f,
                        counter = i
                    });
                }
                
                // Wait for data to be set
                await WaitForFramesAsync(2);
                
                // Verify a few samples
                var firstData = entityManager.GetComponentData<TestComponent>(entities[0]);
                Assert.AreEqual(0.0f, firstData.value, 0.01f, "First entity value");
                
                var middleData = entityManager.GetComponentData<TestComponent>(entities[25]);
                Assert.AreEqual(50.0f, middleData.value, 0.01f, "Middle entity value");
                
                var lastData = entityManager.GetComponentData<TestComponent>(entities[49]);
                Assert.AreEqual(98.0f, lastData.value, 0.01f, "Last entity value");
                
                Debug.Log($"[DOTS-TEST] Successfully created and verified {entities.Length} entities");
            }
            finally
            {
                // Clean up entities
                entityManager.DestroyEntity(entities);
                entities.Dispose();
            }
        });
        
        [UnityTest]
        public IEnumerator Should_MeasurePerformance_WithTracking() => RunAsyncTest(async () =>
        {
            Debug.Log("[DOTS-TEST] Starting performance measurement test");
            
            // Measure entity creation performance
            var elapsed = await MeasureDOTSOperationAsync(async () =>
            {
                // Create entities
                var entities = new NativeArray<Entity>(100, Allocator.Temp);
                entityManager.CreateEntity(
                    entityManager.CreateArchetype(ComponentType.ReadWrite<TestComponent>()),
                    entities
                );
                
                // Wait a frame
                await UniTask.Yield();
                
                // Destroy entities
                entityManager.DestroyEntity(entities);
                entities.Dispose();
            }, "Create100Entities");
            
            Assert.Greater(elapsed, 0f, "Operation should take measurable time");
            Debug.Log($"[DOTS-TEST] Performance test completed: {elapsed * 1000:F2}ms");
        });
        
        [UnityTest]
        public IEnumerator Should_HandleCancellation_Gracefully() => RunAsyncTest(async () =>
        {
            Debug.Log("[DOTS-TEST] Starting cancellation test");
            
            var cts = new System.Threading.CancellationTokenSource();
            var dataArray = new NativeArray<float>(1000, Allocator.TempJob);
            
            try
            {
                var job = new SimpleIncrementJob { values = dataArray };
                var jobHandle = job.Schedule(dataArray.Length, 1);
                
                // Cancel after short delay (properly handle the task)
                var cancelTask = UniTask.Create(async () =>
                {
                    await UniTask.Delay(50);
                    if (!cts.Token.IsCancellationRequested)
                    {
                        cts.Cancel();
                        Debug.Log("[DOTS-TEST] Cancellation requested");
                    }
                });
                
                try
                {
                    await WaitForJobAsync(jobHandle, cts.Token);
                    Debug.Log("[DOTS-TEST] Job completed before cancellation");
                }
                catch (OperationCanceledException)
                {
                    Debug.Log("[DOTS-TEST] Job wait cancelled as expected");
                    jobHandle.Complete(); // Ensure job completes
                }
                
                // Wait for cancel task to complete
                await cancelTask;
            }
            finally
            {
                dataArray.Dispose();
                cts.Dispose();
            }
            
            Debug.Log("[DOTS-TEST] Cancellation test completed");
        });
        
        [UnityTest]
        public IEnumerator TestSimpleDOTSSystem() => RunAsyncTest(async () =>
        {
            Debug.Log("[DOTS-TEST] Starting DOTS system test with DefaultGameObjectInjectionWorld");
            
            // Ensure the default world is set for this test
            EnsureDefaultWorldIsSet();
            
            // Now code that expects DefaultGameObjectInjectionWorld will work
            Assert.IsNotNull(World.DefaultGameObjectInjectionWorld, "DefaultGameObjectInjectionWorld should be set");
            Assert.AreEqual(testWorld, World.DefaultGameObjectInjectionWorld, "Test world should be the default");
            
            // Create an entity using the default world's entity manager
            var defaultEntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var entity = defaultEntityManager.CreateEntity(ComponentType.ReadWrite<TestComponent>());
            
            Assert.IsTrue(defaultEntityManager.Exists(entity), "Entity should exist in default world");
            
            // Set and verify component data
            defaultEntityManager.SetComponentData(entity, new TestComponent { value = 100f, counter = 50 });
            await UniTask.Yield();
            
            var data = defaultEntityManager.GetComponentData<TestComponent>(entity);
            Assert.AreEqual(100f, data.value, 0.01f, "Value should be 100");
            Assert.AreEqual(50, data.counter, "Counter should be 50");
            
            Debug.Log("[DOTS-TEST] Successfully tested with DefaultGameObjectInjectionWorld");
        });
        
        #endregion
    }
}