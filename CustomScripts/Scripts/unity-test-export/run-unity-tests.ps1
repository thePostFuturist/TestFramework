# =============================================================================
# Unity Test Runner with XML Export - PowerShell Version
# =============================================================================
# This script runs Unity tests and exports results to XML format
# Usage: .\run-unity-tests.ps1 [-TestPlatform EditMode|PlayMode] [-TestFilter "namespace.class"]
# =============================================================================

param(
    [ValidateSet("EditMode", "PlayMode")]
    [string]$TestPlatform = "EditMode",

    [string]$TestFilter = "",

    [string]$UnityVersion = "6000.0.47f1",

    [string]$ProjectPath = "D:\Dev\TestFramework",

    [switch]$OpenResults,

    [switch]$Verbose
)

# Enable verbose output if requested
if ($Verbose) {
    $VerbosePreference = "Continue"
}

# Configuration
$unityBasePath = "C:\Program Files\Unity\Hub\Editor"
$unityPath = Join-Path $unityBasePath $UnityVersion "Editor\Unity.exe"

# Validate Unity installation
if (!(Test-Path $unityPath)) {
    Write-Host "[ERROR] Unity not found at: $unityPath" -ForegroundColor Red
    Write-Host "Available Unity versions:" -ForegroundColor Yellow
    Get-ChildItem $unityBasePath -Directory | ForEach-Object { Write-Host "  - $($_.Name)" }
    exit 1
}

# Validate project path
if (!(Test-Path $ProjectPath)) {
    Write-Host "[ERROR] Project not found at: $ProjectPath" -ForegroundColor Red
    exit 1
}

# Create timestamp
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"

# Setup output paths
$testResultsDir = Join-Path $ProjectPath "TestResults"
$xmlFile = Join-Path $testResultsDir "TestResults_$timestamp.xml"
$logFile = Join-Path $testResultsDir "unity_$timestamp.log"
$summaryFile = Join-Path $testResultsDir "TestResults_$timestamp.summary.txt"

# Create TestResults directory
if (!(Test-Path $testResultsDir)) {
    Write-Verbose "Creating TestResults directory..."
    New-Item -ItemType Directory -Path $testResultsDir | Out-Null
}

# Display configuration
Write-Host "=============================================================================" -ForegroundColor Cyan
Write-Host "Unity Test Runner - XML Export (PowerShell)" -ForegroundColor Cyan
Write-Host "=============================================================================" -ForegroundColor Cyan
Write-Host "Unity Version:  $UnityVersion" -ForegroundColor White
Write-Host "Project Path:   $ProjectPath" -ForegroundColor White
Write-Host "Test Platform:  $TestPlatform" -ForegroundColor White
if ($TestFilter) {
    Write-Host "Test Filter:    $TestFilter" -ForegroundColor White
}
Write-Host "Output XML:     $xmlFile" -ForegroundColor White
Write-Host "Log File:       $logFile" -ForegroundColor White
Write-Host "=============================================================================" -ForegroundColor Cyan
Write-Host

# Build Unity arguments
$arguments = @(
    "-batchmode",
    "-quit",
    "-projectPath", $ProjectPath,
    "-runTests",
    "-testPlatform", $TestPlatform,
    "-testResultFile", $xmlFile,
    "-logFile", $logFile
)

# Add test filter if specified
if ($TestFilter) {
    $arguments += "-testFilter"
    $arguments += $TestFilter
}

# Display command (for debugging)
Write-Verbose "Executing: $unityPath $($arguments -join ' ')"

# Run Unity tests
Write-Host "Starting Unity tests..." -ForegroundColor Yellow
$startTime = Get-Date

