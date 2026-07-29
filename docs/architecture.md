# Architecture — TNI-ModManager-Plus (`alpha`)

**Ветка / Branch:** `alpha` · **Стек / Stack:** PowerShell + WPF · **Scope:** [ADR-002](decisions.md) (Mod Manager only)

---

## Русский

Форк — **только Mod Manager** для игроков. Моддинг-кит и исходники модов — upstream [`CJFWeatherhead/TNI-Mods`](https://github.com/CJFWeatherhead/TNI-Mods).

| Область | Путь | Роль |
|---------|------|------|
| Приложение | `mod-manager-plus/` | PowerShell + WPF MM |
| GUI | `mod-manager-plus/ModManagerGUI.ps1` | основной скрипт |
| Лаунчер | `ModManager.bat` → `mod-manager-plus/ModManagerGUI.ps1` | Windows |
| Конфиг модов | маркеры в `entry.lua` (см. `mod-manager-plus/ModManager-README.md`) | Parameters |
| Схема | `mod-manager-plus/mod-metadata-schema.yaml` | контракт `metadata.yaml` (справочно) |
| Docs | `docs/*.md` | архитектура, ADR |

**Userdata игры (Windows):**

```text
%APPDATA%\Godot\app_userdata\Tower Networking Inc\
├── Mods\
├── Mods_Disabled\
├── mod_cache.json
└── settings.json
```

Каталог релизов по умолчанию: `CJFWeatherhead/TNI-Mods`. Steam App ID: `2939600`.

### Связанные документы

- [decisions.md](decisions.md) · [ModManager-README.md](../mod-manager-plus/ModManager-README.md) · [README.md](../README.md)

---

## English

Fork scope is **Mod Manager only** ([ADR-002](decisions.md)): PowerShell + WPF in `mod-manager-plus/`. Kit and mod sources live upstream. See [decisions.md](decisions.md).
