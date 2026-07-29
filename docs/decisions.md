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

- каталог `mod-manager-plus/` (`ModManagerGUI.ps1`, `ModManager-README.md`, опционально `mod-metadata-schema.yaml`)
- корневой лаунчер `ModManager.bat`
- `docs/` (architecture, ADR), `.cursor/`, `AGENTS.md`

**Не** держать in-tree: `mods/`, `programs/`, `ext/`, `include/`, `cmake/`, `lua-typing/`, Hugo-витрину, kit/release CI / copy-mods / CMake-сборку / ps2exe-релиз MM без нового ADR.

Игроки ставят моды из **релизов GitHub** upstream (`CJFWeatherhead/TNI-Mods`). Исходники модов — там же.

Стек MM на `alpha`: **PowerShell + WPF** (не Python).

### Почему

Нужен фокус на менеджере для игроков; кит и каталог модов дублируют upstream и мешают.

### Связанные документы

- [`architecture.md`](architecture.md) · [`ModManager-README.md`](../mod-manager-plus/ModManager-README.md) · [`README.md`](../README.md)

---

## English

## ADR-001: Working base = `alpha`; abandon `beta`/`main`

Accepted 2026-07-29. Only `alpha` is the working branch; the Python/PySide6 `beta`/`main` experiment is closed.

## ADR-002: Scope = Mod Manager only

Accepted 2026-07-29. Keep PowerShell + WPF Mod Manager in `mod-manager-plus/` (plus root `ModManager.bat`) and project docs. Do not keep the modding kit, in-tree mods, Hugo site, or release/kit CI. Mod sources remain upstream.
