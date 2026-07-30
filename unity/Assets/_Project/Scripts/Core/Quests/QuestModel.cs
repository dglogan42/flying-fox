using System.Collections.Generic;

namespace FlyingFox.Core
{
    public enum QuestKind
    {
        BiomeCluster,
        IslandSize,
    }

    public sealed class QuestDef
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int Target { get; set; }
        public int Reward { get; set; }
        public QuestKind Kind { get; set; }
        public BiomeId Biome { get; set; }
    }

    public sealed class QuestState
    {
        public QuestDef Def { get; }
        public int Progress { get; set; }
        public bool Done { get; set; }

        public QuestState(QuestDef def)
        {
            Def = def;
            Progress = 0;
            Done = false;
        }
    }

    public static class QuestCatalog
    {
        /// <summary>Web parity quests from game.js makeQuests().</summary>
        public static List<QuestDef> WebDefaults() => new List<QuestDef>
        {
            new QuestDef
            {
                Id = "forest5", Title = "Fox Den",
                Description = "Connect 5+ forest edges in one region",
                Target = 5, Reward = 40, Kind = QuestKind.BiomeCluster, Biome = BiomeId.Forest,
            },
            new QuestDef
            {
                Id = "water4", Title = "River Run",
                Description = "Connect 4+ water edges in one region",
                Target = 4, Reward = 35, Kind = QuestKind.BiomeCluster, Biome = BiomeId.Water,
            },
            new QuestDef
            {
                Id = "meadow6", Title = "Sunlit Glade",
                Description = "Connect 6+ meadow edges in one region",
                Target = 6, Reward = 45, Kind = QuestKind.BiomeCluster, Biome = BiomeId.Meadow,
            },
            new QuestDef
            {
                Id = "island8", Title = "Home Island",
                Description = "Grow the map to 8 tiles",
                Target = 8, Reward = 25, Kind = QuestKind.IslandSize,
            },
            new QuestDef
            {
                Id = "island16", Title = "Canopy Realm",
                Description = "Grow the map to 16 tiles",
                Target = 16, Reward = 50, Kind = QuestKind.IslandSize,
            },
        };
    }

    /// <summary>
    /// Largest connected biome-edge cluster (edge-node BFS). Parity with game.js largestBiomeCluster.
    /// </summary>
    public static class BiomeClusterAnalyzer
    {
        public static int LargestCluster(BoardModel board, BiomeId biome)
        {
            var adj = new Dictionary<string, List<string>>();
            var nodes = new List<string>();

            string NodeKey(string tileKey, int e) => $"{tileKey}:{e}";

            foreach (var tile in board.All)
            {
                string k = tile.Coord.Key;
                for (int e = 0; e < 6; e++)
                {
                    if (tile.Edges[e] != biome) continue;
                    string nk = NodeKey(k, e);
                    nodes.Add(nk);
                    if (!adj.ContainsKey(nk)) adj[nk] = new List<string>();

                    var n = tile.Coord.Neighbor(e);
                    if (board.TryGet(n, out var nt) &&
                        nt.Edges[HexCoord.OppositeEdge[e]] == biome)
                    {
                        adj[nk].Add(NodeKey(n.Key, HexCoord.OppositeEdge[e]));
                    }

                    int prev = (e + 5) % 6;
                    int next = (e + 1) % 6;
                    if (tile.Edges[prev] == biome) adj[nk].Add(NodeKey(k, prev));
                    if (tile.Edges[next] == biome) adj[nk].Add(NodeKey(k, next));
                }
            }

            var seen = new HashSet<string>();
            int best = 0;
            foreach (var start in nodes)
            {
                if (!seen.Add(start)) continue;
                var stack = new Stack<string>();
                stack.Push(start);
                int count = 0;
                while (stack.Count > 0)
                {
                    var cur = stack.Pop();
                    count++;
                    if (!adj.TryGetValue(cur, out var nbs)) continue;
                    foreach (var nb in nbs)
                    {
                        if (adj.ContainsKey(nb) && seen.Add(nb))
                            stack.Push(nb);
                    }
                }
                if (count > best) best = count;
            }
            return best;
        }
    }

    public static class QuestService
    {
        /// <summary>Update progress; return total reward gained this call.</summary>
        public static int Check(BoardModel board, IList<QuestState> quests)
        {
            int gained = 0;
            foreach (var q in quests)
            {
                if (q.Done) continue;
                if (q.Def.Kind == QuestKind.IslandSize)
                    q.Progress = board.Count;
                else
                    q.Progress = BiomeClusterAnalyzer.LargestCluster(board, q.Def.Biome);

                if (q.Progress >= q.Def.Target)
                {
                    q.Done = true;
                    q.Progress = q.Def.Target;
                    gained += q.Def.Reward;
                }
            }
            return gained;
        }
    }
}
