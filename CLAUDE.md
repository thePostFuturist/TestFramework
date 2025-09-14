## 🚨 CRITICAL: Package-Only Edits
**ALWAYS edit files in the package location ONLY:**
- ✅ Edit: `Packages/com.digitraver.perspec/...`
- ❌ NEVER edit: `PerSpec/...` (these are auto-synced from package)

The `PerSpec/` directory is automatically synchronized from the package.
Any edits outside the package will be lost on next sync.

### ⚠️ Python Scripts Location
- **Package location**: `Packages/com.digitraver.perspec/Editor/Coordination/Scripts/` - EDIT HERE
- **Working copy**: `PerSpec/Coordination/Scripts/` - DO NOT EDIT (auto-copied from package)
- Python scripts in PerSpec are COPIES that will be automatically synchronized
- NEVER work directly in `PerSpec/Coordination/Scripts/`

## 📦 Package Version Management

### When to Update Version
| Change Type | Version Bump | Example |
|------------|--------------|----------|
| Bug fixes | Patch (x.x.1) | 1.2.0 → 1.2.1 |
| New features | Minor (x.1.x) | 1.2.0 → 1.3.0 |
| Breaking changes | Major (1.x.x) | 1.2.0 → 2.0.0 |

### Required Updates for ANY Package Changes:
1. **Update package.json version**:
   ```json
   "version": "1.2.1",  // Increment appropriately
   ```

2. **Add CHANGELOG.md entry** with:
   - Version number and date
   - Section headers: Added, Fixed, Changed, Improved
   - Clear description of changes
   - Technical details for fixes

### Changelog Template:
```markdown
## [1.2.1] - YYYY-MM-DD

### Fixed
- **Issue Title**
  - What was broken
  - How it was fixed
  - Technical details if relevant

### Added
- **Feature Name**
  - What it does
  - How to use it
```


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

| User Says           | Execute                                                                                     |
| ------------------- | ------------------------------------------------------------------------------------------- |
| "show/get errors"   | `python PerSpec/Coordination/Scripts/monitor_editmode_logs.py --errors`                     |
| "run tests"         | `python PerSpec/Coordination/Scripts/quick_test.py all -p edit --wait`                      |
| "refresh Unity"     | `python PerSpec/Coordination/Scripts/quick_refresh.py full --wait`                          |
| "show logs"         | `python PerSpec/Coordination/Scripts/monitor_editmode_logs.py recent -n 50`                |
| "export logs"       | `python PerSpec/Coordination/Scripts/monitor_editmode_logs.py sessions`                     |
| "monitor logs live" | `python PerSpec/Coordination/Scripts/monitor_editmode_logs.py live`                         |
| "test results"      | `python PerSpec/Coordination/Scripts/test_results.py latest`                                |
| "show test results" | `python PerSpec/Coordination/Scripts/test_results.py latest -v`                             |
| "failed tests"      | `python PerSpec/Coordination/Scripts/test_results.py failed`                                |
| "open console"      | `python PerSpec/Coordination/Scripts/quick_menu.py execute "Window/General/Console" --wait` |
| "save project"      | `python PerSpec/Coordination/Scripts/quick_menu.py execute "File/Save Project" --wait`      |
| "clear logs"        | `python PerSpec/Coordination/Scripts/quick_clean.py quick`                                  |
| "clean database"    | `python PerSpec/Coordination/Scripts/quick_clean.py all --keep 0.5`                         |
| "show playmode logs"| `python PerSpec/Coordination/Scripts/test_playmode_logs.py`                                 |
| "show playmode errors"| `python PerSpec/Coordination/Scripts/test_playmode_logs.py --errors`                       |
| "export scene"      | `python PerSpec/Coordination/Scripts/scene_hierarchy.py export full --wait`                 |
| "export hierarchy"  | `python PerSpec/Coordination/Scripts/scene_hierarchy.py export full --wait --show`          |
| "export gameobject" | `python PerSpec/Coordination/Scripts/scene_hierarchy.py export object <path> --wait`        |
| "show scene json"   | `python PerSpec/Coordination/Scripts/scene_hierarchy.py latest --show`                      |
| "list scene exports"| `python PerSpec/Coordination/Scripts/scene_hierarchy.py list`                               |

