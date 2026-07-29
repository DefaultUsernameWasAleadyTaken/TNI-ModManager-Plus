# Architecture Decision Records — TNI-ModManager-Plus (`beta`)

---

## Русский

## ADR-001: Рабочая база = ветка `alpha`, сброс курса `beta`/`main`

| Поле | Значение |
|------|----------|
| **Статус** | Заменено ([ADR-005](#adr-005-рабочая-база--ветка-beta-ветка-alpha-удалена)) |
| **Дата** | 2026-07-29 |
| **Ветка** | `alpha` (исторически) |

### Решение

Единственная рабочая ветка форка — **`alpha`**. Линия **`beta` / `main`** (эксперимент Python/PySide6) **закрыта**.

> Позже содержимое Avalonia-линейки перенесено в `beta`, ветка `alpha` удалена — см. ADR-005.

### Связанные документы

- [`architecture.md`](architecture.md) · [`AGENTS.md`](../AGENTS.md)

---

## ADR-002: Scope форка = только Mod Manager

| Поле | Значение |
|------|----------|
| **Статус** | Принято |
| **Дата** | 2026-07-29 |
| **Ветка** | `alpha` |

### Решение

В репозитории **только** приложение Mod Manager:

- каталог `mod-manager-plus/` (Avalonia/.NET: `src/`, `scripts/`)
- корневые лаунчеры `ModManager.bat` / `ModManager.sh`
- `docs/` (architecture, ADR), `.cursor/`, `AGENTS.md`

Self-contained артефакт: **`TNI-ModManager-Plus`** / `.exe` (имя без версии; версия в assembly / title).

**Не** держать in-tree: `mods/`, `programs/`, kit, Hugo, kit/release CI без нового ADR.

Игроки ставят моды из **релизов GitHub** upstream (`CJFWeatherhead/TNI-Mods`).

### Связанные документы

- [`architecture.md`](architecture.md) · [`ModManager-README.md`](../mod-manager-plus/ModManager-README.md) · [`README.md`](../README.md)

---

## ADR-003: Стек = .NET 8 + Avalonia 11 (Windows + Linux)

| Поле | Значение |
|------|----------|
| **Статус** | Принято |
| **Дата** | 2026-07-29 |
| **Ветка** | `alpha` |

### Решение

Кроссплатформенный GUI: **.NET 8 + Avalonia 11** (MVVM) в `mod-manager-plus/src/`.

- Целевые ОС: **Windows** и **Linux** (Steam Deck).
- UI: современная тёмная тема приложения (**Mod Manager Plus** v1.0.0).
- Ядро без UI: `TniModManager.Core` (paths, GitHub, mods, config, aliases).
- `ui-config.ps1` **не исполняется**; Parameters — из `metadata.yaml` / `mod.jsonc`.
- Userdata: Windows `Mods` / `Mods_Disabled`; Linux `mods` / `mods_disabled`.

### Почему

WPF не работает на Linux. Avalonia ближе к WPF/XAML и даёт один UI на Win+Linux.

### Связанные документы

- [`architecture.md`](architecture.md) · [`README.md`](../README.md)

---

## English

## ADR-001: Working base = `alpha`; abandon Python `beta`/`main` (superseded)

Accepted 2026-07-29; **superseded by ADR-005**. Originally only `alpha` was the working branch.

## ADR-002: Scope = Mod Manager only

Accepted 2026-07-29. Keep Mod Manager app and project docs. Kit/mods/Hugo/CI stay out of tree.

## ADR-003: Stack = .NET 8 + Avalonia 11 (Windows + Linux)

Accepted 2026-07-29. Cross-platform GUI via Avalonia; Core library for paths/GitHub/mods. Do not execute `ui-config.ps1`.

---

## ADR-004: Удаление legacy PowerShell+WPF Mod Manager

| Поле | Значение |
|------|----------|
| **Статус** | Принято |
| **Дата** | 2026-07-29 |
| **Ветка** | `alpha` |

### Решение

Удалить `mod-manager-plus/legacy/ModManagerGUI.ps1` и все runtime/docs-ссылки на PowerShell+WPF менеджер. Единственный продукт — Avalonia (`TNI-ModManager-Plus`). История git сохраняет старый скрипт при необходимости.

### Почему

Порт на Avalonia закрыл потребность в эталоне in-tree; дублирование путает scope.

### English

## ADR-004: Remove legacy PowerShell+WPF Mod Manager

Accepted 2026-07-29. Drop in-tree legacy PS1; Avalonia app is the sole product.

---

## ADR-005: Рабочая база = ветка `beta`; ветка `alpha` удалена

| Поле | Значение |
|------|----------|
| **Статус** | Принято |
| **Дата** | 2026-07-29 |
| **Ветка** | `beta` |

### Решение

- `origin/beta` выровнен на состояние Avalonia Mod Manager (бывший tip `alpha`, коммит линейки UI redesign).
- Ветка **`alpha`** удалена локально и на `origin`.
- Единственная рабочая ветка форка — **`beta`**.
- **`main`** по-прежнему не использовать как продуктовую базу (старый tree / Python MM).

### Почему

После force-reset `beta` ← `alpha` дублировать две одинаковые ветки незачем; `beta` остаётся основной линией форка.

### English

## ADR-005: Working base = `beta`; delete `alpha`

Accepted 2026-07-29. `beta` holds the Avalonia Mod Manager tip; `alpha` removed from origin. Do not use `main` as product base.

**Статус (обновление 2026-07-29):** частично заменён [ADR-006](#adr-006-релизы-приложения-и-роли-веток-betamain): `main` снова релизная линия; `beta` — разработка. Удаление `alpha` остаётся в силе.

---

## ADR-006: Релизы приложения и роли веток beta/main

| Поле | Значение |
|------|----------|
| **Статус** | Принято |
| **Дата** | 2026-07-29 |
| **Ветки** | `beta` (разработка), `main` (релиз) |

### Решение

- Релизы **приложения** Mod Manager Plus — только GitHub Releases репозитория форка `DefaultUsernameWasAleadyTaken/TNI-ModManager-Plus`.
- **Моды** из этого репо не публикуются; каталог модов — upstream `CJFWeatherhead/TNI-Mods`.
- Единый источник версии: [`mod-manager-plus/Version.props`](../mod-manager-plus/Version.props); рантайм читает InformationalVersion сборки.
- Канонические ассеты: `TNI-ModManager-Plus-linux-x64.zip`, `TNI-ModManager-Plus-win-x64.zip`.
- Push в **`main`**: Actions сравнивает `Version.props` с Latest; если версия **новее** (или релизов ещё нет) — publish + tag `vX.Y.Z` + Release. Если версия **та же** — skip.
- Запасной hard-skip: `[skip release]` в сообщении коммита.
- Гайд: [`docs/releasing.md`](releasing.md).

### Почему

Один понятный путь для self-update без ручной загрузки zip; bump версии = явное намерение выложить релиз.

### English

## ADR-006: App releases and beta/main roles

Accepted 2026-07-29. App releases on the fork only; Version.props is the single version source; push to `main` auto-releases when the version is newer than Latest. Mods are never released from this repo.
