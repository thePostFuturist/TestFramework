#!/bin/bash
# analyze-project.sh - Comprehensive C# project analysis

# Configuration
readonly MAX_LINES=750
readonly WARN_LINES=500

# Colors
RED='\033[0;31m'
YELLOW='\033[1;33m'
GREEN='\033[0;32m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${BLUE}=========================================="
echo "    TestFramework C# Analysis"
echo "==========================================${NC}"
echo ""

# Function to analyze a single file
analyze_file() {
    local file="$1"
    local lines=$(wc -l < "$file")
    local filename=$(basename "$file")
    local dirname=$(dirname "$file")
    
    # Count various metrics
    local regions=$(grep -c "^.*#region" "$file" 2>/dev/null || echo 0)
    local endregions=$(grep -c "^.*#endregion" "$file" 2>/dev/null || echo 0)
    local classes=$(grep -c "^\s*\(public\|internal\).*class\s" "$file" 2>/dev/null || echo 0)
    local interfaces=$(grep -c "^\s*\(public\|internal\).*interface\s" "$file" 2>/dev/null || echo 0)
    local async_methods=$(grep -c "async\s\+UniTask" "$file" 2>/dev/null || echo 0)
    local unitask_usage=$(grep -c "UniTask" "$file" 2>/dev/null || echo 0)
    
    # Determine status
    local status="OK"
    local color="$GREEN"
    if [ "$lines" -gt "$MAX_LINES" ]; then
        status="VIOLATION"
        color="$RED"
    elif [ "$lines" -gt "$WARN_LINES" ]; then
        status="WARNING"
        color="$YELLOW"
    fi
    
    echo -e "${color}[$status]${NC} $filename"
    echo "  📁 Location: $dirname"
    echo "  📏 Lines: $lines"
    
    if [ "$classes" -gt 0 ] || [ "$interfaces" -gt 0 ]; then
        echo "  🏗️ Structure: $classes classes, $interfaces interfaces"
    fi
    
    if [ "$regions" -gt 0 ]; then
        echo -e "  📑 Regions: $regions (${GREEN}✓ organized${NC})"
    else
        echo -e "  📑 Regions: 0 (${YELLOW}⚠ consider adding${NC})"
    fi
    
    if [ "$unitask_usage" -gt 0 ]; then
        echo -e "  ⚡ UniTask: $async_methods async methods (${GREEN}✓ modern async${NC})"
    fi
    
    echo ""
}

# Main analysis
echo -e "${BLUE}Analyzing Unity Test Framework files...${NC}"
echo ""

# Analyze DOTS files
echo -e "${BLUE}━━━ DOTS Framework ━━━${NC}"
for file in Assets/TestFramework/DOTS/Core/*.cs Assets/TestFramework/DOTS/Helpers/*.cs; do
    [ -f "$file" ] && analyze_file "$file"
done

# Analyze Unity files
echo -e "${BLUE}━━━ Unity Framework ━━━${NC}"
for file in Assets/TestFramework/Unity/Core/*.cs Assets/TestFramework/Unity/Helpers/*.cs; do
    [ -f "$file" ] && analyze_file "$file"
done

# Analyze Test files
echo -e "${BLUE}━━━ Test Files ━━━${NC}"
for file in Assets/TestFramework/Tests/PlayMode/*.cs; do
    [ -f "$file" ] && analyze_file "$file"
done

# Analyze Editor files
echo -e "${BLUE}━━━ Editor Scripts ━━━${NC}"
for file in Assets/TestFramework/Editor/PrefabFactories/*.cs; do
    [ -f "$file" ] && analyze_file "$file"
done

# Summary statistics
echo -e "${BLUE}=========================================="
echo "           SUMMARY STATISTICS"
echo "==========================================${NC}"

total_files=$(find Assets/TestFramework -name "*.cs" -type f | wc -l)
total_lines=$(find Assets/TestFramework -name "*.cs" -type f -exec wc -l {} + | awk '{sum+=$1} END {print sum}')
violations=$(find Assets/TestFramework -name "*.cs" -type f -exec wc -l {} + | awk -v max=$MAX_LINES '$1 > max {count++} END {print count+0}')
warnings=$(find Assets/TestFramework -name "*.cs" -type f -exec wc -l {} + | awk -v warn=$WARN_LINES -v max=$MAX_LINES '$1 > warn && $1 <= max {count++} END {print count+0}')

echo "  📊 Total C# files: $total_files"
echo "  📏 Total lines of code: $total_lines"
echo -e "  ${RED}❌ Violations (>$MAX_LINES lines): $violations${NC}"
echo -e "  ${YELLOW}⚠️  Warnings (>$WARN_LINES lines): $warnings${NC}"
echo ""

# Check for UniTask adoption
unitask_files=$(grep -l "UniTask" Assets/TestFramework/**/*.cs 2>/dev/null | wc -l)
echo -e "  ${GREEN}⚡ Files using UniTask: $unitask_files${NC}"

# Check for region organization
region_files=$(grep -l "#region" Assets/TestFramework/**/*.cs 2>/dev/null | wc -l)
echo -e "  ${GREEN}📑 Files with regions: $region_files${NC}"

echo ""
echo -e "${GREEN}✅ Analysis complete!${NC}"