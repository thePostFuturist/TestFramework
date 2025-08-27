@echo off
:: =============================================================================
:: Unity Test Runner - Editor Instance Mode (No Batch Mode)
:: =============================================================================
:: This script runs tests in an existing Unity Editor instance without -batchmode
:: This avoids crashes and errors that occur with batch mode
:: 
:: Usage: run-unity-tests-editor.bat [TestSuite] [Platform]
:: 
:: Test Suites:
::   all         - Run all tests (default)
::   edit        - Run EditMode tests only
::   play        - Run PlayMode tests only
::   unit        - Run unit tests
::   integration - Run integration tests
::   critical    - Run critical tests
::   filtered    - Run with custom filter (reads from -testFilter argument)
::
:: Examples:
::   run-unity-tests-editor.bat
::   run-unity-tests-editor.bat all
::   run-unity-tests-editor.bat edit
::   run-unity-tests-editor.bat unit
:: =============================================================================

setlocal enabledelayedexpansion

:: Configuration
set UNITY_VERSION=6000.0.47f1
set UNITY_PATH="C:\Program Files\Unity\Hub\Editor\%UNITY_VERSION%\Editor\Unity.exe"
set PROJECT_PATH=D:\Dev\TestFramework
set TEST_SUITE=%~1
set TEST_PLATFORM=%~2

:: Default values
if "%TEST_SUITE%"=="" set TEST_SUITE=all
if "%TEST_PLATFORM%"=="" set TEST_PLATFORM=EditMode

:: Check if Unity exists
if not exist %UNITY_PATH% (
    echo [ERROR] Unity %UNITY_VERSION% not found at %UNITY_PATH%
    echo.
    echo Available Unity versions:
    dir "C:\Program Files\Unity\Hub\Editor" /b 2>nul
    exit /b 1
)

:: Generate timestamp
for /f "tokens=2 delims==" %%I in ('wmic os get localdatetime /value') do set datetime=%%I
set TIMESTAMP=%datetime:~0,8%_%datetime:~8,6%

:: Setup output paths
set OUTPUT_DIR=%PROJECT_PATH%\TestResults
if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"

set XML_FILE=%OUTPUT_DIR%\TestResults_Editor_%TIMESTAMP%.xml
set LOG_FILE=%OUTPUT_DIR%\unity_editor_%TIMESTAMP%.log
set EXIT_CODE_FILE=%OUTPUT_DIR%\exit_code.txt

:: Delete previous exit code file
if exist "%EXIT_CODE_FILE%" del "%EXIT_CODE_FILE%"

:: Determine execute method based on test suite
set EXECUTE_METHOD=
if /i "%TEST_SUITE%"=="all" (
    set EXECUTE_METHOD=TestFramework.Unity.TestResultExport.Editor.TestRunnerCommandLine.RunAllTests
    set SUITE_NAME=All Tests
) else if /i "%TEST_SUITE%"=="edit" (
    set EXECUTE_METHOD=TestFramework.Unity.TestResultExport.Editor.TestRunnerCommandLine.RunEditModeTests
    set SUITE_NAME=EditMode Tests
) else if /i "%TEST_SUITE%"=="play" (
    set EXECUTE_METHOD=TestFramework.Unity.TestResultExport.Editor.TestRunnerCommandLine.RunPlayModeTests
    set SUITE_NAME=PlayMode Tests
) else if /i "%TEST_SUITE%"=="unit" (
    set EXECUTE_METHOD=TestFramework.Unity.TestResultExport.Editor.TestRunnerCommandLine.RunUnitTests
    set SUITE_NAME=Unit Tests
) else if /i "%TEST_SUITE%"=="integration" (
    set EXECUTE_METHOD=TestFramework.Unity.TestResultExport.Editor.TestRunnerCommandLine.RunIntegrationTests
    set SUITE_NAME=Integration Tests
) else if /i "%TEST_SUITE%"=="critical" (
    set EXECUTE_METHOD=TestFramework.Unity.TestResultExport.Editor.TestRunnerCommandLine.RunCriticalTests
    set SUITE_NAME=Critical Tests
) else if /i "%TEST_SUITE%"=="filtered" (
    set EXECUTE_METHOD=TestFramework.Unity.TestResultExport.Editor.TestRunnerCommandLine.RunFilteredTests
    set SUITE_NAME=Filtered Tests
) else (
    echo [ERROR] Unknown test suite: %TEST_SUITE%
    goto :ShowHelp
)

:: Display header
cls
echo =============================================================================
echo Unity Test Runner - Editor Instance Mode
echo =============================================================================
echo [INFO] This runs tests in Unity Editor without -batchmode
echo [INFO] Unity will open (or use existing instance) to run tests
echo.
echo Configuration:
echo   Unity Version: %UNITY_VERSION%
echo   Project Path:  %PROJECT_PATH%
echo   Test Suite:    %SUITE_NAME%
echo   Output XML:    %XML_FILE%
echo =============================================================================
echo.

