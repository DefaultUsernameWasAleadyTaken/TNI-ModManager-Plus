@echo off
REM Self-contained single-file: TNI-ModManager-Plus.exe (fixed name, no version suffix)
SET PROJ=%~dp0..\src\TniModManager\TniModManager.csproj
SET RID=%1
IF "%RID%"=="" SET RID=win-x64
SET OUT=%~dp0..\publish\%RID%

IF EXIST "%OUT%" rmdir /s /q "%OUT%"
dotnet publish "%PROJ%" -c Release -r %RID% --self-contained true ^
  -p:PublishSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:EnableCompressionInSingleFile=true ^
  -p:DebugType=None ^
  -p:DebugSymbols=false ^
  -o "%OUT%"
echo Published: %OUT%\TNI-ModManager-Plus.exe
dir "%OUT%"
