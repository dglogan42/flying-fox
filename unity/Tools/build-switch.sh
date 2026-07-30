#!/usr/bin/env bash
# Nintendo Switch build helper.
# Real Switch output requires NintendoSDK + Unity Switch module (NDA).
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

echo "=============================================="
echo " Flying Fox — Nintendo Switch build"
echo "=============================================="
echo ""
echo "A store-ready Switch binary cannot be produced without:"
echo "  • Nintendo Developer Portal account"
echo "  • NintendoSDK"
echo "  • Unity 'Nintendo Switch Support' module"
echo ""
echo "Product prep in this repo:"
echo "  docs/SWITCH_ESHP.md"
echo "  docs/store/switch/LISTING.md"
echo "  docs/store/switch/CONTROLS.md"
echo "  Gamepad cursor + place (SwitchCursorController)"
echo ""

if [[ -n "${UNITY_PATH:-}" && -x "${UNITY_PATH}" ]]; then
  echo "Trying Unity batchmethod BuildNintendoSwitch…"
  "$UNITY_PATH" \
    -batchmode -nographics -quit \
    -projectPath "$ROOT" \
    -logFile "$ROOT/Builds/logs/switch.log" \
    -executeMethod FlyingFox.EditorTools.BuildScript.BuildNintendoSwitch \
    || true
  echo "Log: $ROOT/Builds/logs/switch.log"
else
  echo "Set UNITY_PATH to a Switch-capable Editor binary when SDK is installed."
  echo "Example:"
  echo "  UNITY_PATH=/path/to/Unity ./Tools/build-switch.sh"
fi

exit 0