:: Build Unity command (NO -batchmode flag!)
set UNITY_CMD=%UNITY_PATH% -projectPath "%PROJECT_PATH%"
set UNITY_CMD=%UNITY_CMD% -executeMethod %EXECUTE_METHOD%
set UNITY_CMD=%UNITY_CMD% -testResultFile "%XML_FILE%"
set UNITY_CMD=%UNITY_CMD% -logFile "%LOG_FILE%"

:: Add additional arguments for filtered tests
if /i "%TEST_SUITE%"=="filtered" (
    :: Check for additional command line arguments
    if not "%3"=="" set UNITY_CMD=%UNITY_CMD% -testFilter "%3"
    if not "%4"=="" set UNITY_CMD=%UNITY_CMD% -testCategories "%4"
    if not "%5"=="" set UNITY_CMD=%UNITY_CMD% -testPlatform "%5"
)

:: Show warning about Unity opening
echo [WARNING] Unity Editor will open to run tests
echo [INFO] Do not close Unity until tests complete
echo [INFO] Check Unity console for test progress
echo.
echo Press any key to start tests or Ctrl+C to cancel...
pause >nul

:: Record start time
echo.
echo [%date% %time%] Starting %SUITE_NAME% in Unity Editor...
set START_TIME=%time%

:: Run Unity with executeMethod (NOT in batch mode)
echo.
echo Executing: %UNITY_CMD%
echo.
%UNITY_CMD%

:: Unity has exited, check for results
set UNITY_EXIT_CODE=%ERRORLEVEL%

:: Check if exit code file was written
if exist "%EXIT_CODE_FILE%" (
    set /p TEST_EXIT_CODE=<"%EXIT_CODE_FILE%"
    echo [INFO] Test exit code from file: !TEST_EXIT_CODE!
) else (
    set TEST_EXIT_CODE=%UNITY_EXIT_CODE%
    echo [INFO] Using Unity exit code: %UNITY_EXIT_CODE%
)

:: Display results
echo.
echo =============================================================================

if %TEST_EXIT_CODE% EQU 0 (
    echo [SUCCESS] %SUITE_NAME% completed successfully!
    echo.
    echo Test results exported to:
    echo   XML: %XML_FILE%
    echo   Log: %LOG_FILE%
) else (
    echo [FAILURE] %SUITE_NAME% failed with exit code: %TEST_EXIT_CODE%
    echo.
    echo Check the log file for details:
    echo   %LOG_FILE%
    
    :: Check Unity console for errors
    if %UNITY_EXIT_CODE% NEQ 0 (
        echo.
        echo [ERROR] Unity exited with code: %UNITY_EXIT_CODE%
        if %UNITY_EXIT_CODE% EQU 1073741845 (
            echo [ERROR] Unity crashed (Access Violation)
            echo [TIP] This is why we use Editor mode instead of batch mode!
        )
    )
)

:: Parse XML results if available
if exist "%XML_FILE%" (
    echo.
    echo Test Statistics:
    powershell -NoProfile -Command "& { try { [xml]$xml = Get-Content '%XML_FILE%'; $tr = $xml.'test-run'; Write-Host \"  Total:   $($tr.testcasecount)\"; Write-Host \"  Passed:  $($tr.passed)\" -ForegroundColor Green; Write-Host \"  Failed:  $($tr.failed)\" -ForegroundColor Red; Write-Host \"  Skipped: $($tr.skipped)\" -ForegroundColor Yellow; Write-Host \"  Duration: $($tr.duration)s\" } catch { Write-Host '  [Unable to parse XML results]' } }" 2>nul
)

echo =============================================================================

:: Optionally open results folder
if "%6"=="open" (
    echo.
    echo Opening results folder...
    start "" "%OUTPUT_DIR%"
)

:: Exit with test result code
exit /b %TEST_EXIT_CODE%

:ShowHelp
echo.
echo Usage: %~nx0 [TestSuite] [Platform]
echo.
echo Test Suites:
echo   all         - Run all tests (default)
echo   edit        - Run EditMode tests only
echo   play        - Run PlayMode tests only
echo   unit        - Run unit tests
echo   integration - Run integration tests
echo   critical    - Run critical tests
echo   filtered    - Run with custom filter
echo.
echo Examples:
echo   %~nx0
echo   %~nx0 all
echo   %~nx0 edit
echo   %~nx0 unit
echo   %~nx0 filtered "MyNamespace.MyTests" "Unit,Integration" EditMode
echo.
echo Notes:
echo   - Unity Editor will open (not batch mode)
echo   - Tests run in existing Unity instance if already open
echo   - Check Unity console for real-time progress
echo   - XML results exported to TestResults folder
echo.
echo Why Editor Mode?
echo   - Avoids batch mode crashes (error 1073741845)
echo   - Shows visual progress in Unity
echo   - Better error reporting
echo   - Can debug tests interactively
echo.
exit /b 1