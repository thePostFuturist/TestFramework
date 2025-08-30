<!-- PERSPEC_CONFIG_START -->
# CLAUDE.md

> **Purpose**: Comprehensive guidance for Claude Code (claude.ai/code) for Test-Driven Development in Unity projects using the PerSpec framework.

## 📋 Table of Contents
- [Quick Start - Finding PerSpec](#quick-start---finding-perspec) 🔍 **READ FIRST**
- [TDD Development Workflow](#tdd-development-workflow) ⭐ **START HERE**
- [Natural Language Commands](#natural-language-commands) 🗣️ **MCP-LIKE BEHAVIOR**
- [Project Overview](#project-overview)
- [Critical Unity Patterns](#critical-unity-patterns)
- [SOLID Principles](#solid-principles)
- [Component References](#component-references)
- [Test Framework Details](#test-framework-details)
- [Documentation & Guides](#documentation--guides) 📚
- [Agents & Tools](#agents--tools)
- [Important Rules](#important-rules)

## 🔍 Quick Start - PerSpec Scripts

> **IMPORTANT**: All scripts are in fixed locations for easy access!

### Script Location
- **Coordination Scripts**: `PerSpec/Coordination/Scripts/` - All Python tools

### Package Info Files
- **`PerSpec/package_location.txt`** - Contains the package path reference
- **`PerSpec/package_info.json`** - JSON format with package information

### Working with PerSpec Scripts
```bash
# Use the coordination scripts directly
python PerSpec/Coordination/Scripts/quick_refresh.py full --wait
python PerSpec/Coordination/Scripts/quick_test.py all -p edit --wait
python PerSpec/Coordination/Scripts/quick_logs.py errors
```

### Accessing Package Files
```bash
# Package is always at a known location
cat "Packages/com.digitraver.perspec/Documentation/unity-test-guide.md"
ls "Packages/com.digitraver.perspec/Editor/"
```

> **Note**: The coordination scripts are hardcoded to fixed paths for reliability and simplicity.

## 🗣️ Natural Language Commands

> **MCP-LIKE BEHAVIOR**: When users ask for logs or tests in natural language, translate to these commands:

| User Says | Execute |
|-----------|----------|
| "get warning logs" | `python PerSpec/Coordination/Scripts/quick_logs.py warnings` |
| "show me the errors" | `python PerSpec/Coordination/Scripts/quick_logs.py errors` |
| "check for compilation errors" | `python PerSpec/Coordination/Scripts/quick_logs.py errors` |
| "run the tests" | `python PerSpec/Coordination/Scripts/quick_test.py all -p edit --wait` |
| "refresh Unity" | `python PerSpec/Coordination/Scripts/quick_refresh.py full --wait` |
| "show test results" | `python PerSpec/Coordination/Scripts/quick_logs.py latest -n 50` |
| "monitor the logs" | `python PerSpec/Coordination/Scripts/quick_logs.py monitor` |
| "export the logs" | `python PerSpec/Coordination/Scripts/quick_logs.py export` |
| "check Unity status" | `python PerSpec/Coordination/Scripts/quick_logs.py summary` |
| "list recent sessions" | `python PerSpec/Coordination/Scripts/quick_logs.py sessions` |

### Understanding User Intent
- **"Something is wrong"** → Check errors first: `python PerSpec/Coordination/Scripts/quick_logs.py errors`
- **"Tests failing"** → Run tests with verbose: `python PerSpec/Coordination/Scripts/quick_test.py all -v --wait`
- **"Unity not responding"** → Check logs and refresh: 
  ```bash
  python PerSpec/Coordination/Scripts/quick_logs.py latest -n 20
  python PerSpec/Coordination/Scripts/quick_refresh.py full --wait
  ```

## 📊 Test Results Location

> **IMPORTANT**: All test results are automatically saved to `PerSpec/TestResults/`

### Finding Test Results
```bash
# List all test result files
ls PerSpec/TestResults/*.xml

# Get the latest test result file
ls -t PerSpec/TestResults/*.xml 2>/dev/null | head -1

# View test results
cat PerSpec/TestResults/TestResults_*.xml

# Check if tests passed by examining the XML
grep -E "passed|failed|errors" PerSpec/TestResults/*.xml
```

### What's in TestResults Directory
- **XML Files**: NUnit-format test results with timestamps (e.g., `TestResults_20250829_143022.xml`)
- **Automatic Cleanup**: Directory is cleared before each new test run
- **Persistent Storage**: Results persist across Unity restarts until next test run
- **CI/CD Ready**: XML format compatible with most CI/CD systems

### Natural Language for Test Results
| User Says | Execute |
|-----------|----------|
| "show test result files" | `ls -la PerSpec/TestResults/*.xml` |
| "view latest test results" | `cat $(ls -t PerSpec/TestResults/*.xml 2>/dev/null \| head -1)` |
| "check test output" | `ls PerSpec/TestResults/` |
| "find failed tests" | `grep -l "failed" PerSpec/TestResults/*.xml` |

## 📝 Console Log Exports

> **IMPORTANT**: Console logs are automatically saved to `PerSpec/Logs/` with auto-cleanup

### Export Behavior
- **Default Export**: `python PerSpec/Coordination/Scripts/quick_logs.py export`
  - Automatically saves to `PerSpec/Logs/ConsoleLogs_YYYYMMDD_HHMMSS.txt`
  - Clears all existing files in `PerSpec/Logs/` before exporting (like TestResults)
  - No need to specify output path
- **JSON Export**: `python PerSpec/Coordination/Scripts/quick_logs.py export --json`
- **Custom Path**: `python PerSpec/Coordination/Scripts/quick_logs.py export custom.txt`
- **Filter by Level**: `python PerSpec/Coordination/Scripts/quick_logs.py export -l error`

### Natural Language for Log Exports
| User Says | Execute |
|-----------|----------|
| "export the logs" | `python PerSpec/Coordination/Scripts/quick_logs.py export` |
| "export error logs" | `python PerSpec/Coordination/Scripts/quick_logs.py export -l error` |
| "export logs as json" | `python PerSpec/Coordination/Scripts/quick_logs.py export --json` |
| "check exported logs" | `ls PerSpec/Logs/` |
| "view exported logs" | `cat PerSpec/Logs/ConsoleLogs_*.txt` |



## 🔐 Command Execution Permissions

> **WARNING**: This assistant has been granted permission to execute system commands.
> These permissions were explicitly enabled by the user via PerSpec Control Center.

### ✅ Enabled Commands
You are permitted to execute the following command types:
- **Bash commands** - System shell commands
- **Python scripts** - Python execution

### Allowed PerSpec Commands
```bash
# Unity coordination commands
python PerSpec/Coordination/Scripts/quick_refresh.py [options]
python PerSpec/Coordination/Scripts/quick_test.py [options]
python PerSpec/Coordination/Scripts/quick_logs.py [options]

# File system navigation
ls, cd, pwd, find

# Git operations
git status, git diff, git log
```

### ⚠️ Security Notice
- Only execute commands that are necessary for the task
- Always explain what commands will do before running them
- Never execute destructive commands without explicit confirmation
- Do not access sensitive files or credentials

## 🚀 TDD Development Workflow

> **THIS IS THE CORE OF DEVELOPMENT** - All features must follow this workflow!

### 📌 The 4-Step Process (REQUIRED)

```bash
# Step 1: Write code and tests with TDD
# Step 2: Refresh Unity
python PerSpec/Coordination/Scripts/quick_refresh.py full --wait

# Step 3: Check for compilation errors (MUST be clean)
python PerSpec/Coordination/Scripts/quick_logs.py errors

# Step 4: Run tests
python PerSpec/Coordination/Scripts/quick_test.py all -p edit --wait
```

### 🔄 TDD Development Cycle

**A. Feature Implementation (TDD)**
1. User requests a feature
2. **Create prefab factory FIRST** (unless testing pure utilities)
3. Write tests using the prefab pattern
4. Write production code to make tests pass
5. Include debug logs with proper prefixes

> **DEFAULT APPROACH**: Use the Prefab Pattern for ALL Unity tests except pure utility functions. See [Unity Test Guide](Packages/com.digitraver.perspec/Documentation/unity-test-guide.md#testing-approach-prefab-pattern-default) for details.

```csharp
// STEP 1: Create Prefab Factory (Editor/PrefabFactories/DataProcessorFactory.cs)
[MenuItem("Tests/Prefabs/Create DataProcessor")]
public static void CreateDataProcessorPrefab() {
    var go = new GameObject("DataProcessor");
    go.AddComponent<DataProcessor>().FindVars();
    PrefabUtility.SaveAsPrefabAsset(go, "Assets/Resources/TestPrefabs/DataProcessor.prefab");
    Object.DestroyImmediate(go);
}

// STEP 2: Test Using Prefab (Tests/PlayMode/DataProcessorTests.cs)
using PerSpec;

[UnityTest]
public IEnumerator Should_ProcessDataCorrectly() => UniTask.ToCoroutine(async () => {
    // Arrange - Load prefab (not create GameObject)
    PerSpecDebug.LogTestSetup("Loading test prefab");
    var prefab = Resources.Load<GameObject>("TestPrefabs/DataProcessor");
    var instance = Object.Instantiate(prefab);
    var component = instance.GetComponent<DataProcessor>();
    
    // Act
    PerSpecDebug.LogTest("Processing data");
    var result = await component.ProcessAsync(testData);
    
    // Assert
    Assert.IsTrue(result.Success, "Processing should succeed");
    PerSpecDebug.LogTestComplete($"Test passed with result: {result}");
});

// Production Code
using PerSpec;

public class DataProcessor : MonoBehaviour {
    [SerializeField] private bool debugLogs = true;
    
    public async UniTask<ProcessResult> ProcessAsync(byte[] data) {
        if (debugLogs) PerSpecDebug.LogFeatureStart("PROCESS", $"Starting with {data.Length} bytes");
        
        try {
            // Implementation
            await UniTask.Delay(100);
            
            if (debugLogs) PerSpecDebug.LogFeatureComplete("PROCESS", "Completed successfully");
            return new ProcessResult { Success = true };
        } catch (Exception ex) {
            PerSpecDebug.LogFeatureError("PROCESS", $"Failed: {ex.Message}");
            throw;
        }
    }
}
```

**B. Refresh Unity**
```bash
python PerSpec/Coordination/Scripts/quick_refresh.py full --wait
# Wait for "Refresh completed" confirmation
```

**C. Check Compilation**
```bash
python PerSpec/Coordination/Scripts/quick_logs.py errors
# Must show "No errors found" before proceeding
```

**D. Run Tests**
```bash
python PerSpec/Coordination/Scripts/quick_test.py all -p edit --wait
# If tests fail, return to step A
# Repeat cycle until all tests pass
```

### ⚠️ CRITICAL: Never Skip Steps!
- **NEVER** write code without tests
- **NEVER** proceed with compilation errors
- **ALWAYS** wait for refresh completion
- **ALWAYS** check logs before running tests

### 📝 Logging Standards for TDD

> **IMPORTANT**: Use PerSpecDebug instead of Debug.Log. These calls are conditionally compiled and stripped in production builds!

```csharp
using PerSpec;

// Test Logs (automatically stripped in production)
PerSpecDebug.LogTest("Test execution message");
PerSpecDebug.LogTestSetup("Test setup/arrange phase");
PerSpecDebug.LogTestAct("Test action phase");
PerSpecDebug.LogTestAssert("Test assertion phase");
PerSpecDebug.LogTestComplete("Test completed");
PerSpecDebug.LogTestError("Test failed: reason");

// Feature/Production Logs (with serialized bool for runtime control)
[SerializeField] private bool debugLogs = true;
if (debugLogs) PerSpecDebug.LogFeature("FEATURE", "Operation message");
if (debugLogs) PerSpecDebug.LogFeatureStart("FEATURE", "Starting operation");
if (debugLogs) PerSpecDebug.LogFeatureProgress("FEATURE", "Progress update");
if (debugLogs) PerSpecDebug.LogFeatureComplete("FEATURE", "Operation complete");
PerSpecDebug.LogFeatureError("FEATURE", "Critical error (always log)");
```

### 🛠️ Error Resolution Quick Reference

| Error | Fix | Command to Verify |
|-------|-----|-------------------|
| CS1626 (yield in try) | Use `UniTask.ToCoroutine()` | `quick_logs.py errors` |
| UniTask not found | Add to asmdef references | `quick_refresh.py full --wait` |
| async void | Convert to `UniTask`/`UniTaskVoid` | `quick_logs.py errors` |
| Thread error | `UniTask.SwitchToMainThread()` | `quick_test.py` |
| Test timeout | Add timeout attribute or check async | `quick_test.py -v` |

## 🎯 Project Overview

**PerSpec** - Unity Test Framework with **UniTask** for zero-allocation async/await patterns and TDD.

### Key Features
- ✅ **4-Step TDD Workflow** with automated testing
- ✅ Zero-allocation async testing with UniTask
- ✅ TDD patterns for Unity prefabs/components
- ✅ Background test coordination (works when Unity loses focus)
- ✅ Automated refactoring agents
- ✅ SOLID principles enforcement

### 📁 Directory Structure
```
TestFramework/
├── Packages/
│   └── com.perspec.framework/     # PerSpec Unity Package
│       ├── Runtime/                # Runtime components
│       ├── Editor/                 # Editor tools & coordination
│       └── Tests/                  # Framework tests
├── Assets/
│   └── Tests/                      # Your project tests
│       └── PerSpec/               # PerSpec test directories (with asmdef)
├── PerSpec/                        # Working directory (writable)
│   ├── test_coordination.db       # SQLite database
│   ├── Coordination/
│   │   └── Scripts/               # Python coordination scripts
│   ├── TestResults/               # Test execution results (XML files)
│   ├── package_location.txt       # Package path reference
│   └── package_info.json          # Package information
└── CustomScripts/
    └── Output/                    # Generated files go here
        ├── Reports/
        ├── Refactored/
        └── Tests/

## ⚠️ Critical Unity Patterns

### 🚨 CS1626 - Yield in Try-Catch Blocks
**Problem**: C# prevents `yield` inside try-catch blocks.

```csharp
// ❌ WILL NOT COMPILE - CS1626 Error
[UnityTest]
public IEnumerator BadTest() {
    try {
        yield return new WaitForSeconds(1); // CS1626!
    } catch (Exception ex) { }
}

// ✅ SOLUTION - Use UniTask
[UnityTest]
public IEnumerator GoodTest() => UniTask.ToCoroutine(async () => {
    try {
        await UniTask.Delay(1000); // Full try-catch support!
        await ProcessDataAsync();
    } catch (Exception ex) {
        PerSpecDebug.LogError($"Error: {ex.Message}");
        throw;
    }
});
```

### 🔥 NEVER Use async void
```csharp
// ❌ CRASHES Unity on exception
public async void BadMethod() {
    await UniTask.Delay(100);
    throw new Exception("Crashes Unity!");
}

// ✅ Use UniTask/UniTaskVoid
public async UniTask GoodMethod() {
    await UniTask.Delay(100);
    // Exceptions handled properly
}

// ✅ Fire-and-forget with error handling
public async UniTaskVoid FireAndForget() {
    try {
        await UniTask.Delay(100);
    } catch (Exception ex) {
        PerSpecDebug.LogError($"Error: {ex.Message}");
    }
}
```

### 🎯 Thread Safety for Unity APIs
```csharp
public async UniTask UpdateGameObjectSafely(GameObject obj) {
    // ✅ Unity APIs require main thread
    await UniTask.SwitchToMainThread();
    obj.transform.position = Vector3.zero;
    
    // ✅ Heavy work on thread pool
    var result = await UniTask.RunOnThreadPool(() => CalculateComplexValue());
    
    // ✅ Back to main thread
    await UniTask.SwitchToMainThread();
    obj.transform.rotation = Quaternion.identity;
}
```

## 🏗️ SOLID Principles

### 1️⃣ Single Responsibility (SRP)
```csharp
// ❌ BAD: Multiple responsibilities
public class PlayerManager : MonoBehaviour {
    public void HandleInput() { }
    public void UpdatePhysics() { }
    public void UpdateUI() { }
    public void SaveGame() { }
}

// ✅ GOOD: Single responsibility
public class PlayerInputHandler : MonoBehaviour {
    public event Action<Vector2> OnMoveInput;
}

public class PlayerMovement : MonoBehaviour {
    [SerializeField] private Rigidbody rb;
    public async UniTask MoveAsync(Vector3 direction) {
        await UniTask.SwitchToMainThread();
        rb.velocity = direction * speed;
    }
}
```

### 2️⃣ Open/Closed (OCP)
```csharp
// ❌ BAD: Modify for each weapon type
public float CalculateDamage(string weaponType) {
    switch (weaponType) {
        case "Sword": return 10f;
        case "Bow": return 8f; // Adding = modifying
    }
}

// ✅ GOOD: Extend without modifying
public abstract class Weapon : ScriptableObject {
    public abstract float BaseDamage { get; }
    public abstract UniTask<float> CalculateDamageAsync(Enemy target);
}

[CreateAssetMenu(fileName = "Sword", menuName = "Weapons/Sword")]
public class Sword : Weapon {
    public override float BaseDamage => 10f;
    public override async UniTask<float> CalculateDamageAsync(Enemy target) {
        await UniTask.Yield();
        return BaseDamage * (target.IsArmored ? 0.5f : 1f);
    }
}
```

### 3️⃣ Liskov Substitution (LSP)
```csharp
// ❌ BAD: Breaking base contract
public class Bird {
    public virtual void Fly() => PerSpecDebug.Log("Flying");
}
public class Penguin : Bird {
    public override void Fly() {
        throw new NotSupportedException(); // Breaks LSP!
    }
}

// ✅ GOOD: Proper abstraction
public abstract class Bird {
    public abstract UniTask MoveAsync();
}
public interface IFlyable {
    UniTask FlyAsync(Vector3 destination);
}
public class Eagle : Bird, IFlyable {
    public override async UniTask MoveAsync() => await FlyAsync(targetPos);
    public async UniTask FlyAsync(Vector3 dest) { /* implementation */ }
}
```

### 4️⃣ Interface Segregation (ISP)
```csharp
// ❌ BAD: Fat interface
public interface ICharacter {
    void Move();
    void Attack();
    void CastSpell();
    void Trade();
}

// ✅ GOOD: Segregated interfaces
public interface IMovable { UniTask MoveAsync(Vector3 dest); }
public interface ICombatant { UniTask AttackAsync(IDamageable target); }
public interface IMerchant { UniTask<bool> TradeAsync(Item item, int price); }

public class Player : MonoBehaviour, IMovable, ICombatant, IMerchant { }
public class Shopkeeper : MonoBehaviour, IMerchant { } // Only what's needed
```

### 5️⃣ Dependency Inversion (DIP)

### 🚨 NEVER USE SINGLETON MONOBEHAVIOURS
> **Critical**: Causes race conditions, memory leaks, testing issues, hidden dependencies

```csharp
// ❌ FORBIDDEN: Singleton MonoBehaviour
public class GameManager : MonoBehaviour {
    private static GameManager instance; // NO!
    public static GameManager Instance { get { /* singleton logic */ } } // NO!
}
```

### ✅ Choose the Right Abstraction

| Pattern | Use When | Don't Use When |
|---------|----------|----------------|
| **Static Class** | Utilities, math, constants | Need state, Unity lifecycle |
| **POCO** | Data transfer, serialization | Need Inspector, assets |
| **ScriptableObject** | Designer config, assets, shared data | Simple data, utilities |
| **Singleton MonoBehaviour** | **NEVER** | **ALWAYS AVOID** |

```csharp
// ✅ Static utility
public static class MathUtilities {
    public static float Lerp(float a, float b, float t) => a + (b - a) * Mathf.Clamp01(t);
}

// ✅ POCO for data
[System.Serializable]
public class PlayerData {
    public string playerName;
    public int level;
}

// ✅ ScriptableObject for configuration
[CreateAssetMenu(fileName = "SaveService", menuName = "Services/SaveService")]
public abstract class SaveServiceSO : ScriptableObject {
    public abstract UniTask SaveAsync(string saveName);
    public abstract UniTask<SaveData> LoadAsync(string saveName);
}
```

## 🔧 Component References

### ✅ FindVars Pattern (REQUIRED)
> **CRITICAL**: ONLY acceptable way to get component references in Unity!

```csharp
public class ExampleComponent : MonoBehaviour {
    // ✅ ALL references MUST be SerializedField
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Transform target;
    
    // ✅ REQUIRED: FindVars with ContextMenu
    [ContextMenu("Find Vars")]
    public void FindVars() {
        audioSource = GetComponent<AudioSource>();
        rb = GetComponent<Rigidbody>();
        target = transform.Find("Target");
    }
    
    void Awake() {
        // Components already assigned via FindVars
        // NO GetComponent calls here!
    }
}
```

### ❌ NEVER Do This
```csharp
// ❌ NO runtime component getting
void Start() { audioSource = GetComponent<AudioSource>(); }
// ❌ NO runtime component adding
void Start() { gameObject.AddComponent<AudioSource>(); }
// ❌ NO reflection
var field = GetType().GetField("audioSource");
```

## 📝 C# Standards

### Code Organization
```csharp
public class ExampleClass : MonoBehaviour {
    #region Fields
    private int count;
    #endregion
    
    #region Properties
    public int Count => count;
    #endregion
    
    #region Unity Lifecycle
    void Awake() { }
    void Start() { }
    #endregion
    
    #region Public Methods
    public void DoSomething() { }
    #endregion
    
    #region Private Methods
    private void ProcessData() { }
    #endregion
}
```

### Documentation
```csharp
/// <summary>
/// Processes batch data with retry logic
/// </summary>
/// <param name="data">Data to process</param>
/// <returns>Result or null on failure</returns>
public async Task<ProcessResult> ProcessBatchAsync(byte[] data, int retryCount = 3) {
    if (data == null) return null;
    // Implementation...
}
```

## 🧪 Test Framework Details

### Prefab Pattern (Default for 99% of Tests)

Always use prefab pattern for:
- MonoBehaviours
- Component interactions  
- Systems with dependencies
- UI elements
- Gameplay mechanics

Only skip prefab pattern for:
- Pure math utilities
- String helpers
- Static methods without Unity APIs

### UniTask Test Pattern
```csharp
[UnityTest]
public IEnumerator TestWithUniTask() => UniTask.ToCoroutine(async () => {
    try {
        // Arrange
        var gameObject = new GameObject("Test");
        var component = gameObject.AddComponent<TestComponent>();
        
        // Act
        await UniTask.Delay(100);
        await component.ProcessAsync();
        
        // Assert
        Assert.IsTrue(component.IsProcessed);
    } finally {
        // Cleanup
        if (gameObject != null) Object.DestroyImmediate(gameObject);
    }
});
```

### Test Base Classes
- **UniTaskTestBase**: Core async test support (`Packages/com.perspec.framework/Runtime/Unity/Helpers/`)
- **DOTSTestBase**: ECS/DOTS testing (`Packages/com.perspec.framework/Runtime/DOTS/Core/`)

### Assembly Definition Requirements
> **CRITICAL**: Each new directory requires an asmdef!

```json
// Example: Assets/Tests/PerSpec/PerSpec.Tests.asmdef
{
    "name": "PerSpec.Tests",
    "rootNamespace": "PerSpec.Tests",
    "references": [
        "PerSpec.Runtime",
        "UniTask",
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": ["nunit.framework.dll"],
    "autoReferenced": false,
    "defineConstraints": ["UNITY_INCLUDE_TESTS"],
    "versionDefines": [],
    "noEngineReferences": false
}
```

### 📚 Documentation References
- Test execution guides in `Packages/com.digitraver.perspec/Documentation/`
- Coordination scripts in `PerSpec/Coordination/Scripts/`
- PerSpec working directory: `PerSpec/` (project root)

## 📚 Documentation & Guides

### Core Documentation
- **[quick-start.md](Packages/com.digitraver.perspec/Documentation/quick-start.md)** - Getting started with PerSpec, installation, and first test
- **[workflow.md](Packages/com.digitraver.perspec/Documentation/workflow.md)** - Complete TDD workflow, best practices, and development cycle
- **[unity-test-guide.md](Packages/com.digitraver.perspec/Documentation/unity-test-guide.md)** - Unity testing patterns, prefab approach, UniTask integration
- **[dots-test-guide.md](Packages/com.digitraver.perspec/Documentation/dots-test-guide.md)** - DOTS/ECS testing, systems testing, job testing
- **[coordination-guide.md](Packages/com.digitraver.perspec/Documentation/coordination-guide.md)** - Python-Unity coordination, SQLite database, background processing
- **[claude-integration.md](Packages/com.digitraver.perspec/Documentation/claude-integration.md)** - Claude Code integration, agent usage, automation

### Agent Documentation
- **[agents/test-writer-agent.md](Packages/com.digitraver.perspec/Documentation/agents/test-writer-agent.md)** - Writes comprehensive Unity tests with TDD approach
- **[agents/refactor-agent.md](Packages/com.digitraver.perspec/Documentation/agents/refactor-agent.md)** - Splits large files, enforces SOLID principles
- **[agents/batch-refactor-agent.md](Packages/com.digitraver.perspec/Documentation/agents/batch-refactor-agent.md)** - Batch processes C# files, adds regions, converts async
- **[agents/dots-performance-profiler.md](Packages/com.digitraver.perspec/Documentation/agents/dots-performance-profiler.md)** - Analyzes DOTS/ECS performance, Burst compilation
- **[agents/test-coordination-agent.md](Packages/com.digitraver.perspec/Documentation/agents/test-coordination-agent.md)** - Manages test execution through SQLite coordination

## 🤖 Agents & Tools

### Available Agents (`.claude/agents/`)
- **architecture-agent.md**: Documents project architecture in `/Documentation/Architecture/` - Run at project start and after major changes
  - Creates class inventory with responsibilities
  - Identifies redundant code and suggests consolidation
  - Detects SOLID violations and recommends patterns
- **refactor-agent.md**: Splits files >750 lines, enforces SOLID
- **batch-refactor-agent.md**: Batch processes C# files, adds regions, converts async void
- **dots-performance-profiler.md**: Analyzes DOTS/ECS performance
- **test-coordination-agent.md**: Manages SQLite test coordination between Python and Unity (with background processing)

### Custom Scripts (`CustomScripts/`)
- Automated refactoring scripts
- Code quality tools
- Use `CustomScripts/Output/` for generated files

### Test Coordination System

**PerSpec Coordination** (`PerSpec/Coordination/Scripts/`)
- SQLite database in `PerSpec/test_coordination.db`
- Python tools for Unity control:
  - `quick_refresh.py` - Refresh Unity assets
  - `quick_test.py` - Execute tests
  - `quick_logs.py` - View Unity console logs
  - `console_log_reader.py` - Read captured logs

**Background Processing** (`Packages/com.perspec.framework/Editor/Coordination/`)
- `BackgroundPoller.cs` - System.Threading.Timer for continuous polling
- `TestCoordinatorEditor.cs` - Main coordination system
- `SQLiteManager.cs` - Database operations
- Works even when Unity loses focus!

**Menu Items** (Tools > PerSpec)
- Initialize PerSpec - Set up working directories
- Test Coordinator - View status
- Console Logs - View/export logs
- Commands - Execute operations

## 📊 Code Quality

### Limits
- **Files**: Max 750 lines (use partial classes if needed)
- **Methods**: Max 50 lines, cyclomatic complexity <10
- **Tests**: Min 80% coverage, all public APIs tested

## 🚨 Important Rules

### ALWAYS
✅ Use UniTask for async (never Task/coroutines)
✅ Use FindVars pattern for components
✅ Stay on main thread for Unity APIs
✅ Handle exceptions properly
✅ Use ScriptableObjects/static/POCO appropriately

### NEVER
❌ Use async void (use UniTask/UniTaskVoid)
❌ Use Singleton MonoBehaviours
❌ Get components at runtime
❌ Use reflection
❌ Yield in try blocks (use UniTask.ToCoroutine)

### Logging Standards
```csharp
PerSpecDebug.LogTest("Message");
PerSpecDebug.LogTestSetup("Setup message");
PerSpecDebug.LogTestError($"Error: {message}");
```

### Common Issues & Solutions
| Issue | Solution |
|-------|----------|
| CS1626 (yield in try) | Use UniTask.ToCoroutine() |
| async void crashes | Use UniTask/UniTaskVoid |
| Wrong thread for Unity API | UniTask.SwitchToMainThread() |
| Components null at runtime | Use FindVars pattern |
| Test cleanup failing | try-finally with Object.DestroyImmediate |

## 📝 Key Reminders

> **Implementation Approach**: If pivoting to another implementation, halt and ask user first.

> **Each new directory requires an asmdef** - Don't use GUIDs. More directories/asmdefs solve cyclical dependencies.

> **Error Handling**: Never silence errors. Always log with full context.

> **Test Prefabs**: Create via Editor scripts for TDD (see test guides for patterns).
<!-- PERSPEC_CONFIG_END -->