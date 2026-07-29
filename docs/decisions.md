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

Единственная рабочая ветка форка — **`alpha`** (полный pre-fork baseline TNI-Mods: моды, кит, Hugo, PowerShell Mod Manager).

Линия **`beta` / `main`** (Python 3 + PySide6, scope «только Mod Manager», ADR/архитектура того эксперимента) **закрыта**: не развивать, не мержить в `alpha`, не использовать как источник правды для агентов и документации.

Новая разработка ведётся **с нуля от дерева `alpha`**.

### Почему

Эксперимент на `beta`/`main` ушёл не в том направлении. Нужен возврат к исходному киту и понятной базе без наслоения Python MM.

### Следствия

- Cursor rules / `AGENTS.md` / эта страница описывают только `alpha`.
- Удаление remote-веток `beta`/`main` — только по явной просьбе владельца репо (не делается этим ADR автоматически).

### Связанные документы

- [`architecture.md`](architecture.md) · [`AGENTS.md`](../AGENTS.md) · [`ModManager-README.md`](../ModManager-README.md)

---

## English

## ADR-001: Working base = `alpha`; abandon `beta`/`main`

| Field | Value |
|-------|--------|
| **Status** | Accepted |
| **Date** | 2026-07-29 |
| **Branch** | `alpha` |

### Decision

The only working branch is **`alpha`** (full pre-fork TNI-Mods baseline). The **`beta`/`main`** Python/PySide6 Mod-Manager-only experiment is **closed** — do not continue or merge it into `alpha`. New work starts from the `alpha` tree.

### Why

That experiment went the wrong direction; reset to the original kit baseline.

### Related

- [`architecture.md`](architecture.md) · [`AGENTS.md`](../AGENTS.md)
