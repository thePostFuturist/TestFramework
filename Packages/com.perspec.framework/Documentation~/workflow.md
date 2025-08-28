# The 4-Step PerSpec Workflow

## Overview

The PerSpec workflow ensures code quality through a consistent 4-step process:

1. **Write** - Create code and tests
2. **Refresh** - Update Unity assets
3. **Check** - Verify compilation
4. **Test** - Execute tests

## Step 1: Write Code/Tests

Write your implementation and tests using PerSpec base classes:

```csharp
using PerSpec.Runtime.Unity;

public class FeatureTests : UniTaskTestBase
{
    [UnityTest]
    public IEnumerator TestFeature() => UniTask.ToCoroutine(async () =>
    {
        // Your test implementation
    });
}
```

## Step 2: Refresh Unity

Force Unity to recompile and refresh assets:

```bash
python PerSpec/scripts/refresh.bat full --wait   # Windows
./PerSpec/scripts/refresh.sh full --wait         # Mac/Linux
```

**Options:**
- `full` - Complete reimport
- `scripts` - Scripts only
- `--wait` - Wait for completion

## Step 3: Check Compilation

Verify no compilation errors exist:

```bash
python PerSpec/scripts/logs.bat errors   # Windows
./PerSpec/scripts/logs.sh errors         # Mac/Linux
```

**Must see:** "No error logs found" before proceeding.

## Step 4: Run Tests

Execute your tests:

```bash
python PerSpec/scripts/test.bat all -p edit --wait   # Windows
./PerSpec/scripts/test.sh all -p edit --wait         # Mac/Linux
```

**Options:**
- `all` - Run all tests
- `class ClassName` - Specific class
- `method MethodName` - Specific method
- `-p edit` - EditMode tests
- `-p play` - PlayMode tests
- `-p both` - Both modes

## Complete Example

```bash
# 1. Write your feature code
# 2. Refresh Unity
python PerSpec/scripts/refresh.bat full --wait

# 3. Check for errors (MUST be clean)
python PerSpec/scripts/logs.bat errors

# 4. Run tests
python PerSpec/scripts/test.bat all -p edit --wait
```

## Error Resolution

### Compilation Errors

If Step 3 shows errors:

1. Read error details:
   ```bash
   python PerSpec/scripts/logs.bat errors -v
   ```

2. Fix the errors in your code

3. Repeat from Step 2 (Refresh)

### Common Errors

| Error | Solution |
|-------|----------|
| CS1626 (yield in try) | Use `UniTask.ToCoroutine()` |
| UniTask not found | Add to asmdef references |
| Thread exception | Add `UniTask.SwitchToMainThread()` |

## Background Processing

PerSpec continues working when Unity loses focus:
- System.Threading.Timer polls database
- Tests execute in background
- Results update automatically

## Monitoring

### Live Monitoring

```bash
# Watch for new logs
python PerSpec/scripts/logs.bat monitor -l error

# Watch test execution
python PerSpec/scripts/test.bat all -p edit --monitor
```

### Status Checks

From Unity:
- `Tools > PerSpec > Debug > Database Status`
- `Tools > PerSpec > Debug > Polling Status`

## Best Practices

1. **Always follow all 4 steps** - Don't skip compilation check
2. **Fix errors immediately** - Don't accumulate technical debt
3. **Use --wait flags** - Ensure operations complete
4. **Monitor background tasks** - Check polling status regularly

## Automation

Create a batch script for the workflow:

```bash
#!/bin/bash
# perspec-test.sh

echo "Step 2: Refreshing Unity..."
python PerSpec/scripts/refresh.bat full --wait

echo "Step 3: Checking errors..."
python PerSpec/scripts/logs.bat errors
if [ $? -ne 0 ]; then
    echo "Errors found! Fix them first."
    exit 1
fi

echo "Step 4: Running tests..."
python PerSpec/scripts/test.bat all -p both --wait
```

## Troubleshooting

### Tests Not Running

```bash
# Check database
python PerSpec/scripts/logs.bat sessions

# Reset coordination
Tools > PerSpec > Debug > Force Reinitialize
```

### Slow Performance

```bash
# Use targeted refresh
python PerSpec/scripts/refresh.bat scripts --wait

# Run specific tests
python PerSpec/scripts/test.bat class MyTests -p edit
```

## Summary

The 4-step workflow ensures:
- ✅ Code compiles correctly
- ✅ Tests execute reliably
- ✅ Errors caught early
- ✅ Consistent quality

Always: **Write → Refresh → Check → Test**