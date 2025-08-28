# Unified Test Execution Guide for Unity Tests with UniTask

## Overview

This guide provides comprehensive documentation for using the Unity Test Framework with UniTask for zero-allocation async/await testing. The framework provides modern async patterns, helper utilities, and best practices for Unity testing.

## Testing Approach: Prefab Pattern (Default)

> **IMPORTANT**: Use the Prefab Pattern for ALL tests except atomic utility functions. This is the standard approach for Unity testing.

### When to Use Prefab Pattern (99% of cases)
- **Testing any MonoBehaviour** - Components need proper GameObject setup
- **Testing component interactions** - Multiple components working together
- **Testing systems with dependencies** - Audio, physics, rendering systems
- **Testing UI elements** - Canvas, buttons, panels need hierarchy
- **Testing gameplay mechanics** - Player controllers, enemies, pickups
- **Testing with ScriptableObjects** - Configuration and asset references
- **Integration testing** - Full feature testing with multiple systems

### When to Use Simple Tests (Rare - 1% of cases)
- Pure utility functions (math, string manipulation)
- Data structures without Unity dependencies
- Simple validators or parsers
- Static helper methods
- Non-MonoBehaviour POCOs

**Rule of thumb**: If your code touches Unity APIs or inherits from MonoBehaviour, use the Prefab Pattern.

## Quick Start

### Installation

1. Install UniTask via Package Manager:
   ```
   https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask
   ```

2. Reference the test framework assemblies in your test assembly:
   ```json
   {
       "references": [
           "TestFramework.Unity",
           "UniTask",
           "UnityEngine.TestRunner"
       ]
   }
   ```

### Basic Test Example with UniTask

```csharp
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using Cysharp.Threading.Tasks;
using TestFramework.Unity.Core;
using TestFramework.Unity.Helpers;

[TestFixture]
public class MyAsyncTest : UniTaskTestBase
{
    #region Setup and Teardown
    
    [SetUp]
    public override void Setup()
    {
        base.Setup(); // Initializes cancellation token
    }
    
    [TearDown]
    public override void TearDown()
    {
        base.TearDown(); // Cleans up cancellation token
    }
    
    #endregion
    
    #region Tests
    
    [UnityTest]
    public IEnumerator TestExample() => UniTask.ToCoroutine(async () =>
    {
        // Arrange
        var testObject = new GameObject("TestObject");
        
        try
        {
            // Act - Zero allocation async operations
            await UniTask.Delay(1000);
            
            // You can also use helper methods
            await WaitForFramesAsync(60);
            
            // Assert
            Assert.IsNotNull(testObject);
            LogResult(true, "Test passed");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[TEST-ERROR] {ex.Message}");
            throw;
        }
        finally
        {
            // Cleanup
            if (testObject != null)
                Object.DestroyImmediate(testObject);
        }
    });
    
    #endregion
}
```

## Simple Tests (Rare Cases)

For the 1% of cases where you're testing pure logic without Unity dependencies:

```csharp
[TestFixture]
public class MathUtilsTests // No prefab needed - pure logic
{
    [Test]
    public void Should_Calculate_Distance()
    {
        // Simple atomic test - no Unity APIs
        var distance = MathUtils.Distance(Vector3.zero, Vector3.one);
        Assert.AreEqual(Mathf.Sqrt(3), distance, 0.001f);
    }
}
```

**Remember**: If you find yourself writing many simple tests, consider if you're properly testing your Unity integrations!

## Architecture Overview

### Test Infrastructure Components

1. **UniTaskTestBase** (`TestFramework.Unity.Core.UniTaskTestBase`)
   - Base class for async tests
   - Provides cancellation token management
   - Helper methods for async operations

2. **UniTaskTestHelpers** (`TestFramework.Unity.Helpers.UniTaskTestHelpers`)
   - Static helper utilities
   - Retry logic, benchmarking, parallel operations
   - Memory profiling and performance helpers

