# Flying Fox — Steam shipping checklist

Living list. Design: [design/STEAM_V1_DESIGN.md](design/STEAM_V1_DESIGN.md).

## Partner

- [ ] Steamworks partner account + $100 app fee
- [ ] App ID created; `steam_appid.txt` for local tests only (never ship with wrong ID)
- [ ] Store page draft (Coming Soon)

## Build

- [ ] Unity LTS pinned in `unity/ProjectSettings/ProjectVersion.txt`
- [ ] Windows x64 player builds clean
- [ ] Optional: Linux for Deck
- [ ] `FF_STEAM` define on Steam depot; off for offline debug
- [ ] `FF_DEMO` depot for Classic-only demo

## Achievements (v1)

| API name | Title | Rule |
|----------|--------|------|
| FF_FIRST_CANOPY | First Canopy | Natural Classic, empty deck+hand |
| FF_BIRCH | Birch Leaf | Score ≥ 100 |
| FF_OAK | Oak Crown | Score ≥ 280 |
| FF_GOLDEN_FOX | Golden Fox | Score ≥ 400 |
| FF_PERFECT_TEN | Perfect Ten | 10 perfects in one scored run |
| FF_QUEST_MASTER | Quest Master | All 5 quests one run |
| FF_DAILY_FIRST | Morning Dew | Finish a scored Daily |
| FF_DAILY_WEEK | Week in the Woods | Daily streak ≥ 7 |
| FF_MATCHMAKER | Matchmaker | ≥ 50 edge matches one run |
| FF_EMPTY_DECK | Empty Deck | Natural end, empty deck+hand |

## Store

- [ ] Capsule / header / screenshots (min art bar)
- [ ] Trailer ~60–90s
- [ ] Tags: Casual, Strategy, Deckbuilding, Singleplayer, Indie, Hex…
- [ ] Do **not** name-drop Dorfromantik/Hexfell on store page
- [ ] Wishlist push after playable Classic build

## Legal / content

- [ ] Original art + audio (or cleared CC0) listed in THIRD_PARTY.md
- [ ] MIT root LICENSE; commercial Steam OK under MIT if third-party ok
- [ ] Privacy: profile local + optional Steam Cloud only
