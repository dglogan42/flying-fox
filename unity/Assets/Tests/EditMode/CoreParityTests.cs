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
        public void Hub_Is_All_Neutral()
        {
            foreach (var e in DeckFactory.HubEdges)
                Assert.AreEqual(BiomeId.Neutral, e);
        }

        [Test]
        public void Neutral_Wild_Matches_Forest_And_Grants_CanopyLeap()
        {
            var board = new BoardModel();
            board.Place(new PlacedTile(HexCoord.Origin, 0, DeckFactory.HubEdges));

            // Hub N is wild → any edge matches; player's F → Canopy Leap
            // base 2+12+20 + Canopy Leap +6 = 40
            var edges = BiomeCodec.FromChars('F', 'F', 'F', 'F', 'F', 'F');
            var at = HexCoord.Origin.Neighbor(0);
            var ev = PlacementService.Evaluate(board, at, edges);
            Assert.AreEqual(1, ev.Contacts);
            Assert.AreEqual(1, ev.Matches);
            Assert.IsTrue(ev.IsPerfect);
            Assert.AreEqual(BiomeId.Forest, ev.MatchedBiomes[0]);
            Assert.AreEqual(2 + 12 + 20 + 6, PlacementService.ScoreFor(ev, GameBalance.WebParity));
        }

        [Test]
        public void Rock_Anchor_SoftPerfect_On_Next_Place()
        {
            var run = new RunController();
            run.Start(new RunConfig { Seed = 99 }, new SplitMix64Rng(99));
            // Force a rock-matching place is hard without fixed board; unit-test scorer instead:
            var matched = new[] { BiomeId.Rock };
            var ev = new PlacementEval(1, 1, 2, matched); // not hard perfect
            var first = FoxAbilityService.Score(ev, GameBalance.WebParity, false);
            Assert.IsTrue(first.ArmAnchor);
            Assert.AreEqual(10, first.AbilityPoints);

            var soft = FoxAbilityService.Score(
                new PlacementEval(1, 1, 2, new[] { BiomeId.Meadow }),
                GameBalance.WebParity,
                anchorArmed: true);
            Assert.IsTrue(soft.PerfectGranted);
            Assert.IsFalse(soft.HardPerfect);
            Assert.AreEqual(15, soft.AbilityPoints); // sunbeam on soft perfect + meadow
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
