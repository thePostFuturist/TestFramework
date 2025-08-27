# Unity Test Result XML Export

## Quick Start

### 1. Enable XML Export (Editor)
```
Menu: TestFramework → Test Export → Enable XML Export
```

### 2. Command Line Usage
```bash
Unity.exe -batchmode -quit -runTests -testPlatform EditMode -testResultFile results.xml
```

### 3. Programmatic Usage
```csharp
// Register exporter
var exporter = TestResultXMLExporter.RegisterExporter("path/to/output.xml");

// Run tests with API
var api = ScriptableObject.CreateInstance<TestRunnerApi>();
api.RegisterCallbacks(exporter);
api.Execute(new ExecutionSettings(filter));
```

## Features

- **NUnit 3.0 XML Format** - Compatible with Jenkins, TeamCity, Azure DevOps
- **Automatic Export** - Hooks into Unity TestRunner API via ICallbacks
- **Command Line Support** - Full batch mode integration for CI/CD
- **Editor Integration** - Menu items for easy access
- **Summary Files** - Human-readable test summaries alongside XML

## CI/CD Examples

### Jenkins
```groovy
stage('Run Unity Tests') {
    sh 'Unity -batchmode -quit -runTests -testPlatform EditMode -testResultFile test-results.xml'
    junit 'test-results.xml'
}
```

### GitHub Actions
```yaml
- name: Run Unity Tests
  run: Unity -batchmode -quit -runTests -testPlatform EditMode -testResultFile results.xml
  
- name: Publish Test Results
  uses: EnricoMi/publish-unit-test-result-action@v2
  with:
    files: results.xml
```

### Azure DevOps
```yaml
- task: CmdLine@2
  inputs:
    script: Unity.exe -batchmode -quit -runTests -testPlatform EditMode -testResultFile results.xml

- task: PublishTestResults@2
  inputs:
    testResultsFormat: 'NUnit'
    testResultsFiles: 'results.xml'
```

## Command Line Options

- `-testPlatform [EditMode|PlayMode]` - Specify test platform
- `-testResultFile <path>` - Output XML file path
- `-testFilter <name>` - Filter tests by name
- `-testCategories <cat1,cat2>` - Filter by categories

## XML Output Structure

```xml
<test-run testcasecount="50" result="Passed" total="50" passed="48" failed="2">
  <environment framework-version="2023.3.0f1" />
  <test-suite type="Assembly" name="Tests">
    <test-case name="TestMethod" result="Passed" duration="0.123" />
  </test-suite>
</test-run>
```

## File Locations

- **Default Output**: `ProjectRoot/TestResults/TestResults_[timestamp].xml`
- **Summary File**: `ProjectRoot/TestResults/TestResults_[timestamp].summary.txt`