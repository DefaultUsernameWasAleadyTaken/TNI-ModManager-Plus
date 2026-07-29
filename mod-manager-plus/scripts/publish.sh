#!/usr/bin/env bash
# Self-contained publish: fixed binary name TNI-ModManager-Plus (no version in filename).
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PROJ="$ROOT/src/TniModManager/TniModManager.csproj"
RID="${1:-linux-x64}"
OUT="$ROOT/publish/$RID"

export PATH="${DOTNET_ROOT:-$HOME/.dotnet}:$PATH"
dotnet publish "$PROJ" -c Release -r "$RID" --self-contained true \
  -p:PublishSingleFile=false \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -o "$OUT"

echo "Published: $OUT/TNI-ModManager-Plus"
