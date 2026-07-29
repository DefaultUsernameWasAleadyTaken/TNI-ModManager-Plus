# Architecture — TNI-ModManager-Plus (`beta`)

**Ветка / Branch:** `beta` · **Стек / Stack:** .NET 8 + Avalonia 11 · **Scope:** [ADR-002](decisions.md) · **UI:** [ADR-003](decisions.md)

---

## Русский

Форк — **только Mod Manager** для игроков. Моддинг-кит и исходники модов — upstream [`CJFWeatherhead/TNI-Mods`](https://github.com/CJFWeatherhead/TNI-Mods).

| Область | Путь | Роль |
|---------|------|------|
| Solution | `mod-manager-plus/TniModManager.sln` | .NET solution |
| GUI | `mod-manager-plus/src/TniModManager/` | Avalonia desktop |
| Core | `mod-manager-plus/src/TniModManager.Core/` | paths, GitHub, mods, config, aliases |
| Tests | `mod-manager-plus/tests/` | xUnit |
| Лаунчеры | `ModManager.bat`, `ModManager.sh` | Win / Linux |
| Docs | `docs/*.md` | архитектура, ADR |

```text
ui/ (Avalonia)  →  Core  →  Godot userdata + GitHub API
```

**Userdata:**

| ОС | Mods |
|----|------|
| Windows | `…\Tower Networking Inc\Mods` |
| Linux | `…/Tower Networking Inc/mods` |

Каталог релизов: `CJFWeatherhead/TNI-Mods`. Steam App ID: `2939600`.

### UI shell

Главное окно (`Views/MainWindow.axaml`) — тонкий Avalonia shell: `Controls/AppHeader.axaml`
(заголовок, статус, тема / язык / update / launch), прогресс загрузки и `TabControl` с
`Views/ModsView.axaml` и `Views/AliasesView.axaml`. `MainViewModel` служит shell,
а `ModsViewModel` и `AliasesViewModel` содержат логику вкладок. Цвета — Light/Dark `ThemeDictionaries`
через динамические ресурсы; тема и задел языка (`en`) в `mm_plus_ui.json`. Менеджер
устанавливает, обновляет и удаляет управляемые моды, но не переносит их между
enabled/disabled каталогами. Self-update проверяет latest release форка
`DefaultUsernameWasAleadyTaken/TNI-ModManager-Plus`; при невозможности автозамены
открывается страница релиза.

### Связанные документы

- [decisions.md](decisions.md) · [ModManager-README.md](../mod-manager-plus/ModManager-README.md) · [README.md](../README.md)

---

## English

Mod Manager only ([ADR-002](decisions.md)): Avalonia UI + Core library ([ADR-003](decisions.md)).
