#!/bin/bash
# check-file-sizes.sh - Check C# files for size violations and generate report

set -e  # Exit on error

# Configuration
readonly SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
readonly PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
readonly MAX_LINES=750
readonly RECOMMENDED_MAX=500
readonly TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
readonly REPORT_FILE="$PROJECT_ROOT/file-size-report_${TIMESTAMP}.txt"

# Color codes for output
readonly RED='\033[0;31m'
readonly YELLOW='\033[1;33m'
readonly GREEN='\033[0;32m'
readonly NC='\033[0m' # No Color

# Counters
total_files=0
violation_files=0
warning_files=0
ok_files=0

# Functions
function print_header() {
    echo "================================================"
    echo "    C# File Size Analysis Report"
    echo "    Generated: $(date)"
    echo "    Project: $PROJECT_ROOT"
    echo "================================================"
    echo ""
}

function check_file_size() {
    local file=$1
    local filename=$(basename "$file")
    local filepath=$(realpath --relative-to="$PROJECT_ROOT" "$file" 2>/dev/null || echo "$file")
    local lines=$(wc -l < "$file")
    
    ((total_files++))
    
    if [ "$lines" -gt "$MAX_LINES" ]; then
        ((violation_files++))
        echo -e "${RED}✗ VIOLATION${NC}: $filepath - $lines lines (exceeds $MAX_LINES)"
        echo "  VIOLATION: $filepath - $lines lines" >> "$REPORT_FILE"
        return 1
    elif [ "$lines" -gt "$RECOMMENDED_MAX" ]; then
        ((warning_files++))
        echo -e "${YELLOW}⚠ WARNING${NC}: $filepath - $lines lines (exceeds recommended $RECOMMENDED_MAX)"
        echo "  WARNING: $filepath - $lines lines" >> "$REPORT_FILE"
        return 0
    else
        ((ok_files++))
        echo -e "${GREEN}✓ OK${NC}: $filepath - $lines lines"
        return 0
    fi
}

function analyze_complexity() {
    local file=$1
    local filepath=$(realpath --relative-to="$PROJECT_ROOT" "$file" 2>/dev/null || echo "$file")
    
    # Count methods (approximate)
    local method_count=$(grep -c "^\s*\(public\|private\|protected\|internal\).*(" "$file" 2>/dev/null || echo 0)
    
    # Count regions
    local region_count=$(grep -c "#region" "$file" 2>/dev/null || echo 0)
    
    # Count classes/interfaces
    local class_count=$(grep -c "^\s*\(public\|private\|internal\).*\(class\|interface\)" "$file" 2>/dev/null || echo 0)
    
    if [ "$method_count" -gt 20 ] || [ "$class_count" -gt 1 ]; then
        echo "  📊 Complexity: $method_count methods, $class_count classes, $region_count regions"
        echo "    Complexity: $filepath - Methods: $method_count, Classes: $class_count, Regions: $region_count" >> "$REPORT_FILE"
    fi
}

function generate_summary() {
    echo ""
    echo "================================================"
    echo "                 SUMMARY"
    echo "================================================"
    echo "Total C# files analyzed: $total_files"
    echo -e "${RED}Files exceeding $MAX_LINES lines: $violation_files${NC}"
    echo -e "${YELLOW}Files exceeding $RECOMMENDED_MAX lines: $warning_files${NC}"
    echo -e "${GREEN}Files within limits: $ok_files${NC}"
    echo ""
    
    # Add summary to report file
    {
        echo ""
        echo "SUMMARY:"
        echo "--------"
        echo "Total files: $total_files"
        echo "Violations (>$MAX_LINES lines): $violation_files"
        echo "Warnings (>$RECOMMENDED_MAX lines): $warning_files"
        echo "OK files: $ok_files"
    } >> "$REPORT_FILE"
}

function suggest_refactoring() {
    if [ "$violation_files" -gt 0 ]; then
        echo "================================================"
        echo "           REFACTORING SUGGESTIONS"
        echo "================================================"
        echo ""
        echo "Files that exceed $MAX_LINES lines should be refactored using:"
        echo "  1. Partial classes for Unity MonoBehaviours"
        echo "  2. Interface extraction for testability"
        echo "  3. Strategy pattern for multiple algorithms"
        echo "  4. Separate concerns into multiple classes"
        echo ""
        echo "Run the refactoring agent for detailed analysis:"
        echo "  Use .claude/agents/RefactorAgent.md for guidance"
        echo ""
    fi
}

# Main execution
function main() {
    print_header | tee "$REPORT_FILE"
    
    echo "Scanning for C# files..."
    echo ""
    
    # Find all C# files, excluding certain directories
    while IFS= read -r -d '' file; do
        # Skip files in Library, Temp, obj, bin directories
        if [[ "$file" == *"/Library/"* ]] || \
           [[ "$file" == *"/Temp/"* ]] || \
           [[ "$file" == *"/obj/"* ]] || \
           [[ "$file" == *"/bin/"* ]] || \
           [[ "$file" == *".meta" ]]; then
            continue
        fi
        
        check_file_size "$file"
        
        # For files with violations or warnings, analyze complexity
        local lines=$(wc -l < "$file")
        if [ "$lines" -gt "$RECOMMENDED_MAX" ]; then
            analyze_complexity "$file"
        fi
        
    done < <(find "$PROJECT_ROOT" -name "*.cs" -type f -print0 2>/dev/null)
    
    generate_summary
    suggest_refactoring
    
    echo "Full report saved to: $REPORT_FILE"
    echo ""
    
    # Exit with error if violations found
    if [ "$violation_files" -gt 0 ]; then
        echo -e "${RED}❌ FAILED: Found $violation_files files exceeding size limits${NC}"
        exit 1
    else
        echo -e "${GREEN}✅ SUCCESS: All files within size limits${NC}"
        exit 0
    fi
}

# Run if executed directly
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    main "$@"
fi