3. **UniTaskRunner** (`TestFramework.Unity.Helpers.UniTaskRunner`)
   - Manages UniTask operations
   - Application-wide cancellation support
   - Retry logic and parallel execution
   - Singleton pattern for reliable execution

## UniTask Testing Patterns

### Basic Async Test

```csharp
[UnityTest]
public IEnumerator BasicAsyncTest() => UniTask.ToCoroutine(async () =>
{
    // Full async/await support with proper error handling
    await UniTask.Delay(100);
    
    var result = await ProcessDataAsync();
    Assert.IsNotNull(result);
});
```

### Testing with Timeout

```csharp
[UnityTest]
public IEnumerator TestWithTimeout() => UniTask.ToCoroutine(async () =>
{
    await AssertCompletesWithinAsync(async () =>
    {
        await SomeLongRunningOperation();
    }, timeoutMs: 5000);
});
```

### Testing with Cancellation

```csharp
[UnityTest]
public IEnumerator TestWithCancellation() => UniTask.ToCoroutine(async () =>
{
    var cts = CancellationTokenSource.CreateLinkedTokenSource(
        testCancellationTokenSource.Token
    );
    cts.CancelAfter(3000);
    
    try
    {
        await LongOperation(cts.Token);
    }
    catch (OperationCanceledException)
    {
        // Expected cancellation
        LogProgress("Operation cancelled as expected");
    }
});
```

### Parallel Operations

```csharp
[UnityTest]
public IEnumerator TestParallelOperations() => UniTask.ToCoroutine(async () =>
{
    // Run multiple operations in parallel
    var results = await UniTaskTestHelpers.RunInParallelWithResults(
        () => FetchDataAsync("url1"),
        () => FetchDataAsync("url2"),
        () => FetchDataAsync("url3")
    );
    
    Assert.AreEqual(3, results.Length);
    foreach (var result in results)
    {
        Assert.IsNotNull(result);
    }
});
```

## Helper Utilities

### Using UniTaskTestHelpers

```csharp
[UnityTest]
public IEnumerator TestWithHelpers() => UniTask.ToCoroutine(async () =>
{
    // Retry with exponential backoff
    var result = await UniTaskTestHelpers.RetryWithBackoff(
        () => UnreliableOperationAsync(),
        maxRetries: 3,
        initialDelayMs: 100
    );
    
    // Benchmark performance
    var avgTime = await UniTaskTestHelpers.BenchmarkAsync(
        () => PerformanceOperation(),
        iterations: 10,
        warmupIterations: 2
    );
    
    Assert.Less(avgTime, 0.016f, "Should complete within frame time");
});
```

### Working with GameObjects

```csharp
[UnityTest]
public IEnumerator TestWithGameObject() => UniTask.ToCoroutine(async () =>
{
    await UniTaskTestHelpers.WithTestGameObject("TestObject", async (go) =>
    {
        // GameObject is automatically created and destroyed
        var component = go.AddComponent<MyComponent>();
        
        await UniTask.Delay(100);
        
        Assert.IsTrue(component.IsInitialized);
    });
    // GameObject is automatically destroyed here
});
```

### Memory Profiling

```csharp
[UnityTest]
public IEnumerator TestMemoryAllocation() => UniTask.ToCoroutine(async () =>
{
    var allocated = await UniTaskTestHelpers.ProfileMemoryAllocation(async () =>
    {
        // Operation to profile
        await CreateManyObjects();
    }, "Object Creation");
    
    Assert.Less(allocated, 1_000_000, "Should allocate less than 1MB");
});
```

## Writing Tests

### Performance Test Template

```csharp
[UnityTest]
public IEnumerator TestPerformance() => UniTask.ToCoroutine(async () =>
{
    var stopwatch = new System.Diagnostics.Stopwatch();
    
    // Setup test
    var testObject = CreateTestObject();
    
    try
    {
        // Measure performance
        stopwatch.Start();
        await PerformanceOperation();
        stopwatch.Stop();
        
        // Log and validate
        var elapsedMs = stopwatch.ElapsedMilliseconds;
        Debug.Log($"[TIMING] Operation: {elapsedMs}ms");
        Assert.Less(elapsedMs, 100, "Should complete within 100ms");
    }
    finally
    {
        Object.Destroy(testObject);
    }
});
```

