using System.Collections.Generic;

namespace FlyingFox.Core
{
    public sealed class DeckFactory
    {
        public const int DefaultDeckSize = 36;
        public const int DefaultHandSize = 3;

        private int _nextId = 1;

        // game.js presets
        private static readonly string[] Presets =
        {
            "FFFMMM",
            "WWWFFF",
            "MMRRMM",
            "FFWWFF",
            "RRRMMF",
            "WWMMMW",
            "FMMFFM",
            "WFFWWF",
        };

        // Hub tile edges: ["F","F","M","M","W","F"]
        public static readonly BiomeId[] HubEdges =
            BiomeCodec.FromChars('F', 'F', 'M', 'M', 'W', 'F');

        public void ResetIds(int start = 1) => _nextId = start;

        public TileModel MakeTile(BiomeId[] edges) =>
            new TileModel(_nextId++, edges);

        public TileModel MakeHub() => MakeTile(HubEdges);

        public List<TileModel> BuildDeck(IRng rng, int deckSize = DefaultDeckSize)
        {
            var tiles = new List<TileModel>(deckSize);
            foreach (var p in Presets)
                tiles.Add(MakeTile(BiomeCodec.FromChars(p)));

            while (tiles.Count < deckSize)
                tiles.Add(MakeTile(RandomEdges(rng)));

            Shuffle(tiles, rng);
            return tiles;
        }

        /// <summary>Parity with game.js randomEdges() weights.</summary>
        public static BiomeId[] RandomEdges(IRng rng)
        {
            float mode = rng.NextFloat();
            if (mode < 0.35f)
            {
                var a = PickBiome(rng);
                var b = PickOther(rng, a);
                int split = 1 + rng.Next(0, 3);
                var edges = new BiomeId[6];
                for (int i = 0; i < 6; i++)
                    edges[i] = i < split ? a : b;
                return edges;
            }

            if (mode < 0.55f)
            {
                var a = PickBiome(rng);
                var b = PickOther(rng, a);
                var c = PickOther(rng, a, b);
                return new[] { a, a, b, b, c, c };
            }

            if (mode < 0.7f)
            {
                var a = PickBiome(rng);
                var edges = new BiomeId[6];
                for (int i = 0; i < 6; i++) edges[i] = a;
                edges[rng.Next(0, 6)] = PickOther(rng, a);
                return edges;
            }

            var e = new BiomeId[6];
            for (int i = 0; i < 6; i++) e[i] = PickBiome(rng);
            for (int i = 0; i < 6; i++)
            {
                if (rng.NextFloat() < 0.4f)
                    e[i] = e[(i + 5) % 6];
            }
            return e;
        }

        private static BiomeId PickBiome(IRng rng) =>
            (BiomeId)rng.Next(0, 4);

        private static BiomeId PickOther(IRng rng, BiomeId a)
        {
            BiomeId b;
            do { b = PickBiome(rng); } while (b == a);
            return b;
        }

        private static BiomeId PickOther(IRng rng, BiomeId a, BiomeId b)
        {
            BiomeId c;
            do { c = PickBiome(rng); } while (c == a || c == b);
            return c;
        }

        public static void Shuffle<T>(IList<T> list, IRng rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
