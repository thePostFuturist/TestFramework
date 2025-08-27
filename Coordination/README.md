# Unity Test Coordination System

A SQLite-based coordination system that allows external tools (like Claude Code) to trigger Unity test execution through database polling.

## Architecture

- **SQLite Database**: Central coordination point at `Coordination/test_coordination.db`
- **Python Scripts**: Submit test requests and monitor results
- **Unity Editor**: Polls database every second for pending requests and executes tests

## Quick Start

### 1. Database is already initialized

The database has been created with all required tables.

### 2. Submit a test request from Python

```bash
# Run all EditMode tests
python Coordination/Scripts/quick_test.py all -p edit

# Run specific test class
python Coordination/Scripts/quick_test.py class MyTestClass -p edit

# Run specific test method
python Coordination/Scripts/quick_test.py method MyTestClass.TestMethod -p edit

# Run tests by category
python Coordination/Scripts/quick_test.py category Integration -p both

# Check status of request
python Coordination/Scripts/quick_test.py status 1

# Cancel a request
python Coordination/Scripts/quick_test.py cancel 1
```

### 3. Unity automatically picks up and executes tests

Unity Editor polls the database every second and will:
1. Find pending requests
2. Update status to "running"
3. Execute tests with appropriate filters
4. Save individual test results
5. Update status to "completed" or "failed"

## Python API

### Basic Usage

```python
from Coordination.Scripts.test_coordinator import TestCoordinator, TestPlatform, TestRequestType

coordinator = TestCoordinator()

# Submit a request
request_id = coordinator.submit_test_request(
    TestRequestType.ALL,
    TestPlatform.EDIT_MODE
)

# Wait for completion
status = coordinator.wait_for_completion(request_id, timeout=300)

# Get detailed results
results = coordinator.get_test_results(request_id)
coordinator.print_summary(request_id)
```

### Quick Functions

```python
from Coordination.Scripts.test_coordinator import run_all_tests, run_test_class

# Quick test execution
request_id = run_all_tests(TestPlatform.BOTH)
request_id = run_test_class("MyTestClass", TestPlatform.EDIT_MODE)
```

## Unity Menu Items

- **Test Coordination > Check Pending Requests**: Manually check for pending requests
- **Test Coordination > View Database Status**: Show database and system status
- **Test Coordination > Cancel Current Test**: Cancel running test
- **Test Coordination > Toggle Polling**: Enable/disable automatic polling
- **Test Coordination > Debug Polling Status**: Check polling state and timing
- **Test Coordination > Debug > Test Database Connection**: Verify database connectivity
- **Test Coordination > Debug > Check PlayMode Completion Now**: Manually check for completed PlayMode tests
- **Test Coordination > Debug > Manually Process Next Request**: Force process a pending request

## Database Schema

### test_requests
- Main table for test execution requests
- Tracks status: pending, running, completed, failed, cancelled
- Stores test results summary

### test_results
- Individual test results with pass/fail status
- Error messages and stack traces
- Test duration in milliseconds

### execution_log
- Detailed logging from both Python and Unity
- Debug information for troubleshooting

### system_status
- Component heartbeats (Python, Unity, Database)
- System health monitoring

## Features

- ✅ Concurrent-safe SQLite with WAL mode
- ✅ Priority-based test queue
- ✅ Detailed test result tracking
- ✅ Automatic Unity Editor polling
- ✅ Python CLI interface
- ✅ Comprehensive error handling
- ✅ System health monitoring
- ✅ PlayMode test completion detection
- ✅ File monitoring fallback for reliability
- ✅ Automatic status updates on Play mode exit

## How It Works

### EditMode Tests
1. Python submits request to SQLite database
2. Unity polls database every second
3. TestExecutor runs tests using TestRunnerApi
4. Callbacks fire on completion
5. Status updated to "completed" with results

### PlayMode Tests
1. Python submits request to SQLite database
2. Unity polls database and starts PlayMode tests
3. EditorApplication.update callbacks pause during Play mode
4. When Unity exits Play mode, PlayModeTestCompletionChecker activates
5. Checks for new test result files and updates database
6. Status updated to "completed" with results

## Troubleshooting

### Unity not picking up requests
1. Check Unity Console for errors
2. Use menu "Test Coordination > View Database Status"
3. Ensure polling is enabled (menu "Test Coordination > Toggle Polling")
4. Check database exists at `Coordination/test_coordination.db`
5. Try "Test Coordination > Debug > Test Database Connection"

### PlayMode tests stuck in "running" status
- Use menu "Test Coordination > Debug > Check PlayMode Completion Now"
- This manually triggers the completion check
- Should detect any test results files and update status

### Database locked errors
- SQLite WAL mode should prevent most locking
- If persistent, close Unity and Python scripts, then retry

### Reset database
```bash
python Coordination/Scripts/db_initializer.py reset
```