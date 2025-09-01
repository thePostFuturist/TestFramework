# Custom Scripts and Agents

## Overview
This directory contains automated refactoring agents and utility scripts for maintaining clean, testable C# code in Unity projects.

## Directory Structure
```
CustomScripts/
├── Agents/          # Refactoring agent documentation
├── Scripts/         # Executable bash scripts
└── README.md        # This file
```

## Available Agents

### 1. Refactor Agent
**Location**: `Agents/RefactorAgent.md`  
**Purpose**: Monitors and splits C# files exceeding 750 lines  
**Key Features**:
- Automatic file size detection
- Multiple refactoring patterns (Interface, Partial Class, Strategy)
- Unity-specific considerations
- Test preservation strategies

### 2. Batch Refactor Agent  
**Location**: `Agents/BatchRefactorAgent.md`  
**Purpose**: Batch processing of multiple C# files  
**Key Features**:
- Region organization
- Namespace updates
- XML documentation generation
- Code style enforcement
- Test file generation

## Output Directory Convention

**IMPORTANT**: All generated files should be written to `CustomScripts/Output/` directory
- This directory is NOT in gitignore (tracked by version control)
- Create subdirectories within Output/ for organization
- Example: `CustomScripts/Output/Refactored/`, `CustomScripts/Output/Tests/`, `CustomScripts/Output/Reports/`

## Quick Start Scripts

### Check File Sizes
```bash
# Find all C# files over 750 lines and save report
mkdir -p ./CustomScripts/Output/Reports
find . -name "*.cs" -type f -exec wc -l {} + | awk '$1 > 750 {print $2 " - " $1 " lines"}' | sort -rn > ./CustomScripts/Output/Reports/large-files.txt
cat ./CustomScripts/Output/Reports/large-files.txt
```

### Add Regions to All Files
```bash
# Add standard region structure (outputs to Output/Refactored/)
bash ./CustomScripts/Scripts/add-regions.sh
```

### Generate Missing Tests
```bash
# Create test files for classes without tests (outputs to Output/Tests/)
bash ./CustomScripts/Scripts/generate-tests.sh
```

## Code Quality Standards

### File Size Limits
- **Maximum**: 750 lines per file
- **Recommended**: 400-500 lines
- **Minimum**: No minimum, but avoid tiny files

### Method Complexity
- **Maximum**: 50 lines per method
- **Cyclomatic Complexity**: Under 10
- **Parameters**: Maximum 5 parameters

### Test Coverage Requirements
- **Minimum**: 80% code coverage
- **Public APIs**: 100% coverage required
- **Private Methods**: Test through public interfaces

## Writing Custom Scripts

### Template for New Scripts
```bash
#!/bin/bash
# script-name.sh - Brief description

set -e  # Exit on error

# Configuration
readonly SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
readonly PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
readonly OUTPUT_DIR="$PROJECT_ROOT/CustomScripts/Output"

# Ensure output directory exists
mkdir -p "$OUTPUT_DIR"

# Functions
function process_file() {
    local file=$1
    echo "Processing: $file"
    # Add processing logic here
}

# Main execution
function main() {
    echo "Starting script..."
    echo "Output directory: $OUTPUT_DIR"
    
    # Find and process C# files
    find "$PROJECT_ROOT" -name "*.cs" -type f | while read -r file; do
        process_file "$file"
    done
    
    echo "Script completed successfully"
    echo "Results saved to: $OUTPUT_DIR"
}

# Run if executed directly
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    main "$@"
fi
```

## Best Practices

### Before Running Scripts
1. **Version Control**: Commit current changes
2. **Backup**: Create a backup branch
3. **Test First**: Run on a single file
4. **Review**: Check changes with `git diff`

### Script Development
1. **Idempotent**: Scripts should be safe to run multiple times
2. **Verbose**: Provide clear output about actions
3. **Reversible**: Include rollback instructions
4. **Documented**: Add comments and usage examples

## Integration Points

### Unity Editor Integration
Add menu items to run scripts from Unity:

```csharp
using UnityEditor;
using System.Diagnostics;

public static class RefactorMenu
{
    [MenuItem("Tools/Refactor/Check File Sizes")]
    public static void CheckFileSizes()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "bash",
            Arguments = "./CustomScripts/Scripts/check-sizes.sh",
            WorkingDirectory = Application.dataPath + "/..",
            UseShellExecute = false,
            RedirectStandardOutput = true
        };
        
        using (var process = Process.Start(startInfo))
        {
            string output = process.StandardOutput.ReadToEnd();
            UnityEngine.Debug.Log(output);
        }
    }
}
```

### CI/CD Integration
Example GitHub Actions workflow:

```yaml
name: Code Quality

on: [push, pull_request]

jobs:
  quality-check:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      
      - name: Check file sizes
        run: |
          large_files=$(find . -name "*.cs" -exec wc -l {} + | awk '$1 > 750')
          if [ ! -z "$large_files" ]; then
            echo "Files exceeding limit: $large_files"
            exit 1
          fi
      
      - name: Run quality scripts
        run: |
          bash ./CustomScripts/Scripts/check-quality.sh
```

