#!/usr/bin/env bash
# Self-contained single-file: TNI-ModManager-Plus (fixed name, no version in filename).
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PROJ="$ROOT/src/TniModManager/TniModManager.csproj"
RID="${1:-linux-x64}"
OUT="$ROOT/publish/$RID"

export PATH="${DOTNET_ROOT:-$HOME/.dotnet}:$PATH"
rm -rf "$OUT"
dotnet publish "$PROJ" -c Release -r "$RID" --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:EnableCompressionInSingleFile=true \
  -p:DebugType=None \
  -p:DebugSymbols=false \
  -o "$OUT"

echo "Published: $OUT/TNI-ModManager-Plus"
ls -lh "$OUT"
