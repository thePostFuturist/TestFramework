@echo off
:: =============================================================================
:: Unity Targeted Test Runner - Run Specific Tests/Categories
:: =============================================================================
:: This script is designed for CI/CD pipelines to run specific test suites
:: Usage: run-specific-tests.bat <TestSuite> [Platform]
::
:: Predefined Test Suites:
::   unit        - Run unit tests only
::   integration - Run integration tests
::   dots        - Run DOTS/ECS tests
::   performance - Run performance tests
::   critical    - Run critical/smoke tests
::   all         - Run all tests
::   custom      - Specify custom filter (requires additional parameter)
::
:: Examples:
::   run-specific-tests.bat unit
::   run-specific-tests.bat integration PlayMode
::   run-specific-tests.bat dots EditMode
::   run-specific-tests.bat custom "MyNamespace.MyTestClass" EditMode
:: =============================================================================

setlocal enabledelayedexpansion

:: Configuration
set UNITY_VERSION=6000.0.47f1
set UNITY_PATH="C:\Program Files\Unity\Hub\Editor\%UNITY_VERSION%\Editor\Unity.exe"
set PROJECT_PATH=D:\Dev\TestFramework

:: Test suite definitions (customize these for your project)
set SUITE_UNIT=TestFramework.Unity.Tests.Unit
set SUITE_INTEGRATION=TestFramework.Unity.Tests.Integration
set SUITE_DOTS=TestFramework.DOTS.Tests
set SUITE_PERFORMANCE=TestFramework.Performance
set SUITE_CRITICAL=Critical,Smoke,Regression

:: Parse arguments
set TEST_SUITE=%~1
set CUSTOM_FILTER=%~2
set TEST_PLATFORM=%~3

:: Validate test suite parameter
if "%TEST_SUITE%"=="" (
    echo [ERROR] Test suite not specified
    goto :ShowUsage
)

:: Set default platform if not specified
if "%TEST_PLATFORM%"=="" (
    if "%TEST_SUITE%"=="custom" (
        set TEST_PLATFORM=%~3
    ) else (
        set TEST_PLATFORM=%~2
    )
)
if "%TEST_PLATFORM%"=="" set TEST_PLATFORM=EditMode

:: Determine filter based on test suite
set TEST_FILTER=
set TEST_CATEGORIES=
set SUITE_NAME=%TEST_SUITE%

if /i "%TEST_SUITE%"=="unit" (
    set TEST_FILTER=%SUITE_UNIT%
    set SUITE_NAME=Unit Tests
) else if /i "%TEST_SUITE%"=="integration" (
    set TEST_FILTER=%SUITE_INTEGRATION%
    set SUITE_NAME=Integration Tests
) else if /i "%TEST_SUITE%"=="dots" (
    set TEST_FILTER=%SUITE_DOTS%
    set SUITE_NAME=DOTS/ECS Tests
) else if /i "%TEST_SUITE%"=="performance" (
    set TEST_FILTER=%SUITE_PERFORMANCE%
    set SUITE_NAME=Performance Tests
) else if /i "%TEST_SUITE%"=="critical" (
    set TEST_CATEGORIES=%SUITE_CRITICAL%
    set SUITE_NAME=Critical Tests
) else if /i "%TEST_SUITE%"=="all" (
    set SUITE_NAME=All Tests
) else if /i "%TEST_SUITE%"=="custom" (
    if "%CUSTOM_FILTER%"=="" (
        echo [ERROR] Custom filter not specified
        goto :ShowUsage
    )
    set TEST_FILTER=%CUSTOM_FILTER%
    set SUITE_NAME=Custom Tests [%CUSTOM_FILTER%]
) else (
    echo [ERROR] Unknown test suite: %TEST_SUITE%
    goto :ShowUsage
)

:: Check Unity installation
if not exist %UNITY_PATH% (
    echo [ERROR] Unity %UNITY_VERSION% not found
    echo Please update UNITY_VERSION in this script
    exit /b 1
)

:: Generate timestamp
for /f "tokens=2 delims==" %%I in ('wmic os get localdatetime /value') do set datetime=%%I
set TIMESTAMP=%datetime:~0,8%_%datetime:~8,6%

:: Setup output paths
set OUTPUT_DIR=%PROJECT_PATH%\TestResults\%TEST_SUITE%
if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"

set XML_FILE=%OUTPUT_DIR%\Results_%TIMESTAMP%.xml
set LOG_FILE=%OUTPUT_DIR%\unity_%TIMESTAMP%.log
set SUMMARY_FILE=%OUTPUT_DIR%\summary_%TIMESTAMP%.txt

:: Build Unity command
set UNITY_CMD=%UNITY_PATH% -batchmode -quit -projectPath "%PROJECT_PATH%"
set UNITY_CMD=%UNITY_CMD% -runTests -testPlatform %TEST_PLATFORM%
set UNITY_CMD=%UNITY_CMD% -testResultFile "%XML_FILE%" -logFile "%LOG_FILE%"

