# Architecture — TNI-ModManager-Plus (`alpha`)

**Ветка / Branch:** `alpha` · **База / Base:** полный TNI-Mods kit · **Решение / Decision:** [ADR-001](decisions.md)

---

## Русский

Форк работает от **полного baseline** на `alpha` (не от узкого Python MM на `beta`/`main`).

| Область | Путь | Роль |
|---------|------|------|
| Моды (Lua/C++) | `mods/` | Исходники модов (`entry.lua`, `metadata.yaml`, …) |
| C++ / LuaJIT | `programs/`, `ext/`, `include/tni/`, `cmake/` | Сборка sandbox-модов |
| Mod Manager | `ModManagerGUI.ps1`, `ModManager.bat` | PowerShell + WPF GUI для игроков |
| Конфиг модов | `CONFIG-SYSTEM.md`, маркеры в `entry.lua` | Параметры через MM |
| Сайт | `docs/` (Hugo + `content/`) | Витрина модов |
| CI / релизы | `.github/` | Релизы модов, сборки, ps2exe MM |
| Типизация Lua | `lua-typing/` | Stubs для IDE |

**Запуск MM (Windows):** `ModManager.bat` → `ModManagerGUI.ps1`.  
**Копирование модов в userdata:** `copy-mods.sh` / `copy-mods.cmd`.  
**Сборка C++:** `build-gnu.sh` / `build-zig.cmd`.

Слои Python `mod-manager/` / PySide6 **не являются** частью текущей архитектуры.

### Связанные документы

- [decisions.md](decisions.md) · [ModManager-README.md](../ModManager-README.md) · [CONFIG-SYSTEM.md](../CONFIG-SYSTEM.md) · [README.md](../README.md) · [AGENTS.md](../AGENTS.md)

---

## English

This fork’s working architecture is the **full kit baseline on `alpha`**. The Python/PySide6 `mod-manager/` experiment on `beta`/`main` is out of scope ([ADR-001](decisions.md)).

| Area | Path | Role |
|------|------|------|
| Mods | `mods/` | Mod sources |
| Native / LuaJIT | `programs/`, `ext/`, `include/tni/` | Sandbox builds |
| Mod Manager | `ModManagerGUI.ps1`, `ModManager.bat` | PowerShell + WPF |
| Website | `docs/` (Hugo) | Mod showcase |
| CI | `.github/` | Releases / packaging |

See also: [decisions.md](decisions.md), [ModManager-README.md](../ModManager-README.md).
