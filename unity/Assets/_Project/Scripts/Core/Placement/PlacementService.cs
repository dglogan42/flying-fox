using System.Collections.Generic;

namespace FlyingFox.Core
{
    public static class PlacementService
    {
        /// <summary>Parity with game.js evaluatePlacement (includes matched biomes).</summary>
        public static PlacementEval Evaluate(BoardModel board, HexCoord at, BiomeId[] edges)
        {
            int matches = 0, mismatches = 0, contacts = 0;
            var matched = new List<BiomeId>(6);
            for (int e = 0; e < 6; e++)
            {
                var n = at.Neighbor(e);
                if (!board.TryGet(n, out var nt)) continue;
                contacts++;
                int theirEdge = HexCoord.OppositeEdge[e];
                if (nt.Edges[theirEdge] == edges[e])
                {
                    matches++;
                    matched.Add(edges[e]);
                }
                else
                {
                    mismatches++;
                }
            }
            return new PlacementEval(matches, mismatches, contacts, matched);
        }

        public static int ScoreFor(PlacementEval ev, GameBalance bal, bool anchorArmed = false)
        {
            return FoxAbilityService.Score(ev, bal, anchorArmed).Total;
        }
    }
}
