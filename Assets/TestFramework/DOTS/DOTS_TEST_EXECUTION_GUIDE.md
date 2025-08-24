# DOTS Test Execution Guide with UniTask

## Overview

This guide provides comprehensive documentation for testing Unity DOTS (Data-Oriented Technology Stack) applications using modern async/await patterns with UniTask. The DOTS test infrastructure extends the Unity Test Framework with specialized support for Entity Component System (ECS) testing, Burst compilation validation, and Job System testing - all with zero-allocation async operations.

## DOTS Test Infrastructure Architecture

### Directory Structure
```
Assets/TestFramework/DOTS/
├── Core/                           # Core test base classes
│   └── DOTSTestBase.cs            # Base class with UniTask support
├── Helpers/                        # Test utilities
│   ├── DOTSTestConfiguration.cs   # Test configuration
│   └── DOTSTestFactory.cs         # Factory for test objects
└── Prefabs/                        # Test prefabs (optional)
```

### Assembly Definitions

The DOTS test infrastructure uses the following assembly structure:

1. **TestFramework.DOTS** - DOTS test infrastructure with UniTask
2. **TestFramework.Unity** - Base Unity test framework with UniTask
3. **UniTask** - Zero-allocation async/await library

## Using DOTSTestBase with UniTask

All DOTS tests should inherit from `DOTSTestBase` and use UniTask patterns:

```csharp
using TestFramework.DOTS.Core;
using TestFramework.DOTS.Helpers;
using Unity.Entities;
using NUnit.Framework;
using Cysharp.Threading.Tasks;
using System.Collections;

[TestFixture]
public class MyDOTSTest : DOTSTestBase
{
    #region Setup and Teardown
    
    [SetUp]
    public override void Setup()
    {
        base.Setup(); // Creates test world, entity manager, and cancellation token
    }
    
    [TearDown]
    public override void Teardown()
    {
        base.Teardown(); // Disposes world and cancellation token
    }
    
    #endregion
    
    #region Tests
    
    [UnityTest]
    public IEnumerator TestEntityCreation() => RunAsyncTest(async () =>
    {
        // Create test entity asynchronously
        var entity = await CreateTestEntityAsync(
            typeof(Translation),
            typeof(Rotation)
        );
        
        // Validate entity
        var isValid = await ValidateEntityAsync(entity, e => 
            entityManager.HasComponent<Translation>(e) &&
            entityManager.HasComponent<Rotation>(e)
        );
        
        Assert.IsTrue(isValid, "Entity should have required components");
    });
    
    #endregion
}
```

## UniTask Async Patterns for DOTS

### Async Entity Operations

```csharp
[UnityTest]
public IEnumerator TestAsyncEntityOperations() => RunAsyncTest(async () =>
{
    // Create single entity
    var entity = await CreateTestEntityAsync(typeof(Translation));
    
    // Create multiple entities
    var entities = await CreateTestEntitiesAsync(100, 
        typeof(Translation), 
        typeof(Rotation)
    );
    
    try
    {
        // Process entities asynchronously
        await ProcessEntitiesAsync(entities);
        
        // Wait for frames
        await WaitForFramesAsync(5, testCancellationTokenSource.Token);
        
        // Validate all entities
        foreach (var e in entities)
        {
            var valid = await ValidateEntityAsync(e, ValidateEntity);
            Assert.IsTrue(valid);
        }
    }
    finally
    {
        // Cleanup
        entities.Dispose();
    }
});

private async UniTask ProcessEntitiesAsync(NativeArray<Entity> entities)
{
    await UniTask.SwitchToMainThread();
    
    foreach (var entity in entities)
    {
        entityManager.SetComponentData(entity, new Translation 
        { 
            Value = UnityEngine.Random.insideUnitSphere 
        });
    }
}
```

### Async Job Testing

```csharp
[BurstCompile]
struct TestJob : IJob
{
    public NativeArray<float> data;
    
    public void Execute()
    {
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = data[i] * 2.0f;
        }
    }
}

[UnityTest]
public IEnumerator TestJobWithUniTask() => RunAsyncTest(async () =>
{
    var data = new NativeArray<float>(1024, Allocator.TempJob);
    
    try
    {
        // Initialize data
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = i;
        }
        
        // Schedule and wait for job
        var job = new TestJob { data = data };
        var handle = job.Schedule();
        
        // Wait for job completion asynchronously
        await WaitForJobAsync(handle, testCancellationTokenSource.Token);
        
        // Validate results
        for (int i = 0; i < data.Length; i++)
        {
            Assert.AreEqual(i * 2.0f, data[i]);
        }
    }
    finally
    {
        if (data.IsCreated)
            data.Dispose();
    }
});
```