## Troubleshooting

### Common Issues

#### Scripts Not Running on Windows
**Solution**: Use Git Bash or WSL
```bash
# Git Bash
bash ./script.sh

# WSL
wsl bash ./script.sh
```

### Windows-Specific Learnings (From Experience)

#### Path Handling Issues
**Problem**: Windows paths with backslashes cause issues in bash scripts
```bash
# ❌ This fails on Windows
readonly PROJECT_ROOT="D:\\Dev\\Tes
tFramework"

# ✅ Use forward slashes or let bash resolve paths
readonly PROJECT_ROOT="$(pwd)"
# Or
readonly PROJECT_ROOT="/d/Dev/TestFramework"
```

#### Find Command Differences
**Problem**: `find` behaves differently on Windows Git Bash
```bash
# ✅ Use explicit path resolution
find Assets/TestFramework -name "*.cs" -type f 2>/dev/null

# ✅ Handle missing files gracefully
for file in Assets/TestFramework/**/*.cs; do
    [ -f "$file" ] && analyze_file "$file"
done
```

#### Line Counting Issues
**Problem**: `wc -l` may have different output format
```bash
# ✅ Ensure consistent parsing
lines=$(wc -l < "$file" 2>/dev/null || echo 0)
```

#### Color Codes in Git Bash
**Problem**: ANSI color codes may not display correctly
```bash
# ✅ Test color support first
if [ -t 1 ]; then
    RED='\033[0;31m'
    GREEN='\033[0;32m'
    NC='\033[0m'
else
    RED=''
    GREEN=''
    NC=''
fi
```

#### Executable Permissions
**Problem**: Windows doesn't preserve Unix executable bits
```bash
# ✅ Always make scripts executable after cloning
chmod +x ./CustomScripts/Scripts/*.sh

# ✅ Or run explicitly with bash
bash ./CustomScripts/Scripts/script.sh
```

#### Best Practices for Cross-Platform Scripts
1. **Use `$(pwd)` instead of hardcoded paths**
2. **Test file existence before processing**
3. **Handle both `/` and `\` path separators**
4. **Use `2>/dev/null` to suppress Windows-specific errors**
5. **Provide fallback values for commands that might fail**

#### Example Cross-Platform Script Template
```bash
#!/bin/bash
# Works on Windows Git Bash, WSL, Linux, and macOS

set -e

# Platform detection
if [[ "$OSTYPE" == "msys" ]] || [[ "$OSTYPE" == "cygwin" ]]; then
    echo "Running on Windows (Git Bash/Cygwin)"
    PATH_SEP="/"
elif [[ "$OSTYPE" == "linux-gnu"* ]]; then
    echo "Running on Linux"
    PATH_SEP="/"
elif [[ "$OSTYPE" == "darwin"* ]]; then
    echo "Running on macOS"
    PATH_SEP="/"
fi

# Safe path resolution
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
OUTPUT_DIR="$PROJECT_ROOT/CustomScripts/Output"

# Ensure output directory exists
mkdir -p "$OUTPUT_DIR"

# Process files with error handling
process_files() {
    local pattern="${1:-*.cs}"
    local output_subdir="${2:-Processed}"
    local output_path="$OUTPUT_DIR/$output_subdir"
    local count=0
    
    # Create output subdirectory
    mkdir -p "$output_path"
    
    # Use find with proper error handling
    while IFS= read -r -d '' file; do
        if [ -f "$file" ]; then
            echo "Processing: $file"
            # Example: Copy processed file to output directory
            # cp "$file" "$output_path/"
            ((count++))
        fi
    done < <(find "$PROJECT_ROOT" -name "$pattern" -type f -print0 2>/dev/null)
    
    echo "Processed $count files"
    echo "Output saved to: $output_path"
}

# Main execution
main() {
    echo "Project root: $PROJECT_ROOT"
    echo "Output directory: $OUTPUT_DIR"
    process_files "*.cs" "CSharpFiles"
}

main "$@"
```

#### Permission Denied
**Solution**: Make scripts executable
```bash
chmod +x ./CustomScripts/Scripts/*.sh
```

#### File Encoding Issues
**Solution**: Ensure UTF-8 encoding
```bash
# Convert to UTF-8
iconv -f ISO-8859-1 -t UTF-8 input.cs > output.cs
```

## Contributing

### Adding New Agents
1. Create documentation in `Agents/`
2. Include example scripts
3. Add to this README
4. Test thoroughly

### Adding New Scripts
1. Follow the template structure
2. Add error handling
3. Include usage documentation
4. Test on multiple platforms

## Support

For issues or questions:
1. Check agent documentation
2. Review script comments
3. Test with verbose output
4. Create minimal reproduction case

## Version History

### v1.0.0 (Current)
- Initial refactoring agents
- Basic batch processing scripts
- Unity integration examples
- CI/CD templates