# CLAUDE.md

> **Purpose**: This file provides comprehensive guidance to Claude Code (claude.ai/code) for Test-Driven Development (TDD) in Unity projects.

## 📋 Table of Contents

- [Project Overview](#project-overview)
- [Critical Unity Patterns](#critical-unity-patterns)
- [C# Coding Standards](#c-coding-standards)
- [Test Framework Documentation](#test-framework-documentation)
- [Refactoring Agents](#refactoring-agents)
- [Common Issues and Solutions](#common-issues-and-solutions)

## 🎯 Project Overview

This is a **Unity Test Framework** optimized for Test-Driven Development using **UniTask** for zero-allocation async/await patterns.

### Key Features
- ✅ Zero-allocation async testing with UniTask
- ✅ TDD patterns for Unity prefabs and components
- ✅ Automated refactoring agents
- ✅ SOLID principles enforcement
- ✅ Comprehensive test helpers and utilities

### 📁 Output Directory Convention

**IMPORTANT**: All generated files must be organized in subdirectories, never in project root:
- Use `CustomScripts/Output/` for script-generated files
- Create descriptive subdirectories: `Output/Reports/`, `Output/Refactored/`, `Output/Tests/`
- Generated output directories should NOT be in `.gitignore` (track results)
- Example: `CustomScripts/Output/Reports/large-files.txt`


## ⚠️ Critical Unity Patterns

### 🚨 IMPORTANT: CS1626 - Yield in Try-Catch Blocks

> **⚠️ CRITICAL**: C# compiler error **CS1626** prevents `yield` statements inside try-catch blocks. This is a fundamental limitation that affects all Unity coroutine tests.

**Problem:**
```csharp
// ❌ WILL NOT COMPILE - CS1626 Error
[UnityTest]
public IEnumerator BadTest()
{
    try
    {
        yield return new WaitForSeconds(1); // CS1626: Cannot yield in try block
    }
    catch (Exception ex)
    {
        Debug.LogError(ex);
    }
}
```

**Solution - Use UniTask Instead:**
```csharp
// ✅ PREFERRED - Use UniTask for full async/await support
[UnityTest]
public IEnumerator GoodTest() => UniTask.ToCoroutine(async () =>
{
    try
    {
        await UniTask.Delay(1000); // Full try-catch support!
        await ProcessDataAsync();
    }
    catch (Exception ex)
    {
        Debug.LogError($"[TEST-ERROR] {ex.Message}");
        throw;
    }
});
```

### 🔥 NEVER Use async void

> **⚠️ CRITICAL**: `async void` methods can crash Unity if they throw exceptions!

```csharp
// ❌ NEVER DO THIS - Will crash Unity on exception
public async void ProcessDataBad()
{
    await UniTask.Delay(100);
    throw new Exception("This crashes Unity!");
}

// ✅ ALWAYS USE UniTask or UniTaskVoid
public async UniTask ProcessDataGood()
{
    await UniTask.Delay(100);
    // Exceptions handled properly
}

// ✅ For fire-and-forget scenarios
public async UniTaskVoid ProcessDataFireAndForget()
{
    try
    {
        await UniTask.Delay(100);
    }
    catch (Exception ex)
    {
        Debug.LogError($"[ERROR] {ex.Message}");
    }
}
```

### 🎯 Stay on Main Thread for Unity APIs

> **⚠️ IMPORTANT**: Unity API calls must be made from the main thread!

```csharp
public async UniTask UpdateGameObjectSafely(GameObject obj)
{
    // ✅ Ensure main thread before Unity API calls
    await UniTask.SwitchToMainThread();
    
    obj.transform.position = Vector3.zero;
    obj.SetActive(true);
    
    // ✅ CPU-intensive work on thread pool
    var result = await UniTask.RunOnThreadPool(() =>
    {
        // Heavy computation here
        return CalculateComplexValue();
    });
    
    // ✅ Switch back for Unity APIs
    await UniTask.SwitchToMainThread();
    obj.transform.rotation = Quaternion.identity;
}
```

## 📝 C# Coding Standards

### Code Organization with Regions

All C# scripts must be organized using #regions for clarity and maintainability:

```csharp
public class ExampleClass : MonoBehaviour
{
    #region Fields
    private int count;
    private bool isActive;
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

### Documentation Standards

#### XML Documentation for Complex Methods
```csharp
/// <summary>
/// Processes batch data asynchronously with retry logic
/// </summary>
/// <param name="data">The data array to process</param>
/// <param name="retryCount">Number of retry attempts on failure</param>
/// <returns>Processed result or null on failure</returns>
public async Task<ProcessResult> ProcessBatchAsync(byte[] data, int retryCount = 3)
{
    // Validate input parameters
    if (data == null || data.Length == 0)
        return null;

    // Retry logic for resilient processing
    for (int i = 0; i < retryCount; i++)
    {
        try
        {
            // Process data in chunks for memory efficiency
            var result = await ProcessChunksAsync(data);
            return result;
        }
        catch (Exception ex)
        {
            // Log attempt failure and continue
            Debug.LogWarning($"Attempt {i + 1} failed: {ex.Message}");
        }
    }
    
    return null;
}
```

## 🏗️ SOLID Principles

> **📌 IMPORTANT**: Following SOLID principles ensures maintainable, testable, and scalable code.

### 1️⃣ Single Responsibility Principle (SRP)

> **Rule**: A class should have only one reason to change.

#### ❌ BAD: Multiple Responsibilities
```csharp
// ❌ This class does too much!
public class PlayerManager : MonoBehaviour
{
    // Handles input, physics, UI, audio, saves... 
    public void HandleInput() { }
    public void UpdatePhysics() { }
    public void UpdateUI() { }
    public void PlaySound() { }
    public void SaveGame() { }
}
```

#### ✅ GOOD: Single Responsibility
```csharp
// ✅ Each class has ONE clear responsibility
public class PlayerInputHandler : MonoBehaviour
{
    public event Action<Vector2> OnMoveInput;
    public event Action OnJumpInput;
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            OnJumpInput?.Invoke();
    }
}

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private Rigidbody rb;
    
    public async UniTask MoveAsync(Vector3 direction)
    {
        await UniTask.SwitchToMainThread();
        rb.velocity = direction * speed;
    }
}

public class PlayerAudioController : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    
    public void PlayJumpSound() => audioSource.PlayOneShot(jumpClip);
}
```

### 2️⃣ Open/Closed Principle (OCP)

> **Rule**: Classes should be open for extension but closed for modification.

#### ❌ BAD: Modifying Existing Code
```csharp
// ❌ Have to modify this class for each new weapon type
public class WeaponDamage
{
    public float CalculateDamage(string weaponType)
    {
        switch (weaponType)
        {
            case "Sword": return 10f;
            case "Bow": return 8f;
            case "Staff": return 12f;
            // Adding new weapon = modifying this method
            default: return 5f;
        }
    }
}
```

#### ✅ GOOD: Extension Through Abstraction
```csharp
// ✅ Base abstraction
public abstract class Weapon : ScriptableObject
{
    public abstract float BaseDamage { get; }
    public abstract async UniTask<float> CalculateDamageAsync(Enemy target);
}

// ✅ Extend without modifying
[CreateAssetMenu(fileName = "Sword", menuName = "Weapons/Sword")]
public class Sword : Weapon
{
    public override float BaseDamage => 10f;
    
    public override async UniTask<float> CalculateDamageAsync(Enemy target)
    {
        await UniTask.Yield();
        return BaseDamage * (target.IsArmored ? 0.5f : 1f);
    }
}

[CreateAssetMenu(fileName = "MagicStaff", menuName = "Weapons/MagicStaff")]
public class MagicStaff : Weapon
{
    public override float BaseDamage => 12f;
    
    public override async UniTask<float> CalculateDamageAsync(Enemy target)
    {
        await UniTask.Yield();
        return BaseDamage * target.MagicVulnerability;
    }
}
```

### 3️⃣ Liskov Substitution Principle (LSP)

> **Rule**: Derived classes must be substitutable for their base classes without breaking functionality.

#### ❌ BAD: Breaking Base Class Contract
```csharp
// ❌ Derived class breaks base class behavior
public class Bird
{
    public virtual void Fly() => Debug.Log("Flying");
}

public class Penguin : Bird
{
    public override void Fly()
    {
        throw new NotSupportedException("Penguins can't fly!"); // ❌ Breaks LSP
    }
}
```

#### ✅ GOOD: Proper Abstraction
```csharp
// ✅ Better abstraction that doesn't assume all birds fly
public abstract class Bird
{
    public abstract async UniTask MoveAsync();
}

public interface IFlyable
{
    UniTask FlyAsync(Vector3 destination);
}

public class Eagle : Bird, IFlyable
{
    public override async UniTask MoveAsync()
    {
        await FlyAsync(targetPosition);
    }
    
    public async UniTask FlyAsync(Vector3 destination)
    {
        await UniTask.Delay(100);
        transform.position = destination;
    }
}

public class Penguin : Bird
{
    public override async UniTask MoveAsync()
    {
        await SwimAsync();
    }
    
    private async UniTask SwimAsync()
    {
        await UniTask.Delay(200);
        // Swimming logic
    }
}
```

### 4️⃣ Interface Segregation Principle (ISP)

> **Rule**: Clients should not be forced to depend on interfaces they don't use.

#### ❌ BAD: Fat Interface
```csharp
// ❌ Too many responsibilities in one interface
public interface ICharacter
{
    void Move();
    void Attack();
    void UseItem();
    void CastSpell();
    void Trade();
    void Craft();
}

// ❌ NPC doesn't need all these methods
public class NPC : ICharacter
{
    public void Move() { }
    public void Attack() => throw new NotImplementedException(); // ❌
    public void UseItem() => throw new NotImplementedException(); // ❌
    public void CastSpell() => throw new NotImplementedException(); // ❌
    public void Trade() { }
    public void Craft() => throw new NotImplementedException(); // ❌
}
```

#### ✅ GOOD: Segregated Interfaces
```csharp
// ✅ Small, focused interfaces
public interface IMovable
{
    UniTask MoveAsync(Vector3 destination);
}

public interface ICombatant
{
    UniTask AttackAsync(IDamageable target);
    float AttackPower { get; }
}

public interface IDamageable
{
    UniTask TakeDamageAsync(float damage);
    float Health { get; }
}

public interface IMerchant
{
    UniTask<bool> TradeAsync(Item item, int price);
}

// ✅ Classes implement only what they need
public class Player : MonoBehaviour, IMovable, ICombatant, IDamageable, IMerchant
{
    public float AttackPower => 10f;
    public float Health { get; private set; } = 100f;
    
    public async UniTask MoveAsync(Vector3 destination) { }
    public async UniTask AttackAsync(IDamageable target) { }
    public async UniTask TakeDamageAsync(float damage) { }
    public async UniTask<bool> TradeAsync(Item item, int price) { return true; }
}

public class Shopkeeper : MonoBehaviour, IMerchant
{
    // Only implements trading
    public async UniTask<bool> TradeAsync(Item item, int price)
    {
        await UniTask.Delay(100);
        return HasEnoughGold(price);
    }
}
```

### 5️⃣ Dependency Inversion Principle (DIP)

> **Rule**: High-level modules should not depend on low-level modules. Both should depend on abstractions.
> **Unity Pattern**: Choose the right tool - ScriptableObjects for configuration, static classes for utilities, POCOs for data.

### 🚨 NEVER USE SINGLETON MONOBEHAVIOURS

> **⚠️ CRITICAL**: Singleton MonoBehaviours are an anti-pattern that causes numerous issues:
> - Race conditions during scene loading
> - Memory leaks with DontDestroyOnLoad
> - Difficult to test and mock
> - Hidden dependencies that break SOLID principles
> - Scene reload problems in Editor

#### ❌ FORBIDDEN: Singleton MonoBehaviour Pattern
```csharp
// ❌ NEVER DO THIS - Singleton MonoBehaviour anti-pattern
public class GameManager : MonoBehaviour
{
    private static GameManager instance; // ❌ NO!
    
    public static GameManager Instance // ❌ NO!
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<GameManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("GameManager");
                    instance = go.AddComponent<GameManager>();
                    DontDestroyOnLoad(go); // ❌ Causes memory leaks
                }
            }
            return instance;
        }
    }
    
    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject); // ❌ Race condition prone
            return;
        }
        instance = this;
    }
}
```

### ✅ Choose the Right Abstraction

#### 1️⃣ Static Classes for Pure Utilities
```csharp
// ✅ GOOD: Static class for stateless utilities
public static class MathUtilities
{
    public static float Lerp(float a, float b, float t)
    {
        return a + (b - a) * Mathf.Clamp01(t);
    }
    