### Async System Testing

```csharp
[UnityTest]
public IEnumerator TestSystemUpdateAsync() => RunAsyncTest(async () =>
{
    // Create test entities
    var entities = await CreateTestEntitiesAsync(100, typeof(Translation));
    
    try
    {
        // Measure system update performance
        var elapsed = await MeasureDOTSOperationAsync(async () =>
        {
            await WaitForSystemUpdateAsync<MyTestSystem>(testCancellationTokenSource.Token);
        }, "MyTestSystem Update");
        
        // Validate performance
        Assert.Less(elapsed, 0.01667f, "System should update within frame time");
        
        // Wait for multiple frames
        await WaitForFramesAsync(10);
        
        // Validate system effects
        foreach (var entity in entities)
        {
            var translation = entityManager.GetComponentData<Translation>(entity);
            Assert.AreNotEqual(float3.zero, translation.Value);
        }
    }
    finally
    {
        entities.Dispose();
    }
});
```

## Test Configuration

Use `DOTSTestConfiguration` for consistent test setups:

```csharp
// Basic test configuration
var config = DOTSTestConfiguration.CreateDefault();
config.TestEntityCount = 1000;
config.EnableDiagnostics = true;

// Performance test configuration
var perfConfig = DOTSTestConfiguration.CreatePerformanceTest();
perfConfig.TestEntityCount = 10000;
perfConfig.UseJobSystem = true;
perfConfig.EnableBurstCompilation = true;

// Stress test configuration
var stressConfig = DOTSTestConfiguration.CreateStressTest();
stressConfig.TestUpdateInterval = 0.008f; // 120 FPS target
```

## Test Factory with Async Support

```csharp
[UnityTest]
public IEnumerator TestWithFactory() => RunAsyncTest(async () =>
{
    using (var factory = new DOTSTestFactory())
    {
        // Create test setups asynchronously
        await UniTask.SwitchToMainThread();
        
        var config = DOTSTestConfiguration.CreateDefault();
        var testSetup = DOTSTestFactory.CreateBasicTestSetup(config);
        
        // Create test entities
        var entity = DOTSTestFactory.CreateTestEntity(entityManager, config);
        
        // Wait for initialization
        await UniTask.Delay(100);
        
        // Validate setup
        Assert.IsNotNull(testSetup);
        Assert.IsTrue(entityManager.Exists(entity));
        
        // Factory automatically cleans up on disposal
    }
});
```

## Performance Testing with UniTask

```csharp
[TestFixture]
public class PerformanceTests : DOTSTestBase
{
    [UnityTest]
    public IEnumerator TestEntityCreationPerformance() => RunAsyncTest(async () =>
    {
        var config = DOTSTestConfiguration.CreatePerformanceTest();
        
        // Measure entity creation performance
        var elapsed = await MeasureDOTSOperationAsync(async () =>
        {
            for (int i = 0; i < config.TestEntityCount; i++)
            {
                await CreateTestEntityAsync(typeof(Translation), typeof(Rotation));
                
                // Yield periodically to avoid blocking
                if (i % 100 == 0)
                    await UniTask.Yield();
            }
        }, "Entity Creation");
        
        var entitiesPerMs = config.TestEntityCount / (elapsed * 1000);
        Debug.Log($"[PERFORMANCE] Created {entitiesPerMs:F2} entities/ms");
        
        Assert.Greater(entitiesPerMs, 100, "Should create >100 entities per ms");
    });
    
    [UnityTest]
    public IEnumerator TestJobPerformance() => RunAsyncTest(async () =>
    {
        var data = new NativeArray<float>(1_000_000, Allocator.TempJob);
        
        try
        {
            // Benchmark job execution
            var avgTime = await UniTaskTestHelpers.BenchmarkAsync(async () =>
            {
                var job = new TestJob { data = data };
                var handle = job.Schedule();
                await WaitForJobAsync(handle);
            }, iterations: 10, warmupIterations: 2);
            
            Debug.Log($"[PERFORMANCE] Job execution: {avgTime * 1000:F2}ms average");
            Assert.Less(avgTime, 0.01f, "Job should complete within 10ms");
        }
        finally
        {
            data.Dispose();
        }
    });
}
```

## Memory Management with UniTask

```csharp
[UnityTest]
public IEnumerator TestMemoryManagement() => RunAsyncTest(async () =>
{
    // Ensure no memory leaks
    AssertNoMemoryLeaks();
    
    // Profile memory allocation
    var allocated = await ProfileDOTSMemoryAsync(async () =>
    {
        var entities = await CreateTestEntitiesAsync(1000, typeof(Translation));
        
        // Process entities
        await UniTask.Delay(100);
        
        // Clean up
        entityManager.DestroyEntity(entities);
        entities.Dispose();
    });
    
    Debug.Log($"[MEMORY] Total allocated: {allocated:N0} bytes");
    Assert.Less(allocated, 1_000_000, "Should allocate less than 1MB");
});
```

