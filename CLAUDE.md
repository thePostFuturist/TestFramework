# CLAUDE.md

> **Purpose**: Comprehensive guidance for Claude Code (claude.ai/code) for Test-Driven Development in Unity projects.

## 📋 Table of Contents
- [Project Overview](#project-overview)
- [Critical Unity Patterns](#critical-unity-patterns)
- [SOLID Principles](#solid-principles)
- [Component References](#component-references)
- [Test Framework](#test-framework)
- [Agents & Tools](#agents--tools)
- [Important Rules](#important-rules)

## 🎯 Project Overview

Unity Test Framework with **UniTask** for zero-allocation async/await patterns and TDD.

### Key Features
- ✅ Zero-allocation async testing with UniTask
- ✅ TDD patterns for Unity prefabs/components
- ✅ Automated refactoring agents
- ✅ SOLID principles enforcement

### 📁 Output Directory Convention
**IMPORTANT**: Generated files in subdirectories only:
- Use `CustomScripts/Output/` for script-generated files
- Create descriptive subdirectories: `Output/Reports/`, `Output/Refactored/`, `Output/Tests/`
- Example: `CustomScripts/Output/Reports/large-files.txt`

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
        Debug.LogError($"[ERROR] {ex.Message}");
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
        Debug.LogError($"[ERROR] {ex.Message}");
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
    public virtual void Fly() => Debug.Log("Flying");
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

## 🧪 Test Framework

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
- **UniTaskTestBase**: Core async test support
- **DOTSTestBase**: ECS/DOTS testing

### Test Development Workflow

**REQUIRED 4-Step Process:**

1. **Write** code/tests
2. **Refresh**: `python Coordination/Scripts/quick_refresh.py full --wait`
3. **Check**: `python Coordination/Scripts/quick_logs.py errors` (MUST be clean)
4. **Test**: `python Coordination/Scripts/quick_test.py all -p edit --wait`

**Error Resolution:**
| Error | Fix |
|-------|-----|
| CS1626 (yield in try) | Use UniTask.ToCoroutine() |
| UniTask not found | Add to asmdef references |
| async void | Convert to UniTask/UniTaskVoid |
| Thread error | UniTask.SwitchToMainThread() |

**Details:** See `Coordination/README.md` for full command reference and examples.
**Background:** Works even when Unity loses focus (System.Threading.Timer).

### 📚 Documentation References
- `Assets/TestFramework/Unity/UNIFIED_TEST_EXECUTION_GUIDE.md`
- `Assets/TestFramework/DOTS/DOTS_TEST_EXECUTION_GUIDE.md`

## 🤖 Agents & Tools

### Available Agents (`.claude/agents/`)
- **refactor-agent.md**: Splits files >750 lines, enforces SOLID
- **batch-refactor-agent.md**: Batch processes C# files, adds regions, converts async void
- **dots-performance-profiler.md**: Analyzes DOTS/ECS performance
- **test-coordination-agent.md**: Manages SQLite test coordination between Python and Unity (with background processing)

### Custom Scripts (`CustomScripts/`)
- Automated refactoring scripts
- Code quality tools
- Use `CustomScripts/Output/` for generated files

### Test Coordination System (`Coordination/`)
- SQLite database coordination between Python and Unity
- Automatic test execution with status tracking
- PlayMode test completion detection
- **Background processing when Unity loses focus (NEW)**
- Asset refresh coordination
- System.Threading.Timer for continuous polling

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
Debug.Log($"[TEST] Message");
Debug.Log($"[TEST-SETUP] Setup message");
Debug.LogError($"[TEST-ERROR] Error: {message}");
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