### Integration Test Template

```csharp
[UnityTest]
public IEnumerator TestIntegration() => UniTask.ToCoroutine(async () =>
{
    GameObject testObject = null;
    
    try
    {
        // Setup components
        testObject = new GameObject("IntegrationTest");
        var component1 = testObject.AddComponent<Component1>();
        var component2 = testObject.AddComponent<Component2>();
        
        // Wait for initialization
        await UniTask.Delay(500);
        
        // Test interaction
        component1.TriggerAction();
        await UniTask.WaitUntil(() => component2.HasReacted);
        
        // Validate results
        Assert.IsTrue(component2.HasReacted);
        LogResult(true, "Integration successful");
    }
    finally
    {
        if (testObject != null)
            Object.DestroyImmediate(testObject);
    }
});
```

### Stream Processing Test

```csharp
[UnityTest]
public IEnumerator TestStreamProcessing() => UniTask.ToCoroutine(async () =>
{
    // Create test stream
    var stream = UniTaskTestHelpers.CreateTestStream(
        i => i * 2,
        intervalMs: 100,
        count: 5
    );
    
    // Collect stream values
    var results = await UniTaskTestHelpers.CollectStream(stream, 
        testCancellationTokenSource.Token);
    
    // Validate
    Assert.AreEqual(5, results.Count);
    Assert.AreEqual(0, results[0]);
    Assert.AreEqual(8, results[4]);
});
```

## Best Practices

### 1. Always Use UniTask for Async Operations
```csharp
// Good - Zero allocation with UniTask
[UnityTest]
public IEnumerator GoodTest() => UniTask.ToCoroutine(async () =>
{
    await UniTask.Delay(100);
    // Full async/await support
});

// Bad - Old pattern, avoid this
[Test]
public void BadSyncTest()
{
    Thread.Sleep(100); // Blocks thread
}
```

### 2. Use Cancellation Tokens
```csharp
[UnityTest]
public IEnumerator TestWithProperCancellation() => UniTask.ToCoroutine(async () =>
{
    // Use the base class cancellation token
    await LongOperation(testCancellationTokenSource.Token);
    
    // Or create linked tokens for timeouts
    var timeoutToken = UniTaskTestHelpers.CreateTimeoutToken(5000, 
        testCancellationTokenSource.Token);
    
    await OperationWithTimeout(timeoutToken.Token);
});
```

### 3. Proper Resource Management
```csharp
[UnityTest]
public IEnumerator TestWithCleanup() => UniTask.ToCoroutine(async () =>
{
    GameObject testObject = null;
    CancellationTokenSource cts = null;
    
    try
    {
        testObject = new GameObject();
        cts = new CancellationTokenSource();
        
        // Test logic
        await TestOperation(cts.Token);
    }
    finally
    {
        // Always cleanup
        cts?.Cancel();
        cts?.Dispose();
        
        if (testObject != null)
            Object.DestroyImmediate(testObject);
    }
});
```

### 4. Thread Safety
```csharp
[UnityTest]
public IEnumerator TestThreadSafety() => UniTask.ToCoroutine(async () =>
{
    // CPU-intensive work on thread pool
    var result = await UniTask.RunOnThreadPool(() =>
    {
        // Heavy computation
        return CalculateComplexValue();
    });
    
    // Unity API calls on main thread
    await UniTask.SwitchToMainThread();
    
    var go = new GameObject($"Result_{result}");
    Assert.IsNotNull(go);
    Object.Destroy(go);
});
```

