@echo off
setlocal

set SCRIPT_DIR=%~dp0
set PROJECT_ROOT=%SCRIPT_DIR%..\rpg_3d_2604140
pushd "%SCRIPT_DIR%"

echo [1/4] Export FGUI scatter...
pwsh -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%Export-FguiScatter.ps1" -ProjectRoot "%PROJECT_ROOT%"
if %errorlevel% neq 0 (
    echo FGUI scatter export failed. Exiting.
    set EXIT_CODE=%errorlevel%
    goto :FAIL
)

echo [2/4] Validate FGUI export names...
pwsh -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%Validate-FguiExportNames.ps1" -ProjectRoot "%PROJECT_ROOT%"
if %errorlevel% neq 0 (
    echo FGUI export name validation failed. Exiting.
    set EXIT_CODE=%errorlevel%
    goto :FAIL
)

echo [3/4] Validate FGUI scatter manifest...
pwsh -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%Check-FguiScatterManifest.ps1" -ProjectRoot "%PROJECT_ROOT%"
if %errorlevel% neq 0 (
    echo FGUI scatter manifest validation failed. Exiting.
    set EXIT_CODE=%errorlevel%
    goto :FAIL
)

echo [4/4] Validate FGUI movieclip runtime assets...
pwsh -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%Check-FguiMovieClipRuntime.ps1" -ProjectRoot "%PROJECT_ROOT%"
if %errorlevel% neq 0 (
    echo FGUI movieclip runtime validation failed. Exiting.
    set EXIT_CODE=%errorlevel%
    goto :FAIL
)

echo All checks passed.
popd
exit /b 0

:FAIL
echo.
echo Build failed with exit code: %EXIT_CODE%
popd
pause
exit /b %EXIT_CODE%
