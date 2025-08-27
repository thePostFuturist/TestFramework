@echo off
:: =============================================================================
:: Unity Test Runner with XML Export - Basic Version
:: =============================================================================
:: This script runs Unity tests and exports results to XML format
:: Usage: run-unity-tests.bat
:: =============================================================================

setlocal enabledelayedexpansion

:: Configuration - Adjust these paths as needed
set UNITY_VERSION=6000.0.47f1
set UNITY_PATH="C:\Program Files\Unity\Hub\Editor\%UNITY_VERSION%\Editor\Unity.exe"
set PROJECT_PATH=D:\Dev\TestFramework
set TEST_PLATFORM=EditMode

:: Check if Unity exists
if not exist %UNITY_PATH% (
    echo [ERROR] Unity not found at %UNITY_PATH%
    echo Please update UNITY_VERSION in this script
    echo Available versions:
    dir "C:\Program Files\Unity\Hub\Editor" /b 2>nul
    exit /b 1
)

:: Create timestamp for unique filename
for /f "tokens=2 delims==" %%I in ('wmic os get localdatetime /value') do set datetime=%%I
set TIMESTAMP=%datetime:~0,8%_%datetime:~8,6%

:: Create TestResults directory if it doesn't exist
set OUTPUT_DIR=%PROJECT_PATH%\TestResults
if not exist "%OUTPUT_DIR%" (
    echo Creating TestResults directory...
    mkdir "%OUTPUT_DIR%"
)

:: Set output files
set XML_FILE=%OUTPUT_DIR%\TestResults_%TIMESTAMP%.xml
set LOG_FILE=%OUTPUT_DIR%\unity_%TIMESTAMP%.log

:: Display test configuration
echo =============================================================================
echo Unity Test Runner - XML Export
echo =============================================================================
echo Unity Version: %UNITY_VERSION%
echo Project Path:  %PROJECT_PATH%
echo Test Platform: %TEST_PLATFORM%
echo Output XML:    %XML_FILE%
echo Log File:      %LOG_FILE%
echo =============================================================================
echo.
echo Starting Unity tests...

:: Run Unity tests with XML export
%UNITY_PATH% -batchmode -quit ^
    -projectPath "%PROJECT_PATH%" ^
    -runTests ^
    -testPlatform %TEST_PLATFORM% ^
    -testResultFile "%XML_FILE%" ^
    -logFile "%LOG_FILE%"

:: Capture exit code
set EXIT_CODE=%ERRORLEVEL%

:: Display results
echo.
echo =============================================================================
if %EXIT_CODE% EQU 0 (
    echo [SUCCESS] Tests completed successfully!
    echo.
    echo Test results exported to:
    echo   XML: %XML_FILE%
    echo   Log: %LOG_FILE%

    :: Try to display summary if it exists
    set SUMMARY_FILE=%OUTPUT_DIR%\TestResults_%TIMESTAMP%.summary.txt
    if exist "!SUMMARY_FILE!" (
        echo.
        echo Test Summary:
        echo -----------------------------------------------------------------------------
        type "!SUMMARY_FILE!"
        echo -----------------------------------------------------------------------------
    )
) else (
    echo [FAILURE] Tests failed with exit code: %EXIT_CODE%
    echo.
    echo Check the log file for details:
    echo   %LOG_FILE%

    :: Display last few lines of log for quick debugging
    if exist "%LOG_FILE%" (
        echo.
        echo Last 20 lines from log:
        echo -----------------------------------------------------------------------------
        powershell -Command "Get-Content '%LOG_FILE%' -Tail 20"
        echo -----------------------------------------------------------------------------
    )
)
echo =============================================================================

:: Open TestResults folder in Explorer (optional)
:: explorer "%OUTPUT_DIR%"

exit /b %EXIT_CODE%