    public static Vector3 GetRandomPointInCircle(float radius)
    {
        float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2);
        float distance = UnityEngine.Random.Range(0f, radius);
        return new Vector3(Mathf.Cos(angle) * distance, 0, Mathf.Sin(angle) * distance);
    }
}

// ✅ GOOD: Static configuration class
public static class GameConstants
{
    public const int MAX_PLAYERS = 4;
    public const float GRAVITY = -9.81f;
    public const string SAVE_FILE_EXTENSION = ".gamesave";
    
    public static readonly Color PLAYER_COLORS[] = {
        Color.red, Color.blue, Color.green, Color.yellow
    };
}
```

#### 2️⃣ POCOs for Data Transfer
```csharp
// ✅ GOOD: Plain Old C# Object for data
[System.Serializable]
public class PlayerData
{
    public string playerName;
    public int level;
    public float experience;
    public List<string> unlockedAbilities;
    
    public PlayerData(string name)
    {
        playerName = name;
        level = 1;
        experience = 0;
        unlockedAbilities = new List<string>();
    }
}

// ✅ GOOD: POCO for configuration
[System.Serializable]
public class AudioSettings
{
    public float masterVolume = 1.0f;
    public float musicVolume = 0.8f;
    public float sfxVolume = 0.9f;
    public bool muteOnFocusLoss = true;
    
