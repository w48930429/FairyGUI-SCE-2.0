@echo off
setlocal
cd /d "%~dp0"
codex --dangerously-bypass-approvals-and-sandbox %*
endlocal
