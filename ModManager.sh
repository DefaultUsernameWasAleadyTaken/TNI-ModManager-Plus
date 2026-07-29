#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")" && pwd)"
PUB="$ROOT/mod-manager-plus/publish/linux-x64/TNI-ModManager-Plus"
PROJ="$ROOT/mod-manager-plus/src/TniModManager/TniModManager.csproj"

if [[ -x "$PUB" ]]; then
  exec "$PUB"
fi

if ! command -v dotnet >/dev/null 2>&1; then
  if [[ -x "$HOME/.dotnet/dotnet" ]]; then
    export DOTNET_ROOT="$HOME/.dotnet"
    export PATH="$HOME/.dotnet:$PATH"
  else
    echo ".NET 8 SDK not found and no published binary at:"
    echo "  $PUB"
    echo "Install SDK: https://dotnet.microsoft.com/download/dotnet/8.0"
    exit 1
  fi
fi

cd "$ROOT/mod-manager-plus"
exec dotnet run --project "$PROJ"
