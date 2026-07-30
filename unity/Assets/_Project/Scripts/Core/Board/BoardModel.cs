using System.Collections.Generic;

namespace FlyingFox.Core
{
    public sealed class BoardModel
    {
        private readonly Dictionary<string, PlacedTile> _tiles = new Dictionary<string, PlacedTile>();

        public int Count => _tiles.Count;

        public IEnumerable<PlacedTile> All => _tiles.Values;

        public bool TryGet(HexCoord c, out PlacedTile tile) =>
            _tiles.TryGetValue(c.Key, out tile);

        public bool Has(HexCoord c) => _tiles.ContainsKey(c.Key);

        public void Place(PlacedTile tile)
        {
            _tiles[tile.Coord.Key] = tile;
        }

        public void Clear() => _tiles.Clear();

        public bool IsValidPlacement(HexCoord c)
        {
            if (Has(c)) return false;
            if (Count == 0) return c == HexCoord.Origin;
            for (int e = 0; e < 6; e++)
            {
                if (Has(c.Neighbor(e))) return true;
            }
            return false;
        }

        public List<HexCoord> GetEmptyAdjacent()
        {
            var set = new Dictionary<string, HexCoord>();
            foreach (var tile in _tiles.Values)
            {
                for (int e = 0; e < 6; e++)
                {
                    var n = tile.Coord.Neighbor(e);
                    if (!Has(n)) set[n.Key] = n;
                }
            }
            return new List<HexCoord>(set.Values);
        }
    }
}
