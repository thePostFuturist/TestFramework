# Unity Test Export Scripts

This directory contains shell scripts for running Unity tests with XML export functionality, designed for both local development and CI/CD pipelines.

## 📁 Scripts Overview

| Script | Description | Platform |
|--------|-------------|----------|
| `run-unity-tests.bat` | Basic test runner with XML export (batch mode) | Windows |
| `run-unity-tests.ps1` | Advanced PowerShell runner with colored output | Windows |
| `run-unity-tests-advanced.bat` | Feature-rich batch script with options | Windows |
| `run-unity-tests.py` | Cross-platform Python script | Windows/Mac/Linux |
| `run-specific-tests.bat` | Targeted test runner for CI/CD | Windows |
| **`run-unity-tests-editor.bat`** | **Run tests in Unity Editor (no batch mode)** | **Windows** |

## 🚀 Quick Start

### Basic Usage
```batch
# Run tests in Unity Editor (recommended - avoids crashes)
run-unity-tests-editor.bat

# Run EditMode tests with batch mode
run-unity-tests.bat

# Run PlayMode tests
run-unity-tests-advanced.bat PlayMode

# Run specific test class
run-unity-tests.ps1 -TestFilter "MyNamespace.MyTestClass"

# Run tests by category
run-specific-tests.bat critical
```

### 🆕 Editor Mode (Recommended)
If you're experiencing crashes with batch mode (error 1073741845), use the Editor mode script:
```batch
# Run all tests in Unity Editor
run-unity-tests-editor.bat

# Run specific test suite
run-unity-tests-editor.bat unit
run-unity-tests-editor.bat integration
```

## 📝 Script Details

### 1. **run-unity-tests.bat** (Simple)
Basic test runner that automatically:
- Detects Unity installation
- Creates timestamped output files
- Exports XML results and logs
- Shows test summary

**Usage:**
```batch
run-unity-tests.bat
```

### 2. **run-unity-tests.ps1** (PowerShell)
Enhanced runner with:
- Colored console output
- XML parsing and result display
- Verbose mode for debugging
- Failed test details

**Usage:**
```powershell
.\run-unity-tests.ps1 [-TestPlatform EditMode|PlayMode] [-TestFilter "filter"] [-Verbose]

# Examples
.\run-unity-tests.ps1
.\run-unity-tests.ps1 -TestPlatform PlayMode
.\run-unity-tests.ps1 -TestFilter "PlayerTests" -Verbose
.\run-unity-tests.ps1 -OpenResults
```

### 3. **run-unity-tests-advanced.bat** (Advanced)
Full-featured batch script with:
- Command-line argument support
- Test filtering by name or category
- Unity version auto-detection
- Custom summary generation
- Duration tracking

**Usage:**
```batch
run-unity-tests-advanced.bat [Platform] [Filter] [Categories] [open]

# Examples
run-unity-tests-advanced.bat
run-unity-tests-advanced.bat EditMode
run-unity-tests-advanced.bat PlayMode "MyTests.PlayerTests"
run-unity-tests-advanced.bat EditMode "" "Integration,Critical"
run-unity-tests-advanced.bat EditMode "" "" open
```

### 4. **run-unity-tests.py** (Cross-Platform)
Python script that works on all platforms:
- Auto-detects OS and Unity installation
- Argument parsing with help
- XML result parsing
- Exit codes for CI/CD

**Usage:**
```bash
python run-unity-tests.py [options]

# Examples
python run-unity-tests.py
python run-unity-tests.py --platform PlayMode
python run-unity-tests.py --filter "TestNamespace.TestClass"
python run-unity-tests.py --categories "Unit,Integration"
python run-unity-tests.py --unity-version 2022.3.18f1 --verbose
```

### 5. **run-specific-tests.bat** (CI/CD Targeted)
Specialized for running specific test suites:
- Predefined test suite configurations
- CI/CD output formatting (TeamCity compatible)
- Separate output folders per suite
- JUnit XML generation

**Usage:**
```batch
run-specific-tests.bat <TestSuite> [Platform]

# Test Suites:
#   unit        - Unit tests
#   integration - Integration tests
#   dots        - DOTS/ECS tests
#   performance - Performance tests
#   critical    - Critical/smoke tests
#   all         - All tests
#   custom      - Custom filter

# Examples
run-specific-tests.bat unit
run-specific-tests.bat integration PlayMode
run-specific-tests.bat dots
run-specific-tests.bat custom "MyNamespace.SpecificTest" EditMode
```

## 📊 Output Files

All scripts generate the following in `TestResults/` directory:

| File | Description |
|------|-------------|
| `TestResults_[timestamp].xml` | NUnit 3.0 format XML results |
| `unity_[timestamp].log` | Unity console output |
| `TestResults_[timestamp].summary.txt` | Human-readable summary |

## ⚙️ Configuration

### Unity Version
Default: `6000.0.23f1`

To change, edit the `UNITY_VERSION` variable in each script:
```batch
set UNITY_VERSION=2022.3.18f1
```

### Project Path
Default: `D:\Dev\TestFramework`

