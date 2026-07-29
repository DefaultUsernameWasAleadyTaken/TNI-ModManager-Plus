#!/usr/bin/env bash
# Launch Mod Manager Plus: SDK → source, else published binary, else auto-install .NET 8 SDK.
# Force published binary: TNI_MM_PREFER_BUNDLE=1 ./ModManager.sh
# Skip auto-install:     TNI_MM_AUTO_INSTALL_DOTNET=0 ./ModManager.sh
set -euo pipefail

ROOT="$(cd "$(dirname "$0")" && pwd)"
PUB="$ROOT/mod-manager-plus/publish/linux-x64/TNI-ModManager-Plus"
PROJ="$ROOT/mod-manager-plus/src/TniModManager/TniModManager.csproj"
DOTNET_CHANNEL="${TNI_MM_DOTNET_CHANNEL:-8.0}"

prepend_dotnet_paths() {
  if [[ -x "${DOTNET_ROOT:-}/dotnet" ]]; then
    export PATH="$DOTNET_ROOT:$PATH"
  fi
  if [[ -x "$HOME/.dotnet/dotnet" ]]; then
    export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
    export PATH="$HOME/.dotnet:$PATH"
  fi
}

has_dotnet_sdk_8() {
  prepend_dotnet_paths
  command -v dotnet >/dev/null 2>&1 || return 1
  # Runtime alone is not enough for "dotnet run".
  dotnet --list-sdks 2>/dev/null | grep -E '^8\.' >/dev/null
}

install_dotnet_sdk() {
  local installer script
  script="${TMPDIR:-/tmp}/tni-dotnet-install.sh"
  echo "[.NET] SDK ${DOTNET_CHANNEL} not found — installing to \$HOME/.dotnet ..."
  if command -v curl >/dev/null 2>&1; then
    curl -fsSL "https://dot.net/v1/dotnet-install.sh" -o "$script"
  elif command -v wget >/dev/null 2>&1; then
    wget -qO "$script" "https://dot.net/v1/dotnet-install.sh"
  else
    echo "[.NET] Need curl or wget to download the install script." >&2
    return 1
  fi
  chmod +x "$script"
  bash "$script" --channel "$DOTNET_CHANNEL" --install-dir "$HOME/.dotnet"
  export DOTNET_ROOT="$HOME/.dotnet"
  export PATH="$HOME/.dotnet:$PATH"
  echo "[.NET] Done."
}

run_source() {
  cd "$ROOT/mod-manager-plus"
  exec dotnet run --project "$PROJ"
}

run_published() {
  if [[ -x "$PUB" ]]; then
    exec "$PUB"
  fi
  echo "Published binary not found:"
  echo "  $PUB"
  echo "Build it with: ./mod-manager-plus/scripts/publish.sh linux-x64"
  exit 1
}

fail_no_dotnet() {
  echo ".NET ${DOTNET_CHANNEL} SDK not found and no published binary at:"
  echo "  $PUB"
  echo "Install SDK: https://dotnet.microsoft.com/download/dotnet/8.0"
  echo "Or publish:  ./mod-manager-plus/scripts/publish.sh linux-x64"
  exit 1
}

if [[ "${TNI_MM_PREFER_BUNDLE:-0}" == "1" ]]; then
  run_published
fi

if has_dotnet_sdk_8; then
  run_source
fi

if [[ -x "$PUB" ]]; then
  run_published
fi

if [[ "${TNI_MM_AUTO_INSTALL_DOTNET:-1}" == "0" ]]; then
  fail_no_dotnet
fi

install_dotnet_sdk || fail_no_dotnet

if has_dotnet_sdk_8; then
  run_source
fi

fail_no_dotnet