## Dynamic Buffer Testing

```csharp
[UnityTest]
public IEnumerator TestDynamicBuffer() => RunAsyncTest(async () =>
{
    var entity = await CreateTestEntityAsync();
    var buffer = entityManager.AddBuffer<MyBufferElement>(entity);
    
    // Add elements asynchronously
    await UniTask.RunOnThreadPool(() =>
    {
        // Prepare data on thread pool
        for (int i = 0; i < 100; i++)
        {
            // Heavy computation here
        }
    });
    
    await UniTask.SwitchToMainThread();
    
    // Add to buffer on main thread
    for (int i = 0; i < 100; i++)
    {
        buffer.Add(new MyBufferElement { Value = i });
    }
    
    // Validate buffer
    Assert.AreEqual(100, buffer.Length);
    
    // Measure buffer operations
    var elapsed = await MeasureDOTSOperationAsync(async () =>
    {
        for (int i = 0; i < buffer.Length; i++)
        {
            var value = buffer[i].Value;
        }
        await UniTask.Yield();
    }, "Buffer Read");
    
    Assert.Less(elapsed, 0.001f, "Buffer read should be fast");
});
```

## Best Practices

### 1. Always Use UniTask for Async Operations
```csharp
// Good - Uses UniTask
[UnityTest]
public IEnumerator MyTest() => RunAsyncTest(async () =>
{
    await UniTask.Delay(100);
    // Test logic
});

// Bad - Uses coroutines
[UnityTest]
public IEnumerator MyTest()
{
    yield return new WaitForSeconds(0.1f);
    // Test logic
}
```

### 2. Proper Resource Cleanup
```csharp
[UnityTest]
public IEnumerator TestWithCleanup() => RunAsyncTest(async () =>
{
    NativeArray<float> data = default;
    try
    {
        data = new NativeArray<float>(1024, Allocator.TempJob);
        // Use data
        await ProcessDataAsync(data);
    }
    finally
    {
        if (data.IsCreated)
            data.Dispose();
    }
});
```

### 3. Use Cancellation Tokens
```csharp
[UnityTest]
public IEnumerator TestWithCancellation() => RunAsyncTest(async () =>
{
    var cts = CancellationTokenSource.CreateLinkedTokenSource(
        testCancellationTokenSource.Token
    );
    cts.CancelAfter(5000); // 5 second timeout
    
    try
    {
        await LongRunningOperationAsync(cts.Token);
    }
    catch (OperationCanceledException)
    {
        // Handle cancellation gracefully
        Debug.Log("Operation cancelled");
    }
});
```

### 4. Parallel Operations
```csharp
[UnityTest]
public IEnumerator TestParallelOperations() => RunAsyncTest(async () =>
{
    // Run operations in parallel
    var tasks = new[]
    {
        CreateTestEntityAsync(typeof(Translation)),
        CreateTestEntityAsync(typeof(Rotation)),
        CreateTestEntityAsync(typeof(Scale))
    };
    
    var entities = await UniTask.WhenAll(tasks);
    
    // All entities created in parallel
    foreach (var entity in entities)
    {
        Assert.IsTrue(entityManager.Exists(entity));
    }
});
```

## Troubleshooting

### Tests Not Running
1. Ensure UniTask is installed via Package Manager
2. Check assembly references include UniTask
3. Verify `RunAsyncTest` or `UniTask.ToCoroutine` is used

### Memory Leaks
1. Always dispose NativeArrays/NativeLists
2. Use `finally` blocks for cleanup
3. Enable leak detection: `NativeLeakDetection.Mode = NativeLeakDetectionMode.EnabledWithStackTrace`

### Async Deadlocks
1. Avoid blocking on main thread
2. Use `UniTask.SwitchToMainThread()` when needed
3. Always use cancellation tokens with timeouts

### Performance Issues
1. Use `UniTask.Yield()` in long loops
2. Profile with `MeasureDOTSOperationAsync`
3. Run CPU-intensive work on thread pool

## Summary

The DOTS Test Framework with UniTask provides:
1. **Zero-allocation async/await** testing
2. **Full cancellation support** with tokens
3. **Thread-safe operations** with thread switching
4. **Performance profiling** built-in
5. **Memory leak detection** for native collections
6. **Parallel test execution** support

For general Unity testing with UniTask, refer to the [Unified Test Execution Guide](../Unity/UNIFIED_TEST_EXECUTION_GUIDE.md).