if not "%TEST_FILTER%"=="" (
    set UNITY_CMD=%UNITY_CMD% -testFilter "%TEST_FILTER%"
)
if not "%TEST_CATEGORIES%"=="" (
    set UNITY_CMD=%UNITY_CMD% -testCategories "%TEST_CATEGORIES%"
)

:: Display header
cls
echo =============================================================================
echo Unity Targeted Test Runner
echo =============================================================================
echo Test Suite:     %SUITE_NAME%
echo Platform:       %TEST_PLATFORM%
if not "%TEST_FILTER%"=="" echo Filter:         %TEST_FILTER%
if not "%TEST_CATEGORIES%"=="" echo Categories:     %TEST_CATEGORIES%
echo -----------------------------------------------------------------------------
echo Output Files:
echo   Results:      %XML_FILE%
echo   Log:          %LOG_FILE%
echo =============================================================================
echo.

:: Record start time
echo [%date% %time%] Starting %SUITE_NAME%...
set START_TIME=%time%

:: Run tests
%UNITY_CMD%
set EXIT_CODE=%ERRORLEVEL%

:: Record end time
set END_TIME=%time%

:: Process results
echo.
echo =============================================================================

if %EXIT_CODE% EQU 0 (
    echo [SUCCESS] %SUITE_NAME% passed!
    set RESULT_STATUS=PASSED
    set RESULT_COLOR=green
) else (
    echo [FAILURE] %SUITE_NAME% failed with exit code: %EXIT_CODE%
    set RESULT_STATUS=FAILED
    set RESULT_COLOR=red

    :: Show specific error types
    if %EXIT_CODE% EQU 1 echo   Reason: Test failures detected
    if %EXIT_CODE% EQU 2 echo   Reason: Compilation errors
    if %EXIT_CODE% EQU 3 echo   Reason: Unity license issue
)

:: Parse XML results if available
if exist "%XML_FILE%" (
    echo.
    echo Test Statistics:
    powershell -NoProfile -Command "& { [xml]$xml = Get-Content '%XML_FILE%'; $tr = $xml.'test-run'; Write-Host \"  Total:   $($tr.testcasecount)\"; Write-Host \"  Passed:  $($tr.passed)\" -ForegroundColor Green; Write-Host \"  Failed:  $($tr.failed)\" -ForegroundColor Red; Write-Host \"  Skipped: $($tr.skipped)\" -ForegroundColor Yellow }" 2>nul
)

:: Generate summary for CI/CD
(
    echo Test Suite: %SUITE_NAME%
    echo Status: %RESULT_STATUS%
    echo Exit Code: %EXIT_CODE%
    echo Platform: %TEST_PLATFORM%
    echo Start Time: %START_TIME%
    echo End Time: %END_TIME%
    echo XML Results: %XML_FILE%
    echo Log File: %LOG_FILE%
) > "%SUMMARY_FILE%"

:: Generate JUnit-style output for CI/CD (if needed)
if exist "%XML_FILE%" (
    set JUNIT_FILE=%OUTPUT_DIR%\junit_%TIMESTAMP%.xml
    copy "%XML_FILE%" "!JUNIT_FILE!" >nul 2>&1
)

:: Output for CI/CD parsing
echo.
echo CI/CD Output:
echo ##teamcity[testSuiteStarted name='%SUITE_NAME%']
if %EXIT_CODE% NEQ 0 (
    echo ##teamcity[testSuiteFinished name='%SUITE_NAME%' message='Tests failed']
) else (
    echo ##teamcity[testSuiteFinished name='%SUITE_NAME%']
)

echo =============================================================================

:: Exit with test result code
exit /b %EXIT_CODE%

:ShowUsage
echo.
echo Usage: %~nx0 ^<TestSuite^> [Platform] [CustomFilter]
echo.
echo Test Suites:
echo   unit        - Run unit tests
echo   integration - Run integration tests
echo   dots        - Run DOTS/ECS tests
echo   performance - Run performance tests
echo   critical    - Run critical/smoke tests
echo   all         - Run all tests
echo   custom      - Run custom filtered tests (requires filter parameter)
echo.
echo Platforms:
echo   EditMode    - Run Edit Mode tests (default)
echo   PlayMode    - Run Play Mode tests
echo.
echo Examples:
echo   %~nx0 unit
echo   %~nx0 unit EditMode
echo   %~nx0 integration PlayMode
echo   %~nx0 custom "MyNamespace.MyTests" EditMode
echo   %~nx0 critical
echo.
echo Environment Variables:
echo   UNITY_VERSION  - Unity version to use (current: %UNITY_VERSION%)
echo   PROJECT_PATH   - Path to Unity project (current: %PROJECT_PATH%)
echo.
echo Test Suite Definitions (customize in script):
echo   SUITE_UNIT=%SUITE_UNIT%
echo   SUITE_INTEGRATION=%SUITE_INTEGRATION%
echo   SUITE_DOTS=%SUITE_DOTS%
echo   SUITE_PERFORMANCE=%SUITE_PERFORMANCE%
echo   SUITE_CRITICAL=%SUITE_CRITICAL%
echo.
exit /b 1