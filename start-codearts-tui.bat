@echo off
chcp 65001 >nul
setlocal

rem This BAT is stored in the repository root, so its directory is the workspace.
cd /d "%~dp0"

where codearts >nul 2>nul
if errorlevel 1 (
    echo [ERROR] CodeArts CLI was not found in PATH.
    echo Please reinstall CodeArts CLI or add its installers directory to PATH.
    pause
    exit /b 1
)

echo [CodeArts] Starting TUI in: %CD%
call codearts "%CD%"
set "EXIT_CODE=%ERRORLEVEL%"

echo.
echo [CodeArts] TUI exited with code: %EXIT_CODE%
pause
exit /b %EXIT_CODE%
