# TNI Mod Manager Plus

Кроссплатформенный **Mod Manager** (.NET 8 + Avalonia) для [Tower Networking Inc](https://store.steampowered.com/app/2939600/Tower_Networking_Inc/).

Форк [`CJFWeatherhead/TNI-Mods`](https://github.com/CJFWeatherhead/TNI-Mods), суженный до **только Mod Manager** ([ADR-002](docs/decisions.md)). Стек: [ADR-003](docs/decisions.md). Ветка: **`alpha`**.

---

## Русский

### Требования

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Запуск

| ОС | Лаунчер |
|----|---------|
| Windows | `ModManager.bat` |
| Linux / Steam Deck | `./ModManager.sh` |

Или из каталога решения:

```bash
cd mod-manager-plus
dotnet run --project src/TniModManager/TniModManager.csproj
```

### Возможности

- каталог и установка модов из GitHub Releases (`CJFWeatherhead/TNI-Mods`);
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
  legacy/ModManagerGUI.ps1
ModManager.bat / ModManager.sh
docs/
```

### Self-contained бинарник

Имя файла фиксированное: `TNI-ModManager-Plus` (версия внутри сборки / в title). Пользователям .NET не нужен.

### Лицензия

BSD 3-Clause — [`LICENSE`](LICENSE).

---

## English

Cross-platform Mod Manager (.NET 8 + Avalonia) for Windows and Linux. Working branch: **`alpha`**. Launch: `ModManager.bat` / `./ModManager.sh`. See [docs/decisions.md](docs/decisions.md) (ADR-003).
