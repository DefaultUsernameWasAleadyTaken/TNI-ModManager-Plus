# TNI Mod Manager Plus

Cross-platform Mod Manager for Tower Networking Inc (.NET 8 + Avalonia).

## Run (dev)

Needs [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0). Root launchers install it automatically into a user-local folder when missing (and no published binary is present).

```bash
# Linux
./ModManager.sh

# or
cd mod-manager-plus && dotnet run --project src/TniModManager
```

Windows: `ModManager.bat`

- `TNI_MM_PREFER_BUNDLE=1` — run `publish/…` binary only  
- `TNI_MM_AUTO_INSTALL_DOTNET=0` — do not auto-install the SDK
## Self-contained publish (no .NET install for users)

Binary name is fixed: **`TNI-ModManager-Plus`** / **`TNI-ModManager-Plus.exe`** — один self-contained файл (версия в assembly / заголовке окна). На GitHub он упакован в zip с каноническим именем для updater.

```bash
chmod +x mod-manager-plus/scripts/publish.sh
./mod-manager-plus/scripts/publish.sh linux-x64
# → mod-manager-plus/publish/linux-x64/TNI-ModManager-Plus

# Windows:
# mod-manager-plus\scripts\publish.cmd win-x64
# → mod-manager-plus\publish\win-x64\TNI-ModManager-Plus.exe
```

## Features

- Browse / download / update mods from GitHub releases listed in [`mod-sources.json`](mod-sources.json)
- Remove downloaded mods
- Edit `entry.lua` configuration parameters (from `metadata.yaml`)
- Command aliases in game `settings.json`
- Launch game via Steam (`2939600`)
- Light / dark theme (persisted in `mm_plus_ui.json`)
- Self-update check against the fork’s latest GitHub release

## Version & releases

Single source: [`Version.props`](Version.props). Bump it on `beta`, merge to `main` → GitHub Actions publishes both platform zips. See [`docs/releasing.md`](../docs/releasing.md).

Local smoke (no GitHub upload):

```bash
./mod-manager-plus/scripts/make-release.sh
# → mod-manager-plus/dist/TNI-ModManager-Plus-*-x64.zip
```