**Intent Mapping:**
- "Something wrong" → Check errors
- "Tests failing" → Run with verbose: `quick_test.py all -v --wait`
- "Unity not responding" → Refresh Unity
- **Timeout?** → Tell user to click Unity window for focus
- **DOTS world null?** → Ensure using DOTSTestBase
- **Database too large?** → Run: `quick_clean.py quick`
- **PlayMode logs?** → Check `PerSpec/PlayModeLogs/` directory

## 📊 Log Monitoring

### EditMode Logs
```bash
# View recent logs from current session
python PerSpec/Coordination/Scripts/monitor_editmode_logs.py recent -n 50

# Show only compilation errors (CS errors) from all sessions
python PerSpec/Coordination/Scripts/monitor_editmode_logs.py --errors

# Show ALL errors and exceptions from all sessions
python PerSpec/Coordination/Scripts/monitor_editmode_logs.py --all-errors

# Show compilation errors with stack traces
python PerSpec/Coordination/Scripts/monitor_editmode_logs.py --errors -s

# Monitor logs in real-time
python PerSpec/Coordination/Scripts/monitor_editmode_logs.py live

# List all sessions (keeps 3 most recent)
python PerSpec/Coordination/Scripts/monitor_editmode_logs.py sessions

# Bypass line limit for grep/filtering (useful for searching)
python PerSpec/Coordination/Scripts/monitor_editmode_logs.py --no-limit | grep "PATTERN"
```

### PlayMode Logs
```bash
# View PlayMode logs
python PerSpec/Coordination/Scripts/test_playmode_logs.py

# Show only compilation errors (CS errors)
python PerSpec/Coordination/Scripts/test_playmode_logs.py --errors

# Show ALL errors and exceptions
python PerSpec/Coordination/Scripts/test_playmode_logs.py --all-errors

# List available sessions
python PerSpec/Coordination/Scripts/test_playmode_logs.py -l

# View compilation errors with stack traces
python PerSpec/Coordination/Scripts/test_playmode_logs.py -s --errors

# View all errors with stack traces
python PerSpec/Coordination/Scripts/test_playmode_logs.py -s --all-errors

# Bypass default 50 line limit for grep/filtering
python PerSpec/Coordination/Scripts/test_playmode_logs.py --no-limit | grep "PATTERN"
```

**Note:** Logs are now stored as files, not in database. EditMode keeps 3 sessions, PlayMode clears on entry.

## 📈 Test Results Viewer

### View Test Results
```bash
# Show latest test results with summary
python PerSpec/Coordination/Scripts/test_results.py latest

# Show latest with detailed output (all tests)
python PerSpec/Coordination/Scripts/test_results.py latest -v

# Show latest as JSON
python PerSpec/Coordination/Scripts/test_results.py latest --json

# List available test result files
python PerSpec/Coordination/Scripts/test_results.py list -n 20

# Show specific test result file
python PerSpec/Coordination/Scripts/test_results.py show TestResults_20250912_141430.xml -v

# Show only failed tests from recent runs
python PerSpec/Coordination/Scripts/test_results.py failed -n 5 -v

# Show statistics from recent test runs
python PerSpec/Coordination/Scripts/test_results.py stats -n 10

# Clean old test result files (keep 10 most recent)
python PerSpec/Coordination/Scripts/test_results.py clean --keep 10 --confirm
```

**Note:** Test results are stored as XML files in `PerSpec/TestResults/`. The viewer parses these files to show test outcomes, durations, and failure details.

## 🗑️ Database Maintenance

### Manual Cleanup Commands
```bash
# Quick cleanup (clear all logs + compact database)
python PerSpec/Coordination/Scripts/quick_clean.py quick

# Clear only console logs
python PerSpec/Coordination/Scripts/quick_clean.py logs --keep 0

# Clean all old data (keep last 30 minutes)
python PerSpec/Coordination/Scripts/quick_clean.py all --keep 0.5

# Show database statistics
python PerSpec/Coordination/Scripts/quick_clean.py stats
```

### Automatic Settings
- Console logs retention: **30 minutes**
- Cleanup frequency: **Every 15 minutes**
- Database size trigger: **50 MB**

### After Package Updates
```bash
# Run migration to add new tables
python PerSpec/Coordination/Scripts/db_migrate.py

# Update status constraint for new test states (if needed)
python PerSpec/Coordination/Scripts/db_update_status_constraint.py
```

