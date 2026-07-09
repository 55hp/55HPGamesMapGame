using System.Collections.Generic;
using System.Linq;
using hp55games.FranzTools.HexDebugFramework;
using hp55games.MapGame.Features.Configs;
using UnityEngine;

namespace hp55games.MapGame.Features.Debug
{
    /// <summary>
    /// Overlay del Scene Drawer che colora ogni PathCluster in base al riempimento
    /// rispetto al budget MaxTotalTiles:
    ///   verde → ≤ 60% del budget
    ///   giallo → 61-100% del budget
    ///   rosso  → oltre budget
    /// </summary>
    public sealed class ClusterBudgetHeatmap : IHexAnalyzer
    {
        private readonly int _maxTotalTiles;

        private static readonly Color ColorOk      = new Color(0.3f, 0.85f, 0.3f);
        private static readonly Color ColorWarning  = new Color(1f,   0.75f, 0.1f);
        private static readonly Color ColorOver     = new Color(1f,   0.25f, 0.25f);

        public string Name => "Cluster Budget Heatmap";

        public ClusterBudgetHeatmap(int maxTotalTiles)
        {
            _maxTotalTiles = maxTotalTiles;
        }

        public DebugCluster[] Analyze(HexGridRegistry registry, IHexTopology topology)
        {
            var visited = new HashSet<Vector2Int>();
            var results = new List<DebugCluster>();
            int idx = 0;

            foreach (var tile in registry.AllTiles)
            {
                if ((tile.Flags & HexDebugFlags.Road) == 0) continue;
                if (visited.Contains(tile.Coordinates)) continue;

                var cells = new List<IHexCell>();
                var edges = new List<HexGraphEdge>();
                BfsCluster(tile.Coordinates, registry, topology, visited, cells, edges);

                float fill = (float)cells.Count / _maxTotalTiles;
                Color color;
                DebugSeverity severity;

                if (fill <= 0.6f)        { color = ColorOk;      severity = DebugSeverity.Info; }
                else if (fill <= 1.0f)   { color = ColorWarning; severity = DebugSeverity.Warning; }
                else                     { color = ColorOver;    severity = DebugSeverity.Error; }

                results.Add(new DebugCluster(cells, edges)
                {
                    Name = $"Path {idx:D2} ({cells.Count}/{_maxTotalTiles})",
                    Color = color,
                    Severity = severity,
                });
                idx++;
            }

            return results.ToArray();
        }

        private static void BfsCluster(
            Vector2Int start,
            HexGridRegistry registry,
            IHexTopology topology,
            HashSet<Vector2Int> visited,
            List<IHexCell> cells,
            List<HexGraphEdge> edges)
        {
            var queue = new Queue<HexDebugData>();
            visited.Add(start);
            registry.TryGetTile(start, out var startTile);
            queue.Enqueue(startTile);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                cells.Add(current);

                foreach (var n in topology.GetNeighbors(current.Coordinates))
                {
                    if (visited.Contains(n)) continue;
                    if (!registry.TryGetTile(n, out var neighbor)) continue;
                    if ((neighbor.Flags & HexDebugFlags.Road) == 0) continue;

                    visited.Add(n);
                    queue.Enqueue(neighbor);
                    edges.Add(new HexGraphEdge(current, neighbor));
                }
            }
        }
    }
}
