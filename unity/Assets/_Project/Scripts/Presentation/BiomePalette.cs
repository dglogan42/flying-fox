using FlyingFox.Core;
using UnityEngine;

namespace FlyingFox.Presentation
{
    /// <summary>Web-parity biome colors (game.js BIOME_COLOR).</summary>
    public static class BiomePalette
    {
        public static Color Fill(BiomeId b) => b switch
        {
            BiomeId.Forest => Hex("#2d6a4f"),
            BiomeId.Meadow => Hex("#b5c76a"),
            BiomeId.Water => Hex("#1d6a8a"),
            BiomeId.Rock => Hex("#6c757d"),
            BiomeId.Neutral => Hex("#8a7a66"),
            _ => Color.magenta,
        };

        public static Color Edge(BiomeId b) => b switch
        {
            BiomeId.Forest => Hex("#52b788"),
            BiomeId.Meadow => Hex("#d8e2a0"),
            BiomeId.Water => Hex("#4cc9f0"),
            BiomeId.Rock => Hex("#adb5bd"),
            BiomeId.Neutral => Hex("#c4b5a0"),
            _ => Color.white,
        };

        public static Color EmptySlot = new Color(0.25f, 0.56f, 0.42f, 0.22f);
        public static Color EmptySlotHover = new Color(0.25f, 0.56f, 0.42f, 0.45f);
        public static Color GhostTint = new Color(1f, 1f, 1f, 0.55f);
        public static Color PerfectRing = new Color(0.96f, 0.64f, 0.38f, 0.95f);
        public static Color OkRing = new Color(0.58f, 0.84f, 0.7f, 0.85f);

        static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }
    }
}