### 5. Avoid Blocking Operations
```csharp
// Good - Async all the way
[UnityTest]
public IEnumerator GoodAsync() => UniTask.ToCoroutine(async () =>
{
    var result = await AsyncOperation();
    ProcessResult(result);
});

// Bad - Blocks the thread
[UnityTest]
public IEnumerator BadBlocking() => UniTask.ToCoroutine(async () =>
{
    var result = AsyncOperation().GetAwaiter().GetResult(); // Don't do this!
    ProcessResult(result);
});
```

## TDD with Prefabs Pattern (Standard Approach)

### Why Prefab Pattern is the Default

The Prefab Pattern ensures:
- **Consistent test setup** - Same configuration every test run
- **Proper Unity initialization** - Components initialize as they would in production
- **Realistic testing** - Tests reflect actual game behavior
- **Maintainable tests** - Changes to prefab affect all tests uniformly
- **Version control friendly** - Prefab changes are tracked in Git

### Core Principles

1. **Editor Scripts Create Prefabs** - Programmatically build prefabs with all references
2. **Tests Instantiate in Setup** - Load and instantiate prefabs in test setup
3. **Cleanup in Teardown** - Always destroy test instances
4. **One Factory per System** - Each major system gets its own prefab factory

### Directory Structure

```
Assets/
├── Tests/                          # Your test directory
│   ├── Editor/
│   │   └── PrefabFactories/       # Editor scripts for prefab creation
│   │       ├── PlayerPrefabFactory.cs
│   │       ├── EnemyPrefabFactory.cs
│   │       └── UIPrefabFactory.cs
│   ├── PlayMode/                   # PlayMode test scripts
│   │   ├── PlayerTests.cs
│   │   └── EnemyTests.cs
│   └── EditMode/                   # EditMode test scripts (for utilities)
│       └── MathUtilsTests.cs      # Rare: Simple atomic tests
└── Resources/
    └── TestPrefabs/                # Generated prefabs stored here
        ├── PlayerTestPrefab.prefab
        ├── EnemyTestPrefab.prefab
        └── UITestCanvas.prefab
```

### Step 1: Create Your Prefab Factory (Editor Script)

For every system you're testing, create a prefab factory. This is your SOURCE OF TRUTH for test setup:

```csharp
using UnityEngine;
using UnityEditor;
using System.IO;

namespace TestFramework.Editor.PrefabFactories
{
    /// <summary>
    /// Factory for creating test prefabs programmatically
    /// </summary>
    public static class AudioSystemPrefabFactory
    {
        #region Constants
        
        private const string PREFAB_PATH = "Assets/Resources/TestPrefabs/AudioSystemPrefab.prefab";
        
        #endregion
        
        #region Menu Items
        
        [MenuItem("TestFramework/Prefabs/Create Audio System Prefab")]
        public static void CreateAudioSystemPrefab()
        {
            // Ensure directory exists
            var directory = Path.GetDirectoryName(PREFAB_PATH);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                AssetDatabase.Refresh();
            }
            
            // Create root GameObject
            GameObject prefabRoot = new GameObject("AudioSystem");
            
            try
            {
                // Add and configure components
                SetupAudioSource(prefabRoot);
                SetupAudioProcessor(prefabRoot);
                SetupAudioEffects(prefabRoot);
                
                // Create child objects
                CreateAudioChannels(prefabRoot);
                
                // Save as prefab
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PREFAB_PATH);
                Debug.Log($"[TestFramework] Created prefab at: {PREFAB_PATH}");
                
                // Select the created prefab
                var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
                Selection.activeObject = prefabAsset;
            }
            finally
            {
                // Clean up the scene object
                Object.DestroyImmediate(prefabRoot);
            }
        }
        
        #endregion
        
        #region Setup Methods
        
        private static void SetupAudioSource(GameObject root)
        {
            var audioSource = root.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.volume = 0.5f;
            audioSource.spatialBlend = 0f; // 2D sound
        }
        
        private static void SetupAudioProcessor(GameObject root)
        {
            // Add your custom component
            var processor = root.AddComponent<AudioProcessor>();
            
            // Configure the component
            processor.bufferSize = 512;
            processor.sampleRate = 44100;
            
            // Use FindVars pattern to cache references
            processor.FindVars();
        }
        
        private static void SetupAudioEffects(GameObject root)
        {
            // Add audio filters
            var lowPassFilter = root.AddComponent<AudioLowPassFilter>();
            lowPassFilter.cutoffFrequency = 5000;
            
            var reverbFilter = root.AddComponent<AudioReverbFilter>();
            reverbFilter.reverbPreset = AudioReverbPreset.Room;
        }
        
        private static void CreateAudioChannels(GameObject root)
        {
            for (int i = 0; i < 4; i++)
            {
                var channel = new GameObject($"Channel_{i}");
                channel.transform.SetParent(root.transform);
                
                var channelSource = channel.AddComponent<AudioSource>();
                channelSource.playOnAwake = false;
                channelSource.volume = 0.25f;
            }
        }
        
        #endregion
        
        #region Validation
        
        [MenuItem("TestFramework/Prefabs/Validate Audio System Prefab")]
        public static void ValidatePrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            if (prefab == null)
            {
                Debug.LogError($"[TestFramework] Prefab not found at: {PREFAB_PATH}");
                return;
            }
            
            // Validate components exist
            if (!prefab.GetComponent<AudioSource>())
                Debug.LogError("[TestFramework] Missing AudioSource component");
            
            if (!prefab.GetComponent<AudioProcessor>())
                Debug.LogError("[TestFramework] Missing AudioProcessor component");
            
            // Validate children
            var childCount = prefab.transform.childCount;
            if (childCount != 4)
                Debug.LogError($"[TestFramework] Expected 4 channels, found {childCount}");
            
            Debug.Log("[TestFramework] Prefab validation complete");
        }
        
        #endregion
    }
}
```

