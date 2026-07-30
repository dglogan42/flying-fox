# Flying Fox — Unity (Steam v1)

Unity port of the [Flying Fox](https://github.com/dglogan42/flying-fox) hex tile-laying deckbuilder.

Design doc: [`../docs/design/STEAM_V1_DESIGN.md`](../docs/design/STEAM_V1_DESIGN.md)

## Status

| Layer | State |
|-------|--------|
| **Core** (pure C#, no `UnityEngine`) | Hex, board, deck, RNG, score, quests, run |
| **Presentation (PR-07/08 slice)** | Wedge tiles, map, pan/zoom, ghost place, IMGUI HUD |
| **Playable scene** | `Assets/_Project/Scenes/Game.unity` + `GameBootstrap` |
| **UI Toolkit / menus / Steam** | Later (PR-09+) |

Web game remains playable under the repo root (`index.html` / Firefox extension).

## Play in the Editor (first playable)

1. [Unity Hub](https://unity.com/download) → **Add** → `flying-fox/unity` → **Unity 6 LTS**
2. Open **`Assets/_Project/Scenes/Game`**
3. Press **Play**

`GameBootstrap` builds camera, map, input, and starts a **Classic** `RunController` run.

### Controls

| Input | Action |
|--------|--------|
| **LMB** on slot | Place selected hand tile |
| **RMB** / **MMB** / **Space+LMB** | Pan |
| **Scroll** | Zoom (0.45–2.4×) |
| **R** / **E** / **Q** / **Z** | Rotate tile |
| **1–3** / **Tab** | Select / cycle hand |
| **Esc** | Abandon run |
| **Ctrl+N** | New run |
| HUD | Hand, quests, New / End / Same seed |

Ghost shows **+score** and match count. Clicks on the left HUD do not place tiles.

### Runtime hierarchy

```
— Flying Fox —
├── Main Camera (+ MapCameraController)
├── Directional Light
├── HexMap
├── GhostPlacement
└── GameSession (+ MapInput, HUD)
```

## Requirements

1. [Unity Hub](https://unity.com/download)
2. **Unity 6 LTS** (preferred) or **2022.3 LTS** — pin the exact version in `ProjectSettings/ProjectVersion.txt` after install
3. Modules: **Windows Build Support** (Steam primary), optional Linux

Steamworks.NET is added later (`FF_STEAM`); Core does not need it.

## Open the project

```bash
# Unity Hub → Add → /path/to/flying-fox/unity → Unity 6 LTS
```

First open will resolve packages (URP, Input System, Test Framework) and generate `.meta` files.

## Run EditMode tests

In Unity: **Window → General → Test Runner → EditMode → Run All**

Tests live in `Assets/Tests/EditMode/CoreParityTests.cs` and lock web parity (scoring, hex edges, Daily seed, medals).

## Core architecture

```
Assets/_Project/Scripts/Core/     # FlyingFox.Core asmdef — no UnityEngine
  Hex/       BiomeId, HexCoord, HexMath
  Board/     TileModel, BoardModel
  Deck/      DeckFactory (presets + randomEdges parity)
  Rng/       IRng, SplitMix64Rng, DailySeed
  Placement/ PlacementService
  Score/     GameBalance, ScoreBreakdown
  Quests/    QuestCatalog, BiomeClusterAnalyzer, QuestService
  Run/       RunController (headless state machine)
```

**Rule:** keep `Core/` free of `UnityEngine` so logic is testable and portable.

### Quick headless usage (conceptual)

```csharp
var run = new RunController();
run.Start(new RunConfig { Mode = RunMode.Classic, Seed = 42 });
run.RotateSelected(1);
run.TryPlace(run.Board.GetEmptyAdjacent()[0]);
var result = run.BuildResult();
```

Daily:

```csharp
int seed = DailySeed.TodayUtc();
run.Start(new RunConfig { Mode = RunMode.Daily, Seed = seed }, new SplitMix64Rng(seed));
```

## Implementation order (from design PR plan)

1. ~~PR-01 bootstrap~~ (this folder)
2. ~~PR-02–06 Core~~ (scaffolded; flesh tests/fixtures next)
3. **PR-07** Hex mesh view + camera  
4. **PR-08–09** Input + UI Toolkit HUD  
5. **PR-10** Menus  
6. **PR-11–12** Profile + Daily  
7. **PR-13+** Unlocks, audio, Steamworks, polish  

## Modes (v1)

| Mode | Notes |
|------|--------|
| **Classic** | Web rules parity |
| **Daily** | UTC seed via SHA-256 → SplitMix64 |
| Practice Daily | Same puzzle, no meta/achievements |

## Scoring (web parity)

| Action | Points |
|--------|-------:|
| Place | +2 |
| Match edge | +12 |
| Perfect placement | +20 |
| Quests | +25–50 |

Hub tile is free (not counted in place stats). Natural end: empty deck+hand, or no adjacent slots.

## License

Same as parent repo (MIT). See root `LICENSE`.
