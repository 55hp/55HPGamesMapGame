using System.Collections.Generic;
using System.Linq;
using hp55games.FranzTools.HexDebugFramework;
using UnityEngine;

namespace hp55games.MapGame.Features.Debug
{
    /// <summary>
    /// Verifica che tra due placement distinti (EventCluster o tessere singole) esista
    /// almeno 1 tile di gap (distanza ≥ 2), replicando la regola di rejection sampling
    /// della generazione. Segnala come Error ogni coppia di placement adiacenti.
    /// </summary>
    public sealed class EventClusterSpacingAnalyzer : IHexAnalyzer
    {
        // Flag che identificano tile evento (tutto tranne Strada e Neutra)
        private static readonly HexDebugFlags EventFlags =
            MapGameDebugFlags.Battaglia |
            MapGameDebugFlags.Trappola  |
            MapGameDebugFlags.Risorsa   |
            MapGameDebugFlags.Npc       |
            MapGameDebugFlags.Mistery   |
            MapGameDebugFlags.Boss;

        public string Name => "Event Cluster Spacing";

        public DebugCluster[] Analyze(HexGridRegistry registry, IHexTopology topology)
        {
            // Raggruppa le tile evento in placement (componenti connesse)
            var visited = new HashSet<Vector2Int>();
            var placements = new List<List<IHexCell>>();

            foreach (var tile in registry.AllTiles)
            {
                if ((tile.Flags & EventFlags) == 0) continue;
                if (visited.Contains(tile.Coordinates)) continue;

                var group = new List<IHexCell>();
                BfsPlacement(tile.Coordinates, registry, topology, visited, group);
                placements.Add(group);
            }

            // Controlla che nessun placement sia adiacente a un altro
            // Costruisce set di coordinate per ogni placement
            var placementCoordSets = placements
                .Select(p => new HashSet<Vector2Int>(p.Select(c => c.Coordinates)))
                .ToList();

            var violationCells = new List<IHexCell>();
            var results = new List<DebugCluster>();

            for (int i = 0; i < placements.Count; i++)
            {
                foreach (var cell in placements[i])
                {
                    foreach (var neighborCoord in topology.GetNeighbors(cell.Coordinates))
                    {
                        for (int j = 0; j < placements.Count; j++)
                        {
                            if (j == i) continue;
                            if (placementCoordSets[j].Contains(neighborCoord))
                            {
                                if (!violationCells.Contains(cell))
                                    violationCells.Add(cell);
                            }
                        }
                    }
                }
            }

            if (violationCells.Count == 0)
                return new[] { new DebugCluster(new List<IHexCell>(), new List<HexGraphEdge>())
                    { Name = "Event Spacing OK", Severity = DebugSeverity.Info } };

            return new[] { new DebugCluster(violationCells, new List<HexGraphEdge>())
                { Name = $"Event Spacing: {violationCells.Count} violazioni", Severity = DebugSeverity.Error } };
        }

        private static void BfsPlacement(
            Vector2Int start,
            HexGridRegistry registry,
            IHexTopology topology,
            HashSet<Vector2Int> visited,
            List<IHexCell> group)
        {
            var queue = new Queue<Vector2Int>();
            visited.Add(start);
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                registry.TryGetTile(current, out var tile);
                group.Add(tile);

                foreach (var n in topology.GetNeighbors(current))
                {
                    if (visited.Contains(n)) continue;
                    if (!registry.TryGetTile(n, out var neighbor)) continue;
                    if ((neighbor.Flags & EventFlags) == 0) continue;

                    visited.Add(n);
                    queue.Enqueue(n);
                }
            }
        }
    }
}
