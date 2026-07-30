# Flying Fox

A cozy **tile-laying deckbuilder** inspired by games like Hexfell and Dorfromantik.  
Draw tiles from a deck, rotate them, and grow a hex island of forest, meadow, water, and rock for a fox to call home.

Ships as a **standalone web game** and a **Firefox extension** (no build step, no dependencies).

**Repository:** [github.com/dglogan42/flying-fox](https://github.com/dglogan42/flying-fox)

![License: MIT](https://img.shields.io/badge/license-MIT-green)
![Platform: Web / Firefox / Unity](https://img.shields.io/badge/platform-Web%20%7C%20Firefox%20%7C%20Unity-orange)

### Steam / Unity (in progress)

| Path | What |
|------|------|
| [`unity/`](unity/) | Unity project — pure C# **Core** scaffold (hex, deck, score, run) |
| [`docs/design/STEAM_V1_DESIGN.md`](docs/design/STEAM_V1_DESIGN.md) | Full Steam v1 design + PR plan |
| [`docs/STEAM_CHECKLIST.md`](docs/STEAM_CHECKLIST.md) | Shipping checklist |

Open `unity/` in **Unity Hub** (Unity 6 LTS preferred). See [`unity/README.md`](unity/README.md).

---

## Features

- Hex map with four biomes: **Forest**, **Meadow**, **Water**, **Rock**
- **Deck of 36** tiles and a **hand of 3**
- Edge-matching score plus **perfect placement** bonus
- Side **quests** (clusters and island size) for big points
- Pan / zoom map, rotate tiles, best score saved in `localStorage`
- Firefox toolbar button (and **Alt+Shift+F**) to open the game tab

---

## How to play

1. Start a run — you begin with a **fox den** hub tile.
2. Draw tiles into a hand of three from the deck.
3. **Select** a tile, **rotate** it, then **place** it on a glowing adjacent hex.
4. Match edges with neighbors for points; perfect fits score a bonus.
5. Complete quests before the deck runs out.

### Scoring

| Action | Points |
|--------|--------:|
| Place a tile | +2 |
| Matching edge | +12 each |
| Perfect placement (all contacts match) | +20 |
| Quests | +25–50 each |

### Controls

| Input | Action |
|--------|--------|
| Click tile in hand | Select |
| `1` `2` `3` / `Tab` | Select / cycle hand |
| `R` / `Q` / ↻ button | Rotate tile |
| Click glowing hex | Place |
| Drag map | Pan |
| Scroll | Zoom |
| **Alt+Shift+F** (extension) | Open game tab |

---

## Quick start (web)

Open `index.html` in a modern browser, or serve the folder:

```bash
cd flying-fox
python3 -m http.server 8080
# visit http://localhost:8080
```

---

## Firefox extension

### Temporary install (development)

1. Open Firefox → `about:debugging#/runtime/this-firefox`
2. Click **Load Temporary Add-on…**
3. Select `manifest.json` in this directory
4. Click the **Flying Fox** toolbar icon  
   (Extensions puzzle menu → pin it if needed)

Temporary add-ons are removed when Firefox restarts. Use **Reload** on `about:debugging` after code changes.

### Package for distribution

```bash
cd flying-fox
zip -r flying-fox-firefox.zip \
  manifest.json background.js index.html game.js styles.css icons
```

Install the zip from `about:addons` → gear → **Install Add-on From File…**  
(Release Firefox requires signing via [addons.mozilla.org](https://addons.mozilla.org/) for permanent install; Developer Edition / Nightly are more flexible for local unsigned builds.)

### Extension layout

| Path | Role |
|------|------|
| `manifest.json` | WebExtension MV3 (Gecko id, icons, action) |
| `background.js` | Toolbar click → open / focus game tab |
| `index.html` | Game shell |
| `game.js` | Hex map, deck, placement, scoring, quests |
| `styles.css` | UI theme |
| `icons/` | Toolbar & install icons (16–128px) |

No host permissions. Best score uses `localStorage` on the extension page origin.

---

## Project structure

```
flying-fox/
├── index.html          # Game UI
├── game.js             # Core game logic
├── styles.css          # Layout & theme
├── manifest.json       # Firefox extension manifest
├── background.js       # Extension background script
├── icons/              # Extension icons
├── LICENSE             # MIT
├── README.md
└── .gitignore
```

---

## Development

There is no build pipeline. Edit the HTML/CSS/JS and refresh the browser (or reload the temporary add-on).

Optional checks:

```bash
# syntax-check the game script
node --check game.js

# validate manifest JSON
python3 -c "import json; json.load(open('manifest.json')); print('ok')"
```

---

## License

This project is licensed under the [MIT License](LICENSE). See `LICENSE` for the full text.
