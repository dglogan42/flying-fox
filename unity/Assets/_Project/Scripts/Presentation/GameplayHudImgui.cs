using System.Text;
using FlyingFox.Core;
using UnityEngine;

namespace FlyingFox.Presentation
{
    /// <summary>
    /// Temporary OnGUI HUD until UI Toolkit (PR-09). Enough to play a full Classic run.
    /// </summary>
    public sealed class GameplayHudImgui : MonoBehaviour
    {
        RunController _run;
        GUIStyle _title;
        GUIStyle _body;
        GUIStyle _btn;
        bool _styles;

        public void Bind(RunController run) => _run = run;

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
            _btn = new GUIStyle(GUI.skin.button) { fontSize = 14, fontStyle = FontStyle.Bold };
        }

        void OnGUI()
        {
            if (_run == null) return;
            EnsureStyles();

            float pad = 12f;
            GUILayout.BeginArea(new Rect(pad, pad, 320f, Screen.height - pad * 2));
            GUILayout.Label("Flying Fox", _title);
            GUILayout.Label($"Score  {_run.Score}", _body);
            GUILayout.Label($"Deck   {_run.Deck.Count}   ·   Placed {_run.PlacedHandTiles}", _body);
            GUILayout.Label($"Board  {_run.Board.Count} tiles", _body);
            GUILayout.Space(8);

            if (_run.Phase == RunPhase.Playing)
            {
                GUILayout.Label("Hand (click / 1–3 · R rotate · Tab cycle)", _body);
                GUILayout.BeginHorizontal();
                for (int i = 0; i < _run.Hand.Count; i++)
                {
                    var t = _run.Hand[i];
                    string label = i == _run.SelectedHandIndex
                        ? $"> {EdgesShort(t.Edges)} <"
                        : $"  {EdgesShort(t.Edges)}  ";
                    if (GUILayout.Button(label, _btn, GUILayout.Height(36)))
                        _run.SelectHand(i);
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("↻ Rotate", _btn)) _run.RotateSelected(1);
                if (GUILayout.Button("⇄ Cycle", _btn)) _run.CycleHand(1);
                if (GUILayout.Button("New run", _btn)) RequestRestart();
                GUILayout.EndHorizontal();

                GUILayout.Space(10);
                GUILayout.Label("Quests", _title);
                foreach (var q in _run.Quests)
                {
                    string mark = q.Done ? "✓" : "·";
                    GUILayout.Label(
                        $"{mark} {q.Def.Title}  {q.Progress}/{q.Def.Target}  (+{q.Def.Reward})",
                        _body);
                }

                GUILayout.Space(10);
                GUILayout.Label(
                    "LMB place · RMB pan · Scroll zoom · R/Q rotate",
                    _body);
            }
            else if (_run.Phase == RunPhase.Ended)
            {
                var r = _run.BuildResult();
                GUILayout.Label(r.NaturalEnd ? "Canopy complete" : "Run ended", _title);
                GUILayout.Label($"Final score: {r.Score}", _body);
                GUILayout.Label($"Medal: {r.Medal}", _body);
                GUILayout.Label(
                    $"Matches {r.Breakdown.Matches} · Perfects {r.Breakdown.Perfects}\n" +
                    $"Tiles {r.Breakdown.Tiles} · Quests {r.Breakdown.Quests}",
                    _body);
                if (GUILayout.Button("Play again", _btn, GUILayout.Height(40)))
                    RequestRestart();
            }

            GUILayout.EndArea();
        }

        static string EdgesShort(BiomeId[] e)
        {
            var sb = new StringBuilder(6);
            foreach (var b in e) sb.Append(BiomeCodec.ToChar(b));
            return sb.ToString();
        }

        void RequestRestart() => GameSession.Instance?.StartClassicRun();
    }
}
