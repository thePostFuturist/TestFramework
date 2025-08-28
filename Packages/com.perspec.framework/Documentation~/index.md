# PerSpec Testing Framework Documentation

## Overview

PerSpec is a professional Unity testing framework that combines:
- UniTask for zero-allocation async testing
- DOTS/ECS testing support
- SQLite-based Python-Unity coordination
- Intelligent console log capture
- Background test execution

## Core Concepts

### 1. Test Base Classes

**UniTaskTestBase** - For async Unity tests:
```csharp
public class MyTests : UniTaskTestBase
{
    [UnityTest]
    public IEnumerator TestAsync() => UniTask.ToCoroutine(async () =>
    {
        await UniTask.Delay(100);
        Assert.IsTrue(condition);
    });
}
```

**DOTSTestBase** - For ECS/DOTS tests:
```csharp
public class MyDOTSTests : DOTSTestBase
{
    [Test]
    public void TestEntitySystem()
    {
        var entity = EntityManager.CreateEntity();
        // Test DOTS systems
    }
}
```

### 2. Test Coordination

The SQLite coordination system enables:
- External test triggering via Python
- Real-time status monitoring
- Console log capture and analysis
- Background polling when Unity loses focus

### 3. The 4-Step Workflow

1. **Write** code/tests
2. **Refresh**: `python .../quick_refresh.py full --wait`
3. **Check**: `python .../quick_logs.py errors` (must be clean)
4. **Test**: `python .../quick_test.py all -p edit --wait`

## Menu Structure

All PerSpec tools under `Tools > PerSpec`:

```
Tools/
└── PerSpec/
    ├── Test Coordinator
    ├── Console Logs
    ├── Commands/
    │   ├── Check Pending Tests
    │   ├── Cancel Current Test
    │   └── Toggle Auto-Polling
    └── Debug/
        ├── Database Status
        ├── Test Connection
        └── Clear Pending Requests
```

## Package Structure

```
com.perspec.framework/
├── Runtime/           # Test base classes
├── Editor/           # Coordination & tools
├── Documentation~/   # This documentation
└── ScriptingTools/   # Python scripts
```

## Key Features

### Console Log Capture

Intelligent stack trace truncation:
- Removes framework noise
- Preserves user code context
- Reduces LLM token usage

### Background Polling

Tests execute even when Unity loses focus:
- System.Threading.Timer implementation
- Automatic request detection
- Thread-safe SQLite operations

### SOLID Principles

Enforced patterns:
- No singleton MonoBehaviours
- Proper dependency injection
- Interface segregation
- Single responsibility

## Next Steps

- [Quick Start Guide](quick-start.md)
- [Unity Testing Guide](unity-test-guide.md)
- [DOTS Testing Guide](dots-test-guide.md)
- [Coordination System](coordination-guide.md)