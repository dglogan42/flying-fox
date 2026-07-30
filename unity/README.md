# Flying Fox — Unity (Steam v1)

Unity port of the [Flying Fox](https://github.com/dglogan42/flying-fox) hex tile-laying deckbuilder.

Design doc: [`../docs/design/STEAM_V1_DESIGN.md`](../docs/design/STEAM_V1_DESIGN.md)

## Status

| Layer | State |
|-------|--------|
| **Core** (pure C#, no `UnityEngine`) | Scaffolded — hex, board, deck, RNG, score, quests, run |
| **Presentation / UI / Steam** | Not yet (PR-07+) |
| **Unity project files** | Partial — open in Hub to generate Library + .meta |

Web game remains playable under the repo root (`index.html` / Firefox extension).

## Requirements

1. [Unity Hub](https://unity.com/download)
2. **Unity 6 LTS** (preferred) or **2022.3 LTS** — pin the exact version in `ProjectSettings/ProjectVersion.txt` after install
3. Modules: **Windows Build Support** (Steam primary), optional Linux

Steamworks.NET is added later (`FF_STEAM`); Core does not need it.

## Open the project

```bash
# From Unity Hub:
# Add → /path/to/flying-fox/unity
# Open with Unity 6 LTS
```

First open will:

- Resolve packages (URP, Input System, Test Framework)
- Generate `.meta` files under `Assets/`
- Create default scenes if missing — add `Boot`, `MainMenu`, `Game` under `Assets/_Project/Scenes/` (PR-01/10)

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
