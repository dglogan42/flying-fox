using FlyingFox.Core;
using UnityEngine;

namespace FlyingFox.Presentation
{
    /// <summary>Single hex visual: wedge mesh + optional fox marker + outline.</summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class HexTileView : MonoBehaviour
    {
        [SerializeField] float _size = HexMeshUtil.DefaultSize;
        [SerializeField] bool _showFox;
        [SerializeField] float _foxScale = 0.45f;

        MeshFilter _mf;
        MeshRenderer _mr;
        LineRenderer _outline;
        TextMesh _foxLabel;
        Mesh _ownedMesh;
        BiomeId[] _edges;

        static Material _sharedMat;
        static Material _lineMat;

        public HexCoord Coord { get; private set; }
        public float Size => _size;

        public static Material SharedVertexColorMaterial
        {
            get
            {
                if (_sharedMat == null)
                {
                    var shader = Shader.Find("Sprites/Default")
                                 ?? Shader.Find("Unlit/Color")
                                 ?? Shader.Find("Universal Render Pipeline/Unlit")
                                 ?? Shader.Find("UI/Default");
                    _sharedMat = new Material(shader) { name = "FlyingFox.HexVertex" };
                    if (_sharedMat.HasProperty("_Color"))
                        _sharedMat.color = Color.white;
                }
                return _sharedMat;
            }
        }

        public static Material SharedLineMaterial
        {
            get
            {
                if (_lineMat == null)
                {
                    var shader = Shader.Find("Sprites/Default")
                                 ?? Shader.Find("Unlit/Color")
                                 ?? Shader.Find("UI/Default");
                    _lineMat = new Material(shader) { name = "FlyingFox.HexLine" };
                }
                return _lineMat;
            }
        }

        void Awake()
        {
            _mf = GetComponent<MeshFilter>();
            _mr = GetComponent<MeshRenderer>();
            _mr.sharedMaterial = SharedVertexColorMaterial;
            EnsureOutline();
        }

        void OnDestroy()
        {
            if (_ownedMesh != null)
            {
                if (Application.isPlaying) Destroy(_ownedMesh);
                else DestroyImmediate(_ownedMesh);
            }
        }

        public void Setup(HexCoord coord, BiomeId[] edges, float size, bool showFox = false, float alpha = 1f)
        {
            Coord = coord;
            _size = size;
            _showFox = showFox;
            transform.localPosition = HexMeshUtil.ToWorld(coord, size);
            ApplyEdges(edges, alpha);
            SetFoxVisible(showFox);
        }

        public void ApplyEdges(BiomeId[] edges, float alpha = 1f)
        {
            _edges = edges;
            if (_ownedMesh != null)
            {
                if (Application.isPlaying) Destroy(_ownedMesh);
                else DestroyImmediate(_ownedMesh);
            }

            _ownedMesh = HexMeshUtil.BuildWedgeMesh(edges, _size);
            if (alpha < 0.999f)
            {
                var cols = _ownedMesh.colors;
                for (int i = 0; i < cols.Length; i++)
                {
                    var c = cols[i];
                    c.a *= alpha;
                    cols[i] = c;
                }
                _ownedMesh.colors = cols;
            }

            if (_mf == null) _mf = GetComponent<MeshFilter>();
            if (_mr == null) _mr = GetComponent<MeshRenderer>();
            _mf.sharedMesh = _ownedMesh;
            _mr.sharedMaterial = SharedVertexColorMaterial;
            EnsureOutline();
            HexMeshUtil.SetOutlinePositions(_outline, _size * 0.98f);
            _outline.startColor = _outline.endColor = new Color(0f, 0f, 0f, 0.45f * alpha);
        }

        public void SetRing(Color color, float width = 0.06f)
        {
            EnsureOutline();
            _outline.enabled = true;
            _outline.startWidth = _outline.endWidth = width;
            _outline.startColor = _outline.endColor = color;
        }

        public void ClearRing()
        {
            if (_outline == null) return;
            _outline.startColor = _outline.endColor = new Color(0f, 0f, 0f, 0.45f);
            _outline.startWidth = _outline.endWidth = 0.03f;
        }

        void EnsureOutline()
        {
            if (_outline != null) return;
            var go = new GameObject("Outline");
            go.transform.SetParent(transform, false);
            _outline = go.AddComponent<LineRenderer>();
            _outline.sharedMaterial = SharedLineMaterial;
            _outline.textureMode = LineTextureMode.Stretch;
            _outline.numCapVertices = 2;
            _outline.sortingOrder = 2;
            _outline.startWidth = _outline.endWidth = 0.03f;
        }

        void SetFoxVisible(bool on)
        {
            if (!on)
            {
                if (_foxLabel != null) _foxLabel.gameObject.SetActive(false);
                return;
            }

            if (_foxLabel == null)
            {
                var go = new GameObject("Fox");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(0f, 0f, -0.05f);
                go.transform.localScale = Vector3.one * _foxScale;
                _foxLabel = go.AddComponent<TextMesh>();
                _foxLabel.text = "FOX";
                _foxLabel.anchor = TextAnchor.MiddleCenter;
                _foxLabel.alignment = TextAlignment.Center;
                _foxLabel.fontSize = 32;
                _foxLabel.characterSize = 0.08f;
                _foxLabel.color = new Color(0.96f, 0.64f, 0.38f, 1f);
                // Prefer emoji if font supports it
                _foxLabel.text = "🦊";
            }
            _foxLabel.gameObject.SetActive(true);
        }

        public static HexTileView Create(Transform parent, string name = "HexTile")
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>();
            return go.AddComponent<HexTileView>();
        }
    }
}
