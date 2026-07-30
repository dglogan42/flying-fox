using System;
using FlyingFox.Core;
using NUnit.Framework;

namespace FlyingFox.Core.Tests
{
    public class CoreParityTests
    {
        [Test]
        public void Hex_Neighbor_East_IsPlusQ()
        {
            var n = HexCoord.Origin.Neighbor(0);
            Assert.AreEqual(1, n.Q);
            Assert.AreEqual(0, n.R);
        }

        [Test]
        public void Hex_OppositeEdges_AreConsistent()
        {
            for (int e = 0; e < 6; e++)
            {
                int o = HexCoord.OppositeEdge[e];
                Assert.AreEqual(e, HexCoord.OppositeEdge[o]);
            }
        }

        [Test]
        public void RotateEdges_MatchesWebSliceSemantics()
        {
            var edges = BiomeCodec.FromChars('F', 'M', 'W', 'R', 'F', 'M');
            var rot = TileModel.RotateEdges(edges, 1);
            // JS n=1: [e5, e0, e1, e2, e3, e4]
            Assert.AreEqual(BiomeId.Meadow, rot[0]); // was e5
            Assert.AreEqual(BiomeId.Forest, rot[1]); // was e0
        }

        [Test]
        public void Placement_PerfectMatch_Scores_2_Plus_12_Plus_20()
        {
            var board = new BoardModel();
            var hub = DeckFactory.HubEdges;
            board.Place(new PlacedTile(HexCoord.Origin, 0, hub));

            // Place east: contact edge 3 (W) of new tile vs hub edge 0 (E) = Forest
            // New tile edges all Forest for perfect if hub E is F — hub[0]=F
            var edges = BiomeCodec.FromChars('F', 'F', 'F', 'F', 'F', 'F');
            var at = HexCoord.Origin.Neighbor(0); // E of hub
            var ev = PlacementService.Evaluate(board, at, edges);
            Assert.AreEqual(1, ev.Contacts);
            Assert.AreEqual(1, ev.Matches);
            Assert.IsTrue(ev.IsPerfect);
            Assert.AreEqual(2 + 12 + 20, PlacementService.ScoreFor(ev, GameBalance.WebParity));
        }

        [Test]
        public void DailySeed_IsStable_ForKnownDate()
        {
            var a = DailySeed.FromUtcDate(new DateTime(2026, 7, 30, 12, 0, 0, DateTimeKind.Utc));
            var b = DailySeed.FromUtcDate(new DateTime(2026, 7, 30, 23, 59, 0, DateTimeKind.Utc));
            var c = DailySeed.FromKey("FlyingFoxDaily|2026-07-30");
            Assert.AreEqual(a, b);
            Assert.AreEqual(a, c);
        }

        [Test]
        public void SplitMix64_Reseed_IsDeterministic()
        {
            var r1 = new SplitMix64Rng(42);
            var r2 = new SplitMix64Rng(42);
            for (int i = 0; i < 20; i++)
                Assert.AreEqual(r1.Next(0, 1000), r2.Next(0, 1000));
        }

        [Test]
        public void Run_Starts_With_Hub_And_HandOf3()
        {
            var run = new RunController();
            run.Start(new RunConfig { Seed = 12345 }, new SplitMix64Rng(12345));
            Assert.AreEqual(1, run.Board.Count);
            Assert.AreEqual(3, run.Hand.Count);
            Assert.AreEqual(36, run.Deck.Count + run.Hand.Count); // deck was 36, hand drawn from it
            Assert.AreEqual(RunPhase.Playing, run.Phase);
        }

        [Test]
        public void Run_Place_Increases_Score_And_Board()
        {
            var run = new RunController();
            run.Start(new RunConfig { Seed = 7 }, new SplitMix64Rng(7));
            var targets = run.Board.GetEmptyAdjacent();
            Assert.Greater(targets.Count, 0);
            bool ok = run.TryPlace(targets[0]);
            Assert.IsTrue(ok);
            Assert.AreEqual(2, run.Board.Count);
            Assert.GreaterOrEqual(run.Score, 2);
            Assert.AreEqual(1, run.PlacedHandTiles);
        }

        [Test]
        public void Medal_Tiers_Match_Web()
        {
            Assert.AreEqual("Birch Leaf", RunController.MedalFor(100));
            Assert.AreEqual("Golden Fox", RunController.MedalFor(400));
        }
    }
}
