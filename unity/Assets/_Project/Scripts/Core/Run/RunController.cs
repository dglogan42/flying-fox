using System;
using System.Collections.Generic;

namespace FlyingFox.Core
{
    public enum RunMode
    {
        Classic,
        Daily,
        DailyPractice, // no profile / achievement mutation
    }

    public enum RunPhase
    {
        NotStarted,
        Playing,
        Ended,
    }

    public sealed class RunConfig
    {
        public RunMode Mode { get; set; } = RunMode.Classic;
        public int? Seed { get; set; }
        public GameBalance Balance { get; set; } = GameBalance.WebParity;
        public bool IsScored => Mode != RunMode.DailyPractice;
    }

    public sealed class RunResult
    {
        public int Score { get; set; }
        public ScoreBreakdown Breakdown { get; set; }
        public int PlacedHandTiles { get; set; }
        public int BoardSize { get; set; }
        public int PerfectCount { get; set; }
        public int MatchEdgeCount { get; set; }
        public int QuestsCompleted { get; set; }
        public bool NaturalEnd { get; set; }
        public RunMode Mode { get; set; }
        public string Medal { get; set; }
    }

    /// <summary>
    /// Headless run state machine — port of game.js flow without presentation.
    /// </summary>
    public sealed class RunController
    {
        public RunConfig Config { get; private set; }
        public RunPhase Phase { get; private set; } = RunPhase.NotStarted;
        public BoardModel Board { get; } = new BoardModel();
        public List<TileModel> Deck { get; private set; } = new List<TileModel>();
        public List<TileModel> Hand { get; } = new List<TileModel>();
        public int SelectedHandIndex { get; private set; }
        public int Score { get; private set; }
        public ScoreBreakdown Breakdown { get; } = new ScoreBreakdown();
        public List<QuestState> Quests { get; private set; } = new List<QuestState>();
        public int PlacedHandTiles { get; private set; }
        public int PerfectCount { get; private set; }
        public int MatchEdgeCount { get; private set; }
        public bool NaturalEnd { get; private set; }

        private readonly DeckFactory _factory = new DeckFactory();
        private IRng _rng;

        public event Action Changed;

        public void Start(RunConfig config, IRng rng = null)
        {
            Config = config ?? new RunConfig();
            _rng = rng ?? new SplitMix64Rng(config.Seed ?? Environment.TickCount);
            if (config.Seed.HasValue) _rng.Reseed(config.Seed.Value);

            Board.Clear();
            Hand.Clear();
            Score = 0;
            PlacedHandTiles = 0;
            PerfectCount = 0;
            MatchEdgeCount = 0;
            NaturalEnd = false;
            Breakdown.Matches = Breakdown.Perfects = Breakdown.Tiles = Breakdown.Quests = 0;
            SelectedHandIndex = 0;
            _factory.ResetIds(1);

            Quests = new List<QuestState>();
            foreach (var def in QuestCatalog.WebDefaults())
                Quests.Add(new QuestState(def));

            // Hub tile (free — not in hand place stats)
            var hub = _factory.MakeHub();
            Board.Place(new PlacedTile(HexCoord.Origin, hub.Id, hub.Edges));

            Deck = _factory.BuildDeck(_rng, Config.Balance.DeckSize);
            DrawToHand();
            Phase = RunPhase.Playing;
            Raise();
        }

        public void SelectHand(int index)
        {
            if (Phase != RunPhase.Playing || Hand.Count == 0) return;
            if (index < 0 || index >= Hand.Count) return;
            SelectedHandIndex = index;
            Raise();
        }

        public void CycleHand(int delta = 1)
        {
            if (Hand.Count == 0) return;
            SelectedHandIndex = (SelectedHandIndex + delta % Hand.Count + Hand.Count) % Hand.Count;
            Raise();
        }

        public void RotateSelected(int steps = 1)
        {
            if (Phase != RunPhase.Playing || Hand.Count == 0) return;
            Hand[SelectedHandIndex].Rotate(steps);
            Raise();
        }

        public bool TryPlace(HexCoord at)
        {
            if (Phase != RunPhase.Playing || Hand.Count == 0) return false;
            if (!Board.IsValidPlacement(at)) return false;

            var tile = Hand[SelectedHandIndex];
            var ev = PlacementService.Evaluate(Board, at, tile.Edges);
            int placeScore = PlacementService.ScoreFor(ev, Config.Balance);

            Board.Place(new PlacedTile(at, tile.Id, tile.Edges));
            Hand.RemoveAt(SelectedHandIndex);
            if (SelectedHandIndex >= Hand.Count)
                SelectedHandIndex = Math.Max(0, Hand.Count - 1);

            Score += placeScore;
            Breakdown.AddPlacement(ev, Config.Balance);
            PlacedHandTiles++;
            MatchEdgeCount += ev.Matches;
            if (ev.IsPerfect) PerfectCount++;

            int questGain = QuestService.Check(Board, Quests);
            if (questGain > 0)
            {
                Score += questGain;
                Breakdown.AddQuest(questGain);
            }

            DrawToHand();

            if (Hand.Count == 0 && Deck.Count == 0)
            {
                End(natural: true);
                return true;
            }

            if (Hand.Count > 0 && Board.GetEmptyAdjacent().Count == 0)
            {
                End(natural: true);
                return true;
            }

            Raise();
            return true;
        }

        public void Abandon()
        {
            if (Phase != RunPhase.Playing) return;
            End(natural: false);
        }

        public RunResult BuildResult()
        {
            int done = 0;
            foreach (var q in Quests) if (q.Done) done++;
            return new RunResult
            {
                Score = Score,
                Breakdown = Breakdown,
                PlacedHandTiles = PlacedHandTiles,
                BoardSize = Board.Count,
                PerfectCount = PerfectCount,
                MatchEdgeCount = MatchEdgeCount,
                QuestsCompleted = done,
                NaturalEnd = NaturalEnd,
                Mode = Config.Mode,
                Medal = MedalFor(Score),
            };
        }

        public static string MedalFor(int score)
        {
            if (score >= 400) return "Golden Fox";
            if (score >= 280) return "Oak Crown";
            if (score >= 180) return "Pine Badge";
            if (score >= 100) return "Birch Leaf";
            if (score >= 50) return "Sapling Grove";
            return "Keep growing…";
        }

        private void DrawToHand()
        {
            int handSize = Config.Balance.HandSize;
            while (Hand.Count < handSize && Deck.Count > 0)
            {
                var t = Deck[Deck.Count - 1];
                Deck.RemoveAt(Deck.Count - 1);
                Hand.Add(t);
            }
            if (SelectedHandIndex >= Hand.Count)
                SelectedHandIndex = Math.Max(0, Hand.Count - 1);
        }

        private void End(bool natural)
        {
            NaturalEnd = natural;
            Phase = RunPhase.Ended;
            Raise();
        }

        private void Raise() => Changed?.Invoke();
    }
}
