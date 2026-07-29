@echo off
SETLOCAL
SET ROOT=%~dp0
SET PUB=%ROOT%mod-manager-plus\publish\win-x64\TNI-ModManager-Plus.exe
SET PROJ=%ROOT%mod-manager-plus\src\TniModManager\TniModManager.csproj

REM Dev default: run from source when SDK is available.
REM Force published binary: set TNI_MM_PREFER_BUNDLE=1
if not "%TNI_MM_PREFER_BUNDLE%"=="1" (
  where dotnet >nul 2>&1
  if %ERRORLEVEL% EQU 0 (
    cd /d "%ROOT%mod-manager-plus"
    dotnet run --project "%PROJ%"
    if %ERRORLEVEL% NEQ 0 pause
    exit /b %ERRORLEVEL%
  )
)

if exist "%PUB%" (
  start "" "%PUB%"
  exit /b 0
)

echo .NET 8 SDK not found and no published binary at:
echo   %PUB%
echo Install SDK from https://dotnet.microsoft.com/download/dotnet/8.0
pause
exit /b 1
