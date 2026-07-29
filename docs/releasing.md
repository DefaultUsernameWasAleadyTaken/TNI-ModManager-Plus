# Релизы Mod Manager Plus

Как выложить новую версию приложения на GitHub (self-update / Download).

## Коротко

1. На **`beta`** подними версию в одном файле: [`mod-manager-plus/Version.props`](../mod-manager-plus/Version.props).
2. Смержь `beta` → **`main`** и запушь.
3. GitHub Actions сам соберёт `linux-x64` + `win-x64` zip и создаст Release с тегом `vX.Y.Z`.

Если **не** менял `Version.props` (номер тот же, что у Latest) — релиза **не будет** (skip). Код всё равно попадёт в `main`.

Моды из этого репозитория **не** публикуются — каталог модов: upstream [`CJFWeatherhead/TNI-Mods`](https://github.com/CJFWeatherhead/TNI-Mods).

## Версия

Правишь только:

```xml
<Version>1.0.1</Version>
<InformationalVersion>1.0.1</InformationalVersion>
```

в `mod-manager-plus/Version.props` (оба числа одинаковые). Приложение читает версию из сборки.

## Пример

```bash
# на beta
# …правки кода…
# правишь Version.props: 1.0.0 → 1.0.1
git add mod-manager-plus/Version.props
git commit -m "chore: bump version to 1.0.1"
git push origin beta

git checkout main
git merge beta
git push origin main
# → Actions → GitHub Releases / Latest
```

Ассеты (имена фиксированы для updater; внутри — один исполняемый файл):

- `TNI-ModManager-Plus-linux-x64.zip`
- `TNI-ModManager-Plus-win-x64.zip`

## Опционально

- **`[skip release]`** в сообщении коммита на `main` — принудительно не релизить, даже если версию подняли.
- Локальный smoke без публикации: `./mod-manager-plus/scripts/make-release.sh` → zip в `mod-manager-plus/dist/`.

## Связанное

- Workflow: [`.github/workflows/release.yml`](../.github/workflows/release.yml)
- [ADR-006](decisions.md)
