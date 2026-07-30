using System.Text;
using FlyingFox.Core;
using UnityEngine;

namespace FlyingFox.Presentation
{
    /// <summary>
    /// Temporary OnGUI HUD until UI Toolkit (PR-09). Full Classic run + end screen.
    /// Exposes <see cref="IsPointerOverHud"/> so map input ignores UI clicks.
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
        GUIStyle _panel;
        GUIStyle _questDone;
        bool _styles;
        Rect _panelRect;

        public void Bind(RunController run, int seed = 0)
        {
            _run = run;
            _seed = seed;
            _cachedEnd = null;
        }

        void EnsureStyles()
        {
            if (_styles) return;
            _styles = true;

            _title = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.96f, 0.64f, 0.38f) },
            };
            _body = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                normal = { textColor = new Color(0.99f, 0.98f, 0.88f) },
                wordWrap = true,
            };
            _muted = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.56f, 0.7f, 0.6f) },
                wordWrap = true,
            };
            _btn = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                fixedHeight = 32,
            };
            _btnPrimary = new GUIStyle(_btn)
            {
                fontSize = 15,
                fixedHeight = 40,
            };
            _panel = new GUIStyle(GUI.skin.box);
            _questDone = new GUIStyle(_body)
            {
                normal = { textColor = new Color(0.96f, 0.64f, 0.38f) },
            };
        }

        void OnGUI()
        {
            if (_run == null)
            {
                IsPointerOverHud = false;
                return;
            }

            EnsureStyles();

            const float pad = 10f;
            const float width = 340f;
            float height = Screen.height - pad * 2f;
            _panelRect = new Rect(pad, pad, width, height);

            // Soft panel background
            Color prev = GUI.color;
            GUI.color = new Color(0.05f, 0.1f, 0.07f, 0.88f);
            GUI.Box(_panelRect, GUIContent.none);
            GUI.color = prev;

            GUILayout.BeginArea(new Rect(pad + 8f, pad + 8f, width - 16f, height - 16f));

            GUILayout.Label("🦊 Flying Fox", _title);
            GUILayout.Label("Classic canopy run", _muted);
            if (_seed != 0)
                GUILayout.Label($"Seed {_seed}", _muted);

            GUILayout.Space(6);
            DrawStat("Score", _run.Score.ToString());
            DrawStat("Deck", _run.Deck.Count.ToString());
            DrawStat("Placed", _run.PlacedHandTiles.ToString());
            DrawStat("Board", $"{_run.Board.Count} tiles");

            GUILayout.Space(10);

            if (_run.Phase == RunPhase.Playing)
                DrawPlaying();
            else if (_run.Phase == RunPhase.Ended)
                DrawEnded();

            GUILayout.EndArea();

            // IMGUI mouse is top-left origin
            Vector2 guiMouse = Event.current != null
                ? Event.current.mousePosition
                : new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            IsPointerOverHud = _panelRect.Contains(guiMouse);
        }

        void DrawStat(string label, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, _muted, GUILayout.Width(70));
            GUILayout.Label(value, _body);
            GUILayout.EndHorizontal();
        }

        void DrawPlaying()
        {
            GUILayout.Label("Hand", _title);
            GUILayout.Label("1–3 select · R/Q rotate · Tab cycle", _muted);

            GUILayout.BeginHorizontal();
            for (int i = 0; i < _run.Hand.Count; i++)
            {
                var t = _run.Hand[i];
                bool sel = i == _run.SelectedHandIndex;
                string label = sel
                    ? $"▸ {EdgesShort(t.Edges)}"
                    : $"  {EdgesShort(t.Edges)}";
                var style = sel ? _btnPrimary : _btn;
                if (GUILayout.Button(label, style, GUILayout.Height(sel ? 44 : 36)))
                    _run.SelectHand(i);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("↻ Rotate", _btn)) _run.RotateSelected(1);
            if (GUILayout.Button("⇄ Cycle", _btn)) _run.CycleHand(1);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("New run", _btn))
                GameSession.Instance?.StartClassicRun();
            if (GUILayout.Button("End run", _btn))
                _run.Abandon();
            GUILayout.EndHorizontal();

            GUILayout.Space(12);
            GUILayout.Label("Quests", _title);
            foreach (var q in _run.Quests)
            {
                var style = q.Done ? _questDone : _body;
                string mark = q.Done ? "✓" : "○";
                GUILayout.Label($"{mark} {q.Def.Title}", style);
                GUILayout.Label($"   {q.Progress}/{q.Def.Target}  ·  +{q.Def.Reward} pts", _muted);

                // Simple progress bar
                Rect r = GUILayoutUtility.GetRect(18, 8);
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
                "LMB place · RMB pan · Scroll zoom\nEsc abandon · Ctrl+N new run",
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
