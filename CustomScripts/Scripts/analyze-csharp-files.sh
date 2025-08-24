#!/bin/bash
# analyze-csharp-files.sh - Analyze C# files for size and complexity

set -e  # Exit on error

# Configuration
readonly MAX_LINES=750
readonly RECOMMENDED_MAX=500
readonly REPORT_FILE="file-analysis-report.txt"

# Color codes
readonly RED='\033[0;31m'
readonly YELLOW='\033[1;33m'
readonly GREEN='\033[0;32m'
readonly NC='\033[0m' # No Color

# Counters
total_files=0
violation_files=0
warning_files=0

echo "=========================================="
echo "    C# File Analysis Report"
echo "    $(date)"
echo "=========================================="
echo ""

# Create report file
{
    echo "C# File Analysis Report"
    echo "Generated: $(date)"
    echo "================================="
    echo ""
} > "$REPORT_FILE"

echo "Analyzing C# files in Assets/TestFramework..."
echo ""

# Process C# files
for file in $(find Assets/TestFramework -name "*.cs" -type f 2>/dev/null | head -20); do
    if [ -f "$file" ]; then
        lines=$(wc -l < "$file" 2>/dev/null || echo 0)
        filename=$(basename "$file")
        
        ((total_files++))
        
        if [ "$lines" -gt "$MAX_LINES" ]; then
            ((violation_files++))
            echo -e "${RED}✗ VIOLATION${NC}: $file"
            echo "  Lines: $lines (max: $MAX_LINES)"
            echo "VIOLATION: $file - $lines lines" >> "$REPORT_FILE"
            
            # Check for regions
            regions=$(grep -c "#region" "$file" 2>/dev/null || echo 0)
            echo "  Regions: $regions"
            
        elif [ "$lines" -gt "$RECOMMENDED_MAX" ]; then
            ((warning_files++))
            echo -e "${YELLOW}⚠ WARNING${NC}: $file"
            echo "  Lines: $lines (recommended max: $RECOMMENDED_MAX)"
            echo "WARNING: $file - $lines lines" >> "$REPORT_FILE"
            
        else
            echo -e "${GREEN}✓ OK${NC}: $filename ($lines lines)"
        fi
    fi
done

echo ""
echo "=========================================="
echo "              SUMMARY"
echo "=========================================="
echo "Total files analyzed: $total_files"
echo -e "Violations (>${MAX_LINES}): ${RED}$violation_files${NC}"
echo -e "Warnings (>${RECOMMENDED_MAX}): ${YELLOW}$warning_files${NC}"

{
    echo ""
    echo "SUMMARY"
    echo "-------"
    echo "Total files: $total_files"
    echo "Violations: $violation_files"
    echo "Warnings: $warning_files"
} >> "$REPORT_FILE"

echo ""
echo "Report saved to: $REPORT_FILE"

# Check specific test files
echo ""
echo "=========================================="
echo "       SAMPLE FILE ANALYSIS"
echo "=========================================="

# Analyze our sample test files
for testfile in "Assets/TestFramework/Unity/Core/UniTaskTestBase.cs" \
                "Assets/TestFramework/Unity/Helpers/UniTaskRunner.cs" \
                "Assets/TestFramework/Tests/PlayMode/SampleSystemPrefabTests.cs"; do
    if [ -f "$testfile" ]; then
        lines=$(wc -l < "$testfile")
        methods=$(grep -c "^\s*\(public\|private\|protected\)" "$testfile" 2>/dev/null || echo 0)
        regions=$(grep -c "#region" "$testfile" 2>/dev/null || echo 0)
        
        echo ""
        echo "📄 $(basename "$testfile")"
        echo "   Lines: $lines"
        echo "   Methods (approx): $methods"
        echo "   Regions: $regions"
        
        if [ "$regions" -gt 0 ]; then
            echo -e "   ${GREEN}✓ Has region organization${NC}"
        else
            echo -e "   ${YELLOW}⚠ No regions found${NC}"
        fi
    fi
done

echo ""
echo "=========================================="
echo "Analysis complete!"