    // Can have methods too
    public float GetEffectiveVolume(float baseVolume)
    {
        return baseVolume * sfxVolume * masterVolume;
    }
}
```

#### 3️⃣ ScriptableObjects for Designer-Configurable Systems
```csharp
// ✅ USE ScriptableObjects when you need:
// - Designer/Artist configuration in Inspector
// - Asset-based configuration
// - Multiple configurations (dev/staging/production)
// - Shared data between scenes
```

### 📊 When to Use Each Pattern

| Pattern | Use When | Don't Use When |
|---------|----------|----------------|
| **Static Class** | • Pure utility functions<br>• Mathematical operations<br>• Constants and enums<br>• Stateless helpers | • Need instance-specific state<br>• Require Unity lifecycle<br>• Need serialization |
| **POCO** | • Data transfer objects<br>• Simple configuration<br>• JSON/XML serialization<br>• Unit testing | • Need Unity Inspector<br>• Require asset references<br>• Complex behavior |
| **ScriptableObject** | • Designer configuration<br>• Asset references needed<br>• Multiple config variants<br>• Shared between scenes | • Simple data structures<br>• Pure logic/utilities<br>• Temporary runtime data |
| **❌ Singleton MonoBehaviour** | **NEVER** | **ALWAYS AVOID** |

### ✅ Complete DIP Example with Mixed Patterns

```csharp
// ✅ POCO for data
[System.Serializable]
public class SaveData
{
    public int level;
    public float playTime;
    public Vector3 lastPosition;
}

// ✅ Static utility for file operations
public static class SaveFileUtility
{
    public static string GetSavePath(string filename)
    {
        return Path.Combine(Application.persistentDataPath, "Saves", filename);
    }
    
    public static async UniTask<bool> FileExistsAsync(string path)
    {
        return await UniTask.RunOnThreadPool(() => File.Exists(path));
    }
}

// ✅ Abstract base ScriptableObject for configuration
public abstract class SaveServiceSO : ScriptableObject
{
    public abstract UniTask SaveAsync(string saveName);
    public abstract UniTask<SaveData> LoadAsync(string saveName);
    
    // ✅ Static method to load default instance from Resources
    public static T GetDefaultPath<T>() where T : SaveServiceSO
    {
        string resourcePath = $"Services/{typeof(T).Name}";
        T service = Resources.Load<T>(resourcePath);
        
        if (service == null)
        {
            Debug.LogError($"[DIP] Failed to load {typeof(T).Name} from Resources/{resourcePath}");
        }
        
        return service;
    }
}

// ✅ Concrete implementations as ScriptableObjects
[CreateAssetMenu(fileName = "LocalSaveService", menuName = "Services/LocalSaveService")]
public class LocalSaveServiceSO : SaveServiceSO
{
    [SerializeField] private string saveDirectory = "Saves";
    [SerializeField] private bool useCompression = true;
    
