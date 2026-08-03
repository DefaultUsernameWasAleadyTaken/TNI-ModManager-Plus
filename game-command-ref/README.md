# Terminal & alias reference (processed)

## Русский

Справочник знаний об **игровом терминале** и `cmd_alias` для помощника создания алиасов в Mod Manager.

Факты сформулированы как **наблюдаемые в игре** (`man`, примеры команд, `program list` / `program describe`, синтаксис алиасов).  
В этих JSON/Markdown **намеренно нет** внутренних путей ресурсов, имён файлов ассетов и прочей служебной информации о происхождении данных.

### Файлы

| Файл | Содержание |
|------|------------|
| [`terminal-commands.json`](terminal-commands.json) | 33 команды: summary, usage, examples, `requires_on` / `requires_using` |
| [`installable-programs.json`](installable-programs.json) | 60 программ: description, `install_size` (code/data/total), cpu/gpu load и др. |
| [`alias-syntax.json`](alias-syntax.json) | Грамматика `cmd_alias`, reserved keywords, `$n` / `${n}` |
| [`shell-vocabulary.json`](shell-vocabulary.json) | Лексика для autocomplete: типы `scan`, примеры traffic, имена команд/программ |
| [`COMMANDS.md`](COMMANDS.md) / [`PROGRAMS.md`](PROGRAMS.md) | Таблицы для чтения |

### Алиасы (кратко)

- Хранение: userdata игры → `settings.json` → `cmd_alias`
- Параметры: `$1`… и `${1}`…
- Цель/отладчик: `on`, `using`, `always on`, `always using`
- Поток: `try` / `then` / `else`, несколько команд через `;`
- Часть ключевых слов и имён built-in команд зарезервированы (см. `alias-syntax.json`)

### Программы и место на устройстве

В `installable-programs.json`:

- `install_size.code` / `install_size.data` — стороны размера установки (code+data в UI игры)
- `install_size.total` — сумма, если известны оба
- при наличии: `cpu_load`, `gpu_load`, `produce_factor`, `consume_factor`, `traffic_class`

### Статус

Набор собран для будущего alias-helper. **Перед публикацией в git** — отдельный review на утечки формулировок.

Пересборка для maintainers: локальный `tools/harvest_all.py` (в `.gitignore`; `TNI_EXTRACT_ROOT` или `--extract`). Выход проходит санитайзер запрещённых маркеров.

---

## English

Processed in-game terminal / `cmd_alias` knowledge for Mod Manager alias helpers.

JSON/Markdown intentionally omit asset paths and dump artifacts — player-observable facts only (`man`, command examples, `program describe`, alias syntax).

Includes install sizes (code/data/total) and load metrics where available. Treat as **not publication-ready** until reviewed.
