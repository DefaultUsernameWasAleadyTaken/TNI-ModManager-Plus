@echo off
REM Self-contained publish: TNI-ModManager-Plus.exe (fixed name, no version suffix)
SET PROJ=%~dp0..\src\TniModManager\TniModManager.csproj
SET RID=%1
IF "%RID%"=="" SET RID=win-x64
SET OUT=%~dp0..\publish\%RID%

dotnet publish "%PROJ%" -c Release -r %RID% --self-contained true -o "%OUT%"
echo Published: %OUT%\TNI-ModManager-Plus.exe