### Step 2: Write Tests Using the Prefab

```csharp
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using Cysharp.Threading.Tasks;
using TestFramework.Unity.Core;

namespace TestFramework.Tests.PlayMode
{
    [TestFixture]
    public class AudioSystemPrefabTests : UniTaskTestBase
    {
        #region Fields
        
        private GameObject audioSystemInstance;
        private AudioProcessor audioProcessor;
        private AudioSource mainAudioSource;
        
        #endregion
        
        #region Setup and Teardown
        
        [SetUp]
        public override void Setup()
        {
            base.Setup();
            
            // Load and instantiate the prefab
            var prefab = Resources.Load<GameObject>("TestPrefabs/AudioSystemPrefab");
            Assert.IsNotNull(prefab, "AudioSystemPrefab not found. Run 'TestFramework/Prefabs/Create Audio System Prefab' menu item");
            
            audioSystemInstance = Object.Instantiate(prefab);
            audioSystemInstance.name = "AudioSystem_TestInstance";
            
            // Cache component references
            audioProcessor = audioSystemInstance.GetComponent<AudioProcessor>();
            mainAudioSource = audioSystemInstance.GetComponent<AudioSource>();
            
            // Validate setup
            Assert.IsNotNull(audioProcessor, "AudioProcessor component missing");
            Assert.IsNotNull(mainAudioSource, "AudioSource component missing");
        }
        
        [TearDown]
        public override void TearDown()
        {
            // Clean up test instance
            if (audioSystemInstance != null)
            {
                Object.DestroyImmediate(audioSystemInstance);
                audioSystemInstance = null;
            }
            
            audioProcessor = null;
            mainAudioSource = null;
            
            base.TearDown();
        }
        
        #endregion
        
        #region Tests
        
        [UnityTest]
        public IEnumerator Should_Initialize_With_Correct_Configuration() => UniTask.ToCoroutine(async () =>
        {
            // Assert - Verify initial configuration
            Assert.AreEqual(512, audioProcessor.bufferSize);
            Assert.AreEqual(44100, audioProcessor.sampleRate);
            Assert.AreEqual(0.5f, mainAudioSource.volume);
            Assert.IsFalse(mainAudioSource.playOnAwake);
            
            await UniTask.Yield();
        });
        
        [UnityTest]
        public IEnumerator Should_Have_Four_Audio_Channels() => UniTask.ToCoroutine(async () =>
        {
            // Act
            var channels = audioSystemInstance.GetComponentsInChildren<AudioSource>();
            
            // Assert - 1 main + 4 channels = 5 total
            Assert.AreEqual(5, channels.Length);
            
            // Verify channel names
            for (int i = 0; i < 4; i++)
            {
                var channelObj = audioSystemInstance.transform.Find($"Channel_{i}");
                Assert.IsNotNull(channelObj, $"Channel_{i} not found");
            }
            
            await UniTask.Yield();
        });
        
        [UnityTest]
        public IEnumerator Should_Process_Audio_Buffer() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            float[] testBuffer = new float[512];
            for (int i = 0; i < testBuffer.Length; i++)
            {
                testBuffer[i] = Mathf.Sin(2 * Mathf.PI * i / 512f);
            }
            
            // Act
            await audioProcessor.ProcessBufferAsync(testBuffer, testCancellationTokenSource.Token);
            
            // Assert
            Assert.IsTrue(audioProcessor.IsProcessing);
            Assert.Greater(audioProcessor.LastProcessedSamples, 0);
            
            await UniTask.Delay(100);
        });
        
        [UnityTest]
        public IEnumerator Should_Apply_Audio_Effects() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var lowPassFilter = audioSystemInstance.GetComponent<AudioLowPassFilter>();
            var reverbFilter = audioSystemInstance.GetComponent<AudioReverbFilter>();
            
            // Assert - Verify effects are configured
            Assert.IsNotNull(lowPassFilter);
            Assert.IsNotNull(reverbFilter);
            Assert.AreEqual(5000, lowPassFilter.cutoffFrequency);
            Assert.AreEqual(AudioReverbPreset.Room, reverbFilter.reverbPreset);
            
            // Act - Modify effects
            lowPassFilter.cutoffFrequency = 2000;
            
            await UniTask.Delay(50);
            
            // Assert - Verify change applied
            Assert.AreEqual(2000, lowPassFilter.cutoffFrequency);
        });
        
        [UnityTest]
        public IEnumerator Should_Handle_Concurrent_Channel_Playback() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var channels = new AudioSource[4];
            for (int i = 0; i < 4; i++)
            {
                var channelObj = audioSystemInstance.transform.Find($"Channel_{i}");
                channels[i] = channelObj.GetComponent<AudioSource>();
            }
            
            // Act - Start all channels
            var playTasks = new UniTask[4];
            for (int i = 0; i < 4; i++)
            {
                int index = i; // Capture for closure
                playTasks[i] = PlayChannelAsync(channels[index], index * 100);
            }
            
            await UniTask.WhenAll(playTasks);
            
            // Assert - All channels should be playing
            foreach (var channel in channels)
            {
                Assert.IsTrue(channel.isPlaying);
            }
            
            // Cleanup
            foreach (var channel in channels)
            {
                channel.Stop();
            }
        });
        
        #endregion
        
        #region Helper Methods
        
        private async UniTask PlayChannelAsync(AudioSource channel, int delayMs)
        {
            await UniTask.Delay(delayMs, cancellationToken: testCancellationTokenSource.Token);
            channel.Play();
            await UniTask.Yield();
        }
        
        #endregion
    }
}
```

