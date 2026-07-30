#!/usr/bin/env bash
# Run EditMode tests in batchmode.
# Usage: ./Tools/run-editmode-tests.sh

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

find_unity() {
  if [[ -n "${UNITY_PATH:-}" && -x "${UNITY_PATH}" ]]; then
    echo "$UNITY_PATH"
    return
  fi
  if command -v unity-editor >/dev/null 2>&1; then
    command -v unity-editor
    return
  fi
  local candidates=(
    "$HOME/Unity/Hub/Editor"/*/Editor/Unity
    "/opt/unity/Editor/Unity"
  )
  local c
  for c in ${candidates[@]+"${candidates[@]}"}; do
    if [[ -x "$c" ]]; then
      echo "$c"
      return
    fi
  done
  echo "ERROR: Unity Editor not found. Set UNITY_PATH." >&2
  exit 1
}

UNITY="$(find_unity)"
RESULTS="${RESULTS_PATH:-$ROOT/Builds/TestResults}"
mkdir -p "$RESULTS"
LOG="$RESULTS/editmode.log"

echo "==> EditMode tests via $UNITY"
"$UNITY" \
  -batchmode \
  -nographics \
  -projectPath "$ROOT" \
  -runTests \
  -testPlatform EditMode \
  -testResults "$RESULTS/EditMode-results.xml" \
  -logFile "$LOG"

echo "==> Results: $RESULTS/EditMode-results.xml"
echo "==> Log: $LOG"
