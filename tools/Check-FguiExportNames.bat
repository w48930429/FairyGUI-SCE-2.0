@echo off
setlocal

set SCRIPT_DIR=%~dp0
set PROJECT_ROOT=%SCRIPT_DIR%..\new_260409
pushd "%SCRIPT_DIR%"

pwsh -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%Validate-FguiExportNames.ps1" -ProjectRoot "%PROJECT_ROOT%"
set EXIT_CODE=%ERRORLEVEL%

popd

if not %EXIT_CODE%==0 (
    echo FGUI export name validation failed.
    exit /b %EXIT_CODE%
)

echo FGUI export name validation passed.
exit /b 0

