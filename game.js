(() => {
  "use strict";

  // ─── constants ──────────────────────────────────────────
  const BIOME = {
    FOREST: "F",
    MEADOW: "M",
    WATER: "W",
    ROCK: "R",
  };

  const BIOME_COLOR = {
    F: { fill: "#2d6a4f", edge: "#52b788", label: "Forest" },
    M: { fill: "#b5c76a", edge: "#d8e2a0", label: "Meadow" },
    W: { fill: "#1d6a8a", edge: "#4cc9f0", label: "Water" },
    R: { fill: "#6c757d", edge: "#adb5bd", label: "Rock" },
  };

  const BIOME_LIST = ["F", "M", "W", "R"];
  const HAND_SIZE = 3;
  const DECK_SIZE = 36;
  const HEX_SIZE = 36; // flat-top radius
  const STORAGE_KEY = "flying-fox-deck-best";

  // Flat-top hex neighbor deltas (edge index 0 = E, then clockwise)
  // edges: 0 E, 1 SE, 2 SW, 3 W, 4 NW, 5 NE
  const EDGE_DELTA = [
    [+1, 0], // 0 E
    [0, +1], // 1 SE
    [-1, +1], // 2 SW
    [-1, 0], // 3 W
    [0, -1], // 4 NW
    [+1, -1], // 5 NE
  ];
  const OPPOSITE = [3, 4, 5, 0, 1, 2];

  // ─── DOM ────────────────────────────────────────────────
  const canvas = document.getElementById("map");
  const ctx = canvas.getContext("2d");
  const handEl = document.getElementById("hand");
  const questListEl = document.getElementById("quest-list");
  const scoreEl = document.getElementById("score");
  const deckEl = document.getElementById("deck-count");
  const placedEl = document.getElementById("placed-count");
  const titleScreen = document.getElementById("title-screen");
  const endScreen = document.getElementById("end-screen");
  const bestTitleEl = document.getElementById("best-title");
  const bestEndEl = document.getElementById("best-end");
  const finalScoreEl = document.getElementById("final-score");
  const medalEl = document.getElementById("medal");
  const endBreakdownEl = document.getElementById("end-breakdown");
  const endTitleEl = document.getElementById("end-title");
  const handHintEl = document.getElementById("hand-hint");
  const stageEl = document.getElementById("stage");

  // ─── state ──────────────────────────────────────────────
  let map = new Map(); // key "q,r" -> { q, r, edges: string[6], id }
  let deck = [];
  let hand = [];
  let selectedHand = 0;
  let score = 0;
  let placed = 0;
  let matchPoints = 0;
  let questPoints = 0;
  let quests = [];
  let best = Number(localStorage.getItem(STORAGE_KEY) || 0);
  let running = false;
  let hoverHex = null; // {q,r} or null
  let breakdown = { matches: 0, perfects: 0, quests: 0, tiles: 0 };

  // camera
  let camX = 0;
  let camY = 0;
  let camZoom = 1;
  let dragging = false;
  let dragStart = null;
  let camStart = null;
  let didDrag = false;

  let animT = 0;
  let lastPlace = null; // pulse effect
  let raf = 0;

  // ─── hex math ───────────────────────────────────────────
  function key(q, r) {
    return `${q},${r}`;
  }

  function hexToPixel(q, r) {
    const x = HEX_SIZE * (1.5 * q);
    const y = HEX_SIZE * ((Math.sqrt(3) / 2) * q + Math.sqrt(3) * r);
    return { x, y };
  }

  function pixelToHex(px, py) {
    const q = ((2 / 3) * px) / HEX_SIZE;
    const r = ((-1 / 3) * px + (Math.sqrt(3) / 3) * py) / HEX_SIZE;
    return axialRound(q, r);
  }

  function axialRound(q, r) {
    let x = q;
    let z = r;
    let y = -x - z;
    let rx = Math.round(x);
    let ry = Math.round(y);
    let rz = Math.round(z);
    const xDiff = Math.abs(rx - x);
    const yDiff = Math.abs(ry - y);
    const zDiff = Math.abs(rz - z);
    if (xDiff > yDiff && xDiff > zDiff) rx = -ry - rz;
    else if (yDiff > zDiff) ry = -rx - rz;
    else rz = -rx - ry;
    return { q: rx, r: rz };
  }

  function hexCorners(cx, cy, size) {
    const pts = [];
    for (let i = 0; i < 6; i++) {
      const angle = (Math.PI / 180) * (60 * i);
      pts.push([cx + size * Math.cos(angle), cy + size * Math.sin(angle)]);
    }
    return pts;
  }

  function neighbor(q, r, edge) {
    const [dq, dr] = EDGE_DELTA[edge];
    return { q: q + dq, r: r + dr };
  }

  // ─── tiles / deck ───────────────────────────────────────
  let tileSeq = 0;

  function makeTile(edges) {
    return { id: ++tileSeq, edges: edges.slice() };
  }

  function rotateEdges(edges, steps) {
    const n = ((steps % 6) + 6) % 6;
    if (!n) return edges.slice();
    return edges.slice(6 - n).concat(edges.slice(0, 6 - n));
  }

  function rotateHandTile(steps = 1) {
    const t = hand[selectedHand];
    if (!t) return;
    t.edges = rotateEdges(t.edges, steps);
    renderHand();
    draw();
  }

  function randomEdges() {
    // Bias toward coherent wedges (looks better, more Dorfromantik-like)
    const mode = Math.random();
    if (mode < 0.35) {
      // two biomes split
      const a = pick(BIOME_LIST);
      let b = pick(BIOME_LIST);
      while (b === a) b = pick(BIOME_LIST);
      const split = 1 + Math.floor(Math.random() * 3);
      return Array.from({ length: 6 }, (_, i) => (i < split ? a : b));
    }
    if (mode < 0.55) {
      // three wedges
      const a = pick(BIOME_LIST);
      let b = pick(BIOME_LIST);
      while (b === a) b = pick(BIOME_LIST);
      let c = pick(BIOME_LIST);
      while (c === a || c === b) c = pick(BIOME_LIST);
      return [a, a, b, b, c, c];
    }
    if (mode < 0.7) {
      // mostly one biome
      const a = pick(BIOME_LIST);
      const edges = Array(6).fill(a);
      edges[Math.floor(Math.random() * 6)] = pick(BIOME_LIST.filter((x) => x !== a));
      return edges;
    }
    // pure random with mild smoothing
    const edges = Array.from({ length: 6 }, () => pick(BIOME_LIST));
    for (let i = 0; i < 6; i++) {
      if (Math.random() < 0.4) edges[i] = edges[(i + 5) % 6];
    }
    return edges;
  }

  function pick(arr) {
    return arr[Math.floor(Math.random() * arr.length)];
  }

  function shuffle(arr) {
    for (let i = arr.length - 1; i > 0; i--) {
      const j = Math.floor(Math.random() * (i + 1));
      [arr[i], arr[j]] = [arr[j], arr[i]];
    }
    return arr;
  }

  function buildDeck() {
    const tiles = [];
    // Guaranteed variety starters
    const presets = [
      ["F", "F", "F", "M", "M", "M"],
      ["W", "W", "W", "F", "F", "F"],
      ["M", "M", "R", "R", "M", "M"],
      ["F", "F", "W", "W", "F", "F"],
      ["R", "R", "R", "M", "M", "F"],
      ["W", "W", "M", "M", "M", "W"],
      ["F", "M", "M", "F", "F", "M"],
      ["W", "F", "F", "W", "W", "F"],
    ];
    for (const p of presets) tiles.push(makeTile(p));
    while (tiles.length < DECK_SIZE) tiles.push(makeTile(randomEdges()));
    return shuffle(tiles);
  }

  function drawToHand() {
    while (hand.length < HAND_SIZE && deck.length > 0) {
      hand.push(deck.pop());
    }
    if (selectedHand >= hand.length) selectedHand = Math.max(0, hand.length - 1);
    updateHUD();
    renderHand();
  }

  // ─── placement / scoring ────────────────────────────────
  function getEmptyAdjacent() {
    const set = new Map();
    for (const tile of map.values()) {
      for (let e = 0; e < 6; e++) {
        const n = neighbor(tile.q, tile.r, e);
        const k = key(n.q, n.r);
        if (!map.has(k)) set.set(k, n);
      }
    }
    return [...set.values()];
  }

  function evaluatePlacement(q, r, edges) {
    let matches = 0;
    let mismatches = 0;
    let contacts = 0;
    for (let e = 0; e < 6; e++) {
      const n = neighbor(q, r, e);
      const nt = map.get(key(n.q, n.r));
      if (!nt) continue;
      contacts++;
      const theirEdge = nt.edges[OPPOSITE[e]];
      if (theirEdge === edges[e]) matches++;
      else mismatches++;
    }
    return { matches, mismatches, contacts };
  }

  function isValidPlacement(q, r) {
    if (map.has(key(q, r))) return false;
    if (map.size === 0) return q === 0 && r === 0;
    // must touch existing
    for (let e = 0; e < 6; e++) {
      const n = neighbor(q, r, e);
      if (map.has(key(n.q, n.r))) return true;
    }
    return false;
  }

  function placeTile(q, r) {
    if (!running) return;
    const tile = hand[selectedHand];
    if (!tile) return;
    if (!isValidPlacement(q, r)) return;

    const eval_ = evaluatePlacement(q, r, tile.edges);
    // Soft rule: allow mismatch but reward matches heavily
    const placeScore =
      eval_.matches * 12 +
      (eval_.matches === eval_.contacts && eval_.contacts > 0 ? 20 : 0) +
      2; // base place

    map.set(key(q, r), { q, r, edges: tile.edges.slice(), id: tile.id });
    hand.splice(selectedHand, 1);
    if (selectedHand >= hand.length) selectedHand = Math.max(0, hand.length - 1);

    score += placeScore;
    matchPoints += placeScore;
    placed++;
    breakdown.matches += eval_.matches * 12;
    if (eval_.matches === eval_.contacts && eval_.contacts > 0) {
      breakdown.perfects += 20;
    }
    breakdown.tiles += 2;

    lastPlace = { q, r, t: performance.now() };
    const questGain = checkQuests();
    if (questGain > 0) {
      score += questGain;
      questPoints += questGain;
      breakdown.quests += questGain;
    }

    drawToHand();
    updateHUD();
    renderQuests();
    draw();

    if (hand.length === 0 && deck.length === 0) {
      endRun(true);
    } else if (hand.length > 0 && !hasAnyValidPlay()) {
      // Can still cycle/rotate — only end if no empty adjacent at all
      const empties = getEmptyAdjacent();
      if (empties.length === 0) endRun(true);
      else {
        handHintEl.textContent =
          "No perfect fits required — place on any glowing hex. Mismatches just score less.";
      }
    } else {
      handHintEl.textContent = hintForSelection();
    }
  }

  function hasAnyValidPlay() {
    return getEmptyAdjacent().length > 0 && hand.length > 0;
  }

  // ─── quests ─────────────────────────────────────────────
  function makeQuests() {
    return [
      {
        id: "forest5",
        title: "Fox Den",
        desc: "Connect 5+ forest edges in one region",
        target: 5,
        reward: 40,
        biome: "F",
        done: false,
        progress: 0,
      },
      {
        id: "water4",
        title: "River Run",
        desc: "Connect 4+ water edges in one region",
        target: 4,
        reward: 35,
        biome: "W",
        done: false,
        progress: 0,
      },
      {
        id: "meadow6",
        title: "Sunlit Glade",
        desc: "Connect 6+ meadow edges in one region",
        target: 6,
        reward: 45,
        biome: "M",
        done: false,
        progress: 0,
      },
      {
        id: "island8",
        title: "Home Island",
        desc: "Grow the map to 8 tiles",
        target: 8,
        reward: 25,
        type: "size",
        done: false,
        progress: 0,
      },
      {
        id: "island16",
        title: "Canopy Realm",
        desc: "Grow the map to 16 tiles",
        target: 16,
        reward: 50,
        type: "size",
        done: false,
        progress: 0,
      },
    ];
  }

  /** Largest connected edge-region of a biome (edges that match across tiles) */
  function largestBiomeCluster(biome) {
    // Graph of matched edge pairs of this biome
    const nodes = []; // {k, e}
    const nodeKey = (k, e) => `${k}:${e}`;
    const adj = new Map();

    for (const tile of map.values()) {
      const k = key(tile.q, tile.r);
      for (let e = 0; e < 6; e++) {
        if (tile.edges[e] !== biome) continue;
        const nk = nodeKey(k, e);
        nodes.push(nk);
        if (!adj.has(nk)) adj.set(nk, []);
        // connect to opposite edge on neighbor if same biome
        const n = neighbor(tile.q, tile.r, e);
        const nt = map.get(key(n.q, n.r));
        if (nt && nt.edges[OPPOSITE[e]] === biome) {
          const ok = nodeKey(key(n.q, n.r), OPPOSITE[e]);
          adj.get(nk).push(ok);
        }
        // connect adjacent edges on same tile if same biome
        const prev = (e + 5) % 6;
        const next = (e + 1) % 6;
        if (tile.edges[prev] === biome) adj.get(nk).push(nodeKey(k, prev));
        if (tile.edges[next] === biome) adj.get(nk).push(nodeKey(k, next));
      }
    }

    // BFS components — count unique tiles in each, or unique edges
    const seen = new Set();
    let bestSize = 0;
    for (const start of nodes) {
      if (seen.has(start)) continue;
      const stack = [start];
      seen.add(start);
      const tilesIn = new Set();
      let count = 0;
      while (stack.length) {
        const cur = stack.pop();
        count++;
        tilesIn.add(cur.split(":")[0]);
        for (const nb of adj.get(cur) || []) {
          if (!seen.has(nb) && adj.has(nb)) {
            seen.add(nb);
            stack.push(nb);
          }
        }
      }
      // progress = number of edges in cluster (or tiles — use edges for "connected edges")
      bestSize = Math.max(bestSize, count);
    }
    return bestSize;
  }

  function checkQuests() {
    let gained = 0;
    for (const q of quests) {
      if (q.done) continue;
      if (q.type === "size") {
        q.progress = map.size;
      } else {
        q.progress = largestBiomeCluster(q.biome);
      }
      if (q.progress >= q.target) {
        q.done = true;
        q.progress = q.target;
        gained += q.reward;
      }
    }
    return gained;
  }

  function renderQuests() {
    questListEl.innerHTML = "";
    for (const q of quests) {
      const li = document.createElement("li");
      li.className = "quest" + (q.done ? " done" : "");
      const pct = Math.min(100, Math.round((q.progress / q.target) * 100));
      li.innerHTML = `
        <div class="quest-title">${q.done ? "✓ " : ""}${q.title}</div>
        <div class="quest-desc">${q.desc}</div>
        <div class="quest-reward">${q.done ? "Claimed" : `+${q.reward} pts`} · ${q.progress}/${q.target}</div>
        <div class="quest-progress"><i style="width:${pct}%"></i></div>
      `;
      questListEl.appendChild(li);
    }
  }

  // ─── draw tile art ──────────────────────────────────────
  function paintHexTile(g, cx, cy, size, edges, opts = {}) {
    const corners = hexCorners(cx, cy, size);
    // wedge fill per edge sector
    for (let e = 0; e < 6; e++) {
      const c = BIOME_COLOR[edges[e]];
      g.fillStyle = c.fill;
      g.beginPath();
      g.moveTo(cx, cy);
      g.lineTo(corners[e][0], corners[e][1]);
      g.lineTo(corners[(e + 1) % 6][0], corners[(e + 1) % 6][1]);
      g.closePath();
      g.fill();
    }

    // subtle center blend
    const grd = g.createRadialGradient(cx, cy, 0, cx, cy, size * 0.45);
    grd.addColorStop(0, "rgba(255,255,255,0.12)");
    grd.addColorStop(1, "rgba(0,0,0,0)");
    g.fillStyle = grd;
    g.beginPath();
    g.moveTo(corners[0][0], corners[0][1]);
    for (let i = 1; i < 6; i++) g.lineTo(corners[i][0], corners[i][1]);
    g.closePath();
    g.fill();

    // edge strokes colored by biome
    for (let e = 0; e < 6; e++) {
      const c = BIOME_COLOR[edges[e]];
      g.strokeStyle = c.edge;
      g.lineWidth = size * 0.08;
      g.lineCap = "round";
      g.beginPath();
      g.moveTo(corners[e][0], corners[e][1]);
      g.lineTo(corners[(e + 1) % 6][0], corners[(e + 1) % 6][1]);
      g.stroke();
    }

    // outer rim
    g.strokeStyle = opts.selected ? "#f4a261" : "rgba(0,0,0,0.45)";
    g.lineWidth = opts.selected ? 2.5 : 1.5;
    g.beginPath();
    g.moveTo(corners[0][0], corners[0][1]);
    for (let i = 1; i < 6; i++) g.lineTo(corners[i][0], corners[i][1]);
    g.closePath();
    g.stroke();

    if (opts.ghost) {
      g.fillStyle = "rgba(254, 250, 224, 0.08)";
      g.beginPath();
      g.moveTo(corners[0][0], corners[0][1]);
      for (let i = 1; i < 6; i++) g.lineTo(corners[i][0], corners[i][1]);
      g.closePath();
      g.fill();
    }

    if (opts.fox) {
      g.font = `${Math.floor(size * 0.7)}px serif`;
      g.textAlign = "center";
      g.textBaseline = "middle";
      g.fillText("🦊", cx, cy + 1);
    }
  }

  function renderHand() {
    handEl.innerHTML = "";
    hand.forEach((tile, i) => {
      const card = document.createElement("div");
      card.className = "tile-card" + (i === selectedHand ? " selected" : "");
      card.setAttribute("role", "option");
      card.setAttribute("aria-selected", i === selectedHand ? "true" : "false");
      card.tabIndex = 0;

      const c = document.createElement("canvas");
      c.width = 120;
      c.height = 120;
      const g = c.getContext("2d");
      g.clearRect(0, 0, 120, 120);
      paintHexTile(g, 60, 60, 48, tile.edges, { selected: i === selectedHand });

      const badge = document.createElement("span");
      badge.className = "badge";
      badge.textContent = i === selectedHand ? "▸" : String(i + 1);

      card.appendChild(c);
      card.appendChild(badge);
      card.addEventListener("click", () => {
        selectedHand = i;
        handHintEl.textContent = hintForSelection();
        renderHand();
        draw();
      });
      handEl.appendChild(card);
    });

    if (hand.length === 0) {
      const empty = document.createElement("p");
      empty.style.color = "var(--muted)";
      empty.style.fontSize = "0.85rem";
      empty.textContent = deck.length ? "Drawing…" : "Deck empty";
      handEl.appendChild(empty);
    }
  }

  function hintForSelection() {
    const t = hand[selectedHand];
    if (!t) return "No tiles left — run complete.";
    return "Selected tile ready · R / ↻ to rotate · click a glowing hex to place";
  }

  // ─── map rendering ──────────────────────────────────────
  function resizeCanvas() {
    const rect = stageEl.getBoundingClientRect();
    const dpr = Math.min(window.devicePixelRatio || 1, 2);
    canvas.width = Math.max(1, Math.round(rect.width * dpr));
    canvas.height = Math.max(1, Math.round(rect.height * dpr));
    canvas.style.width = `${rect.width}px`;
    canvas.style.height = `${rect.height}px`;
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
  }

  function screenToWorld(sx, sy) {
    const rect = canvas.getBoundingClientRect();
    const x = (sx - rect.left - rect.width / 2) / camZoom - camX;
    const y = (sy - rect.top - rect.height / 2) / camZoom - camY;
    return { x, y };
  }

  function draw() {
    const rect = canvas.getBoundingClientRect();
    const w = rect.width;
    const h = rect.height;
    ctx.clearRect(0, 0, w, h);

    // soft vignette bg already in CSS; draw grid dots
    ctx.save();
    ctx.translate(w / 2, h / 2);
    ctx.scale(camZoom, camZoom);
    ctx.translate(camX, camY);

    // valid placements glow
    const empties = running ? getEmptyAdjacent() : [];
    const selected = hand[selectedHand];

    for (const cell of empties) {
      const { x, y } = hexToPixel(cell.q, cell.r);
      const isHover =
        hoverHex && hoverHex.q === cell.q && hoverHex.r === cell.r;

      if (selected && isHover) {
        paintHexTile(ctx, x, y, HEX_SIZE * 0.95, selected.edges, { ghost: true, selected: true });
        // match feedback ring
        const ev = evaluatePlacement(cell.q, cell.r, selected.edges);
        const ok = ev.contacts > 0 && ev.matches === ev.contacts;
        ctx.strokeStyle = ok ? "rgba(244, 162, 97, 0.9)" : "rgba(149, 213, 178, 0.7)";
        ctx.lineWidth = 2.5;
        const corners = hexCorners(x, y, HEX_SIZE);
        ctx.beginPath();
        ctx.moveTo(corners[0][0], corners[0][1]);
        for (let i = 1; i < 6; i++) ctx.lineTo(corners[i][0], corners[i][1]);
        ctx.closePath();
        ctx.stroke();

        // score preview
        const pts =
          ev.matches * 12 +
          (ev.matches === ev.contacts && ev.contacts > 0 ? 20 : 0) +
          2;
        ctx.fillStyle = "#fefae0";
        ctx.font = "bold 14px Segoe UI, sans-serif";
        ctx.textAlign = "center";
        ctx.fillText(`+${pts}`, x, y - HEX_SIZE - 6);
        if (ev.contacts) {
          ctx.font = "11px Segoe UI, sans-serif";
          ctx.fillStyle = "#8fb39a";
          ctx.fillText(`${ev.matches}/${ev.contacts} match`, x, y - HEX_SIZE + 10);
        }
      } else {
        // empty slot marker
        const corners = hexCorners(x, y, HEX_SIZE * 0.92);
        ctx.fillStyle = isHover
          ? "rgba(64, 145, 108, 0.28)"
          : "rgba(64, 145, 108, 0.12)";
        ctx.strokeStyle = isHover
          ? "rgba(149, 213, 178, 0.8)"
          : "rgba(64, 145, 108, 0.45)";
        ctx.lineWidth = 1.5;
        ctx.setLineDash([4, 4]);
        ctx.beginPath();
        ctx.moveTo(corners[0][0], corners[0][1]);
        for (let i = 1; i < 6; i++) ctx.lineTo(corners[i][0], corners[i][1]);
        ctx.closePath();
        ctx.fill();
        ctx.stroke();
        ctx.setLineDash([]);
      }
    }

    // placed tiles
    for (const tile of map.values()) {
      const { x, y } = hexToPixel(tile.q, tile.r);
      let size = HEX_SIZE;
      if (lastPlace && lastPlace.q === tile.q && lastPlace.r === tile.r) {
        const age = (performance.now() - lastPlace.t) / 350;
        if (age < 1) size = HEX_SIZE * (0.85 + 0.15 * Math.min(1, age));
      }
      const isOrigin = tile.q === 0 && tile.r === 0;
      paintHexTile(ctx, x, y, size, tile.edges, { fox: isOrigin });
    }

    ctx.restore();
  }

  function loop(ts) {
    animT = ts;
    draw();
    raf = requestAnimationFrame(loop);
  }

  // ─── run lifecycle ──────────────────────────────────────
  function updateHUD() {
    scoreEl.textContent = String(score);
    deckEl.textContent = String(deck.length);
    placedEl.textContent = String(placed);
  }

  function updateBestUI() {
    bestTitleEl.textContent = `Best: ${best}`;
    bestEndEl.textContent = `Best: ${best}`;
  }

  function medalFor(s) {
    if (s >= 400) return "🏆 Golden Fox — legendary landscaper";
    if (s >= 280) return "🥇 Oak Crown — canopy architect";
    if (s >= 180) return "🥈 Pine Badge — skilled builder";
    if (s >= 100) return "🥉 Birch Leaf — solid den";
    if (s >= 50) return "🍃 Sapling Grove — nice start";
    return "Keep growing the island…";
  }

  function startRun() {
    map = new Map();
    deck = buildDeck();
    hand = [];
    selectedHand = 0;
    score = 0;
    placed = 0;
    matchPoints = 0;
    questPoints = 0;
    breakdown = { matches: 0, perfects: 0, quests: 0, tiles: 0 };
    quests = makeQuests();
    running = true;
    hoverHex = null;
    lastPlace = null;

    // starter tile — cozy forest hub
    const hub = makeTile(["F", "F", "M", "M", "W", "F"]);
    map.set(key(0, 0), { q: 0, r: 0, edges: hub.edges, id: hub.id });
    placed = 1;

    camX = 0;
    camY = 0;
    camZoom = 1;

    drawToHand();
    updateHUD();
    renderQuests();
    titleScreen.classList.add("hidden");
    endScreen.classList.add("hidden");
    handHintEl.textContent = hintForSelection();
    draw();
  }

  function endRun(natural) {
    if (!running) return;
    running = false;

    // leftover quests don't auto-complete
    if (score > best) {
      best = score;
      localStorage.setItem(STORAGE_KEY, String(best));
    }
    updateBestUI();

    endTitleEl.textContent = natural ? "Canopy complete" : "Run ended";
    finalScoreEl.textContent = String(score);
    medalEl.textContent = medalFor(score);
    endBreakdownEl.innerHTML = `
      <li><span>Edge matches</span><span>${breakdown.matches}</span></li>
      <li><span>Perfect placements</span><span>${breakdown.perfects}</span></li>
      <li><span>Quests</span><span>${breakdown.quests}</span></li>
      <li><span>Tiles placed</span><span>${breakdown.tiles}</span></li>
    `;
    endScreen.classList.remove("hidden");
  }

  // ─── input ──────────────────────────────────────────────
  function onPointerDown(e) {
    if (!running && e.target === canvas) return;
    dragging = true;
    didDrag = false;
    dragStart = { x: e.clientX, y: e.clientY };
    camStart = { x: camX, y: camY };
    stageEl.classList.add("dragging");
  }

  function onPointerMove(e) {
    if (dragging && dragStart) {
      const dx = e.clientX - dragStart.x;
      const dy = e.clientY - dragStart.y;
      if (Math.hypot(dx, dy) > 4) didDrag = true;
      camX = camStart.x + dx / camZoom;
      camY = camStart.y + dy / camZoom;
    }

    if (running) {
      const world = screenToWorld(e.clientX, e.clientY);
      const hex = pixelToHex(world.x, world.y);
      const valid = getEmptyAdjacent().some((c) => c.q === hex.q && c.r === hex.r);
      hoverHex = valid ? hex : null;
    }
  }

  function onPointerUp(e) {
    stageEl.classList.remove("dragging");
    if (dragging && !didDrag && running) {
      const world = screenToWorld(e.clientX, e.clientY);
      const hex = pixelToHex(world.x, world.y);
      if (isValidPlacement(hex.q, hex.r)) {
        placeTile(hex.q, hex.r);
      }
    }
    dragging = false;
    dragStart = null;
  }

  function onWheel(e) {
    e.preventDefault();
    const factor = e.deltaY > 0 ? 0.9 : 1.1;
    camZoom = Math.min(2.4, Math.max(0.45, camZoom * factor));
  }

  canvas.addEventListener("pointerdown", onPointerDown);
  window.addEventListener("pointermove", onPointerMove);
  window.addEventListener("pointerup", onPointerUp);
  canvas.addEventListener("wheel", onWheel, { passive: false });

  document.getElementById("btn-start").addEventListener("click", startRun);
  document.getElementById("btn-again").addEventListener("click", startRun);
  document.getElementById("btn-new").addEventListener("click", () => {
    if (running && !confirm("Start a new run? Current progress will be lost.")) return;
    startRun();
  });
  document.getElementById("btn-rotate").addEventListener("click", () => rotateHandTile(1));
  document.getElementById("btn-cycle").addEventListener("click", () => {
    if (hand.length < 2) return;
    selectedHand = (selectedHand + 1) % hand.length;
    handHintEl.textContent = hintForSelection();
    renderHand();
    draw();
  });

  window.addEventListener("keydown", (e) => {
    if (e.target.matches("input, textarea")) return;
    if (e.code === "KeyR") {
      e.preventDefault();
      rotateHandTile(1);
    } else if (e.code === "KeyQ") {
      e.preventDefault();
      rotateHandTile(-1);
    } else if (e.code === "Digit1" || e.code === "Digit2" || e.code === "Digit3") {
      const i = Number(e.code.slice(-1)) - 1;
      if (i < hand.length) {
        selectedHand = i;
        handHintEl.textContent = hintForSelection();
        renderHand();
        draw();
      }
    } else if (e.code === "Tab") {
      e.preventDefault();
      if (hand.length) {
        selectedHand = (selectedHand + (e.shiftKey ? hand.length - 1 : 1)) % hand.length;
        handHintEl.textContent = hintForSelection();
        renderHand();
        draw();
      }
    }
  });

  window.addEventListener("resize", () => {
    resizeCanvas();
    draw();
  });

  // ─── boot ───────────────────────────────────────────────
  updateBestUI();
  resizeCanvas();
  raf = requestAnimationFrame(loop);
})();