    public override async UniTask SaveAsync(string saveName)
    {
        await UniTask.RunOnThreadPool(() =>
        {
            string path = Path.Combine(saveDirectory, saveName);
            // Save to local file with settings from SO
            Debug.Log($"[SAVE] Saving to {path} (Compression: {useCompression})");
        });
    }
    
    public override async UniTask<SaveData> LoadAsync(string saveName)
    {
        return await UniTask.RunOnThreadPool(() =>
        {
            string path = Path.Combine(saveDirectory, saveName);
            // Load from local file
            return new SaveData();
        });
    }
}

[CreateAssetMenu(fileName = "CloudSaveService", menuName = "Services/CloudSaveService")]
public class CloudSaveServiceSO : SaveServiceSO
{
    [SerializeField] private string cloudEndpoint = "https://api.game.com/saves";
    [SerializeField] private int timeoutMs = 5000;
    
    public override async UniTask SaveAsync(string saveName)
    {
        await UniTask.Delay(100); // Simulated network delay
        Debug.Log($"[SAVE] Uploading to {cloudEndpoint} (Timeout: {timeoutMs}ms)");
    }
    
    public override async UniTask<SaveData> LoadAsync(string saveName)
    {
        await UniTask.Delay(100); // Simulated network delay
        return new SaveData();
    }
}

// ✅ GameManager using ScriptableObject dependency
public class GameManager : MonoBehaviour
{
    // ✅ SerializedField for editor assignment
    [SerializeField] private SaveServiceSO saveService;
    
    #region FindVars Pattern
    
    [ContextMenu("Find Vars")]
    public void FindVars()
    {
        // ✅ Try to load from default path if not assigned
        if (saveService == null)
        {
            saveService = SaveServiceSO.GetDefaultPath<LocalSaveServiceSO>();
        }
    }
    
    #endregion
    
    #region Unity Lifecycle
    
    void Awake()
    {
        // ✅ Fallback loading on Awake if not assigned in editor
        if (saveService == null)
        {
            saveService = SaveServiceSO.GetDefaultPath<LocalSaveServiceSO>();
            
            if (saveService == null)
            {
                Debug.LogError("[GameManager] SaveService not found! Please assign in Inspector or place in Resources/Services/");
            }
        }
    }
    
    #endregion
    
    #region Save Operations
    
    public async UniTaskVoid SaveGameAsync()
    {
        if (saveService == null)
        {
            Debug.LogError("[SAVE] No save service configured!");
            return;
        }
        
        try
        {
            await saveService.SaveAsync("autosave");
            Debug.Log("[SAVE] Game saved successfully");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SAVE-ERROR] {ex.Message}");
        }
    }
    
    public async UniTaskVoid LoadGameAsync()
    {
        if (saveService == null)
        {
            Debug.LogError("[LOAD] No save service configured!");
            return;
        }
        
        try
        {
            var data = await saveService.LoadAsync("autosave");
            ApplyLoadedData(data);
            Debug.Log("[LOAD] Game loaded successfully");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LOAD-ERROR] {ex.Message}");
        }
    }
    
    #endregion
}

// ✅ Test-friendly mock service
[CreateAssetMenu(fileName = "MockSaveService", menuName = "Services/MockSaveService")]
public class MockSaveServiceSO : SaveServiceSO
{
    [SerializeField] private bool simulateFailure = false;
    [SerializeField] private int simulatedDelayMs = 0;
    
    public override async UniTask SaveAsync(string saveName)
    {
        if (simulatedDelayMs > 0)
            await UniTask.Delay(simulatedDelayMs);
            
        if (simulateFailure)
            throw new Exception("Simulated save failure");
            
        Debug.Log($"[TEST] Mock save: {saveName}");
    }
    
    public override async UniTask<SaveData> LoadAsync(string saveName)
    {
        if (simulatedDelayMs > 0)
            await UniTask.Delay(simulatedDelayMs);
            
        if (simulateFailure)
            throw new Exception("Simulated load failure");
            
        return new SaveData { Level = 99, Gold = 9999 };
    }
}

// ✅ Settings ScriptableObject pattern for configuration
[CreateAssetMenu(fileName = "GameSettings", menuName = "Settings/GameSettings")]
public class GameSettingsSO : ScriptableObject
{
    [Header("Save System")]
    [SerializeField] private SaveServiceSO primarySaveService;
    [SerializeField] private SaveServiceSO backupSaveService;
    
    [Header("Audio")]
    [SerializeField] private float masterVolume = 1.0f;
    [SerializeField] private float effectsVolume = 0.8f;
    
    // ✅ Static getter for default settings
    private static GameSettingsSO cachedInstance;
    public static GameSettingsSO GetDefaultPath()
    {
        if (cachedInstance == null)
        {
            cachedInstance = Resources.Load<GameSettingsSO>("Settings/GameSettings");
            
            if (cachedInstance == null)
            {
                Debug.LogError("[SETTINGS] GameSettings not found in Resources/Settings/");
            }
        }
        return cachedInstance;
    }
    
    public SaveServiceSO GetSaveService(bool useBackup = false)
    {
        return useBackup ? backupSaveService : primarySaveService;
    }
}

// ✅ Example usage in a MonoBehaviour
public class SaveSystemController : MonoBehaviour
{
    [SerializeField] private GameSettingsSO gameSettings;
    [SerializeField] private SaveServiceSO overrideSaveService; // Optional override
    
