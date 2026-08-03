# Architecture — TNI-ModManager-Plus

**Ветки / Branches:** `beta` (разработка) · `main` (релиз) · **Стек / Stack:** .NET 8 + Avalonia 11 · **Scope:** [ADR-002](decisions.md) · **Релизы:** [ADR-006](decisions.md), [releasing.md](releasing.md)

---

## Русский

Форк — **только Mod Manager** для игроков. Моддинг-кит и исходники модов — upstream [`CJFWeatherhead/TNI-Mods`](https://github.com/CJFWeatherhead/TNI-Mods).

| Область | Путь | Роль |
|---------|------|------|
| Solution | `mod-manager-plus/TniModManager.sln` | .NET solution |
| GUI | `mod-manager-plus/src/TniModManager/` | Avalonia desktop |
| Core | `mod-manager-plus/src/TniModManager.Core/` | paths, GitHub, mods, config, aliases |
| Tests | `mod-manager-plus/tests/` | xUnit |
| Версия | `mod-manager-plus/Version.props` | единственный источник версии приложения |
| Лаунчеры | `ModManager.bat`, `ModManager.sh` | Win / Linux: `dotnet run` при SDK 8, иначе `publish/…`, иначе автоустановка SDK 8 (user-local) |
| Docs | `docs/*.md` | архитектура, ADR, releasing |

```text
ui/ (Avalonia)  →  Core  →  Godot userdata + GitHub API
```

**Userdata:**

| ОС | Mods |
|----|------|
| Windows | `…\Tower Networking Inc\Mods` |
| Linux | `…/Tower Networking Inc/mods` |

Каталог модов: список репозиториев в [`mod-manager-plus/mod-sources.json`](../mod-manager-plus/mod-sources.json) (сейчас `CJFWeatherhead/TNI-Mods` и `DefaultUsernameWasAleadyTaken/TNI-data-extractor`). Steam App ID: `2939600`.

После успешной загрузки релизов GitHub каталог пишется в `release_cache.json` (рядом с `mod_cache.json` в userdata). При старте менеджер **не** дергает GitHub: показывает кэш + установленные моды. Сеть — только по кнопке «Обновить». При ошибке API (в т.ч. rate limit 403/429) список тоже не очищается.

### UI shell

Главное окно (`Views/MainWindow.axaml`) — тонкий Avalonia shell: `Controls/AppHeader.axaml`
(заголовок, статус, тема / язык / update / launch), прогресс загрузки и `TabControl` с
`Views/ModsView.axaml` и `Views/AliasesView.axaml`. `MainViewModel` служит shell,
а `ModsViewModel` и `AliasesViewModel` содержат логику вкладок. Вкладка Aliases
(Alias Studio): список с поиском и превью команды, шаблоны при создании, автосинхрон
черновика в список; Save и «Открыть папку» слева. Редактор — многострочные сегменты `;`,
компактный шаг N/M, сплит Live Preview | справка, автодополнение (Ctrl+Space).
Цвета — Light/Dark `ThemeDictionaries` через динамические ресурсы; тема и язык
(`en` / `ru`) в `mm_plus_ui.json`. Менеджер устанавливает, обновляет и удаляет
управляемые моды, но не переносит их между enabled/disabled каталогами.
Self-update проверяет latest release форка
`DefaultUsernameWasAleadyTaken/TNI-ModManager-Plus`; при невозможности автозамены
открывается страница релиза.

### Релизы приложения

Версия — `Version.props`. Push в `main` с **новой** версией запускает [`.github/workflows/release.yml`](../.github/workflows/release.yml) (два zip + GitHub Release). Подробности: [releasing.md](releasing.md).

### Связанные документы

- [decisions.md](decisions.md) · [releasing.md](releasing.md) · [ModManager-README.md](../mod-manager-plus/ModManager-README.md) · [README.md](../README.md)

---

## English

Mod Manager only ([ADR-002](decisions.md)): Avalonia UI + Core ([ADR-003](decisions.md)). App releases on push to `main` when `Version.props` is newer ([ADR-006](decisions.md)). Successful GitHub catalog fetches are saved to `release_cache.json`; on API failure (including rate limit) the UI keeps the cached catalog instead of dropping to installed-only. The Aliases tab (Alias Studio) has search, create templates, draft auto-sync, Save/Open-folder on the left list, multiline `;` editing, compact step bar, and a Preview|Manual split with rich autocomplete from `alias_helper_catalog.json`.
