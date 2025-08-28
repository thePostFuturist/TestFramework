# PerSpec Testing Framework

Professional Unity TDD framework with UniTask async patterns, DOTS support, and SQLite-based test coordination.

## Features

- **Zero-allocation async testing** with UniTask
- **TDD patterns** for Unity prefabs and components
- **SQLite coordination** between Python and Unity
- **Intelligent console log capture** with stack trace truncation
- **Background test polling** even when Unity loses focus
- **SOLID principles enforcement**

## Installation

### Via Package Manager

1. Open Unity Package Manager
2. Click "+" → "Add package from git URL..."
3. Enter: `https://github.com/yourusername/perspec.git`

### Local Development

1. Clone repository to `Packages/com.perspec.framework/`
2. Unity will automatically detect the package

## Quick Start

### 1. Access PerSpec Tools

All tools available under `Tools > PerSpec` menu:
- **Test Coordinator** - Main test execution window
- **Console Logs** - View Unity console logs
- **Commands** - Manual test controls
- **Debug** - Database and connection tools

### 2. Run Tests via Python

```bash
# Initialize database
python Packages/com.perspec.framework/ScriptingTools/Coordination/Scripts/db_initializer.py

# Run all tests
python Packages/com.perspec.framework/ScriptingTools/Coordination/Scripts/quick_test.py all -p both --wait

# Check console logs
python Packages/com.perspec.framework/ScriptingTools/Coordination/Scripts/quick_logs.py errors
```

### 3. Write Tests with UniTask

```csharp
using PerSpec.Runtime.Unity;
using UnityEngine.TestTools;
using Cysharp.Threading.Tasks;

public class MyTests : UniTaskTestBase
{
    [UnityTest]
    public IEnumerator TestAsync() => UniTask.ToCoroutine(async () =>
    {
        // Your async test code here
        await UniTask.Delay(100);
        Assert.IsTrue(true);
    });
}
```

## Documentation

- [Getting Started](Documentation~/quick-start.md)
- [4-Step Workflow](Documentation~/workflow.md)
- [Unity Testing Guide](Documentation~/unity-test-guide.md)
- [DOTS Testing Guide](Documentation~/dots-test-guide.md)
- [Coordination System](Documentation~/coordination-guide.md)

## Requirements

- Unity 2021.3+
- UniTask 2.3.3+
- Unity Test Framework 1.3.0+

## License

MIT License - See LICENSE.md for details