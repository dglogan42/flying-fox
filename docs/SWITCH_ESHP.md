# Flying Fox — Nintendo Switch (eShop) store version

## Reality check (read first)

A **real Switch eShop binary** requires:

1. [Nintendo Developer Portal](https://developer.nintendo.com/) registration (company/individual)
2. Accepted developer agreement + environment access
3. **NintendoSDK** + Unity **Nintendo Switch Support** module (not public; Unity Hub after Nintendo approval)
4. Lotcheck / certification (TRC/XR / platform guidelines)
5. eShop product page, pricing, ratings (IARC), screenshots, trailer

This repo **cannot** ship a `.nsp` / upload to eShop without those. What we maintain here is a **Switch-ready product slice**: gamepad-first controls, docked/handheld UI notes, platform stubs, store listing draft, and build hooks that compile when `UNITY_SWITCH` is available.

---

## Product positioning (eShop)

| Field | Draft |
|--------|--------|
| **Title** | Flying Fox |
| **Short** | Cozy hex tile-laying deckbuilder — grow a canopy for a fox |
| **Genre** | Puzzle / Strategy / Casual |
| **Players** | 1 |
| **Modes** | TV / Tabletop / Handheld |
| **Save data** | Required (profile, bests, unlocks) |
| **Online** | Not required for v1 (local Daily seed only) |
| **Price** | TBD (indie cozy tier; often $4.99–$9.99 USD) |
| **Age** | Expected **Everyone** / PEGI 3 / CERO A — confirm with IARC questionnaire |

### Long description (draft)

> Grow a tiny island home for a clever fox. Draw tiles, rotate them, and match forest, meadow, water, and rock edges to score. Complete quests, trigger fox abilities on each biome, and chase medals before the deck runs out.  
>  
> Includes Classic runs, a shared Daily island (local), and a cozy presentation designed for TV and handheld.  
>  
> Full controller support. No online account required.

### Keywords / tags (draft)

`puzzle`, `strategy`, `deckbuilding`, `hex`, `cozy`, `casual`, `single player`, `indie`, `relaxing`

### Do **not** use on store page

- Names of other commercial games (Dorfromantik, Hexfell, etc.) as comparison titles in marketing that Nintendo/legal would flag — keep original voice.

---

## Control scheme (locked for cert-friendly UX)

| Input | Action |
|--------|--------|
| **Left stick / D-pad** | Move placement cursor among valid hexes |
| **A** | Place selected hand tile |
| **B** | Rotate tile CW (hold + A still place) |
| **X** | Cycle hand |
| **Y** | Rotate tile CCW |
| **L / ZL** | Select previous hand slot |
| **R / ZR** | Select next hand slot / rotate CW |
| **Right stick** | Pan camera |
| **ZL+ZR** or **−** | Zoom out / in (or right-stick click) |
| **+** | Pause / results confirm |
| **−** | Abandon run (confirm) |

Touch (handheld): tap hex to place, swipe rotate optional stretch.

Keyboard/mouse remain for Editor / PC builds.

---

## Technical requirements (Unity)

| Area | Approach |
|------|----------|
| Engine | Unity LTS with **Switch module** from Nintendo + Unity |
| Script define | `FF_SWITCH` + platform `UNITY_SWITCH` |
| Input | Gamepad cursor (see `SwitchCursorController`, gamepad in `MapInputController`) |
| Resolution | Dynamic; UI scale for 720p handheld / 1080p docked |
| Safe area | TV overscan margin on HUD |
| Save | `IProfileStore` → NX save data API under `#if UNITY_SWITCH` |
| Audio | Mixer; respect system mute / headphones |
| Performance | Target **60 FPS** docked & handheld; Core is lightweight |
| Language | English first; string table ready for EN/JA/ES/FR/DE |

### Build (when SDK installed)

```text
// Editor (with Switch module)
Flying Fox → Build → Nintendo Switch   // when UNITY_SWITCH present

// Or Nintendo's Unity build pipeline per their docs
```

Local stub: `unity/Tools/build-switch.sh` prints prerequisites if SDK missing.

### CI note

Switch builds **cannot** run on public GitHub runners (SDK NDA). Build on a licensed Windows machine or Nintendo-approved CI.

---

## Certification checklist (high level)

Use Nintendo’s current TRC/lotcheck docs (versioned; verify on portal). Typical cozy single-player items:

- [ ] Title boots to interactive state within guideline time
- [ ] + opens pause / options; can return to play
- [ ] Language matches eShop locale where claimed
- [ ] Save: create / load / delete (or overwrite) flows tested
- [ ] No crashes on sleep/wake, dock/undock, controller disconnect
- [ ] Controllers: Joy-Con grip, dual Joy-Con, Pro Controller, handheld
- [ ] Suspend/resume retains run or returns to safe menu
- [ ] No unlicensed IP / font / audio
- [ ] Age rating icons and privacy text if required
- [ ] Performance: no multi-second freezes on place/draw

---

## Asset list for eShop page

| Asset | Spec (confirm portal) |
|--------|------------------------|
| Icon | 1024×1024 (and generated sizes) |
| Screenshots | ≥6, docked + handheld recommended |
| Trailer | 30–90s, H.264, no competitor logos |
| Logo / wordmark | Transparent PNG |
| Description | Short + long per locale |

Working folder: `docs/store/switch/` (listing text + control card).

---

## Roadmap to store

| Phase | Work | Owner |
|-------|------|--------|
| **S0** | Register Nintendo developer; request Switch Unity support | You |
| **S1** | Gamepad cursor + **pause menu** + **dock/undock UI scale** (in repo) | Engineering |
| **S2** | NX save wrapper, sleep/dock handlers | Engineering + SDK |
| **S3** | Art bar + trailer + IARC | Creative |
| **S4** | Lotcheck master → submission | You + QA |
| **S5** | Release / patch pipeline | You |

**S1 is implemented in this repo:**

| Feature | Types |
|---------|--------|
| Gamepad hex cursor | `SwitchCursorController` |
| Pause (+ / Esc / Start) | `PauseMenuController` — timeScale 0, resume / new / abandon confirm / controls |
| Dock ↔ undock UI | `DisplayModeService` — form factor, safe area + TV overscan, font scale, ortho base |
| Suspend / focus loss | Auto-pause on `OnApplicationPause` / focus loss |

S2–S5 need Nintendo SDK access.

### Simulate dock / undock in Editor

Resize the Game view:

| Approx size | Mode |
|-------------|------|
| ≤1280×800 | **Handheld** (UI scale 1.0, tighter ortho) |
| ≥1600×900 | **Docked / Desktop** (UI scale ~1.1–1.28, TV overscan margin) |

Pause shows current form factor and resolution.

---

## Related

- [BUILD_PIPELINE.md](BUILD_PIPELINE.md) — PC/Steam/WebGL CI  
- [STEAM_CHECKLIST.md](STEAM_CHECKLIST.md) — Steam parallel track  
- [design/STEAM_V1_DESIGN.md](design/STEAM_V1_DESIGN.md) — core product (modes, abilities)  
- Unity: `Scripts/Platform/`, `Scripts/Presentation/SwitchCursorController.cs`  