### Best Practices for Prefab TDD

1. **Start with the Factory**
   - ALWAYS create the prefab factory before writing tests
   - The factory defines your test's "production environment"
   - Update the factory as requirements change

2. **Validate Prefab Before Tests**
   - Check prefab exists in Setup()
   - Provide helpful error messages if missing
   - Include menu path in error message

3. **Clean Resource Management**
   - Instantiate in Setup()
   - Destroy in TearDown()
   - Use try-finally blocks for critical cleanup

4. **Component Caching**
   - Cache frequently used components in Setup()
   - Avoid repeated GetComponent calls
   - Validate components exist before tests

5. **Prefab Versioning**
   - Include version number in prefab name if needed
   - Document breaking changes in factory script
   - Provide migration scripts for major changes

## Advanced Patterns

### Custom Async Assertions

```csharp
public static class AsyncAssertions
{
    public static async UniTask AssertEventuallyTrue(
        Func<bool> condition,
        string message = null,
        int timeoutMs = 5000)
    {
        var startTime = Time.realtimeSinceStartup;
        
        while (!condition())
        {
            if ((Time.realtimeSinceStartup - startTime) * 1000 > timeoutMs)
            {
                Assert.Fail(message ?? $"Condition not met within {timeoutMs}ms");
            }
            await UniTask.Yield();
        }
    }
}

// Usage
[UnityTest]
public IEnumerator TestEventually() => UniTask.ToCoroutine(async () =>
{
    StartAsyncOperation();
    
    await AsyncAssertions.AssertEventuallyTrue(
        () => IsOperationComplete(),
        "Operation should complete",
        timeoutMs: 3000
    );
});
```

