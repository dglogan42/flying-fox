using System.Text;
using FlyingFox.Core;
using UnityEngine;

namespace FlyingFox.Presentation
{
    /// <summary>
    /// Temporary OnGUI HUD until UI Toolkit (PR-09). Scales for docked vs handheld.
    /// </summary>
    public sealed class GameplayHudImgui : MonoBehaviour
    {
        public static bool IsPointerOverHud { get; private set; }

        RunController _run;
        int _seed;
        RunResult _cachedEnd;
        GUIStyle _title;
        GUIStyle _body;
        GUIStyle _muted;
        GUIStyle _btn;
        GUIStyle _btnPrimary;
        GUIStyle _questDone;
        float _styleScale = -1f;
        Rect _panelRect;

        public void Bind(RunController run, int seed = 0)
        {
            _run = run;
            _seed = seed;
            _cachedEnd = null;
        }

        void EnsureStyles(float uiScale)
        {
            if (_styleScale > 0f && Mathf.Abs(_styleScale - uiScale) < 0.01f && _title != null)
                return;
            _styleScale = uiScale;

            int T(int v) => Mathf.Max(10, Mathf.RoundToInt(v * uiScale));

            _title = new GUIStyle(GUI.skin.label)
            {
                fontSize = T(22),
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.96f, 0.64f, 0.38f) },
            };
            _body = new GUIStyle(GUI.skin.label)
            {
                fontSize = T(14),
                normal = { textColor = new Color(0.99f, 0.98f, 0.88f) },
                wordWrap = true,
            };
            _muted = new GUIStyle(GUI.skin.label)
            {
                fontSize = T(12),
                normal = { textColor = new Color(0.56f, 0.7f, 0.6f) },
                wordWrap = true,
            };
            _btn = new GUIStyle(GUI.skin.button)
            {
                fontSize = T(13),
                fontStyle = FontStyle.Bold,
                fixedHeight = T(32),
            };
            _btnPrimary = new GUIStyle(_btn)
            {
                fontSize = T(15),
                fixedHeight = T(40),
            };
            _questDone = new GUIStyle(_body)
            {
                normal = { textColor = new Color(0.96f, 0.64f, 0.38f) },
            };
        }

        void OnGUI()
        {
            if (_run == null || PauseMenuController.IsPaused)
            {
                IsPointerOverHud = false;
                return;
            }

            float ui = DisplayModeService.Instance != null
                ? DisplayModeService.Instance.UiScale
                : 1f;
            EnsureStyles(ui);

            Rect safe = DisplayModeService.Instance != null
                ? DisplayModeService.Instance.SafeGuiRect
                : new Rect(0, 0, Screen.width, Screen.height);

            float pad = 10f * ui;
            float width = Mathf.Clamp(340f * ui, 260f, safe.width * 0.42f);
            // Handheld: slightly wider fraction so text fits
            if (DisplayModeService.Instance != null &&
                DisplayModeService.Instance.FormFactor == DisplayFormFactor.Handheld)
                width = Mathf.Clamp(300f * ui, 240f, safe.width * 0.48f);

            float height = safe.height - pad * 2f;
            _panelRect = new Rect(safe.x + pad, safe.y + pad, width, height);

            Color prev = GUI.color;
            GUI.color = new Color(0.05f, 0.1f, 0.07f, 0.88f);
            GUI.Box(_panelRect, GUIContent.none);
            GUI.color = prev;

            GUILayout.BeginArea(new Rect(
                _panelRect.x + 8f * ui,
                _panelRect.y + 8f * ui,
                _panelRect.width - 16f * ui,
                _panelRect.height - 16f * ui));

            GUILayout.Label("🦊 Flying Fox", _title);
            string mode = "Classic canopy run";
            if (DisplayModeService.Instance != null)
                mode += $" · {DisplayModeService.Instance.FormFactor}";
            GUILayout.Label(mode, _muted);
            if (_seed != 0)
                GUILayout.Label($"Seed {_seed}", _muted);

            GUILayout.Space(6 * ui);
            DrawStat("Score", _run.Score.ToString());
            DrawStat("Deck", _run.Deck.Count.ToString());
            DrawStat("Placed", _run.PlacedHandTiles.ToString());
            DrawStat("Board", $"{_run.Board.Count} tiles");
            if (_run.AnchorArmed)
                GUILayout.Label("⚓ Anchor ARMED", _title);

            GUILayout.Space(6 * ui);
            GUILayout.Label("Fox abilities", _title);
            GUILayout.Label("🌲 Canopy Leap  +6 / Forest match", _muted);
            GUILayout.Label("☀️ Sunbeam  +15 perfect + Meadow", _muted);
            GUILayout.Label("💧 Eddy  Water match → draw +1", _muted);
            GUILayout.Label("🪨 Anchor  +10 · soft perfect next", _muted);
            if (_run.LastAbilityProcs != null && _run.LastAbilityProcs.Count > 0)
            {
                foreach (var p in _run.LastAbilityProcs)
                    GUILayout.Label($"→ {p.Name}: {p.Detail}", _body);
            }

            GUILayout.Space(10 * ui);

            if (_run.Phase == RunPhase.Playing)
                DrawPlaying(ui);
            else if (_run.Phase == RunPhase.Ended)
                DrawEnded();

            GUILayout.EndArea();

            Vector2 guiMouse = Event.current != null
                ? Event.current.mousePosition
                : new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            IsPointerOverHud = _panelRect.Contains(guiMouse);
        }

        void DrawStat(string label, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, _muted, GUILayout.Width(70 * (_styleScale > 0 ? _styleScale : 1f)));
            GUILayout.Label(value, _body);
            GUILayout.EndHorizontal();
        }

        void DrawPlaying(float ui)
        {
            GUILayout.Label("Hand", _title);
            GUILayout.Label("1–3 select · R/Q rotate · Tab cycle · + pause", _muted);

            GUILayout.BeginHorizontal();
            for (int i = 0; i < _run.Hand.Count; i++)
            {
                var t = _run.Hand[i];
                bool sel = i == _run.SelectedHandIndex;
                string label = sel ? $"▸ {EdgesShort(t.Edges)}" : $"  {EdgesShort(t.Edges)}";
                var style = sel ? _btnPrimary : _btn;
                if (GUILayout.Button(label, style, GUILayout.Height(sel ? 44 * ui : 36 * ui)))
                    _run.SelectHand(i);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("↻ Rotate", _btn)) _run.RotateSelected(1);
            if (GUILayout.Button("⇄ Cycle", _btn)) _run.CycleHand(1);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Pause", _btn))
                PauseMenuController.Instance?.Pause();
            if (GUILayout.Button("New run", _btn))
                GameSession.Instance?.StartClassicRun();
            if (GUILayout.Button("End run", _btn))
                _run.Abandon();
            GUILayout.EndHorizontal();

            GUILayout.Space(12 * ui);
            GUILayout.Label("Quests", _title);
            foreach (var q in _run.Quests)
            {
                var style = q.Done ? _questDone : _body;
                string mark = q.Done ? "✓" : "○";
                GUILayout.Label($"{mark} {q.Def.Title}", style);
                GUILayout.Label($"   {q.Progress}/{q.Def.Target}  ·  +{q.Def.Reward} pts", _muted);

                Rect r = GUILayoutUtility.GetRect(18, 8 * ui);
                r.xMin += 12;
                Color c = GUI.color;
                GUI.color = new Color(0.05f, 0.08f, 0.06f, 1f);
                GUI.DrawTexture(r, Texture2D.whiteTexture);
                float pct = q.Def.Target <= 0 ? 0f : Mathf.Clamp01(q.Progress / (float)q.Def.Target);
                var fill = r;
                fill.width *= pct;
                GUI.color = q.Done
                    ? new Color(0.96f, 0.64f, 0.38f, 1f)
                    : new Color(0.25f, 0.56f, 0.42f, 1f);
                if (fill.width > 0.5f)
                    GUI.DrawTexture(fill, Texture2D.whiteTexture);
                GUI.color = c;
                GUILayout.Space(4);
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label(
                "LMB place · gamepad A place · + / Esc pause\n" +
                "Dock/undock rescales UI automatically",
                _muted);
        }

        void DrawEnded()
        {
            _cachedEnd ??= _run.BuildResult();
            var r = _cachedEnd;

            GUILayout.Label(r.NaturalEnd ? "Canopy complete" : "Run ended", _title);
            GUILayout.Space(4);
            GUILayout.Label($"Score  {r.Score}", _body);
            GUILayout.Label($"Medal  {r.Medal}", _title);
            GUILayout.Space(6);
            GUILayout.Label("Breakdown", _muted);
            GUILayout.Label($"  Edge matches     {r.Breakdown.Matches}", _body);
            GUILayout.Label($"  Perfect bonuses  {r.Breakdown.Perfects}", _body);
            GUILayout.Label($"  Fox abilities    {r.Breakdown.Abilities}", _body);
            GUILayout.Label($"  Tiles placed     {r.Breakdown.Tiles}", _body);
            GUILayout.Label($"  Quests           {r.Breakdown.Quests}", _body);
            GUILayout.Label($"  Perfects count   {r.PerfectCount}", _muted);
            GUILayout.Label($"  Match edges      {r.MatchEdgeCount}", _muted);
            GUILayout.Label($"  Quests done      {r.QuestsCompleted}/5", _muted);

            GUILayout.Space(16);
            if (GUILayout.Button("Play again", _btnPrimary))
                GameSession.Instance?.StartClassicRun();
            if (GUILayout.Button("Same seed", _btn))
                GameSession.Instance?.StartClassicRun(_seed);
        }

        static string EdgesShort(BiomeId[] e)
        {
            var sb = new StringBuilder(6);
            foreach (var b in e) sb.Append(BiomeCodec.ToChar(b));
            return sb.ToString();
        }

        void OnDisable() => IsPointerOverHud = false;
    }
}
