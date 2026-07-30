using System.Collections.Generic;

namespace FlyingFox.Core
{
    /// <summary>
    /// Web parity defaults (game.js / README). Later mirrored by a ScriptableObject wrapper.
    /// </summary>
    public sealed class GameBalance
    {
        public int PlacePoints { get; set; } = 2;
        public int MatchPoints { get; set; } = 12;
        public int PerfectBonus { get; set; } = 20;
        public int HandSize { get; set; } = 3;
        public int DeckSize { get; set; } = 36;
        public float HexSize { get; set; } = 36f;
        public float ZoomMin { get; set; } = 0.45f;
        public float ZoomMax { get; set; } = 2.4f;

        public static GameBalance WebParity { get; } = new GameBalance();
    }

    public readonly struct PlacementEval
    {
        public readonly int Matches;
        public readonly int Mismatches;
        public readonly int Contacts;
        public readonly IReadOnlyList<BiomeId> MatchedBiomes;

        public PlacementEval(int matches, int mismatches, int contacts, IReadOnlyList<BiomeId> matchedBiomes = null)
        {
            Matches = matches;
            Mismatches = mismatches;
            Contacts = contacts;
            MatchedBiomes = matchedBiomes ?? System.Array.Empty<BiomeId>();
        }

        public bool IsPerfect => Contacts > 0 && Matches == Contacts;
    }

    public sealed class ScoreBreakdown
    {
        public int Matches;   // sum of matches * matchPoints
        public int Perfects;  // sum of perfect bonuses
        public int Tiles;     // sum of place points (hand places only)
        public int Quests;
        public int Abilities; // fox biome ability points

        public int Total => Matches + Perfects + Tiles + Quests + Abilities;

        public void AddPlacement(PlacementEval ev, GameBalance bal, bool perfectGranted, int abilityPoints)
        {
            Matches += ev.Matches * bal.MatchPoints;
            if (perfectGranted) Perfects += bal.PerfectBonus;
            Tiles += bal.PlacePoints;
            Abilities += abilityPoints;
        }

        public void AddQuest(int reward) => Quests += reward;
    }
}
