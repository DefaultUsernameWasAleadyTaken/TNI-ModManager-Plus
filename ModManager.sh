#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")" && pwd)"
PUB="$ROOT/mod-manager-plus/publish/linux-x64/TNI-ModManager-Plus"
PROJ="$ROOT/mod-manager-plus/src/TniModManager/TniModManager.csproj"

ensure_dotnet() {
  if command -v dotnet >/dev/null 2>&1; then
    return 0
  fi
  if [[ -x "$HOME/.dotnet/dotnet" ]]; then
    export DOTNET_ROOT="$HOME/.dotnet"
    export PATH="$HOME/.dotnet:$PATH"
    return 0
  fi
  return 1
}

# Dev default: run from source when SDK is available.
# Force published binary with: TNI_MM_PREFER_BUNDLE=1 ./ModManager.sh
if [[ "${TNI_MM_PREFER_BUNDLE:-0}" != "1" ]] && ensure_dotnet; then
  cd "$ROOT/mod-manager-plus"
  exec dotnet run --project "$PROJ"
fi

if [[ -x "$PUB" ]]; then
  exec "$PUB"
fi

echo ".NET 8 SDK not found and no published binary at:"
echo "  $PUB"
echo "Install SDK: https://dotnet.microsoft.com/download/dotnet/8.0"
echo "Or publish: ./mod-manager-plus/scripts/publish.sh linux-x64"
exit 1
