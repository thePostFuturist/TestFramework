#!/bin/bash
# analyze-large-files.sh - Find and analyze C# files exceeding 750 lines

set -e  # Exit on error

# Configuration
readonly SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
readonly PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
readonly OUTPUT_DIR="$PROJECT_ROOT/CustomScripts/Output"
readonly REPORTS_DIR="$OUTPUT_DIR/Reports"
readonly LINE_LIMIT=750

# Colors for output (Windows-compatible)
if [ -t 1 ]; then
    RED='\033[0;31m'
    GREEN='\033[0;32m'
    YELLOW='\033[1;33m'
    NC='\033[0m' # No Color
else
    RED=''
    GREEN=''
    YELLOW=''
    NC=''
fi

# Ensure output directories exist
mkdir -p "$REPORTS_DIR"

# Function to analyze a single file
analyze_file() {
    local file="$1"
    local lines
    
    # Windows-safe line counting
    lines=$(wc -l < "$file" 2>/dev/null || echo 0)
    
    if [ "$lines" -gt "$LINE_LIMIT" ]; then
        echo -e "${RED}[LARGE]${NC} $file - $lines lines"
        
        # Get relative path for report
        local relative_path="${file#$PROJECT_ROOT/}"
        echo "$relative_path,$lines" >> "$REPORTS_DIR/large-files.csv"
        
        # Analyze file structure
        local classes=$(grep -c "^[[:space:]]*public class" "$file" 2>/dev/null || echo 0)
        local methods=$(grep -c "^[[:space:]]*\(public\|private\|protected\|internal\).*(" "$file" 2>/dev/null || echo 0)
        local regions=$(grep -c "#region" "$file" 2>/dev/null || echo 0)
        
        echo "  Classes: $classes, Methods: $methods, Regions: $regions" >> "$REPORTS_DIR/large-files-detail.txt"
        return 0
    else
        return 1
    fi
}

# Main execution
main() {
    echo "========================================="
    echo "Large File Analysis"
    echo "========================================="
    echo "Project Root: $PROJECT_ROOT"
    echo "Output Directory: $OUTPUT_DIR"
    echo "Line Limit: $LINE_LIMIT"
    echo ""
    
    # Initialize report files
    echo "File,Lines" > "$REPORTS_DIR/large-files.csv"
    echo "Large File Analysis Report - $(date)" > "$REPORTS_DIR/large-files-detail.txt"
    echo "=========================================" >> "$REPORTS_DIR/large-files-detail.txt"
    
    local large_count=0
    local total_count=0
    
    # Process all C# files in Assets/TestFramework
    echo "Analyzing C# files in Assets/TestFramework..."
    
    # Windows-compatible find with error handling
    while IFS= read -r file; do
        if [ -f "$file" ]; then
            ((total_count++))
            if analyze_file "$file"; then
                ((large_count++))
            fi
        fi
    done < <(find "$PROJECT_ROOT/Assets/TestFramework" -name "*.cs" -type f 2>/dev/null)
    
    # Generate summary
    echo ""
    echo "========================================="
    echo -e "${YELLOW}Summary:${NC}"
    echo "Total C# files analyzed: $total_count"
    echo -e "Files exceeding $LINE_LIMIT lines: ${RED}$large_count${NC}"
    
    if [ "$large_count" -gt 0 ]; then
        echo ""
        echo -e "${YELLOW}Top 5 largest files:${NC}"
        sort -t',' -k2 -rn "$REPORTS_DIR/large-files.csv" | head -6 | tail -5 | while IFS=',' read -r file lines; do
            echo "  $file - $lines lines"
        done
        
        echo ""
        echo -e "${GREEN}Reports saved to:${NC}"
        echo "  - $REPORTS_DIR/large-files.csv"
        echo "  - $REPORTS_DIR/large-files-detail.txt"
        
        echo ""
        echo -e "${YELLOW}Recommended action:${NC}"
        echo "  Run the refactor agent to split these files:"
        echo "  bash ./CustomScripts/Scripts/refactor-large-files.sh"
    else
        echo -e "${GREEN}✓ All files are within the size limit!${NC}"
    fi
    
    echo "========================================="
}

# Run if executed directly
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    main "$@"
fi