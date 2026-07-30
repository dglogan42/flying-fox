# Flying Fox — Build pipeline

## Overview

| Pipeline | When | Needs Unity license? | What it does |
|----------|------|----------------------|--------------|
| **CI** (`ci.yml`) | Every push / PR | No | Compiles pure Core + smoke run; checks web/extension files |
| **Unity EditMode** (`unity-tests.yml`) | Unity path changes + manual | **Yes** (secrets) | Runs `CoreParityTests` via [game-ci](https://game.ci) |
| **Unity Player** (`unity-build.yml`) | Manual (workflow_dispatch) | **Yes** | Windows / Linux / WebGL player artifacts |
| **Local scripts** | Dev machine | Local Editor install | `unity/Tools/build.sh`, `run-editmode-tests.sh` |

```
push / PR
   │
   ├─► ci.yml ── Core smoke + static checks  (always)
   │
   └─► unity-tests.yml ── EditMode  (if UNITY_LICENSE set)

workflow_dispatch
   └─► unity-build.yml ── player zip artifact
```

## Local builds

### Prerequisites

1. Unity Hub + **Unity 6 LTS** (match `unity/ProjectSettings/ProjectVersion.txt`)
2. Modules: **Windows Build Support**, optional Linux / WebGL
3. Open the project once so `Library/` generates

### Player build

```bash
cd unity
chmod +x Tools/build.sh Tools/run-editmode-tests.sh

# Windows x64 → unity/Builds/Windows/FlyingFox.exe
./Tools/build.sh windows

# Linux x64 (Steam Deck-friendly)
./Tools/build.sh linux

# WebGL
./Tools/build.sh webgl

# With scripting define (Steam / demo)
FF_DEFINES=FF_STEAM ./Tools/build.sh windows
FF_DEFINES=FF_DEMO ./Tools/build.sh windows

# Custom Unity binary
UNITY_PATH="$HOME/Unity/Hub/Editor/6000.0.xxf1/Editor/Unity" ./Tools/build.sh windows
```

Menu alternative (Editor open): **Flying Fox → Build → Windows x64**.

### EditMode tests

```bash
cd unity
./Tools/run-editmode-tests.sh
# Results: Builds/TestResults/EditMode-results.xml
```

Or **Window → General → Test Runner → EditMode → Run All**.

### Core-only verify (no Unity)

```bash
# from repo root
bash Tools/ci-verify-core.sh
```

## GitHub Actions setup (Unity jobs)

1. Create a Unity license file for CI ([game-ci activation](https://game.ci/docs/github/activation)).
2. Repo **Settings → Secrets and variables → Actions**:

| Secret | Purpose |
|--------|---------|
| `UNITY_LICENSE` | Full contents of `Unity_lic.ulf` (or activation file) |
| `UNITY_EMAIL` | Unity account email |
| `UNITY_PASSWORD` | Unity account password |

3. Pin the real editor version in:
   - `unity/ProjectSettings/ProjectVersion.txt`
   - `.github/workflows/unity-tests.yml` → `unityVersion:`
   - `.github/workflows/unity-build.yml` → `unityVersion:`

4. Run **Actions → Unity Player Build → Run workflow**.

Without secrets, Unity workflows **skip** (`if: secrets.UNITY_LICENSE != ''`); Core CI still runs.

## Outputs

| Path | Content |
|------|---------|
| `unity/Builds/Windows/` | Windows player |
| `unity/Builds/Linux/` | Linux player |
| `unity/Builds/WebGL/` | WebGL |
| `unity/Builds/logs/` | Local batch logs |
| `unity/Builds/TestResults/` | Local NUnit XML |
| GitHub Actions artifacts | Uploaded player / test results |

`unity/Builds/` is gitignored.

## Scripting defines

| Define | Use |
|--------|-----|
| *(none)* | Offline / store-agnostic player |
| `FF_STEAM` | Steamworks init path (when package wired) |
| `FF_DEMO` | Classic-only demo strip (design) |

Pass via `FF_DEFINES` for local CLI or set in **Player Settings** for Editor builds.

## Steam (later)

1. Build Windows with `FF_STEAM`.
2. Upload with SteamCMD / partner site depots (see `STEAM_CHECKLIST.md`).
3. Keep `steam_appid.txt` out of git (already ignored).

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| `Unity not found` | Set `UNITY_PATH` to Editor binary |
| Build exits 1 | Read `unity/Builds/logs/*.log` or CI log |
| game-ci license fail | Re-activate license file; check secrets |
| Missing scenes | Ensure `EditorBuildSettings` lists `Game.unity` |
| Core CI fails on MathF | Use netstandard2.1 + modern SDK (workflow uses .NET 8) |

## Related

- [STEAM_V1_DESIGN.md](design/STEAM_V1_DESIGN.md) — product/PR plan  
- [STEAM_CHECKLIST.md](STEAM_CHECKLIST.md) — ship checklist  
- [unity/README.md](../unity/README.md) — open & play  
