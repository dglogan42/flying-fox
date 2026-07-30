#!/usr/bin/env bash
# Local Unity player builds for Flying Fox.
# Usage:
#   ./Tools/build.sh windows
#   ./Tools/build.sh linux
#   ./Tools/build.sh webgl
#   ./Tools/build.sh all
#   FF_DEFINES=FF_STEAM ./Tools/build.sh windows
#   UNITY_PATH=/path/to/Unity ./Tools/build.sh windows

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

TARGET="${1:-windows}"
OUTPUT="${OUTPUT_PATH:-$ROOT/Builds}"
METHOD_NS="FlyingFox.EditorTools.BuildScript"

find_unity() {
  if [[ -n "${UNITY_PATH:-}" && -x "${UNITY_PATH}" ]]; then
    echo "$UNITY_PATH"
    return
  fi
  if command -v unity-editor >/dev/null 2>&1; then
    command -v unity-editor
    return
  fi
  # Common Hub install locations
  local candidates=(
    "$HOME/Unity/Hub/Editor"/*/Editor/Unity
    "/opt/unity/Editor/Unity"
    "/Applications/Unity/Hub/Editor"/*/Unity.app/Contents/MacOS/Unity
  )
  local c
  for c in ${candidates[@]+"${candidates[@]}"}; do
    if [[ -x "$c" ]]; then
      echo "$c"
      return
    fi
  done
  echo "ERROR: Unity Editor not found. Set UNITY_PATH to the Unity binary." >&2
  exit 1
}

UNITY="$(find_unity)"
echo "==> Unity: $UNITY"
echo "==> Project: $ROOT"
echo "==> Output: $OUTPUT"
echo "==> Target: $TARGET"

run_build() {
  local method="$1"
  local log="$OUTPUT/logs/build-$(date -u +%Y%m%dT%H%M%SZ).log"
  mkdir -p "$OUTPUT/logs"
  echo "==> $method (log: $log)"

  local extra=()
  if [[ -n "${FF_DEFINES:-}" ]]; then
    extra+=(-defines "$FF_DEFINES")
  fi

  "$UNITY" \
    -batchmode \
    -nographics \
    -quit \
    -projectPath "$ROOT" \
    -logFile "$log" \
    -executeMethod "${METHOD_NS}.${method}" \
    -outputPath "$OUTPUT" \
    "${extra[@]}"

  echo "==> Done $method"
}

case "$TARGET" in
  windows|win|win64)
    run_build BuildWindows64
    ;;
  linux|linux64)
    run_build BuildLinux64
    ;;
  webgl)
    run_build BuildWebGL
    ;;
  all|desktop)
    run_build BuildAllDesktop
    ;;
  *)
    echo "Unknown target: $TARGET (windows|linux|webgl|all)" >&2
    exit 2
    ;;
esac

echo "==> Artifacts under $OUTPUT"
ls -la "$OUTPUT" || true