    [ContextMenu("Find Vars")]
    public void FindVars()
    {
        // Load settings if not assigned
        if (gameSettings == null)
        {
            gameSettings = GameSettingsSO.GetDefaultPath();
        }
        
        // Load default save service if no override
        if (overrideSaveService == null && gameSettings != null)
        {
            overrideSaveService = gameSettings.GetSaveService();
        }
    }
    
    void Awake()
    {
        // Ensure dependencies are loaded
        if (gameSettings == null)
        {
            gameSettings = GameSettingsSO.GetDefaultPath();
        }
        
        // Use override if specified, otherwise use from settings
        var saveService = overrideSaveService ?? gameSettings?.GetSaveService();
        
        if (saveService == null)
        {
            Debug.LogError("[SaveSystemController] No save service available!");
        }
    }
}
```

### 🎯 DRY (Don't Repeat Yourself)

> **Rule**: Extract common functionality into reusable components.

```csharp
// ✅ Reusable test utilities following DRY
public static class TestUtilities
{
    #region GameObject Creation
    
    public static async UniTask<GameObject> CreateTestObjectAsync(params Type[] components)
    {
        await UniTask.SwitchToMainThread();
        
        var go = new GameObject("TestObject");
        foreach (var component in components)
        {
            go.AddComponent(component);
        }
        return go;
    }
    
    #endregion
    
    #region Validation Helpers
    
    public static async UniTask<bool> ValidateComponentsAsync<T>(GameObject obj) where T : Component
    {
        await UniTask.Yield();
        return obj.GetComponent<T>() != null;
    }
    
    #endregion
}
```

### 📊 SOLID in Unity Context

#### Example: Complete Audio System Following SOLID

```csharp
// 1️⃣ SRP: Each class has one responsibility
public interface IAudioPlayer
{
    UniTask PlayAsync(AudioClip clip, float volume);
}

public interface IAudioMixer
{
    void SetMasterVolume(float volume);
    void SetEffectsVolume(float volume);
}

// 2️⃣ OCP: Extensible through inheritance
public abstract class AudioEffect : ScriptableObject
{
    public abstract void ApplyEffect(AudioSource source);
}

// 3️⃣ LSP: Implementations are substitutable
public class StandardAudioPlayer : IAudioPlayer
{
    public async UniTask PlayAsync(AudioClip clip, float volume)
    {
        await UniTask.SwitchToMainThread();
        // Play audio
    }
}

// 4️⃣ ISP: Focused interfaces
public interface IPoolable
{
    void OnPoolGet();
    void OnPoolReturn();
}

// 5️⃣ DIP: Depends on abstractions
public class AudioManager : MonoBehaviour
{
    private IAudioPlayer audioPlayer;
    private IAudioMixer audioMixer;
    
    public void Initialize(IAudioPlayer player, IAudioMixer mixer)
    {
        audioPlayer = player;
        audioMixer = mixer;
    }
}
```

### Testability Requirements

1. All public methods must be testable in EditMode or PlayMode
2. Use dependency injection for external dependencies
3. Avoid static state that persists between tests
4. Mock external systems using interfaces

```csharp
// Testable design with dependency injection
public interface IDataService
{
    Task<string> GetDataAsync();
}

public class BusinessLogic
{
    private readonly IDataService dataService;
    
    public BusinessLogic(IDataService dataService)
    {
        this.dataService = dataService;
    }
    
    public async Task<bool> ProcessAsync()
    {
        var data = await dataService.GetDataAsync();
        return !string.IsNullOrEmpty(data);
    }
}
```

## C# Unity Patterns

### UniTask for Async Testing (Preferred)

**Modern Approach**: Use UniTask for zero-allocation async/await testing.

```csharp
[UnityTest]
public IEnumerator MyAsyncTest() => UniTask.ToCoroutine(async () =>
{
    // Full try-catch support with async/await
    try
    {
        // Async operations with proper error handling
        await InitializeComponentsAsync();
        await UniTask.Delay(1000);
        
        var result = await ProcessDataAsync();
        Assert.IsNotNull(result);
    }
    catch (Exception ex)
    {
        Debug.LogError($"Test failed: {ex.Message}");
        throw;
    }
});
```

**Benefits of UniTask**:
- Zero allocation after initial cache
- 150-290% performance improvement over coroutines
- Full exception handling support
- Cancellation token support
- Works on all platforms including WebGL

### Async Best Practices

#### NEVER Use async void
```csharp
// BAD - async void can crash Unity
public async void ProcessDataBad()
{
    await UniTask.Delay(100);
    // Exception here will crash Unity!
}

// GOOD - Use UniTask or UniTaskVoid
public async UniTask ProcessDataGood()
{
    await UniTask.Delay(100);
    // Exceptions are properly handled
}

