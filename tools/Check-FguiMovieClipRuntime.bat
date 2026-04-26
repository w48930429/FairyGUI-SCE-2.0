@echo off
setlocal

set SCRIPT_DIR=%~dp0
set PROJECT_ROOT=%SCRIPT_DIR%..\new_260409
pushd "%SCRIPT_DIR%"

pwsh -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%Check-FguiMovieClipRuntime.ps1" -ProjectRoot "%PROJECT_ROOT%"
set EXIT_CODE=%ERRORLEVEL%

popd

if not %EXIT_CODE%==0 (
    echo FGUI movieclip runtime check failed.
    exit /b %EXIT_CODE%
)

echo FGUI movieclip runtime check passed.
exit /b 0

