# AGENTS — TNI-ModManager-Plus

## Русский

- **Рабочая ветка:** только `alpha`.
- **Не трогать как продуктовую базу:** `beta`, `main` (устаревший Python MM).
- **Scope ([ADR-002](docs/decisions.md)):** только Mod Manager — каталог `mod-manager-plus/`, корневой `ModManager.bat`, `docs/`. Не возвращать `mods/`, kit, Hugo без нового ADR.
- **Стек:** PowerShell + WPF (`mod-manager-plus/ModManagerGUI.ps1`).
- **Язык с пользователем:** русский.
- **Git publish** (commit / push / PR): только по явной просьбе. PR в upstream — тоже только по явной просьбе.
- Правила Cursor: `.cursor/rules/`.

## English

- Working branch: `alpha` only. Scope: Mod Manager only in `mod-manager-plus/` (PowerShell + WPF). See [ADR-002](docs/decisions.md).
