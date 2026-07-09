using System.Collections.Generic;
using System.Linq;
using hp55games.FranzTools.HexDebugFramework;
using UnityEngine;

namespace hp55games.MapGame.Features.Debug
{
    /// <summary>
    /// Scorre i confini tra PathCluster diversi e segnala con DebugSeverity.Error
    /// qualsiasi coppia di celle Strada appartenenti a due PathCluster distinti
    /// che risultano direttamente adiacenti (senza tile Neutra di separazione).
    /// </summary>
    public sealed class NeutraSeparationAnalyzer : IHexAnalyzer
    {
        public string Name => "Neutra Separation";

        public DebugCluster[] Analyze(HexGridRegistry registry, IHexTopology topology)
        {
            // Identifica i PathCluster come componenti connesse di tile Strada
            var clusterMap = new Dictionary<Vector2Int, int>(); // coord → cluster ID
            int clusterId = 0;

            foreach (var tile in registry.AllTiles)
            {
                if ((tile.Flags & HexDebugFlags.Road) == 0) continue;
                if (clusterMap.ContainsKey(tile.Coordinates)) continue;

                BfsLabel(tile.Coordinates, registry, topology, clusterMap, clusterId);
                clusterId++;
            }

            // Trova violazioni: due tile Strada di cluster diversi direttamente adiacenti
            var violationCells = new List<IHexCell>();

            foreach (var tile in registry.AllTiles)
            {
                if ((tile.Flags & HexDebugFlags.Road) == 0) continue;
                if (!clusterMap.TryGetValue(tile.Coordinates, out int ownCluster)) continue;

                foreach (var neighborCoord in topology.GetNeighbors(tile.Coordinates))
                {
                    if (!registry.TryGetTile(neighborCoord, out var neighbor)) continue;
                    if ((neighbor.Flags & HexDebugFlags.Road) == 0) continue;
                    if (!clusterMap.TryGetValue(neighborCoord, out int neighborCluster)) continue;

                    if (neighborCluster != ownCluster && !violationCells.Contains(tile))
                        violationCells.Add(tile);
                }
            }

            if (violationCells.Count == 0)
                return new[] { new DebugCluster(new List<IHexCell>(), new List<HexGraphEdge>())
                    { Name = "Neutra Separation OK", Severity = DebugSeverity.Info } };

            return new[] { new DebugCluster(violationCells, new List<HexGraphEdge>())
                { Name = $"Neutra Separation: {violationCells.Count} violazioni", Severity = DebugSeverity.Error } };
        }

        private static void BfsLabel(
            Vector2Int start,
            HexGridRegistry registry,
            IHexTopology topology,
            Dictionary<Vector2Int, int> clusterMap,
            int id)
        {
            var queue = new Queue<Vector2Int>();
            clusterMap[start] = id;
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var n in topology.GetNeighbors(current))
                {
                    if (clusterMap.ContainsKey(n)) continue;
                    if (!registry.TryGetTile(n, out var neighbor)) continue;
                    if ((neighbor.Flags & HexDebugFlags.Road) == 0) continue;

                    clusterMap[n] = id;
                    queue.Enqueue(n);
                }
            }
        }
    }
}
