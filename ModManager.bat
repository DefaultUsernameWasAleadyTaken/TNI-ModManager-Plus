@echo off
SETLOCAL EnableExtensions EnableDelayedExpansion
SET "ROOT=%~dp0"
SET "PUB=%ROOT%mod-manager-plus\publish\win-x64\TNI-ModManager-Plus.exe"
SET "PROJ=%ROOT%mod-manager-plus\src\TniModManager\TniModManager.csproj"
SET "DOTNET_CHANNEL=8.0"

REM Force published binary: set TNI_MM_PREFER_BUNDLE=1
REM Skip auto-install of SDK: set TNI_MM_AUTO_INSTALL_DOTNET=0

if /I "%TNI_MM_PREFER_BUNDLE%"=="1" goto run_published

call :ensure_dotnet_sdk
if not errorlevel 1 goto run_source

if exist "%PUB%" goto run_published

if /I "%TNI_MM_AUTO_INSTALL_DOTNET%"=="0" goto fail_no_dotnet

echo.
echo [.NET] SDK %DOTNET_CHANNEL% not found — installing to %%LOCALAPPDATA%%\Microsoft\dotnet ...
echo       (other dotnet versions on PATH are not enough for this app)
call :install_dotnet_sdk
if errorlevel 1 goto fail_no_dotnet

call :ensure_dotnet_sdk
if not errorlevel 1 goto run_source

goto fail_no_dotnet

:run_source
REM WinExe + start: не оставляем консоль от "dotnet run" (dotnet.exe — console app).
cd /d "%ROOT%mod-manager-plus"
echo Building Mod Manager Plus...
dotnet build "%PROJ%" -nologo -v q
if errorlevel 1 (
  echo Build failed.
  pause
  exit /b 1
)
SET "EXE=%ROOT%mod-manager-plus\src\TniModManager\bin\Debug\net8.0\TNI-ModManager-Plus.exe"
if not exist "!EXE!" SET "EXE=%ROOT%mod-manager-plus\src\TniModManager\bin\Release\net8.0\TNI-ModManager-Plus.exe"
if not exist "!EXE!" (
  echo Built binary not found under bin\Debug|Release\net8.0\
  pause
  exit /b 1
)
start "" "!EXE!"
exit /b 0

:run_published
if exist "%PUB%" (
  start "" "%PUB%"
  exit /b 0
)
echo Published binary not found:
echo   %PUB%
echo Build it with: mod-manager-plus\scripts\publish.cmd win-x64
pause
exit /b 1

:fail_no_dotnet
echo.
echo .NET %DOTNET_CHANNEL% SDK not found and no published binary at:
echo   %PUB%
echo Install SDK: https://dotnet.microsoft.com/download/dotnet/8.0
echo Or publish:  mod-manager-plus\scripts\publish.cmd win-x64
pause
exit /b 1

REM --- helpers ---------------------------------------------------------------

:ensure_dotnet_sdk
REM Prefer user-local installs first (auto-install / portable).
if exist "%LOCALAPPDATA%\Microsoft\dotnet\dotnet.exe" (
  SET "PATH=%LOCALAPPDATA%\Microsoft\dotnet;%PATH%"
  SET "DOTNET_ROOT=%LOCALAPPDATA%\Microsoft\dotnet"
)
if exist "%USERPROFILE%\.dotnet\dotnet.exe" (
  SET "PATH=%USERPROFILE%\.dotnet;%PATH%"
  SET "DOTNET_ROOT=%USERPROFILE%\.dotnet"
)

where dotnet >nul 2>&1
if errorlevel 1 exit /b 1

REM Need an 8.x SDK (Runtime alone / older SDK is not enough for "dotnet run").
dotnet --list-sdks 2>nul | findstr /R /C:"^8\." >nul
if errorlevel 1 exit /b 1
exit /b 0

:install_dotnet_sdk
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$ErrorActionPreference='Stop';" ^
  "$installDir=Join-Path $env:LOCALAPPDATA 'Microsoft\dotnet';" ^
  "$script=Join-Path $env:TEMP 'tni-dotnet-install.ps1';" ^
  "Write-Host '[.NET] Downloading install script...';" ^
  "Invoke-WebRequest -Uri 'https://dot.net/v1/dotnet-install.ps1' -OutFile $script -UseBasicParsing;" ^
  "Write-Host '[.NET] Installing SDK channel %DOTNET_CHANNEL% to' $installDir;" ^
  "& $script -Channel '%DOTNET_CHANNEL%' -InstallDir $installDir;" ^
  "Write-Host '[.NET] Done.'"
exit /b %ERRORLEVEL%