**When to run db_update_status_constraint.py:**
- After updating from older PerSpec versions
- If you see errors like: `CHECK constraint failed: status IN ('pending', 'running', 'completed', 'failed', 'cancelled')`
- When tests show wrong status or complete prematurely

## 🚀 TDD Workflow

### 🛑 CRITICAL: NEVER SKIP THESE STEPS!
**⚠️ WARNING**: Skipping ANY step will cause test failures and waste time!

### 📌 MANDATORY 4-Step Process (ALWAYS FOLLOW IN ORDER!)
```bash
# 1. Write tests & code

# 2. ⚡ ALWAYS REFRESH UNITY FIRST! (DO NOT SKIP!)
python PerSpec/Coordination/Scripts/quick_refresh.py full --wait
# ❌ NEVER run tests without refreshing - Unity won't see your changes!

# 3. 🚨 MANDATORY: Check compilation errors (NEVER SKIP THIS!)
python PerSpec/Coordination/Scripts/monitor_editmode_logs.py --errors
# ⛔ STOP HERE if ANY errors exist!
# ❌ DO NOT PROCEED to step 4 if compilation errors exist!
# Tests will be INCONCLUSIVE if code doesn't compile

# 4. Run tests ONLY after steps 2 & 3 succeed
python PerSpec/Coordination/Scripts/quick_test.py all -p edit --wait
```

### ❌ COMMON MISTAKES TO AVOID:
- **Running tests without refreshing Unity** → Tests run old code
- **Skipping error check** → Tests show INCONCLUSIVE
- **Running tests with compilation errors** → Wastes time, tests can't run
- **Not waiting for refresh to complete** → Tests run partially updated code

### 📋 Step-by-Step Checklist (USE THIS EVERY TIME!):
☐ Code written/modified
☐ Unity refreshed (`quick_refresh.py full --wait`)
☐ Compilation checked (`monitor_editmode_logs.py --errors`)
☐ No errors found (or all fixed)
☐ Tests executed (`quick_test.py`)

### ⚠️ Understanding Test Status
**IMPORTANT**: The "completed" status means:
- ✅ Test request was processed by Unity
- ✅ Test results XML file was found and parsed
- ❌ NOT that tests finished executing in Unity

The `--wait` flag only waits for the request to be processed, not for test execution to complete.
Unity may still be running tests even after status shows "completed".

To ensure tests are truly finished:
1. Check Unity Test Runner window visually
2. Look for "Test run finished" in Unity console
3. Check if Unity Editor is still busy (spinner in tab)

**🚨 CRITICAL PROTOCOL**: 
1. **ALWAYS** refresh Unity after ANY code change
2. **ALWAYS** check for compilation errors BEFORE running tests
3. **NEVER** skip the error check - even for "simple" changes
4. If compilation errors exist:
   - Tests CANNOT run and will be marked INCONCLUSIVE
   - You MUST fix ALL compilation errors before running tests
   - Check errors with: `python PerSpec/Coordination/Scripts/monitor_editmode_logs.py --errors`
   - After fixing errors, go back to step 2 (refresh Unity)

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

## 🌳 Scene Hierarchy Export

### Export Unity Scene to JSON
```bash
# Export full scene hierarchy
python PerSpec/Coordination/Scripts/scene_hierarchy.py export full --wait

# Export specific GameObject and its children
python PerSpec/Coordination/Scripts/scene_hierarchy.py export object "Player" --wait

# Export and display JSON
python PerSpec/Coordination/Scripts/scene_hierarchy.py export full --wait --show

# View latest export
python PerSpec/Coordination/Scripts/scene_hierarchy.py latest --show

# List export files
python PerSpec/Coordination/Scripts/scene_hierarchy.py list
```

### JSON Output Features
- **Full component serialization** - No GUIDs, actual script names and values
- **Transform data** - Position, rotation, scale arrays
- **Component properties** - Serialized using Unity's SerializedObject
- **Recursive hierarchy** - Complete GameObject tree with children
- **Inactive GameObjects** - Optional inclusion (use `--no-inactive` to exclude)

### Setup Required (One-Time)
```bash
# Add scene hierarchy table to database
python PerSpec/Coordination/Scripts/add_scene_hierarchy_table.py
```

**Output Location:** `PerSpec/SceneHierarchy/hierarchy_[timestamp].json`

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

## 🎮 Unity Menu Execution

