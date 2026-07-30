namespace FlyingFox.Core
{
    /// <summary>Parity with game.js BIOME letters F/M/W/R.</summary>
    public enum BiomeId : byte
    {
        Forest = 0, // F
        Meadow = 1, // M
        Water = 2,  // W
        Rock = 3,   // R
    }

    public static class BiomeCodec
    {
        public static char ToChar(BiomeId b) => b switch
        {
            BiomeId.Forest => 'F',
            BiomeId.Meadow => 'M',
            BiomeId.Water => 'W',
            BiomeId.Rock => 'R',
            _ => '?',
        };

        public static BiomeId FromChar(char c) => char.ToUpperInvariant(c) switch
        {
            'F' => BiomeId.Forest,
            'M' => BiomeId.Meadow,
            'W' => BiomeId.Water,
            'R' => BiomeId.Rock,
            _ => throw new System.ArgumentOutOfRangeException(nameof(c), c, "Unknown biome"),
        };

        public static BiomeId[] FromChars(string edges)
        {
            if (edges == null || edges.Length != 6)
                throw new System.ArgumentException("Need exactly 6 biome chars", nameof(edges));
            var a = new BiomeId[6];
            for (int i = 0; i < 6; i++) a[i] = FromChar(edges[i]);
            return a;
        }

        public static BiomeId[] FromChars(params char[] edges)
        {
            if (edges == null || edges.Length != 6)
                throw new System.ArgumentException("Need exactly 6 biome chars", nameof(edges));
            var a = new BiomeId[6];
            for (int i = 0; i < 6; i++) a[i] = FromChar(edges[i]);
            return a;
        }
    }
}
