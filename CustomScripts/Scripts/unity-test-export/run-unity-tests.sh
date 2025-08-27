#!/bin/bash
# =============================================================================
# Unity Test Runner with XML Export - Bash Version
# =============================================================================
# This script runs Unity tests and exports results to XML format
# Works on Linux and macOS
# Usage: ./run-unity-tests.sh [OPTIONS]
# =============================================================================

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
WHITE='\033[1;37m'
NC='\033[0m' # No Color

# Default configuration
UNITY_VERSION="6000.0.47f1"
PROJECT_PATH="${PROJECT_PATH:-$(pwd)}"
TEST_PLATFORM="EditMode"
TEST_FILTER=""
TEST_CATEGORIES=""
VERBOSE=0
OPEN_RESULTS=0

# Platform detection
OS_TYPE=$(uname -s)
case "$OS_TYPE" in
    Linux*)
        UNITY_BASE_PATHS=(
            "/opt/Unity/Hub/Editor"
            "/opt/Unity-${UNITY_VERSION}"
            "$HOME/Unity/Hub/Editor"
        )
        UNITY_EXECUTABLE="Editor/Unity"
        OPEN_CMD="xdg-open"
        ;;
    Darwin*)
        UNITY_BASE_PATHS=(
            "/Applications/Unity/Hub/Editor"
            "/Applications/Unity ${UNITY_VERSION}"
            "$HOME/Applications/Unity/Hub/Editor"
        )
        UNITY_EXECUTABLE="Unity.app/Contents/MacOS/Unity"
        OPEN_CMD="open"
        ;;
    CYGWIN*|MINGW*|MSYS*)
        echo -e "${YELLOW}[WARNING] Detected Windows environment. Use .bat or .ps1 scripts instead.${NC}"
        UNITY_BASE_PATHS=(
            "/c/Program Files/Unity/Hub/Editor"
        )
        UNITY_EXECUTABLE="Editor/Unity.exe"
        OPEN_CMD="explorer"
        ;;
    *)
        echo -e "${RED}[ERROR] Unknown operating system: $OS_TYPE${NC}"
        exit 1
        ;;
esac

# Function to display usage
show_usage() {
    cat << EOF
Unity Test Runner with XML Export

Usage: $0 [OPTIONS]

Options:
    -p, --platform PLATFORM    Test platform: EditMode or PlayMode (default: EditMode)
    -f, --filter FILTER        Filter tests by name
    -c, --categories CATS      Comma-separated test categories
    -u, --unity VERSION        Unity version (default: $UNITY_VERSION)
    -d, --project DIR          Project directory (default: current directory)
    -o, --open                 Open results folder after completion
    -v, --verbose              Enable verbose output
    -h, --help                 Show this help message

Examples:
    $0
    $0 --platform PlayMode
    $0 --filter "MyNamespace.MyTestClass"
    $0 --categories "Unit,Integration" --verbose
    $0 -p EditMode -f "PlayerTests" -o

Environment Variables:
    PROJECT_PATH    Override default project path
    UNITY_VERSION   Override default Unity version

EOF
    exit 0
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -p|--platform)
            TEST_PLATFORM="$2"
            shift 2
            ;;
        -f|--filter)
            TEST_FILTER="$2"
            shift 2
            ;;
        -c|--categories)
            TEST_CATEGORIES="$2"
            shift 2
            ;;
        -u|--unity)
            UNITY_VERSION="$2"
            shift 2
            ;;
        -d|--project)
            PROJECT_PATH="$2"
            shift 2
            ;;
        -o|--open)
            OPEN_RESULTS=1
            shift
            ;;
        -v|--verbose)
            VERBOSE=1
            shift
            ;;
        -h|--help)
            show_usage
            ;;
        *)
            echo -e "${RED}[ERROR] Unknown option: $1${NC}"
            show_usage
            ;;
    esac
done

# Validate test platform
if [[ "$TEST_PLATFORM" != "EditMode" && "$TEST_PLATFORM" != "PlayMode" ]]; then
    echo -e "${RED}[ERROR] Invalid test platform: $TEST_PLATFORM${NC}"
    echo "Valid options: EditMode, PlayMode"
    exit 1
fi

