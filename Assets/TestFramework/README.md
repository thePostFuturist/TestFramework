# Unity Test Framework

A comprehensive, reusable test framework for Unity projects with specialized support for DOTS (Data-Oriented Technology Stack).

## Overview

This framework provides a robust testing infrastructure that can be used in any Unity project. It includes file-based logging, test lifecycle management, helper utilities, and specialized support for DOTS/ECS testing.

## Features

- 🔄 **Zero-allocation async/await** - UniTask-powered testing with no garbage
- 🚀 **DOTS/ECS support** - Specialized testing for Unity's Data-Oriented Technology Stack
- 🧪 **Performance testing** - Built-in performance measurement and validation
- 🔍 **Memory leak detection** - Automatic detection of native memory leaks
- 🛠️ **Helper utilities** - Async runners, cancellation support, and more
- 📊 **Test-Driven Development** - Clean code patterns and SOLID principles

## Installation

1. Copy the entire `TestFramework` folder to your Unity project's `Assets` directory
2. Install UniTask for modern async testing:
   - Add via Package Manager: `https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask`
3. Ensure you have the required Unity packages installed:
   - Unity Test Framework
   - Unity.Entities (for DOTS support)
   - Unity.Burst (for DOTS support)

## Quick Start

### Modern Async Test with UniTask

```csharp
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using Cysharp.Threading.Tasks;
using TestFramework.Unity.Core;

public class MyAsyncTest : UniTaskTestBase
{
    #region Tests
    
    [UnityTest]
    public IEnumerator TestExample() => UniTask.ToCoroutine(async () =>
    {
        // Arrange
        var testObject = new GameObject("TestObject");
        
        try
        {
            // Act - Zero allocation async/await
            await UniTask.Delay(1000);
            
            // Assert
            Assert.IsNotNull(testObject);
        }
        finally
        {
            // Cleanup
            Object.Destroy(testObject);
        }
    });
    
    #endregion
}
```

### DOTS Test

```csharp
using Unity.Entities;
using NUnit.Framework;
using TestFramework.DOTS.Core;

public class MyDOTSTest : DOTSTestBase
{
    [Test]
    public void TestEntityCreation()
    {
        var entity = CreateTestEntity(typeof(Translation));
        Assert.IsTrue(entityManager.Exists(entity));
        LogResult("EntityCreation", true);
    }
}
```

## Project Structure

```
TestFramework/
├── Unity/                          # General Unity test framework
│   ├── Core/
│   │   └── UniTaskTestBase.cs    # Base class for UniTask async tests
│   ├── Helpers/
│   │   ├── UniTaskTestHelpers.cs # UniTask test utilities
│   │   └── UniTaskRunner.cs      # UniTask runner and manager
│   ├── TestFramework.Unity.asmdef # Assembly definition
│   └── UNIFIED_TEST_EXECUTION_GUIDE.md # Unity test documentation
│
├── DOTS/                           # DOTS-specific test framework
│   ├── Core/
│   │   ├── DOTSTestBase.cs       # Base class for DOTS tests with UniTask
│   │   └── DOTSTestRunMarkerCallback.cs # DOTS test markers
│   ├── Helpers/
│   │   ├── DOTSTestConfiguration.cs # Test configuration
│   │   └── DOTSTestFactory.cs    # Factory for test objects
│   ├── TestFramework.DOTS.asmdef # Assembly definition
│   └── DOTS_TEST_EXECUTION_GUIDE.md # DOTS test documentation
│
└── README.md                       # This file
```

## Key Components

### UniTaskTestBase
Base class for async tests that provides:
- Automatic cancellation token management
- Helper methods for async operations
- Zero-allocation test patterns

### UniTaskRunner
Manages UniTask operations with:
- Application-wide cancellation support
- Retry logic and parallel execution
- Thread-safe async operations

### DOTSTestBase
Base class for DOTS tests that provides:
- Automatic World and EntityManager setup/teardown
- Helper methods for entity creation
- DOTS-specific logging methods
- Memory leak detection

### DOTSTestFactory
Factory for creating test objects and entities with various configurations for different test scenarios.

## Usage Examples

### Performance Testing

```csharp
[UnityTest]
public IEnumerator TestPerformance()
{
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    
    // Operation to measure
    yield return PerformOperation();
    
    stopwatch.Stop();
    Debug.Log($"[TIMING] Operation: {stopwatch.ElapsedMilliseconds}ms");
    
    Assert.Less(stopwatch.ElapsedMilliseconds, 100, "Should complete within 100ms");
}
```

### Memory Leak Detection

```csharp
public class MemoryTest : DOTSTestBase
{
    [Test]
    public void TestNoLeaks()
    {
        AssertNoMemoryLeaks();
        
        var buffer = new NativeArray<float>(1024, Allocator.Temp);
        // Use buffer...
        buffer.Dispose(); // Must dispose to pass leak detection
    }
}
```

### Async Operations

```csharp
[UnityTest]
public IEnumerator TestAsyncOperation()
{
    Task<string> asyncTask = FetchDataAsync();
    yield return asyncTask.AsCoroutine();
    
    Assert.AreEqual("expected", asyncTask.Result);
}
```

## Test Output

### Console Output
Test results are displayed in the Unity console with structured prefixes:
```
[TEST] General test message
[TEST-START] Test beginning
[RESULT] TestName: PASSED - Details
[EXCEPTION] TestName: ExceptionType
[TIMING] Operation: 45.23ms
[DOTS-TEST] DOTS-specific message
```

## Assembly References

To use this framework in your test assemblies:

```json
{
    "name": "YourProject.Tests",
    "references": [
        "TestFramework.Unity",
        "TestFramework.DOTS"  // If using DOTS
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": true,  // If using DOTS
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ]
}
```

## Best Practices

1. **Use regions** to organize test code
2. **Clean up resources** in TearDown methods
3. **Follow AAA pattern** - Arrange, Act, Assert
4. **Set timeouts** for async operations to prevent hanging tests
5. **Dispose native collections** when using DOTS to avoid memory leaks
6. **Use test categories** to organize and filter test execution
7. **Write XML documentation** for complex test methods
8. **Keep tests atomic** - one test per method

## Documentation

- [Unity Test Execution Guide](Unity/UNIFIED_TEST_EXECUTION_GUIDE.md) - Comprehensive Unity testing documentation
- [DOTS Test Execution Guide](DOTS/DOTS_TEST_EXECUTION_GUIDE.md) - DOTS/ECS testing documentation

## Requirements

- Unity 2020.3 or later
- Unity Test Framework package
- Unity.Entities package (for DOTS support)
- Unity.Burst package (for DOTS support)

## License

This test framework is provided as-is for use in Unity projects. Feel free to modify and extend it for your needs.

## Contributing

This framework is designed to be generic and reusable. If you have improvements or bug fixes, consider:
1. Keeping the framework generic (no project-specific code)
2. Adding documentation for new features
3. Following the existing code structure and naming conventions

## Support

For issues or questions about using this framework:
1. Check the documentation guides
2. Review the example code in this README
3. Look at the test templates in the documentation