To change, edit the `PROJECT_PATH` variable:
```batch
set PROJECT_PATH=C:\MyProject
```

## 🔧 CI/CD Integration

### Jenkins
```groovy
stage('Test') {
    bat 'CustomScripts\\Scripts\\unity-test-export\\run-unity-tests.bat'
    junit 'TestResults\\*.xml'
}
```

### GitHub Actions
```yaml
- name: Run Unity Tests
  run: |
    cd CustomScripts/Scripts/unity-test-export
    ./run-unity-tests.bat
  shell: cmd
  
- name: Upload Test Results
  uses: actions/upload-artifact@v3
  with:
    name: test-results
    path: TestResults/*.xml
```

### Azure DevOps
```yaml
- script: |
    cd CustomScripts\Scripts\unity-test-export
    run-specific-tests.bat unit
  displayName: 'Run Unit Tests'

- task: PublishTestResults@2
  inputs:
    testResultsFormat: 'NUnit'
    testResultsFiles: 'TestResults/**/*.xml'
```

### TeamCity
```xml
<build>
  <step>
    <runner type="simpleRunner">
      <parameters>
        <param name="script.content">
          cd CustomScripts\Scripts\unity-test-export
          run-specific-tests.bat critical
        </param>
      </parameters>
    </runner>
  </step>
</build>
```

## 🎯 Test Categories

Configure test categories in your Unity tests:
```csharp
[Test]
[Category("Unit")]
[Category("Critical")]
public void MyUnitTest() { }

[UnityTest]
[Category("Integration")]
public IEnumerator MyIntegrationTest() { }
```

Then run by category:
```batch
run-unity-tests-advanced.bat EditMode "" "Unit,Critical"
```

## 📈 Exit Codes

| Code | Meaning |
|------|---------|
| 0 | All tests passed |
| 1 | Test failures |
| 2 | Compilation errors |
| 3 | Unity license issue |
| >3 | Other errors |

## 🐛 Troubleshooting

### Unity Not Found
- Update `UNITY_VERSION` in scripts
- Check Unity Hub installation path
- Ensure Unity is installed at standard location

### No XML Output
- Check TestResults folder permissions
- Verify Unity Test Framework package is installed
- Check Unity console logs for errors

### Tests Not Running
- Ensure project compiles without errors
- Check test assembly definitions
- Verify test platform matches test attributes

## 📚 Requirements

- Unity 2020.3+ with Test Framework package
- Windows for .bat/.ps1 scripts
- Python 3.6+ for .py script
- Project at `D:\Dev\TestFramework` (or update path)

## 🔗 Related Documentation

- [Unity Test Framework](https://docs.unity3d.com/Packages/com.unity.test-framework@latest)
- [NUnit XML Format](https://github.com/nunit/docs/wiki/Test-Result-XML-Format)
- [Unity Command Line](https://docs.unity3d.com/Manual/CommandLineArguments.html)

### 6. **run-unity-tests-editor.bat** (Editor Mode - NEW)
Runs tests in existing Unity Editor instance without batch mode:
- Avoids batch mode crashes (error 1073741845)
- Visual progress in Unity Editor
- Better error reporting
- Can debug tests interactively

**Usage:**
```batch
run-unity-tests-editor.bat [TestSuite]

# Test Suites:
#   all         - All tests (default)
#   edit        - EditMode tests only
#   play        - PlayMode tests only
#   unit        - Unit tests
#   integration - Integration tests
#   critical    - Critical tests

# Examples
run-unity-tests-editor.bat
run-unity-tests-editor.bat unit
run-unity-tests-editor.bat play
```

## 🎯 Unity Editor Integration

The test system now includes direct Unity Editor integration:

### Menu Commands
Access via `TestFramework > Run Tests` menu:
- **Run All Tests (Editor Instance)** - Ctrl+Shift+Alt+T
- **Run EditMode Tests**
- **Run PlayMode Tests**
- **Run Unit Tests**
- **Run Integration Tests**
- **Run Critical Tests**
- **Open Test Runner Window**

### Test Runner Window
Open via `TestFramework > Run Tests > Open Test Runner Window`:
- Visual test configuration
- Filter by namespace/class/category
- Real-time progress display
- Recent results viewer
- Quick filter buttons

### Command Line via executeMethod
Run tests without batch mode using Unity's -executeMethod:
```batch
Unity.exe -projectPath "D:\Dev\TestFramework" -executeMethod TestRunnerCommandLine.RunAllTests
Unity.exe -projectPath "D:\Dev\TestFramework" -executeMethod TestRunnerCommandLine.RunUnitTests
```

## 💡 Tips

1. **Experiencing Crashes?**: Use `run-unity-tests-editor.bat` instead of batch mode scripts
2. **For Visual Feedback**: Use the Test Runner Window in Unity Editor
3. **For CI/CD**: Use `run-specific-tests.bat` for targeted testing
4. **For Debugging**: Use PowerShell script with `-Verbose` flag
5. **For Cross-Platform**: Use Python script
6. **For Quick Tests**: Use Editor menu commands

## 📄 License

These scripts are provided as-is for use with the TestFramework project.