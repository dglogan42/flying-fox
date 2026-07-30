using System;

namespace FlyingFox.Core
{
    public sealed class TileModel
    {
        public int Id { get; }
        public BiomeId[] Edges { get; private set; }

        public TileModel(int id, BiomeId[] edges)
        {
            if (edges == null || edges.Length != 6)
                throw new ArgumentException("Tile needs 6 edges", nameof(edges));
            Id = id;
            Edges = (BiomeId[])edges.Clone();
        }

        public TileModel Clone() => new TileModel(Id, Edges);

        /// <summary>Rotate edges — parity with game.js rotateEdges.</summary>
        public void Rotate(int steps)
        {
            Edges = RotateEdges(Edges, steps);
        }

        /// <summary>
        /// game.js: edges.slice(6-n).concat(edges.slice(0, 6-n))
        /// </summary>
        public static BiomeId[] RotateEdges(BiomeId[] edges, int steps)
        {
            int n = ((steps % 6) + 6) % 6;
            if (n == 0) return (BiomeId[])edges.Clone();
            var result = new BiomeId[6];
            int k = 0;
            for (int i = 6 - n; i < 6; i++) result[k++] = edges[i];
            for (int i = 0; i < 6 - n; i++) result[k++] = edges[i];
            return result;
        }
    }

    public sealed class PlacedTile
    {
        public HexCoord Coord { get; }
        public int TileId { get; }
        public BiomeId[] Edges { get; }

        public PlacedTile(HexCoord coord, int tileId, BiomeId[] edges)
        {
            Coord = coord;
            TileId = tileId;
            Edges = (BiomeId[])edges.Clone();
        }
    }
}
