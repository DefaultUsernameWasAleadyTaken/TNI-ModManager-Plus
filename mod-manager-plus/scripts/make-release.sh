#!/usr/bin/env bash
# Local smoke: publish + zip (does NOT create GitHub Release — that is CI on main).
# Usage: ./mod-manager-plus/scripts/make-release.sh [linux-x64|win-x64|all]
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
DIST="$ROOT/dist"
RID_ARG="${1:-all}"
export PATH="${DOTNET_ROOT:-$HOME/.dotnet}:$PATH"

VERSION="$(grep -oP '(?<=<Version>)[^<]+' "$ROOT/Version.props" | head -1)"
echo "Version.props = $VERSION"

mkdir -p "$DIST"

publish_and_zip() {
  local rid="$1"
  bash "$ROOT/scripts/publish.sh" "$rid"
  local out="$ROOT/publish/$rid"
  local zip_path="$DIST/TNI-ModManager-Plus-${rid}.zip"
  rm -f "$zip_path"
  (cd "$out" && zip -qr "$zip_path" .)
  echo "  → $zip_path"
}

case "$RID_ARG" in
  linux-x64|win-x64)
    publish_and_zip "$RID_ARG"
    ;;
  all)
    publish_and_zip linux-x64
    set +e
    publish_and_zip win-x64
    WIN_EC=$?
    set -e
    if [[ "$WIN_EC" -ne 0 ]]; then
      echo "WARN: win-x64 failed locally — CI builds it on windows-latest." >&2
    fi
    ;;
  *)
    echo "Usage: $0 [linux-x64|win-x64|all]" >&2
    exit 1
    ;;
esac

ls -lh "$DIST"/TNI-ModManager-Plus-*.zip 2>/dev/null || true
echo
echo "GitHub Release is automatic: bump Version.props → merge to main → Actions."
echo "See docs/releasing.md"