// GOOD - For fire-and-forget with proper error handling
public async UniTaskVoid ProcessDataFireAndForget()
{
    try
    {
        await UniTask.Delay(100);
    }
    catch (Exception ex)
    {
        Debug.LogError($"Error: {ex.Message}");
    }
}
```

#### Stay on Main Thread for Unity APIs
```csharp
// GOOD - Ensure Unity API calls are on main thread
public async UniTask UpdateGameObject(GameObject obj)
{
    // Switch to main thread before Unity API calls
    await UniTask.SwitchToMainThread();
    
    obj.transform.position = Vector3.zero;
    obj.SetActive(true);
    
    // Can do CPU work on thread pool
    await UniTask.RunOnThreadPool(() =>
    {
        // Heavy computation here
    });
    
    // Switch back to main thread for Unity APIs
    await UniTask.SwitchToMainThread();
    obj.transform.rotation = Quaternion.identity;
}
```

#### Use Cancellation Tokens
```csharp
// GOOD - Always support cancellation
public async UniTask<T> LoadDataAsync(CancellationToken cancellationToken = default)
{
    // Check for cancellation
    cancellationToken.ThrowIfCancellationRequested();
    
    // Pass token through async calls
    var data = await FetchAsync(cancellationToken);
    
    // Check again after async operations
    cancellationToken.ThrowIfCancellationRequested();
    
    return ProcessData(data);
}
```

### ⚠️ Legacy Coroutine Pattern (Avoid When Possible)

> **Note**: Only use this pattern when UniTask is not available. Always prefer UniTask for new code.

```csharp
// ⚠️ LEGACY PATTERN - Use only if UniTask unavailable
[UnityTest]
public IEnumerator LegacyTest()
{
    string testName = "LegacyTest";
    Exception caughtException = null;
    
    // Yields MUST be OUTSIDE try blocks (CS1626)
    yield return InitializeComponents();
    
    try
    {
        // Non-yielding code only
        DoSomething();
    }
    catch (Exception ex)
    {
        caughtException = ex;
        Debug.LogError($"[TEST-ERROR] {testName}: {ex.Message}");
    }
    
    if (caughtException != null)
        throw caughtException;
}
```

### Implementation Approach
**IMPORTANT**: If you are pivoting to another implementation, halt and ask the user about this pivot first.

### Code Requirements
### Component Order Matters
Some Unity components depend on execution order. Always verify:
1. Script execution order in Project Settings
2. Component order on GameObjects
3. Initialization sequence

### Coroutine Best Practices
- Use `WaitForSecondsRealtime` for timing (not `WaitForSeconds`)
- Handle exceptions with the pattern above
- Clean up coroutines in OnDestroy

### Performance Considerations
- Profile before optimizing
- Cache component references
- Minimize Update() logic
- Use object pooling for frequently created/destroyed objects

### Troubleshooting Tests

**Connection Closed Error**
- Unity needs compilation time
- Wait 5 seconds and retry

**No Completion Marker**
- Check if Unity is still running: `ps aux | grep Unity`
- Look for crash dumps in Temp/

**Irrelevant Console Logs**
- Filter logs by category
- Use `includeStackTrace=False`
- Check Unity console filters

## General Unity Patterns

## 🔧 Component References - FindVars Pattern

### ✅ FindVars Pattern (REQUIRED)

> **⚠️ CRITICAL**: This is the ONLY acceptable way to get component references in Unity!

```csharp
public class ExampleComponent : MonoBehaviour
{
    // ✅ ALL references MUST be SerializedField
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Transform target;
    
    // ✅ REQUIRED: FindVars method with ContextMenu
    [ContextMenu("Find Vars")]
    public void FindVars()
    {
        audioSource = GetComponent<AudioSource>();
        rb = GetComponent<Rigidbody>();
        target = transform.Find("Target");
    }
    
    void Awake()
    {
        // ✅ Components already assigned via FindVars
        // NO null checks needed for editor-assigned references
    }
}
```

### ❌ NEVER Do This

```csharp
// ❌ NEVER get components at runtime
void Start() {
    audioSource = GetComponent<AudioSource>(); // NO!
}

// ❌ NEVER add components at runtime
void Start() {
    gameObject.AddComponent<AudioSource>(); // NO!
}

// ❌ NEVER use reflection
var field = GetType().GetField("audioSource"); // NO!
```

### 🚫 No Reflection
- **DO NOT** use reflection in code
- If reflection seems necessary, ask the user first
- Use the FindVars pattern instead (see below)

#### Each new directory requires an asmdef
- Do not use GUIDs
- If you run into cyclical dependencies, the solution is MORE directories and more ASMDEFs.
- Keep this in mind as you architect so you can prevent it rather than fix post factum.

#### Component References - FindVars Pattern (REQUIRED)
**CRITICAL**: Cache ALL reference classes as `[SerializedField]` in a FindVars method with a `[ContextMenu]` attribute.

This method will be run at EDITOR TIME, not runtime. This is the ONLY acceptable way to get component references.

**Full Implementation Example:**
```csharp
public class MyComponent : MonoBehaviour
{
    // ALL component references MUST be SerializedField
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioStreamInput2D audioStreamInput2D;
    [SerializeField] private AudioStreamInput2DCapture_V2 audioCapture;
    [SerializeField] private AgoraPublisherV2_Simple publisher;
    [SerializeField] private Transform targetTransform;
    [SerializeField] private Rigidbody rb;
    
    // This method MUST exist for finding components
    [ContextMenu("Find Vars")]
    public void FindVars()
    {
        // Get components on same GameObject
        audioSource = GetComponent<AudioSource>();
        audioStreamInput2D = GetComponent<AudioStreamInput2D>();
        audioCapture = GetComponent<AudioStreamInput2DCapture_V2>();
        publisher = GetComponent<AgoraPublisherV2_Simple>();
        rb = GetComponent<Rigidbody>();
        
        // Get components in children
        targetTransform = transform.Find("Target");
        
        // Get components in parent
        var parentComponent = GetComponentInParent<SomeComponent>();
        
        // This runs at EDITOR TIME when you right-click the component
        // and select "Find Vars" from the context menu
    }
    
    void Awake()
    {
        // DO NOT use GetComponent here!
        // DO NOT use FindObjectOfType here!
        // DO NOT add components here!
        
        // Serialized components found via FindVars will fail naturally if null
        // No null checks needed for editor-assigned references
    }
}
```

**How to Use:**
1. Add the component to a GameObject
2. Right-click the component in Inspector
3. Select "Find Vars" from context menu
4. All references are now cached and serialized
5. Save the scene/prefab

**NEVER DO THIS:**
```csharp
// WRONG - Don't get components at runtime
void Start() {
    audioSource = GetComponent<AudioSource>(); // NO!
}

