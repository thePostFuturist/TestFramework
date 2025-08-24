#!/bin/bash
# generate-test-report.sh - Generate test coverage report for C# files

set -e  # Exit on error

# Configuration
readonly SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
readonly PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
readonly OUTPUT_DIR="$PROJECT_ROOT/CustomScripts/Output"
readonly TESTS_DIR="$OUTPUT_DIR/TestReports"
readonly TIMESTAMP=$(date +"%Y%m%d_%H%M%S")

# Ensure output directories exist
mkdir -p "$TESTS_DIR"

# Function to check if a test file exists
check_test_file() {
    local source_file="$1"
    local filename=$(basename "$source_file" .cs)
    local test_file_patterns=(
        "*/${filename}Tests.cs"
        "*/${filename}Test.cs"
        "*/${filename}_Tests.cs"
        "*/${filename}_Test.cs"
    )
    
    for pattern in "${test_file_patterns[@]}"; do
        if find "$PROJECT_ROOT" -path "$pattern" -type f 2>/dev/null | grep -q .; then
            return 0
        fi
    done
    
    return 1
}

# Main execution
main() {
    echo "========================================="
    echo "Test Coverage Report Generator"
    echo "========================================="
    echo "Project Root: $PROJECT_ROOT"
    echo "Output Directory: $TESTS_DIR"
    echo "Timestamp: $TIMESTAMP"
    echo ""
    
    # Initialize report
    local report_file="$TESTS_DIR/test-coverage-${TIMESTAMP}.md"
    
    cat > "$report_file" << EOF
# Test Coverage Report
Generated: $(date)

## Summary

EOF
    
    local total_files=0
    local files_with_tests=0
    local files_without_tests=0
    
    echo "## Files With Tests" >> "$report_file"
    echo "" >> "$report_file"
    
    # Check test coverage for TestFramework
    echo "Analyzing test coverage..."
    
    while IFS= read -r file; do
        if [ -f "$file" ]; then
            ((total_files++))
            local relative_path="${file#$PROJECT_ROOT/}"
            local filename=$(basename "$file")
            
            # Skip test files themselves
            if [[ "$filename" == *"Test"* ]] || [[ "$filename" == *"Tests"* ]]; then
                continue
            fi
            
            if check_test_file "$file"; then
                ((files_with_tests++))
                echo "- ✅ $relative_path" >> "$report_file"
                echo "✅ $filename has tests"
            else
                ((files_without_tests++))
            fi
        fi
    done < <(find "$PROJECT_ROOT/Assets/TestFramework" -name "*.cs" -type f 2>/dev/null | grep -v "/Tests/")
    
    echo "" >> "$report_file"
    echo "## Files Without Tests" >> "$report_file"
    echo "" >> "$report_file"
    
    # Second pass for files without tests
    while IFS= read -r file; do
        if [ -f "$file" ]; then
            local relative_path="${file#$PROJECT_ROOT/}"
            local filename=$(basename "$file")
            
            # Skip test files themselves
            if [[ "$filename" == *"Test"* ]] || [[ "$filename" == *"Tests"* ]]; then
                continue
            fi
            
            if ! check_test_file "$file"; then
                echo "- ❌ $relative_path" >> "$report_file"
                echo "❌ $filename needs tests"
            fi
        fi
    done < <(find "$PROJECT_ROOT/Assets/TestFramework" -name "*.cs" -type f 2>/dev/null | grep -v "/Tests/")
    
    # Calculate coverage percentage
    local coverage=0
    if [ "$total_files" -gt 0 ]; then
        coverage=$((files_with_tests * 100 / total_files))
    fi
    
    # Update summary in report
    sed -i "s/## Summary/## Summary\n\n- Total Files: $total_files\n- Files with Tests: $files_with_tests\n- Files without Tests: $files_without_tests\n- Coverage: ${coverage}%/" "$report_file"
    
    # Display summary
    echo ""
    echo "========================================="
    echo "Test Coverage Summary:"
    echo "  Total Files: $total_files"
    echo "  Files with Tests: $files_with_tests"
    echo "  Files without Tests: $files_without_tests"
    echo "  Coverage: ${coverage}%"
    echo ""
    echo "Report saved to:"
    echo "  $report_file"
    echo "========================================="
}

# Run if executed directly
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    main "$@"
fi