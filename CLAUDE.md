<!-- PERSPEC_CONFIG_START -->
<!-- PERSPEC_CONFIG_START -->
# CLAUDE.md

> **Purpose**: TDD guidance for Claude Code in Unity projects using PerSpec framework.

## 📋 Quick Navigation
- [Script Locations](#script-locations) 🔍
- [Natural Language Commands](#natural-language-commands) 🗣️
- [TDD Workflow](#tdd-workflow) ⭐
- [Critical Patterns](#critical-patterns) 🚨
- [Test Requirements](#test-requirements) 🧪
- [Important Rules](#important-rules) ⚠️

## 🔍 Script Locations

```bash
# Fixed paths for reliability
PerSpec/Coordination/Scripts/       # Python coordination tools
PerSpec/package_location.txt        # Package path reference
Packages/com.digitraver.perspec/    # Package location
```

## 🗣️ Natural Language Commands

| User Says | Execute |
|-----------|---------|
| "show/get errors" | `python PerSpec/Coordination/Scripts/quick_logs.py errors` |
| "run tests" | `python PerSpec/Coordination/Scripts/quick_test.py all -p edit --wait` |
| "refresh Unity" | `python PerSpec/Coordination/Scripts/quick_refresh.py full --wait` |
| "show logs" | `python PerSpec/Coordination/Scripts/quick_logs.py latest -n 50` |
| "export logs" | `python PerSpec/Coordination/Scripts/quick_logs.py export` |
| "test results" | `cat $(ls -t PerSpec/TestResults/*.xml 2>/dev/null \| head -1)` |

**Intent Mapping:**
- "Something wrong" → Check errors
- "Tests failing" → Run with verbose: `quick_test.py all -v --wait`
- "Unity not responding" → Refresh Unity
- **Timeout?** → Tell user to click Unity window for focus
- **DOTS world null?** → Ensure using DOTSTestBase

## 🚀 TDD Workflow

### 📌 4-Step Process (REQUIRED)
```bash
# 1. Write tests & code
# 2. Refresh Unity
python PerSpec/Coordination/Scripts/quick_refresh.py full --wait
# 3. Check compilation
python PerSpec/Coordination/Scripts/quick_logs.py errors
# 4. Run tests
python PerSpec/Coordination/Scripts/quick_test.py all -p edit --wait
```

### 🎯 Test Execution
```bash
# Run ALL tests
quick_test.py all -p edit --wait

# Run by CLASS (use FULL namespace)
quick_test.py class Tests.PlayMode.SimplePerSpecTest -p play --wait

# Run specific METHOD
quick_test.py method Tests.PlayMode.SimplePerSpecTest.Should_Pass -p play --wait
```

## 🤖 Agent Usage

### Decision Matrix
- **Score 1-3**: NO agents - direct edits only
- **Score 4-7**: ONE specialized agent
- **Score 8+**: MULTIPLE agents in PARALLEL

### When to Use Agents
| Task | Use Agent? | Example |
|------|------------|---------|
| Complex feature (5+ files) | ✅ YES | "Implement auth system" |
| Test suite creation | ✅ YES | "Write comprehensive tests" |
| Simple fix | ❌ NO | "Fix null reference" |
| View file | ❌ NO | "Show Player class" |

### Agent Patterns
```python
# Complex feature - PARALLEL execution
Task(test-writer-agent): "Create test suite for inventory system"
Task(refactor-agent): "Prepare existing code for inventory"

# Simple tasks - NO AGENTS
Edit: Fix null check on line 42
Read: Show PlayerController
```

## 📖 Documentation Access

**Get package path first:**
```bash
cat PerSpec/package_location.txt  # Returns: Packages/com.digitraver.perspec
```

**Then read as needed:**
| Scenario | Read |
|----------|------|
| Writing Unity tests | `{package_path}/Documentation/unity-test-guide.md` |
| DOTS/ECS work | `{package_path}/Documentation/dots-test-guide.md` |
| Python issues | `{package_path}/Documentation/coordination-guide.md` |
| Using agents | `{package_path}/Documentation/agents/[agent-name].md` |

## 🎯 Test Facade Pattern

### ✅ CORRECT Pattern
```csharp
// PRODUCTION CLASS
public class PlayerController : MonoBehaviour 
{
    private float health = 100f;
    
    public void TakeDamage(float amount) {
        if (!isInvulnerable)
            health -= amount;
    }
    
    #if UNITY_EDITOR
    // Test facades - ONLY in production code
    public void Test_SetHealth(float value) => health = value;
    public float Test_GetHealth() => health;
    #endif
}

// TEST CODE - No directives needed!
[UnityTest]
public IEnumerator Should_TakeDamage() => UniTask.ToCoroutine(async () => {
    var player = Object.Instantiate(prefab).GetComponent<PlayerController>();
    
    player.Test_SetHealth(100f);  // Direct call - no #if needed
    player.TakeDamage(30f);
    
    Assert.AreEqual(70f, player.Test_GetHealth());
});
```

### ❌ FORBIDDEN
- Compiler directives in test code
- Using reflection for private access
- Making private methods public
- Test parameters in production methods

## ⚠️ Critical Patterns

### CS1626 - Yield in Try-Catch
```csharp
// ❌ WRONG
[UnityTest]
public IEnumerator BadTest() {
    try {
        yield return new WaitForSeconds(1); // CS1626!
    } catch { }
}

// ✅ CORRECT
[UnityTest]
public IEnumerator GoodTest() => UniTask.ToCoroutine(async () => {
    try {
        await UniTask.Delay(1000);
    } catch (Exception ex) {
        PerSpecDebug.LogError($"[ERROR] {ex.Message}");
        throw;
    }
});
```

### Never async void
```csharp
// ❌ Crashes Unity
public async void BadMethod() { }

// ✅ Use UniTask
public async UniTask GoodMethod() { }
public async UniTaskVoid FireAndForget() { }
```

### Thread Safety
```csharp
public async UniTask UpdateSafely(GameObject obj) {
    await UniTask.SwitchToMainThread();  // Unity APIs need main thread
    obj.transform.position = Vector3.zero;
}
```

## 🏗️ SOLID Principles

### Single Responsibility
```csharp
// ✅ Each class does ONE thing
public class PlayerMovement : MonoBehaviour { }
public class PlayerCombat : MonoBehaviour { }
```

### Open/Closed
```csharp
// ✅ Extend via abstraction
public abstract class Weapon : ScriptableObject {
    public abstract UniTask<float> CalculateDamageAsync(Enemy target);
}
```

### Dependency Inversion
**🚨 NEVER use Singleton MonoBehaviours!**

Use instead:
- Static classes for utilities
- ScriptableObjects for configuration
- POCO for data

## 🔧 Component References

### ✅ FindVars Pattern (REQUIRED)
```csharp
public class ExampleComponent : MonoBehaviour {
    [SerializeField] private AudioSource audioSource;
    
    [ContextMenu("Find Vars")]
    public void FindVars() {
        audioSource = GetComponent<AudioSource>();
    }
}
```

### ❌ NEVER
- Get components at runtime
- Add components at runtime  
- Use reflection for components

## 🧪 Test Requirements

### MANDATORY Base Classes
```csharp
using PerSpec.Runtime.Unity;
using PerSpec.Runtime.DOTS;  // For DOTS tests

[TestFixture]
public class MyTest : UniTaskTestBase  // For Unity tests
{
}

[TestFixture] 
public class MyDOTSTest : DOTSTestBase  // For DOTS/ECS tests (auto-sets DefaultWorld)
{
}
```

### Required References
```json
{
    "references": [
        "PerSpec.Runtime",
        "PerSpec.Runtime.Debug",
        "UniTask",
        "UnityEngine.TestRunner"
    ]
}
```

### Prefab Pattern Default
Use for: MonoBehaviours, components, UI, gameplay
Skip for: Pure utilities, math, string helpers

### Test Pattern
```csharp
[UnityTest]
public IEnumerator TestName() => UniTask.ToCoroutine(async () => {
    try {
        // Arrange
        var prefab = Resources.Load<GameObject>("TestPrefabs/Player");
        var instance = Object.Instantiate(prefab);
        
        // Act
        await instance.GetComponent<Player>().DoActionAsync();
        
        // Assert
        Assert.IsTrue(condition);
    } finally {
        if (instance) Object.DestroyImmediate(instance);
    }
});
```

## 📝 Logging

```csharp
using PerSpec;

// Test-specific logs
PerSpecDebug.LogTestSetup("setup phase");
PerSpecDebug.LogTestAction("action phase");
PerSpecDebug.LogTestAssert("assertion phase");
PerSpecDebug.LogTestComplete("test completed");
PerSpecDebug.LogTestError("test failed");

// Feature-specific logs (two parameters: feature name, message)
PerSpecDebug.LogFeatureStart("AUTH", "Starting authentication");
PerSpecDebug.LogFeatureProgress("AUTH", "Validating token");
PerSpecDebug.LogFeatureComplete("AUTH", "Login successful");
PerSpecDebug.LogFeatureError("AUTH", "Invalid credentials");

// General logs
PerSpecDebug.Log("general message");
PerSpecDebug.LogWarning("warning message");
PerSpecDebug.LogError("error message - always important");
```

## 🚨 Important Rules

### ALWAYS
✅ Use UniTask (never Task/coroutines)  
✅ Use FindVars for components  
✅ Stay on main thread for Unity APIs  
✅ Use test facades for private access  
✅ Follow 4-step TDD workflow  

### NEVER
❌ async void → Use UniTask/UniTaskVoid  
❌ Singleton MonoBehaviours  
❌ Runtime GetComponent  
❌ Reflection for private access  
❌ Compiler directives in tests  
❌ Skip TDD steps  

## 📊 Quick Reference

### Error Fixes
| Error | Solution |
|-------|----------|
| CS1626 | UniTask.ToCoroutine() |
| async void | UniTask/UniTaskVoid |
| Thread error | SwitchToMainThread() |
| Null components | FindVars pattern |
| DefaultGameObjectInjectionWorld null | Inherit from DOTSTestBase |
| NativeArray disposed | Use try-finally with Dispose() |

### Project Structure
```
TestFramework/
├── Packages/com.digitraver.perspec/  # Package
├── Assets/Tests/                      # Your tests
├── PerSpec/                           # Working dir
│   ├── Coordination/Scripts/          # Python tools
│   ├── TestResults/                   # XML results
│   └── test_coordination.db           # SQLite
└── CustomScripts/Output/              # Generated
```

### Available Agents
- **test-writer-agent**: Comprehensive tests with TDD
- **refactor-agent**: Split large files, SOLID
- **batch-refactor-agent**: Batch C# processing
- **dots-performance-profiler**: DOTS/ECS analysis
- **architecture-agent**: Document architecture

## 📝 Reminders

> **Pivoting?** Ask user first  
> **New directory?** Needs asmdef  
> **Errors?** Log with context  
> **Test prefabs?** Use Editor scripts
<!-- PERSPEC_CONFIG_END -->
<!-- PERSPEC_CONFIG_END -->

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