// WRONG - Don't add components at runtime
void Start() {
    gameObject.AddComponent<AudioSource>(); // NO!
}

// WRONG - Don't use reflection
var component = GetType().GetField("audioSource")... // NO!
```

#### Error Handling
- **NEVER** silence errors
- **DO NOT** add components at runtime - use FindVars instead
- Always log errors with full context

#### Logging Standards
Add logs with consistent prefixes:
```csharp
Debug.Log($"[TEST] General message");
Debug.Log($"[TEST-SETUP] Setup phase message");
Debug.Log($"[TEST-EXECUTE] Execution phase message");
Debug.LogError($"[TEST-ERROR] Error: {message}");
```


## 📚 Test Framework Documentation

> **📖 IMPORTANT**: Refer to these comprehensive guides for detailed testing patterns:

### 📘 Unity Test Execution Guide
**Location**: `Assets/TestFramework/Unity/UNIFIED_TEST_EXECUTION_GUIDE.md`

#### Key Topics Covered:
- ✅ UniTask setup and configuration
- ✅ Async/await test patterns
- ✅ Helper utilities and benchmarking
- ✅ Memory profiling and performance testing
- ✅ **TDD with Prefabs Pattern** (NEW)
  - Editor scripts for prefab creation
  - Test setup/teardown patterns
  - Component validation strategies

#### Prefab TDD Pattern Example:
```csharp
// 1️⃣ Editor Script Creates Prefab (Editor/PrefabFactories/)
[MenuItem("TestFramework/Prefabs/Create System Prefab")]
public static void CreateSystemPrefab()
{
    GameObject prefabRoot = new GameObject("System");
    // Configure components programmatically
    SetupComponents(prefabRoot);
    PrefabUtility.SaveAsPrefabAsset(prefabRoot, PREFAB_PATH);
    Object.DestroyImmediate(prefabRoot);
}

// 2️⃣ Test Instantiates in Setup (Tests/PlayMode/)
[SetUp]
public override void Setup()
{
    base.Setup();
    var prefab = Resources.Load<GameObject>("TestPrefabs/SystemPrefab");
    systemInstance = Object.Instantiate(prefab);
}

// 3️⃣ Test Destroys in TearDown
[TearDown]
public override void TearDown()
{
    if (systemInstance != null)
        Object.DestroyImmediate(systemInstance);
    base.TearDown();
}
```

### 📙 DOTS Test Execution Guide
**Location**: `Assets/TestFramework/DOTS/DOTS_TEST_EXECUTION_GUIDE.md`

#### Key Topics Covered:
- ✅ ECS world setup and teardown
- ✅ Job system testing with UniTask
- ✅ Burst compilation validation
- ✅ Performance profiling for DOTS
- ✅ Entity and system testing patterns

## 🎮 Test Framework Architecture

### Core Components

1. **Unity Test Framework** (`Assets/TestFramework/Unity/`)
   - Core test base classes and helpers
   - Coroutine and async/await support
   - Test lifecycle management

2. **DOTS Test Framework** (`Assets/TestFramework/DOTS/`)
   - ECS-specific test utilities
   - Job system test helpers
   - Burst compiler test support

3. **Custom Scripts** (`CustomScripts/`)
   - Automated refactoring agents
   - Batch processing scripts
   - Code quality tools

### Test Patterns

#### EditMode Tests with UniTask
```csharp
[TestFixture]
public class ExampleEditModeTests : UniTaskTestBase
{
    #region Setup and Teardown
    
    [SetUp]
    public override void Setup()
    {
        base.Setup();
        // Additional test initialization
    }
    
    [TearDown]
    public override void TearDown()
    {
        // Cleanup
        base.TearDown();
    }
    
    #endregion
    
    #region Tests
    
    [Test]
    public void Should_ValidateData_When_InputIsValid()
    {
        // Arrange
        var data = new TestData();
        
        // Act
        var result = ValidateData(data);
        
        // Assert
        Assert.IsTrue(result);
    }
    
    [UnityTest]
    public IEnumerator Should_ProcessAsync_WithUniTask() => UniTask.ToCoroutine(async () =>
    {
        // Arrange
        var processor = new DataProcessor();
        
        // Act - Full async/await support
        var result = await processor.ProcessAsync(testCancellationTokenSource.Token);
        
        // Assert
        Assert.IsNotNull(result);
        LogResult(true, "Async processing completed");
    });
    
    #endregion
}
```

#### PlayMode Tests with UniTask
```csharp
[TestFixture]
public class ExamplePlayModeTests : UniTaskTestBase
{
    #region UniTask Tests
    
    [UnityTest]
    public IEnumerator Should_ProcessAsync_When_ComponentIsActive() => UniTask.ToCoroutine(async () =>
    {
        // Arrange
        GameObject gameObject = null;
        try
        {
            gameObject = new GameObject("TestObject");
            var component = gameObject.AddComponent<TestComponent>();
            
            // Act - Use UniTask delays
            await UniTask.Delay(100);
            
            // Assert
            Assert.IsTrue(component.IsProcessed);
        }
        finally
        {
            // Cleanup
            if (gameObject != null)
                Object.Destroy(gameObject);
        }
    });
    
    [UnityTest]
    public IEnumerator Should_HandleTimeout_Gracefully() => UniTask.ToCoroutine(async () =>
    {
        // Test with timeout
        await AssertCompletesWithinAsync(async () =>
        {
            await SomeOperationAsync();
        }, timeoutMs: 5000);
    });
    
    #endregion
}
```

#### DOTS Tests with UniTask
```csharp
[TestFixture]
public class ExampleDOTSTests : DOTSTestBase
{
    #region UniTask DOTS Tests
    
