# Architecture Decision Records — TNI-ModManager-Plus (`alpha`)

---

## Русский

## ADR-001: Рабочая база = ветка `alpha`, сброс курса `beta`/`main`

| Поле | Значение |
|------|----------|
| **Статус** | Принято |
| **Дата** | 2026-07-29 |
| **Ветка** | `alpha` |

### Решение

Единственная рабочая ветка форка — **`alpha`**. Линия **`beta` / `main`** (эксперимент Python/PySide6) **закрыта**.

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

## ADR-001: Working base = `alpha`; abandon `beta`/`main`

Accepted 2026-07-29. Only `alpha` is the working branch; the Python/PySide6 `beta`/`main` experiment is closed.

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
