# AGENTS — TNI-ModManager-Plus

## Русский

- **Ветки ([ADR-006](docs/decisions.md)):** `beta` — разработка; `main` — релизная линия (авторелиз при новой версии в `Version.props`).
- **Не использовать:** ветку `alpha` (удалена).
- **Scope ([ADR-002](docs/decisions.md)):** только Mod Manager — `mod-manager-plus/`, лаунчеры, `docs/`.
- **Стек ([ADR-003](docs/decisions.md)):** .NET 8 + Avalonia 11; Core в `src/TniModManager.Core/`.
- **Версия приложения:** только [`mod-manager-plus/Version.props`](mod-manager-plus/Version.props). Релизы: [`docs/releasing.md`](docs/releasing.md).
- **Язык с пользователем:** русский.
- **Git publish:** только по явной просьбе. PR в upstream — тоже только по явной просьбе.
- Правила Cursor: `.cursor/rules/`.

## English

- Branches: `beta` = development, `main` = release ([ADR-006](docs/decisions.md)). Stack: .NET 8 + Avalonia ([ADR-003](docs/decisions.md)). Scope: Mod Manager only. Version: `Version.props`.
