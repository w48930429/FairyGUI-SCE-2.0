@echo off
setlocal

set SCRIPT_DIR=%~dp0
set PROJECT_ROOT=%SCRIPT_DIR%..\rpg_3d_2604140
pushd "%SCRIPT_DIR%"

pwsh -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%Export-FguiScatter.ps1" -ProjectRoot "%PROJECT_ROOT%"
set EXIT_CODE=%ERRORLEVEL%

popd

if not %EXIT_CODE%==0 (
    echo FGUI scatter export failed.
    echo.
    echo Export failed with exit code: %EXIT_CODE%
    pause
    exit /b %EXIT_CODE%
)

echo FGUI scatter export passed.
exit /b 0