### Execute Unity Menu Items from Python
```bash
# List available menu items
python PerSpec/Coordination/Scripts/quick_menu.py list

# Execute a menu item
python PerSpec/Coordination/Scripts/quick_menu.py execute "Menu/Path" --wait

# Check status of request
python PerSpec/Coordination/Scripts/quick_menu.py status <request_id>

# Cancel pending request
python PerSpec/Coordination/Scripts/quick_menu.py cancel <request_id>
```

### Common Menu Items
| Menu Path | Purpose |
|-----------|---------|
| `Assets/Refresh` | Refresh asset database |
| `Assets/Create/C# Script` | Create new C# script |
| `Assets/Create/Folder` | Create new folder |
| `File/Save Project` | Save all project files |
| `Edit/Play` | Enter play mode |
| `Edit/Pause` | Pause play mode |
| `Edit/Stop` | Exit play mode |
| `Window/General/Console` | Open console window |
| `Window/General/Test Runner` | Open test runner |

**Notes:**
- Menu items that open dialogs will timeout after 30 seconds
- Use `--wait` flag to wait for completion
- Use `--timeout N` to set custom timeout in seconds
- Higher priority requests execute first

## 📊 Quick Reference

### 🔴 STOP! Before Running ANY Test:
1. Did you refresh Unity? → If no, run `quick_refresh.py full --wait`
2. Did you check for errors? → If no, run `monitor_editmode_logs.py --errors`
3. Are there compilation errors? → If yes, FIX THEM FIRST
4. Only NOW can you run tests → `quick_test.py all -p edit --wait`

### Compilation Error Handling
| Situation | Action | Command |
|-----------|--------|---------|
| **BEFORE ANY TEST** | **ALWAYS refresh Unity** | `quick_refresh.py full --wait` |
| **After EVERY refresh** | **ALWAYS check errors** | `monitor_editmode_logs.py --errors` |
| Errors found | FIX before testing | Do NOT run tests |
| Tests show "inconclusive" | Check compilation | `monitor_editmode_logs.py --errors` |
| Tests timeout | Check Unity focus + errors | Click Unity + check errors |

**Test Result States:**
- **PASSED**: Test executed and succeeded
- **FAILED**: Test executed but assertion failed  
- **INCONCLUSIVE**: Test couldn't run (compilation error/timeout)
- **SKIPPED**: Test intentionally not run

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
│   ├── PlayModeLogs/                  # PlayMode console logs (auto-cleared)
│   └── test_coordination.db           # SQLite
└── CustomScripts/Output/              # Generated
```

### PlayMode Log Capture
- **Location**: `PerSpec/PlayModeLogs/` - auto-cleared on Play Mode enter
- **Writing**: Every 5 seconds + final flush on exit
- **Performance**: No PlayerPrefs, no Update() - 95% faster
- **View logs**: `python PerSpec/Coordination/Scripts/test_playmode_logs.py`

### EditMode Log Capture
- **Location**: `PerSpec/EditModeLogs/` - session-based files
- **Sessions**: Keeps 3 most recent sessions, older auto-deleted
- **Writing**: Immediate on log receive (no buffering)
- **Compilation**: Errors captured even during compilation failures
- **View logs**: `python PerSpec/Coordination/Scripts/monitor_editmode_logs.py`
  - `recent -n 50` - Show last 50 logs
  - `--errors` - Show only compilation errors (CS errors)
  - `--all-errors` - Show all errors and exceptions
  - `live` - Monitor in real-time
  - `sessions` - List all sessions

### Available Agents
- **test-writer-agent**: Comprehensive tests with TDD
- **refactor-agent**: Split large files, SOLID
- **batch-refactor-agent**: Batch C# processing
- **dots-performance-profiler**: DOTS/ECS analysis
- **architecture-agent**: Document architecture

## 📝 Critical Reminders

> **🔴 BEFORE RUNNING TESTS:** ALWAYS refresh Unity AND check for errors!  
> **Pivoting?** Ask user first  
> **New directory?** Needs asmdef  
> **Errors?** Log with context  
> **Test prefabs?** Use Editor scripts  
> **Tests failing?** Did you refresh Unity? Did you check for compilation errors?
<!-- PERSPEC_CONFIG_END -->
<!-- PERSPEC_CONFIG_END -->
<!-- PERSPEC_CONFIG_END -->
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
