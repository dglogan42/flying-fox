using FlyingFox.Core;
using UnityEngine;

namespace FlyingFox.Presentation
{
    /// <summary>
    /// Hover ghost for the selected hand tile: translucent wedges, match ring, score pop.
    /// </summary>
    public sealed class GhostPlacementView : MonoBehaviour
    {
        [SerializeField] float _hexSize = HexMeshUtil.DefaultSize;
        [SerializeField] float _bobAmplitude = 0.04f;
        [SerializeField] float _bobSpeed = 3.2f;

        HexTileView _ghost;
        TextMesh _scoreLabel;
        TextMesh _subLabel;
        HexCoord _lastCoord;
        string _lastEdgeKey;
        bool _visible;
        Vector3 _baseGhostPos;

        public bool IsVisible => _visible;

        public void Ensure()
        {
            if (_ghost != null) return;

            _ghost = HexTileView.Create(transform, "GhostTile");
            _ghost.gameObject.SetActive(false);

            _scoreLabel = CreateLabel("ScorePreview", 48, 0.065f, Color.white);
            _subLabel = CreateLabel("MatchPreview", 36, 0.045f, new Color(0.7f, 0.85f, 0.75f));
            _subLabel.gameObject.SetActive(false);
        }

        static TextMesh CreateLabel(string name, int fontSize, float charSize, Color color)
        {
            var go = new GameObject(name);
            var tm = go.AddComponent<TextMesh>();
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontSize = fontSize;
            tm.characterSize = charSize;
            tm.color = color;
            tm.fontStyle = FontStyle.Bold;
            // Keep text readable against map
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = 20;
            go.SetActive(false);
            return tm;
        }

        public void SetHexSize(float size) => _hexSize = size;

        public void Hide()
        {
            Ensure();
            _visible = false;
            _lastEdgeKey = null;
            _ghost.gameObject.SetActive(false);
            _scoreLabel.gameObject.SetActive(false);
            _subLabel.gameObject.SetActive(false);
        }

        public void Show(HexCoord at, BiomeId[] edges, PlacementEval eval, GameBalance bal)
        {
            Ensure();
            _visible = true;

            string edgeKey = EdgeKey(edges);
            bool same = _ghost.gameObject.activeSelf && _lastCoord == at && _lastEdgeKey == edgeKey;
            if (!same)
            {
                _ghost.Setup(at, edges, _hexSize, false, 0.62f);
                _lastCoord = at;
                _lastEdgeKey = edgeKey;
            }

            bool perfect = eval.IsPerfect;
            _ghost.SetRing(
                perfect ? BiomePalette.PerfectRing : BiomePalette.OkRing,
                perfect ? 0.1f : 0.07f);
            _ghost.gameObject.SetActive(true);
            _baseGhostPos = HexMeshUtil.ToWorld(at, _hexSize);

            int pts = PlacementService.ScoreFor(eval, bal);
            _scoreLabel.text = $"+{pts}";
            _scoreLabel.color = perfect
                ? BiomePalette.PerfectRing
                : new Color(0.99f, 0.98f, 0.88f);

            if (eval.Contacts > 0)
            {
                _subLabel.text = perfect
                    ? $"{eval.Matches}/{eval.Contacts} perfect"
                    : $"{eval.Matches}/{eval.Contacts} match";
                _subLabel.color = perfect
                    ? BiomePalette.PerfectRing
                    : BiomePalette.OkRing;
                _subLabel.gameObject.SetActive(true);
            }
            else
            {
                _subLabel.text = "no contacts";
                _subLabel.color = new Color(0.7f, 0.7f, 0.7f);
                _subLabel.gameObject.SetActive(true);
            }

            _scoreLabel.gameObject.SetActive(true);
            LayoutLabels();
        }

        void Update()
        {
            if (!_visible || _ghost == null || !_ghost.gameObject.activeSelf) return;

            float bob = Mathf.Sin(Time.unscaledTime * _bobSpeed) * _bobAmplitude;
            _ghost.transform.position = _baseGhostPos + new Vector3(0f, bob, -0.02f);
            LayoutLabels();
        }

        void LayoutLabels()
        {
            var top = _baseGhostPos + new Vector3(0f, _hexSize * 1.25f, -0.12f);
            _scoreLabel.transform.position = top;
            _subLabel.transform.position = top + new Vector3(0f, -_hexSize * 0.35f, 0f);
        }

        static string EdgeKey(BiomeId[] edges)
        {
            if (edges == null || edges.Length != 6) return "";
            char[] c = new char[6];
            for (int i = 0; i < 6; i++) c[i] = BiomeCodec.ToChar(edges[i]);
            return new string(c);
        }
    }
}
