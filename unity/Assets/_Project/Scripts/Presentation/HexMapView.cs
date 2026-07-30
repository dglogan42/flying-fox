using System.Collections.Generic;
using FlyingFox.Core;
using UnityEngine;

namespace FlyingFox.Presentation
{
    /// <summary>Renders placed tiles and empty-adjacent slot markers.</summary>
    public sealed class HexMapView : MonoBehaviour
    {
        [SerializeField] float _hexSize = HexMeshUtil.DefaultSize;
        [SerializeField] Transform _tilesRoot;
        [SerializeField] Transform _slotsRoot;

        readonly Dictionary<string, HexTileView> _tiles = new Dictionary<string, HexTileView>();
        readonly List<HexTileView> _slotPool = new List<HexTileView>();
        readonly List<HexTileView> _activeSlots = new List<HexTileView>();

        public float HexSize => _hexSize;

        public void EnsureRoots()
        {
            if (_tilesRoot == null)
            {
                var t = new GameObject("Tiles");
                t.transform.SetParent(transform, false);
                _tilesRoot = t.transform;
            }
            if (_slotsRoot == null)
            {
                var s = new GameObject("Slots");
                s.transform.SetParent(transform, false);
                _slotsRoot = s.transform;
            }
        }

        public void SetHexSize(float size) => _hexSize = size;

        public void Rebuild(BoardModel board)
        {
            EnsureRoots();
            var live = new HashSet<string>();
            foreach (var pt in board.All)
            {
                live.Add(pt.Coord.Key);
                if (!_tiles.TryGetValue(pt.Coord.Key, out var view))
                {
                    view = HexTileView.Create(_tilesRoot, $"Tile_{pt.Coord.Key}");
                    _tiles[pt.Coord.Key] = view;
                }
                bool fox = pt.Coord == HexCoord.Origin;
                view.Setup(pt.Coord, pt.Edges, _hexSize, fox);
                view.gameObject.SetActive(true);
            }

            // Remove missing
            var remove = new List<string>();
            foreach (var kv in _tiles)
            {
                if (!live.Contains(kv.Key))
                {
                    if (Application.isPlaying) Destroy(kv.Value.gameObject);
                    else DestroyImmediate(kv.Value.gameObject);
                    remove.Add(kv.Key);
                }
            }
            foreach (var k in remove) _tiles.Remove(k);

            RefreshSlots(board);
        }

        public void RefreshSlots(BoardModel board, HexCoord? hover = null)
        {
            EnsureRoots();
            foreach (var s in _activeSlots)
                s.gameObject.SetActive(false);
            _activeSlots.Clear();

            var empties = board.GetEmptyAdjacent();
            int i = 0;
            foreach (var c in empties)
            {
                var slot = GetSlot(i++);
                // Empty slot: dashed feel via dark wedges
                var edges = new[]
                {
                    BiomeId.Forest, BiomeId.Forest, BiomeId.Forest,
                    BiomeId.Forest, BiomeId.Forest, BiomeId.Forest,
                };
                slot.Setup(c, edges, _hexSize * 0.92f, false, 0.2f);
                bool isHover = hover.HasValue && hover.Value == c;
                slot.SetRing(isHover ? BiomePalette.EmptySlotHover : BiomePalette.EmptySlot,
                    isHover ? 0.07f : 0.04f);
                slot.gameObject.SetActive(true);
                _activeSlots.Add(slot);
            }
        }

        HexTileView GetSlot(int index)
        {
            while (_slotPool.Count <= index)
            {
                var v = HexTileView.Create(_slotsRoot, $"Slot_{_slotPool.Count}");
                v.gameObject.SetActive(false);
                _slotPool.Add(v);
            }
            return _slotPool[index];
        }

        public HexCoord? WorldToHex(Vector3 world) =>
            HexMeshUtil.FromWorld(world, _hexSize);
    }
}