### Test Data Generators

```csharp
[UnityTest]
public IEnumerator TestWithGeneratedData() => UniTask.ToCoroutine(async () =>
{
    // Generate test data asynchronously
    var testData = await UniTask.RunOnThreadPool(() =>
    {
        var data = new List<TestData>();
        for (int i = 0; i < 1000; i++)
        {
            data.Add(GenerateTestData(i));
        }
        return data;
    });
    
    await UniTask.SwitchToMainThread();
    
    // Process on main thread
    foreach (var item in testData)
    {
        ProcessTestData(item);
    }
    
    Assert.AreEqual(1000, processedCount);
});
```

## Test Development Workflow

Follow the **[4-Step Process](../../CLAUDE.md#test-development-workflow)** - REQUIRED for all Unity development.

### Unity-Specific Commands

**PlayMode vs EditMode:**
```bash
# PlayMode tests (with automatic completion detection)
python Coordination/Scripts/quick_test.py all -p play --wait

# EditMode tests (faster, no Play mode)
python Coordination/Scripts/quick_test.py all -p edit --wait

# Specific class
python Coordination/Scripts/quick_test.py class MyTestClass -p edit --wait
```

**Monitor Execution:**
```bash
# Real-time monitoring
python Coordination/Scripts/quick_logs.py monitor -l error

# Check test status
python Coordination/Scripts/quick_test.py status <request_id>
```

For full command reference and examples, see `Coordination/README.md`.

## Troubleshooting

### Tests Not Running
1. Ensure UniTask is installed correctly
2. Check assembly references include UniTask
3. Verify using `UniTask.ToCoroutine()` for [UnityTest] methods
4. Check for compilation errors using: `python Coordination/Scripts/quick_logs.py errors`

### Memory Leaks
1. Always dispose CancellationTokenSource
2. Destroy GameObjects in finally blocks
3. Use `using` statements for disposables
4. Call Resources.UnloadUnusedAssets() in teardown

### Async Deadlocks
1. Never use .Result or .GetAwaiter().GetResult()
2. Use UniTask.SwitchToMainThread() for Unity API
3. Always include timeouts with cancellation tokens
4. Avoid mixing UniTask with System.Threading.Tasks

### Performance Issues
1. Use UniTask.Yield() in long loops
2. Profile with UniTaskTestHelpers.BenchmarkAsync
3. Run CPU work on thread pool
4. Minimize main thread blocking

## Summary

The Unity Test Framework with UniTask provides:
1. **Zero-allocation async/await** - No garbage collection pressure
2. **Full cancellation support** - Proper timeout handling
3. **Thread-safe operations** - Easy thread switching
4. **Rich helper utilities** - Retry, benchmark, parallel operations
5. **Clean error handling** - Full try-catch-finally support
6. **Modern C# patterns** - Pure async/await throughout

For DOTS-specific testing with UniTask, refer to the [DOTS Test Execution Guide](../DOTS/DOTS_TEST_EXECUTION_GUIDE.md).