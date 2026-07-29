# TNI Mod Manager Plus

PowerShell + WPF **Mod Manager** для [Tower Networking Inc](https://store.steampowered.com/app/2939600/Tower_Networking_Inc/).

Форк [`CJFWeatherhead/TNI-Mods`](https://github.com/CJFWeatherhead/TNI-Mods), суженный до **только Mod Manager** ([ADR-002](docs/decisions.md)). Рабочая ветка: **`alpha`**.

Исходники модов и моддинг-кит — в upstream TNI-Mods; этот репозиторий их не содержит.

---

## Русский

### Запуск (Windows)

Двойной клик по [`ModManager.bat`](ModManager.bat) → [`mod-manager-plus/ModManagerGUI.ps1`](mod-manager-plus/ModManagerGUI.ps1).

Нужны PowerShell 5.1+ или PowerShell 7+, .NET Framework 4.5+ (WPF).

### Возможности

- каталог и установка модов из GitHub Releases (по умолчанию `CJFWeatherhead/TNI-Mods`);
- обновление / удаление / вкл-выкл;
- параметры мода через маркеры в `entry.lua` (см. [mod-manager-plus/ModManager-README.md](mod-manager-plus/ModManager-README.md));
- алиасы команд в `settings.json` игры;
- запуск игры через Steam.

Подробнее: [mod-manager-plus/ModManager-README.md](mod-manager-plus/ModManager-README.md).

### Карта репозитория

```text
mod-manager-plus/            # приложение Mod Manager
  ModManagerGUI.ps1          # GUI (PowerShell + WPF)
  ModManager-README.md       # руководство пользователя
  mod-metadata-schema.yaml   # схема metadata (справочно)
ModManager.bat               # лаунчер Windows → mod-manager-plus/
docs/                        # architecture, ADR
```

### Лицензия

BSD 3-Clause — [`LICENSE`](LICENSE). Attribution upstream: Alf-André Walla / TNI-Mods.

---

## English

Windows Mod Manager only (PowerShell + WPF). Working branch: **`alpha`**. Kit and mod sources live upstream. Launch: `ModManager.bat`. See [mod-manager-plus/ModManager-README.md](mod-manager-plus/ModManager-README.md), [docs/decisions.md](docs/decisions.md).
