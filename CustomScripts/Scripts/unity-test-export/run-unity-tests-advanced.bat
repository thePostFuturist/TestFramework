@echo off
:: =============================================================================
:: Unity Test Runner with XML Export - Advanced Version
:: =============================================================================
:: This script runs Unity tests with configurable options
:: Usage: run-unity-tests-advanced.bat [EditMode|PlayMode] [TestFilter] [Categories]
:: Examples:
::   run-unity-tests-advanced.bat
::   run-unity-tests-advanced.bat EditMode
::   run-unity-tests-advanced.bat PlayMode "TestNamespace.TestClass"
::   run-unity-tests-advanced.bat EditMode "" "Integration,Critical"
:: =============================================================================

setlocal enabledelayedexpansion

:: Default Configuration
set UNITY_VERSION=6000.0.23f1
set DEFAULT_PLATFORM=EditMode
set PROJECT_PATH=D:\Dev\TestFramework

:: Parse command line arguments
set TEST_PLATFORM=%~1
set TEST_FILTER=%~2
set TEST_CATEGORIES=%~3

:: Use defaults if not specified
if "%TEST_PLATFORM%"=="" set TEST_PLATFORM=%DEFAULT_PLATFORM%
if "%TEST_PLATFORM%"=="help" goto :ShowHelp
if "%TEST_PLATFORM%"=="-h" goto :ShowHelp
if "%TEST_PLATFORM%"=="/?" goto :ShowHelp

:: Validate test platform
if /i not "%TEST_PLATFORM%"=="EditMode" if /i not "%TEST_PLATFORM%"=="PlayMode" (
    echo [ERROR] Invalid test platform: %TEST_PLATFORM%
    echo Valid options: EditMode, PlayMode
    exit /b 1
)

:: Find Unity installation
call :FindUnity UNITY_PATH
if "%UNITY_PATH%"=="" (
    echo [ERROR] Unity %UNITY_VERSION% not found
    echo Please install Unity or update UNITY_VERSION in this script
    exit /b 1
)

:: Generate timestamp
for /f "tokens=2 delims==" %%I in ('wmic os get localdatetime /value') do set datetime=%%I
set TIMESTAMP=%datetime:~0,8%_%datetime:~8,6%

:: Setup paths
set OUTPUT_DIR=%PROJECT_PATH%\TestResults
set XML_FILE=%OUTPUT_DIR%\TestResults_%TEST_PLATFORM%_%TIMESTAMP%.xml
set LOG_FILE=%OUTPUT_DIR%\unity_%TEST_PLATFORM%_%TIMESTAMP%.log
set SUMMARY_FILE=%OUTPUT_DIR%\TestResults_%TEST_PLATFORM%_%TIMESTAMP%.summary.txt

:: Create output directory
if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"

:: Build command line
set UNITY_CMD=%UNITY_PATH% -batchmode -quit -projectPath "%PROJECT_PATH%" -runTests -testPlatform %TEST_PLATFORM%
set UNITY_CMD=%UNITY_CMD% -testResultFile "%XML_FILE%" -logFile "%LOG_FILE%"

:: Add optional filters
if not "%TEST_FILTER%"=="" (
    set UNITY_CMD=%UNITY_CMD% -testFilter "%TEST_FILTER%"
)
if not "%TEST_CATEGORIES%"=="" (
    set UNITY_CMD=%UNITY_CMD% -testCategories "%TEST_CATEGORIES%"
)

:: Display configuration
call :PrintHeader
echo Configuration:
echo   Unity Version:    %UNITY_VERSION%
echo   Project Path:     %PROJECT_PATH%
echo   Test Platform:    %TEST_PLATFORM%
if not "%TEST_FILTER%"=="" echo   Test Filter:      %TEST_FILTER%
if not "%TEST_CATEGORIES%"=="" echo   Test Categories:  %TEST_CATEGORIES%
echo.
echo Output Files:
echo   XML Results:      %XML_FILE%
echo   Unity Log:        %LOG_FILE%
echo   Summary:          %SUMMARY_FILE%
call :PrintLine
echo.

:: Start timer
set START_TIME=%time%

:: Run tests
echo [%time%] Starting Unity tests...
echo.
echo Command: %UNITY_CMD%
echo.

%UNITY_CMD%
set EXIT_CODE=%ERRORLEVEL%

:: Calculate duration
call :GetDuration "%START_TIME%" "%time%" DURATION

:: Process results
echo.
call :PrintLine
call :ProcessResults %EXIT_CODE%
echo.
echo Test Duration: %DURATION%
call :PrintLine

:: Generate custom summary
call :GenerateSummary

:: Optionally open results
if "%4"=="open" (
    echo Opening results folder...
    start "" "%OUTPUT_DIR%"
)

exit /b %EXIT_CODE%

:: =============================================================================
:: Functions
:: =============================================================================

:FindUnity
:: Find Unity installation
set "%1="
set UNITY_BASE=C:\Program Files\Unity\Hub\Editor

:: First try exact version
if exist "%UNITY_BASE%\%UNITY_VERSION%\Editor\Unity.exe" (
    set "%1=%UNITY_BASE%\%UNITY_VERSION%\Editor\Unity.exe"
    goto :eof
)

:: Try to find any 6000.x version
for /d %%D in ("%UNITY_BASE%\6000.*") do (
    if exist "%%D\Editor\Unity.exe" (
        set "%1=%%D\Editor\Unity.exe"
        set UNITY_VERSION=%%~nxD
        goto :eof
    )
)

