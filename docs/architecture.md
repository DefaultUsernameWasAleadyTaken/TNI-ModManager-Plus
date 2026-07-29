# Architecture — TNI-ModManager-Plus (`alpha`)

**Ветка / Branch:** `alpha` · **Стек / Stack:** .NET 8 + Avalonia 11 · **Scope:** [ADR-002](decisions.md) · **UI:** [ADR-003](decisions.md)

---

## Русский

Форк — **только Mod Manager** для игроков. Моддинг-кит и исходники модов — upstream [`CJFWeatherhead/TNI-Mods`](https://github.com/CJFWeatherhead/TNI-Mods).

| Область | Путь | Роль |
|---------|------|------|
| Solution | `mod-manager-plus/TniModManager.sln` | .NET solution |
| GUI | `mod-manager-plus/src/TniModManager/` | Avalonia desktop |
| Core | `mod-manager-plus/src/TniModManager.Core/` | paths, GitHub, mods, config, aliases |
| Tests | `mod-manager-plus/tests/` | xUnit |
| Legacy | `mod-manager-plus/legacy/ModManagerGUI.ps1` | эталон WPF (не runtime) |
| Лаунчеры | `ModManager.bat`, `ModManager.sh` | Win / Linux |
| Docs | `docs/*.md` | архитектура, ADR |

```text
ui/ (Avalonia)  →  Core  →  Godot userdata + GitHub API
```

**Userdata:**

| ОС | Mods | Disabled |
|----|------|----------|
| Windows | `…\Tower Networking Inc\Mods` | `Mods_Disabled` |
| Linux | `…/Tower Networking Inc/mods` | `mods_disabled` |

Каталог релизов: `CJFWeatherhead/TNI-Mods`. Steam App ID: `2939600`.

### Связанные документы

- [decisions.md](decisions.md) · [ModManager-README.md](../mod-manager-plus/ModManager-README.md) · [README.md](../README.md)

---

## English

Mod Manager only ([ADR-002](decisions.md)): Avalonia UI + Core library ([ADR-003](decisions.md)). Legacy PowerShell GUI is reference-only under `legacy/`.