    [UnityTest]
    public IEnumerator Should_CreateEntity_Async() => RunAsyncTest(async () =>
    {
        // Create entity asynchronously
        var entity = await CreateTestEntityAsync(typeof(Translation));
        
        // Validate entity
        var isValid = await ValidateEntityAsync(entity, e => entityManager.HasComponent<Translation>(e));
        Assert.IsTrue(isValid);
        
        // Measure performance
        var elapsed = await MeasureDOTSOperationAsync(async () =>
        {
            await WaitForFramesAsync(10);
        }, "Entity Processing");
        
        Assert.Less(elapsed, 0.1f, "Should complete within 100ms");
    });
    
    #endregion
}
```

## Running Tests

### Command Line
```bash
# Run EditMode tests
Unity -runTests -projectPath . -testPlatform EditMode

# Run PlayMode tests
Unity -runTests -projectPath . -testPlatform PlayMode

# Run specific test category
Unity -runTests -projectPath . -testPlatform EditMode -testFilter "CategoryName"
```

### In Unity Editor
1. Open Test Runner: Window > General > Test Runner
2. Select EditMode or PlayMode tab
3. Run all tests or specific fixtures

## 🤖 Refactoring Agents

> **📁 Location**: `.claude/agents/`
> **📚 Script Templates**: See `CustomScripts/README.md` for cross-platform bash script patterns

### Available Agents

#### 🔨 Refactor Agent
**File**: `.claude/agents/refactor-agent.md`
- **Purpose**: Monitors and splits C# files exceeding 750 lines
- **Capabilities**:
  - Automatic file size detection
  - Interface extraction patterns
  - Partial class organization
  - Strategy pattern implementation
  - SOLID principles enforcement
  - Test coverage preservation

#### 🔄 Batch Refactor Agent
**File**: `.claude/agents/batch-refactor-agent.md`
- **Purpose**: Batch processes multiple C# files
- **Capabilities**:
  - Region organization across files
  - Namespace updates (project-wide)
  - XML documentation generation
  - Code style enforcement
  - Async void to UniTask conversion
  - Test file generation with UniTask patterns
- **Script Reference**: Uses templates from `CustomScripts/README.md` for Windows compatibility

#### 🚀 DOTS Performance Profiler
**File**: `.claude/agents/dots-performance-profiler.md`
- **Purpose**: Analyzes Unity DOTS/ECS performance
- **Capabilities**:
  - Burst compilation efficiency analysis
  - Job scheduling optimization
  - NativeArray memory patterns
  - Cache line optimization
  - Unity Profiler integration

### Using Agents with Scripts

When agents need to execute bash scripts, they should follow the patterns in `CustomScripts/README.md`:

```bash
# Example: Agent using cross-platform script template
#!/bin/bash
# From CustomScripts/README.md template

set -e

# Platform-safe path resolution
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

# Use the script patterns from CustomScripts
# - Handle Windows paths correctly
# - Use 2>/dev/null for error suppression
# - Test file existence before processing
```

### 📜 Automated Operations

**Batch Operations** (via batch-refactor-agent):
- ➕ Add regions to C# files
- 📏 Check file sizes and complexity
- 🧪 Generate UniTask test templates
- ✨ Enforce code style standards
- 🔄 Convert async void to UniTask/UniTaskVoid
- 📝 Generate XML documentation
- 🔍 Analyze dependencies

**Performance Operations** (via dots-performance-profiler):
- ⚡ Profile Burst compilation
- 📊 Measure job execution times
- 💾 Track memory allocations
- 🎯 Identify bottlenecks

## 📊 Code Quality Guidelines

### 📏 File Size Limits
- **Maximum**: 750 lines per file
- **Strategy**: Use partial classes or extract interfaces
- **Rule**: One class per file (except nested types)

### 🎯 Method Complexity
- **Maximum**: 50 lines per method
- **Cyclomatic Complexity**: Under 10
- **Action**: Extract helper methods for clarity

### 🧪 Test Coverage Requirements
- **Minimum**: 80% code coverage
- **Mandatory**: All public APIs must have tests
- **Critical**: Integration tests for critical paths

## 🚨 Common Issues and Solutions

### Issue: CS1626 - Cannot yield in try block
**Solution**: Use UniTask.ToCoroutine() pattern (see Critical Unity Patterns above)

### Issue: async void crashes Unity
**Solution**: Always use UniTask or UniTaskVoid instead

### Issue: Unity API called from wrong thread
**Solution**: Use UniTask.SwitchToMainThread() before Unity API calls

### Issue: Components not found at runtime
**Solution**: Use FindVars pattern and assign in Editor

### Issue: Test cleanup not happening
**Solution**: Always use try-finally blocks with Object.DestroyImmediate in TearDown

## 📝 Important Reminders

> ⚠️ **ALWAYS** use UniTask instead of Task or coroutines for async operations

> ⚠️ **NEVER** use async void - use UniTask or UniTaskVoid

> ⚠️ **ALWAYS** stay on main thread for Unity API calls

> ⚠️ **NEVER** use reflection - use FindVars pattern

> ⚠️ **ALWAYS** create prefabs via Editor scripts for TDD

> 🚨 **NEVER** use Singleton MonoBehaviours - use ScriptableObjects, static classes, or POCOs instead

> ✅ **USE** static classes for utilities, POCOs for data, ScriptableObjects for configuration

> ⚠️ **REFER** to test guides for comprehensive patterns:
> - `Assets/TestFramework/Unity/UNIFIED_TEST_EXECUTION_GUIDE.md`
> - `Assets/TestFramework/DOTS/DOTS_TEST_EXECUTION_GUIDE.md`
