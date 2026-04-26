@echo off
setlocal

set SCRIPT_DIR=%~dp0
set PROJECT_ROOT=%SCRIPT_DIR%..\new_260409
pushd "%SCRIPT_DIR%"

pwsh -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%Check-FguiMigrationRefs.ps1" -ProjectRoot "%PROJECT_ROOT%"
set EXIT_CODE=%ERRORLEVEL%

popd

if not %EXIT_CODE%==0 (
    echo FGUI migration reference check failed.
    exit /b %EXIT_CODE%
)

echo FGUI migration reference check passed.
exit /b 0


