# TNI Mod Manager Plus

Кроссплатформенный **Mod Manager** (.NET 8 + Avalonia) для [Tower Networking Inc](https://store.steampowered.com/app/2939600/Tower_Networking_Inc/).

Форк [`CJFWeatherhead/TNI-Mods`](https://github.com/CJFWeatherhead/TNI-Mods), суженный до **только Mod Manager** ([ADR-002](docs/decisions.md)). Стек: [ADR-003](docs/decisions.md). Ветки: **`beta`** (разработка), **`main`** (релиз) — [ADR-006](docs/decisions.md), [docs/releasing.md](docs/releasing.md).

---

## Русский

### Требования

- Для разработки: [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)  
  (`ModManager.bat` / `ModManager.sh` **поставят SDK автоматически**, если его нет и нет готового `publish/…` бинарника)
- Для игроков: self-contained zip из Releases — .NET не нужен

### Запуск

| ОС | Лаунчер |
|----|---------|
| Windows | `ModManager.bat` |
| Linux / Steam Deck | `./ModManager.sh` |

Переменные окружения:

- `TNI_MM_PREFER_BUNDLE=1` — сразу published binary
- `TNI_MM_AUTO_INSTALL_DOTNET=0` — не ставить SDK автоматически

Или из каталога решения:

```bash
cd mod-manager-plus
dotnet run --project src/TniModManager/TniModManager.csproj
```

### Возможности

- каталог и установка модов из GitHub Releases ([`mod-sources.json`](mod-manager-plus/mod-sources.json));
- обновление / удаление / вкл-выкл;
- параметры через маркеры в `entry.lua`;
- алиасы в `settings.json` игры;
- запуск игры через Steam.

Подробнее: [mod-manager-plus/ModManager-README.md](mod-manager-plus/ModManager-README.md).

### Карта репозитория

```text
mod-manager-plus/
  src/TniModManager/           # Avalonia GUI → binary TNI-ModManager-Plus
  src/TniModManager.Core/      # логика без UI
  tests/
  scripts/publish.sh|.cmd      # self-contained publish
  Version.props                # версия приложения
ModManager.bat / ModManager.sh
docs/                          # в т.ч. releasing.md
```

### Self-contained бинарник

Имя файла фиксированное: `TNI-ModManager-Plus` (версия внутри сборки / в title). Пользователям .NET не нужен.

### Лицензия

BSD 3-Clause — [`LICENSE`](LICENSE).

---

## English

### Requirements

- Dev: [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) — launchers **auto-install** it when missing (unless a `publish/…` binary exists)
- End users: self-contained Release zips need no .NET install

### Launch

`ModManager.bat` / `./ModManager.sh`. Env: `TNI_MM_PREFER_BUNDLE=1` (use published binary), `TNI_MM_AUTO_INSTALL_DOTNET=0` (disable SDK auto-install).

Branches: **`beta`** (dev), **`main`** (release). See [docs/releasing.md](docs/releasing.md).
