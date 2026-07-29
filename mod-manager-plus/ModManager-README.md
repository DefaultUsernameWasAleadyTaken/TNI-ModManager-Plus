# TNI Mod Manager Plus

Cross-platform Mod Manager for Tower Networking Inc (.NET 8 + Avalonia).

## Run (dev)

Requires [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```bash
# Linux
./ModManager.sh

# or
cd mod-manager-plus && dotnet run --project src/TniModManager
```

Windows: `ModManager.bat`

## Self-contained publish (no .NET install for users)

Binary name is fixed: **`TNI-ModManager-Plus`** / **`TNI-ModManager-Plus.exe`** (version lives inside the assembly / window title for future in-app updates).

```bash
chmod +x mod-manager-plus/scripts/publish.sh
./mod-manager-plus/scripts/publish.sh linux-x64
# → mod-manager-plus/publish/linux-x64/TNI-ModManager-Plus

# Windows:
# mod-manager-plus\scripts\publish.cmd win-x64
# → mod-manager-plus\publish\win-x64\TNI-ModManager-Plus.exe
```

## Features

- Browse / download / update mods from `CJFWeatherhead/TNI-Mods` GitHub releases
- Enable/disable manual mods; remove downloaded mods
- Edit `entry.lua` configuration parameters (from `metadata.yaml`)
- Command aliases in game `settings.json`
- Launch game via Steam (`2939600`)
