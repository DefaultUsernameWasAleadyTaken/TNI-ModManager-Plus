@echo off
REM Tower Networking Inc - Mod Manager Launcher
REM Launches PowerShell Mod Manager from mod-manager-plus\

SET SCRIPT_DIR=%~dp0
SET MM_DIR=%SCRIPT_DIR%mod-manager-plus
cd /d "%MM_DIR%"

where pwsh >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    pwsh -NoProfile -ExecutionPolicy Bypass -File "%MM_DIR%\ModManagerGUI.ps1"
) else (
    powershell -NoProfile -ExecutionPolicy Bypass -File "%MM_DIR%\ModManagerGUI.ps1"
)

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo An error occurred. Press any key to exit...
    pause >nul
)
