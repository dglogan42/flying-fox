using FlyingFox.Core;
using UnityEngine;

namespace FlyingFox.Presentation
{
    /// <summary>Hover ghost for selected hand tile + score preview label.</summary>
    public sealed class GhostPlacementView : MonoBehaviour
    {
        [SerializeField] float _hexSize = HexMeshUtil.DefaultSize;

        HexTileView _ghost;
        TextMesh _scoreLabel;
        bool _visible;

        public void Ensure()
        {
            if (_ghost != null) return;
            _ghost = HexTileView.Create(transform, "Ghost");
            _ghost.gameObject.SetActive(false);

            var go = new GameObject("ScorePreview");
            go.transform.SetParent(transform, false);
            _scoreLabel = go.AddComponent<TextMesh>();
            _scoreLabel.anchor = TextAnchor.LowerCenter;
            _scoreLabel.alignment = TextAlignment.Center;
            _scoreLabel.fontSize = 48;
            _scoreLabel.characterSize = 0.06f;
            _scoreLabel.color = Color.white;
            _scoreLabel.gameObject.SetActive(false);
        }

        public void SetHexSize(float size) => _hexSize = size;

        public void Hide()
        {
            Ensure();
            _visible = false;
            _ghost.gameObject.SetActive(false);
            _scoreLabel.gameObject.SetActive(false);
        }

        public void Show(HexCoord at, BiomeId[] edges, PlacementEval eval, GameBalance bal)
        {
            Ensure();
            _visible = true;
            _ghost.Setup(at, edges, _hexSize, false, 0.65f);
            bool perfect = eval.IsPerfect;
            _ghost.SetRing(perfect ? BiomePalette.PerfectRing : BiomePalette.OkRing, 0.08f);
            _ghost.gameObject.SetActive(true);

            int pts = PlacementService.ScoreFor(eval, bal);
            _scoreLabel.text = eval.Contacts > 0
                ? $"+{pts}\n{eval.Matches}/{eval.Contacts}"
                : $"+{pts}";
            var world = HexMeshUtil.ToWorld(at, _hexSize);
            _scoreLabel.transform.position = world + new Vector3(0f, _hexSize * 1.15f, -0.1f);
            _scoreLabel.gameObject.SetActive(true);
        }
    }
}