:: Try to find any version
for /d %%D in ("%UNITY_BASE%\*") do (
    if exist "%%D\Editor\Unity.exe" (
        set "%1=%%D\Editor\Unity.exe"
        set UNITY_VERSION=%%~nxD
        echo [WARNING] Using Unity version: %%~nxD
        goto :eof
    )
)
goto :eof

:ProcessResults
if %1 EQU 0 (
    echo [SUCCESS] All tests passed!
    call :ParseXMLResults
) else if %1 EQU 1 (
    echo [FAILURE] Some tests failed!
    call :ParseXMLResults
    call :ShowFailedTests
) else if %1 EQU 2 (
    echo [ERROR] Compilation errors detected!
    echo Check the log file for details: %LOG_FILE%
    if exist "%LOG_FILE%" (
        echo.
        echo Compilation errors:
        findstr /i "error CS" "%LOG_FILE%"
    )
) else if %1 EQU 3 (
    echo [ERROR] Unity license issue!
    echo Please activate your Unity license
) else (
    echo [ERROR] Unknown error code: %1
    echo Check the log file for details: %LOG_FILE%
)
goto :eof

:ParseXMLResults
:: Try to parse XML results using PowerShell
if not exist "%XML_FILE%" (
    echo [WARNING] XML results file not found
    goto :eof
)

echo.
echo Test Results Summary:
powershell -NoProfile -Command "& { [xml]$xml = Get-Content '%XML_FILE%'; $tr = $xml.'test-run'; Write-Host \"  Total Tests:  $($tr.testcasecount)\"; Write-Host \"  Passed:       $($tr.passed)\" -ForegroundColor Green; Write-Host \"  Failed:       $($tr.failed)\" -ForegroundColor $(if ($tr.failed -eq '0') {'Gray'} else {'Red'}); Write-Host \"  Skipped:      $($tr.skipped)\" -ForegroundColor Yellow; Write-Host \"  Duration:     $($tr.duration)s\" }" 2>nul
goto :eof

:ShowFailedTests
:: Display failed tests from XML
echo.
echo Failed Tests:
powershell -NoProfile -Command "& { [xml]$xml = Get-Content '%XML_FILE%'; $xml.SelectNodes('//test-case[@result=\"Failed\"]') | ForEach-Object { Write-Host \"  - $($_.fullname)\" -ForegroundColor Red } }" 2>nul
goto :eof

:GenerateSummary
:: Create a custom summary file
(
    echo Unity Test Results Summary
    echo ==========================
    echo Generated: %date% %time%
    echo.
    echo Configuration:
    echo   Platform: %TEST_PLATFORM%
    if not "%TEST_FILTER%"=="" echo   Filter: %TEST_FILTER%
    if not "%TEST_CATEGORIES%"=="" echo   Categories: %TEST_CATEGORIES%
    echo   Duration: %DURATION%
    echo.
    echo Results:
    if exist "%XML_FILE%" (
        powershell -NoProfile -Command "& { [xml]$xml = Get-Content '%XML_FILE%'; $tr = $xml.'test-run'; Write-Host \"  Total: $($tr.testcasecount)\"; Write-Host \"  Passed: $($tr.passed)\"; Write-Host \"  Failed: $($tr.failed)\"; Write-Host \"  Skipped: $($tr.skipped)\" }" 2>nul
    ) else (
        echo   [No XML results available]
    )
    echo.
    echo Files:
    echo   XML: %XML_FILE%
    echo   Log: %LOG_FILE%
) > "%SUMMARY_FILE%"

echo Summary saved to: %SUMMARY_FILE%
goto :eof

:GetDuration
:: Calculate duration between two times
set START=%~1
set END=%~2
:: Simple duration calculation (not perfect but works for most cases)
set /a HOURS=%END:~0,2%-%START:~0,2%
set /a MINS=%END:~3,2%-%START:~3,2%
set /a SECS=%END:~6,2%-%START:~6,2%
if %SECS% LSS 0 set /a SECS+=60 & set /a MINS-=1
if %MINS% LSS 0 set /a MINS+=60 & set /a HOURS-=1
set "%3=%HOURS%h %MINS%m %SECS%s"
goto :eof

:PrintHeader
echo =============================================================================
echo Unity Test Runner with XML Export - Advanced
echo =============================================================================
goto :eof

:PrintLine
echo -----------------------------------------------------------------------------
goto :eof

:ShowHelp
echo.
call :PrintHeader
echo.
echo Usage: %~nx0 [TestPlatform] [TestFilter] [TestCategories] [open]
echo.
echo Parameters:
echo   TestPlatform    - EditMode or PlayMode (default: EditMode)
echo   TestFilter      - Filter tests by name (e.g., "Namespace.Class.Method")
echo   TestCategories  - Comma-separated categories (e.g., "Unit,Integration")
echo   open           - Add "open" as 4th parameter to open results folder
echo.
echo Examples:
echo   %~nx0
echo   %~nx0 EditMode
echo   %~nx0 PlayMode
echo   %~nx0 PlayMode "MyTests.PlayerTests"
echo   %~nx0 EditMode "" "Critical,Regression"
echo   %~nx0 EditMode "" "" open
echo.
echo Environment Variables:
echo   UNITY_VERSION   - Unity version to use (current: %UNITY_VERSION%)
echo   PROJECT_PATH    - Path to Unity project (current: %PROJECT_PATH%)
echo.
call :PrintLine
exit /b 0