try {
    $process = Start-Process -FilePath $unityPath `
                            -ArgumentList $arguments `
                            -Wait `
                            -PassThru `
                            -NoNewWindow `
                            -RedirectStandardOutput "$testResultsDir\stdout_$timestamp.txt" `
                            -RedirectStandardError "$testResultsDir\stderr_$timestamp.txt"

    $exitCode = $process.ExitCode
    $duration = (Get-Date) - $startTime

    Write-Host
    Write-Host "=============================================================================" -ForegroundColor Cyan

    if ($exitCode -eq 0) {
        Write-Host "[SUCCESS] Tests completed successfully!" -ForegroundColor Green
        Write-Host "Duration: $($duration.ToString('mm\:ss'))" -ForegroundColor White
        Write-Host
        Write-Host "Test results exported to:" -ForegroundColor White
        Write-Host "  XML: $xmlFile" -ForegroundColor Gray
        Write-Host "  Log: $logFile" -ForegroundColor Gray

        # Parse and display XML results summary
        if (Test-Path $xmlFile) {
            try {
                [xml]$xmlContent = Get-Content $xmlFile
                $testRun = $xmlContent.'test-run'

                Write-Host
                Write-Host "Test Results Summary:" -ForegroundColor White
                Write-Host "  Total Tests:  $($testRun.testcasecount)" -ForegroundColor Gray
                Write-Host "  Passed:       $($testRun.passed)" -ForegroundColor Green
                Write-Host "  Failed:       $($testRun.failed)" -ForegroundColor $(if ($testRun.failed -eq "0") { "Gray" } else { "Red" })
                Write-Host "  Skipped:      $($testRun.skipped)" -ForegroundColor Yellow
                Write-Host "  Duration:     $($testRun.duration)s" -ForegroundColor Gray

                # Show failed test details if any
                if ([int]$testRun.failed -gt 0) {
                    Write-Host
                    Write-Host "Failed Tests:" -ForegroundColor Red
                    $xmlContent.SelectNodes("//test-case[@result='Failed']") | ForEach-Object {
                        Write-Host "  - $($_.fullname)" -ForegroundColor Red
                        if ($_.failure.message.'#cdata-section') {
                            Write-Host "    $($_.failure.message.'#cdata-section')" -ForegroundColor DarkRed
                        }
                    }
                }
            }
            catch {
                Write-Warning "Could not parse XML results: $_"
            }
        }

        # Display summary file if it exists
        if (Test-Path $summaryFile) {
            Write-Host
            Write-Host "Summary File Contents:" -ForegroundColor White
            Write-Host "-----------------------------------------------------------------------------" -ForegroundColor Gray
            Get-Content $summaryFile | Write-Host
            Write-Host "-----------------------------------------------------------------------------" -ForegroundColor Gray
        }

    } else {
        Write-Host "[FAILURE] Tests failed with exit code: $exitCode" -ForegroundColor Red
        Write-Host "Duration: $($duration.ToString('mm\:ss'))" -ForegroundColor White

        # Common exit codes
        switch ($exitCode) {
            1 { Write-Host "  Reason: Test failures detected" -ForegroundColor Yellow }
            2 { Write-Host "  Reason: Compilation errors" -ForegroundColor Yellow }
            3 { Write-Host "  Reason: Unity license issue" -ForegroundColor Yellow }
            default { Write-Host "  Reason: Unknown error" -ForegroundColor Yellow }
        }

        Write-Host
        Write-Host "Check the log file for details:" -ForegroundColor White
        Write-Host "  $logFile" -ForegroundColor Gray

        # Display last lines of log for debugging
        if (Test-Path $logFile) {
            Write-Host
            Write-Host "Last 30 lines from log:" -ForegroundColor Yellow
            Write-Host "-----------------------------------------------------------------------------" -ForegroundColor Gray
            Get-Content $logFile -Tail 30 | Write-Host
            Write-Host "-----------------------------------------------------------------------------" -ForegroundColor Gray
        }
    }

    Write-Host "=============================================================================" -ForegroundColor Cyan

    # Open results folder if requested
    if ($OpenResults) {
        Write-Host
        Write-Host "Opening results folder..." -ForegroundColor Yellow
        Start-Process explorer.exe $testResultsDir
    }

    # Return exit code for CI/CD pipelines
    exit $exitCode

}
catch {
    Write-Host "[ERROR] Failed to run Unity tests: $_" -ForegroundColor Red
    exit 1
}