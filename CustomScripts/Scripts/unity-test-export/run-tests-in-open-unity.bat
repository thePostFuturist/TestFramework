@echo off
:: =============================================================================
:: Run Tests in Already-Open Unity Instance
:: =============================================================================
:: This script communicates with an already-running Unity Editor instance
:: It creates a trigger file that Unity monitors to start tests
:: =============================================================================

setlocal enabledelayedexpansion

set PROJECT_PATH=D:\Dev\TestFramework
set TEST_SUITE=%~1

:: Default to all tests
if "%TEST_SUITE%"=="" set TEST_SUITE=all

:: Clear TestResults directory first
set OUTPUT_DIR=%PROJECT_PATH%\TestResults
if exist "%OUTPUT_DIR%" (
    echo Clearing previous test results...
    del /Q "%OUTPUT_DIR%\*.*" 2>nul
    for /d %%i in ("%OUTPUT_DIR%\*") do rd /s /q "%%i" 2>nul
)

:: Create trigger file that Unity will detect
set TRIGGER_FILE=%PROJECT_PATH%\Assets\TestFramework\Unity\TestResultExport\Editor\run_tests_trigger.txt

echo =============================================================================
echo Run Tests in Already-Open Unity Instance
echo =============================================================================
echo [INFO] This script triggers tests in your already-open Unity Editor
echo [INFO] Make sure Unity is running with the TestFramework project
echo.
echo Test Suite: %TEST_SUITE%
echo [INFO] Cleared TestResults directory
echo =============================================================================
echo.

:: Write test configuration to trigger file
echo test_suite=%TEST_SUITE% > "%TRIGGER_FILE%"
echo timestamp=%date% %time% >> "%TRIGGER_FILE%"

echo [SUCCESS] Test trigger created!
echo.
echo Tests will start automatically in Unity...
echo Check Unity Console for progress
echo Results will be saved to TestResults folder
echo =============================================================================

:: Create TestResults folder if needed
set OUTPUT_DIR=%PROJECT_PATH%\TestResults
if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"

:: Exit immediately without pause
exit /b 0