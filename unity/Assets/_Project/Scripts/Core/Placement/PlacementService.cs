namespace FlyingFox.Core
{
    public static class PlacementService
    {
        /// <summary>Parity with game.js evaluatePlacement.</summary>
        public static PlacementEval Evaluate(BoardModel board, HexCoord at, BiomeId[] edges)
        {
            int matches = 0, mismatches = 0, contacts = 0;
            for (int e = 0; e < 6; e++)
            {
                var n = at.Neighbor(e);
                if (!board.TryGet(n, out var nt)) continue;
                contacts++;
                int theirEdge = HexCoord.OppositeEdge[e];
                if (nt.Edges[theirEdge] == edges[e]) matches++;
                else mismatches++;
            }
            return new PlacementEval(matches, mismatches, contacts);
        }

        public static int ScoreFor(PlacementEval ev, GameBalance bal)
        {
            int s = ev.Matches * bal.MatchPoints + bal.PlacePoints;
            if (ev.IsPerfect) s += bal.PerfectBonus;
            return s;
        }
    }
}