# Function to find Unity installation
find_unity() {
    local unity_path=""
    
    # Check if Unity is in PATH
    if command -v Unity &> /dev/null; then
        unity_path="Unity"
        echo -e "${GREEN}[INFO] Found Unity in PATH${NC}"
        return 0
    fi
    
    # Search for Unity installation
    for base_path in "${UNITY_BASE_PATHS[@]}"; do
        # Try exact version
        if [[ -d "$base_path/$UNITY_VERSION" ]]; then
            unity_path="$base_path/$UNITY_VERSION/$UNITY_EXECUTABLE"
            if [[ -x "$unity_path" ]]; then
                UNITY_PATH="$unity_path"
                echo -e "${GREEN}[INFO] Found Unity $UNITY_VERSION at: $unity_path${NC}"
                return 0
            fi
        fi
        
        # Try base path directly
        if [[ -x "$base_path/$UNITY_EXECUTABLE" ]]; then
            unity_path="$base_path/$UNITY_EXECUTABLE"
            UNITY_PATH="$unity_path"
            echo -e "${YELLOW}[WARNING] Using Unity at: $unity_path${NC}"
            return 0
        fi
        
        # Search for any Unity version
        if [[ -d "$base_path" ]]; then
            for version_dir in "$base_path"/*; do
                if [[ -d "$version_dir" && -x "$version_dir/$UNITY_EXECUTABLE" ]]; then
                    unity_path="$version_dir/$UNITY_EXECUTABLE"
                    UNITY_PATH="$unity_path"
                    UNITY_VERSION=$(basename "$version_dir")
                    echo -e "${YELLOW}[WARNING] Using Unity $UNITY_VERSION at: $unity_path${NC}"
                    return 0
                fi
            done
        fi
    done
    
    echo -e "${RED}[ERROR] Unity not found. Please install Unity $UNITY_VERSION${NC}"
    echo "Searched locations:"
    for path in "${UNITY_BASE_PATHS[@]}"; do
        echo "  - $path"
    done
    return 1
}

# Function to print header
print_header() {
    echo -e "${CYAN}=============================================================================${NC}"
    echo -e "${CYAN}Unity Test Runner with XML Export (Bash)${NC}"
    echo -e "${CYAN}=============================================================================${NC}"
}

# Function to print separator
print_line() {
    echo -e "${CYAN}-----------------------------------------------------------------------------${NC}"
}

# Function to parse XML results
parse_xml_results() {
    local xml_file="$1"
    
    if [[ ! -f "$xml_file" ]]; then
        echo -e "${YELLOW}[WARNING] XML results file not found${NC}"
        return
    fi
    
    # Try to use xmllint if available
    if command -v xmllint &> /dev/null; then
        local total=$(xmllint --xpath "string(/test-run/@testcasecount)" "$xml_file" 2>/dev/null)
        local passed=$(xmllint --xpath "string(/test-run/@passed)" "$xml_file" 2>/dev/null)
        local failed=$(xmllint --xpath "string(/test-run/@failed)" "$xml_file" 2>/dev/null)
        local skipped=$(xmllint --xpath "string(/test-run/@skipped)" "$xml_file" 2>/dev/null)
        local duration=$(xmllint --xpath "string(/test-run/@duration)" "$xml_file" 2>/dev/null)
        
        echo
        echo -e "${WHITE}Test Results Summary:${NC}"
        echo -e "  Total Tests:  $total"
        echo -e "  Passed:       ${GREEN}$passed${NC}"
        if [[ "$failed" == "0" ]]; then
            echo -e "  Failed:       $failed"
        else
            echo -e "  Failed:       ${RED}$failed${NC}"
        fi
        echo -e "  Skipped:      ${YELLOW}$skipped${NC}"
        echo -e "  Duration:     ${duration}s"
        
        # Show failed tests if any
        if [[ "$failed" != "0" && "$failed" != "" ]]; then
            echo
            echo -e "${RED}Failed Tests:${NC}"
            xmllint --xpath "//test-case[@result='Failed']/@fullname" "$xml_file" 2>/dev/null | \
                sed 's/fullname="//g' | sed 's/"//g' | while read -r test; do
                echo -e "  ${RED}✗ $test${NC}"
            done
        fi
    else
        # Fallback to basic grep parsing
        echo
        echo -e "${WHITE}Test Results Summary:${NC}"
        grep -o 'testcasecount="[^"]*"' "$xml_file" | sed 's/[^0-9]//g' | while read -r count; do
            echo "  Total Tests: $count"
        done
        grep -o 'passed="[^"]*"' "$xml_file" | sed 's/[^0-9]//g' | while read -r count; do
            echo -e "  Passed: ${GREEN}$count${NC}"
        done
        grep -o 'failed="[^"]*"' "$xml_file" | sed 's/[^0-9]//g' | while read -r count; do
            if [[ "$count" == "0" ]]; then
                echo "  Failed: $count"
            else
                echo -e "  Failed: ${RED}$count${NC}"
            fi
        done
    fi
}

# Main execution
main() {
    # Find Unity installation
    if ! find_unity; then
        exit 1
    fi
    
    # Validate project path
    if [[ ! -d "$PROJECT_PATH" ]]; then
        echo -e "${RED}[ERROR] Project path not found: $PROJECT_PATH${NC}"
        exit 1
    fi
    
    # Create timestamp
    TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
    
    # Setup output paths
    TEST_RESULTS_DIR="$PROJECT_PATH/TestResults"
    XML_FILE="$TEST_RESULTS_DIR/TestResults_${TEST_PLATFORM}_${TIMESTAMP}.xml"
    LOG_FILE="$TEST_RESULTS_DIR/unity_${TEST_PLATFORM}_${TIMESTAMP}.log"
    SUMMARY_FILE="$TEST_RESULTS_DIR/TestResults_${TEST_PLATFORM}_${TIMESTAMP}.summary.txt"
    
    # Create TestResults directory
    mkdir -p "$TEST_RESULTS_DIR"
    
    # Display configuration
    print_header
    echo -e "${WHITE}Unity Version:${NC}  $UNITY_VERSION"
    echo -e "${WHITE}Project Path:${NC}   $PROJECT_PATH"
    echo -e "${WHITE}Test Platform:${NC}  $TEST_PLATFORM"
    [[ -n "$TEST_FILTER" ]] && echo -e "${WHITE}Test Filter:${NC}    $TEST_FILTER"
    [[ -n "$TEST_CATEGORIES" ]] && echo -e "${WHITE}Categories:${NC}     $TEST_CATEGORIES"
    echo
    echo -e "${WHITE}Output Files:${NC}"
    echo "  XML Results:  $XML_FILE"
    echo "  Unity Log:    $LOG_FILE"
    echo "  Summary:      $SUMMARY_FILE"
    print_line
    
    # Build Unity command
    UNITY_CMD=(
        "$UNITY_PATH"
        -batchmode
        -quit
        -projectPath "$PROJECT_PATH"
        -runTests
        -testPlatform "$TEST_PLATFORM"
        -testResultFile "$XML_FILE"
        -logFile "$LOG_FILE"
    )
    
    # Add optional parameters
    [[ -n "$TEST_FILTER" ]] && UNITY_CMD+=(-testFilter "$TEST_FILTER")
    [[ -n "$TEST_CATEGORIES" ]] && UNITY_CMD+=(-testCategories "$TEST_CATEGORIES")
    
    # Display command if verbose
    if [[ $VERBOSE -eq 1 ]]; then
        echo -e "${BLUE}Command:${NC} ${UNITY_CMD[*]}"
        echo
    fi
    
    # Run Unity tests
    echo -e "\n[$(date +%H:%M:%S)] ${YELLOW}Starting Unity tests...${NC}"
    
    START_TIME=$(date +%s)
    
    # Execute Unity command
    if [[ $VERBOSE -eq 1 ]]; then
        "${UNITY_CMD[@]}"
    else
        "${UNITY_CMD[@]}" > /dev/null 2>&1
    fi
    
    EXIT_CODE=$?
    END_TIME=$(date +%s)
    DURATION=$((END_TIME - START_TIME))
    
    # Format duration
    DURATION_MIN=$((DURATION / 60))
    DURATION_SEC=$((DURATION % 60))
    DURATION_STR="${DURATION_MIN}m ${DURATION_SEC}s"
    
    echo
    print_line
    
    # Process results
    if [[ $EXIT_CODE -eq 0 ]]; then
        echo -e "${GREEN}[SUCCESS] All tests passed!${NC}"
        echo -e "Duration: $DURATION_STR"
        
        # Parse XML results
        parse_xml_results "$XML_FILE"
        
    else
        echo -e "${RED}[FAILURE] Tests failed with exit code: $EXIT_CODE${NC}"
        echo -e "Duration: $DURATION_STR"
        
        case $EXIT_CODE in
            1)
                echo -e "${YELLOW}  Reason: Test failures detected${NC}"
                ;;
            2)
                echo -e "${YELLOW}  Reason: Compilation errors${NC}"
                ;;
            3)
                echo -e "${YELLOW}  Reason: Unity license issue${NC}"
                ;;
            *)
                echo -e "${YELLOW}  Reason: Unknown error${NC}"
                ;;
        esac
        
        # Parse XML results even on failure
        parse_xml_results "$XML_FILE"
        
        # Show last lines of log
        if [[ -f "$LOG_FILE" && $VERBOSE -eq 1 ]]; then
            echo
            echo -e "${YELLOW}Last 20 lines from log:${NC}"
            print_line
            tail -20 "$LOG_FILE"
            print_line
        fi
    fi
    
    # Generate summary file
    cat > "$SUMMARY_FILE" << EOF
Unity Test Results Summary
==========================
Generated: $(date)

Configuration:
  Platform: $TEST_PLATFORM
  Unity Version: $UNITY_VERSION
  Project: $PROJECT_PATH
  Filter: ${TEST_FILTER:-none}
  Categories: ${TEST_CATEGORIES:-none}
  Duration: $DURATION_STR

Results:
  Exit Code: $EXIT_CODE
  Status: $([ $EXIT_CODE -eq 0 ] && echo "SUCCESS" || echo "FAILURE")

Output Files:
  XML: $XML_FILE
  Log: $LOG_FILE
EOF
    
    echo
    echo -e "${WHITE}Summary saved to:${NC} $SUMMARY_FILE"
    print_header
    
    # Open results folder if requested
    if [[ $OPEN_RESULTS -eq 1 ]]; then
        echo
        echo -e "${YELLOW}Opening results folder...${NC}"
        $OPEN_CMD "$TEST_RESULTS_DIR" 2>/dev/null &
    fi
    
    exit $EXIT_CODE
}

# Run main function
main