# Test Coordination Agent

> **Purpose**: Automate Unity test execution through SQLite database coordination between Python and Unity Editor.

## Capabilities

This agent specializes in:
- Setting up and managing SQLite test coordination systems
- Submitting test requests via Python scripts
- Monitoring test execution and results
- Troubleshooting PlayMode test completion issues
- Handling database synchronization between Python and Unity

## Key Features

### Database Coordination
- SQLite database at `Coordination/test_coordination.db`
- WAL mode for concurrent access
- Automatic polling every second in Unity
- Status tracking: pending → running → completed/failed

### Test Execution
- **EditMode Tests**: Direct execution with callback-based completion
- **PlayMode Tests**: File monitoring with post-Play mode detection
- Priority-based queue system
- Detailed result tracking

### Python Interface
```python
from Coordination.Scripts.test_coordinator import TestCoordinator, TestPlatform, TestRequestType

coordinator = TestCoordinator()
request_id = coordinator.submit_test_request(
    TestRequestType.ALL,
    TestPlatform.PLAY_MODE
)
status = coordinator.wait_for_completion(request_id, timeout=60)
coordinator.print_summary(request_id)
```

### Quick Commands
```bash
# Run all PlayMode tests
python Coordination/Scripts/quick_test.py all -p play --wait

# Check status
python Coordination/Scripts/quick_test.py status

# Run specific test class
python Coordination/Scripts/quick_test.py class MyTestClass -p edit
```

## Unity Components

### TestCoordinatorEditor
- Polls database every second
- Processes pending requests
- Manages test execution flow

### TestExecutor
- Dual-detection system (callbacks + file monitoring)
- Handles both EditMode and PlayMode tests
- Automatic fallback mechanisms

### PlayModeTestCompletionChecker
- Monitors Play mode state changes
- Detects test completion after exiting Play mode
- Updates database with results

## Common Issues & Solutions

### PlayMode Tests Not Completing
**Issue**: Tests stuck in "running" status after PlayMode execution

**Solution**:
1. EditorApplication.update callbacks don't run during Play mode
2. PlayModeTestCompletionChecker detects completion on Play mode exit
3. Use "Test Coordination > Debug > Check PlayMode Completion Now" for manual check

### Database Connection Issues
**Issue**: Unity not picking up pending requests

**Solution**:
1. Check Unity Console for SQLiteManager errors
2. Use "Test Coordination > Debug > Test Database Connection"
3. Ensure polling is enabled via "Test Coordination > Toggle Polling"
4. Verify database exists at `Coordination/test_coordination.db`

### File Monitoring Not Working
**Issue**: Test results generated but status not updated

**Solution**:
1. File monitoring requires Unity to be in Edit mode
2. Check `TestResults/` directory for XML files
3. Ensure TestExecutor has proper file system permissions
4. Manual trigger: `PlayModeTestCompletionChecker.ManualCheck()`

## Database Schema

### test_requests
```sql
id INTEGER PRIMARY KEY
request_type TEXT -- 'all', 'class', 'method', 'category'
test_filter TEXT
test_platform TEXT -- 'EditMode', 'PlayMode', 'Both'
status TEXT -- 'pending', 'running', 'completed', 'failed'
priority INTEGER
created_at TIMESTAMP
started_at TIMESTAMP
completed_at TIMESTAMP
total_tests INTEGER
passed_tests INTEGER
failed_tests INTEGER
duration_seconds REAL
```

### test_results
```sql
id INTEGER PRIMARY KEY
request_id INTEGER
test_name TEXT
result TEXT -- 'Passed', 'Failed', 'Skipped'
duration_ms REAL
error_message TEXT
stack_trace TEXT
```

## Debug Menu Items

- **Test Coordination > Check Pending Requests**: Process pending requests
- **Test Coordination > View Database Status**: Database health check
- **Test Coordination > Debug Polling Status**: Check polling state
- **Test Coordination > Debug > Check PlayMode Completion Now**: Manual completion check
- **Test Coordination > Debug > Test Database Connection**: Verify connectivity

## Implementation Notes

### Threading Considerations
- Unity API calls must stay on main thread
- SQLite operations are thread-safe with WAL mode
- File monitoring uses EditorApplication.update

### Performance
- 1-second polling interval (adjustable)
- File checks every 2 seconds during monitoring
- 5-minute timeout for long-running tests

### Error Handling
- Comprehensive try-catch blocks in all callbacks
- Fallback mechanisms for failed callbacks
- Detailed execution logging in database

## Best Practices

1. **Always wait for PlayMode tests to exit Play mode** for status updates
2. **Check Unity Console** for [TestExecutor-FM] logs when debugging
3. **Use debug menu items** for manual intervention when needed
4. **Monitor TestResults directory** for generated XML files
5. **Keep database clean** - reset if corruption suspected

## Example Workflow

```bash
# 1. Initialize database (if needed)
python Coordination/Scripts/db_initializer.py

# 2. Submit test request
python Coordination/Scripts/quick_test.py all -p play --wait

# 3. Unity automatically:
#    - Picks up request
#    - Runs tests
#    - Updates status on completion

# 4. Check results
python Coordination/Scripts/quick_test.py status 1
```

## Related Files

- `Coordination/Scripts/test_coordinator.py` - Python interface
- `Assets/TestCoordination/Editor/TestCoordinatorEditor.cs` - Unity polling
- `Assets/TestCoordination/Editor/TestExecutor.cs` - Test execution
- `Assets/TestCoordination/Editor/PlayModeTestCompletionChecker.cs` - PlayMode detection
- `Assets/TestCoordination/Editor/SQLiteManager.cs` - Database operations