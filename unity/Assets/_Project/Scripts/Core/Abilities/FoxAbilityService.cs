using System.Collections.Generic;

namespace FlyingFox.Core
{
    public sealed class AbilityProc
    {
        public BiomeId Biome;
        public string Name;
        public string Detail;
    }

    public sealed class AbilityScoreResult
    {
        public int BasePoints;
        public int PerfectPoints;
        public int AbilityPoints;
        public int Total => BasePoints + PerfectPoints + AbilityPoints;
        public bool PerfectGranted;
        public bool HardPerfect;
        public bool EddyDraw;
        public bool ArmAnchor;
        public readonly List<AbilityProc> Procs = new List<AbilityProc>();
    }

    /// <summary>
    /// Per-biome fox abilities (web parity with game.js FOX_ABILITIES / scorePlacement).
    /// </summary>
    public static class FoxAbilityService
    {
        public const int ForestExtraPerMatch = 6;
        public const int MeadowSunbeam = 15;
        public const int RockFlat = 10;
        public const int HandMax = 4;

        public static AbilityScoreResult Score(
            PlacementEval eval,
            GameBalance bal,
            bool anchorArmed)
        {
            var r = new AbilityScoreResult
            {
                BasePoints = eval.Matches * bal.MatchPoints + bal.PlacePoints,
            };

            bool hardPerfect = eval.IsPerfect;
            bool softPerfect = anchorArmed && eval.Contacts > 0 && eval.Mismatches <= 1 && eval.Matches >= 1;
            r.HardPerfect = hardPerfect;
            r.PerfectGranted = hardPerfect || softPerfect;
            r.PerfectPoints = r.PerfectGranted ? bal.PerfectBonus : 0;

            int f = Count(eval.MatchedBiomes, BiomeId.Forest);
            int m = Count(eval.MatchedBiomes, BiomeId.Meadow);
            int w = Count(eval.MatchedBiomes, BiomeId.Water);
            int rock = Count(eval.MatchedBiomes, BiomeId.Rock);

            if (f > 0)
            {
                int bonus = f * ForestExtraPerMatch;
                r.AbilityPoints += bonus;
                r.Procs.Add(new AbilityProc
                {
                    Biome = BiomeId.Forest,
                    Name = "Canopy Leap",
                    Detail = $"+{bonus} ({f}× forest)",
                });
            }

            if (r.PerfectGranted && m > 0)
            {
                r.AbilityPoints += MeadowSunbeam;
                r.Procs.Add(new AbilityProc
                {
                    Biome = BiomeId.Meadow,
                    Name = "Sunbeam",
                    Detail = $"+{MeadowSunbeam}",
                });
            }

            if (w > 0)
            {
                r.EddyDraw = true;
                r.Procs.Add(new AbilityProc
                {
                    Biome = BiomeId.Water,
                    Name = "Eddy",
                    Detail = "draw +1",
                });
            }

            if (rock > 0)
            {
                r.AbilityPoints += RockFlat;
                r.ArmAnchor = true;
                r.Procs.Add(new AbilityProc
                {
                    Biome = BiomeId.Rock,
                    Name = "Anchor",
                    Detail = $"+{RockFlat} · soft perfect next",
                });
            }

            if (anchorArmed && softPerfect && !hardPerfect)
            {
                r.Procs.Add(new AbilityProc
                {
                    Biome = BiomeId.Rock,
                    Name = "Anchor grip",
                    Detail = "soft perfect!",
                });
            }

            return r;
        }

        static int Count(IReadOnlyList<BiomeId> list, BiomeId b)
        {
            if (list == null) return 0;
            int n = 0;
            for (int i = 0; i < list.Count; i++)
                if (list[i] == b) n++;
            return n;
        }
    }
}
