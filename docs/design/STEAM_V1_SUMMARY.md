# Design Summary — Flying Fox Steam v1

**Date:** 2026-07-30  
**Design doc:** `/tmp/grok-1000/grok-design-doc-2c5a44e5.md` (rev 2 — review amendments)  
**Review file:** `/tmp/grok-1000/grok-design-review-2c5a44e5.md`  
**Status:** Draft (all 18 review issues addressed)

## What was produced

A full product + technical design for porting browser/Firefox **Flying Fox** (`/home/oem/flying-fox/`, core loop in `game.js`) to a **Unity Steam standalone v1**, revised after design review.

### Headline product bets
- **Modes:** Classic (rules parity with web) + **Daily** (UTC seed); Endless deferred
- **Daily lock:** SHA-256(`FlyingFoxDaily|yyyy-MM-dd`) → LE int32 seed; **SplitMix64** via `IRng`; golden seed vectors
- **Scoring parity:** +2 / +12 / +20; breakdown invariant; hub free (not in tile points)
- **Achievements:** 10 locked; **`FF_EMPTY_DECK`** (not Homestead / placed≥36)
- **Meta:** 3 fox skins, 3 table themes; practice Daily mutates nothing
- **Stack pins:** Unity LTS (exact string at P0), URP 2D, **UI Toolkit all UI**, **Steamworks.NET**, System.Text.Json, AppRoot registry, `Application.persistentDataPath`
- **Steam:** Windows primary, Cloud profile, no leaderboards v1
- **PR plan:** Split core PRs (03a/03b), Steam not blocked on cosmetics, min art bar before public store shots

### Document structure
Goals/non-goals, modes, gameplay port map, Unity architecture, profile schema + mutation rules, content budget, Steamworks, IP/licensing, art/audio, store/demo, security, observability, rollout, roadmap, alternatives, open questions, **Key Decisions (KD1–KD20)**, **Appendix A** web constants, **PR Plan** (~20+ ordered PRs with dependency graph).

## Source systems cited
- `game.js` — biomes, hex math, deck/hand, placement/scoring/breakdown, quests/clusters, medals, camera, end conditions
- `index.html` / `README.md` — shell UX, scoring table
- `LICENSE` — MIT, Copyright 2026 David Logan
- Extension files — not ported

## Review revision
All open issues (1–18) marked **addressed** in the review file with per-issue Response fields and a Revision Summary. No wontfix / needs-user-input. Residual product choices remain as design Open Questions (price, Unity version string, partner account, etc.).
