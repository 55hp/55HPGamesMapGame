using System.Collections.Generic;
using UnityEngine;

namespace hp55games.FranzTools.HexDebugFramework
{
    /// <summary>
    /// Analyzer di riferimento: raggruppa le celle Road in segmenti connessi tramite BFS
    /// e segnala come Error ogni segmento che supera MaxRoadLength.
    /// Non contiene riferimenti diretti a tipi MapGame: legge solo HexDebugFlags.Road.
    /// </summary>
    public sealed class RoadAnalyzer : IHexAnalyzer
    {
        private readonly int _maxRoadLength;

        public string Name => "Road Analyzer";

        public RoadAnalyzer(int maxRoadLength)
        {
            _maxRoadLength = maxRoadLength;
        }

        public DebugCluster[] Analyze(HexGridRegistry registry, IHexTopology topology)
        {
            var visited = new HashSet<Vector2Int>();
            var clusters = new List<DebugCluster>();
            int index = 0;

            foreach (var tile in registry.AllTiles)
            {
                if ((tile.Flags & HexDebugFlags.Road) == 0)
                    continue;

                if (visited.Contains(tile.Coordinates))
                    continue;

                var clusterCells = new List<IHexCell>();
                var clusterEdges = new List<HexGraphEdge>();
                var queue = new Queue<IHexCell>();

                visited.Add(tile.Coordinates);
                queue.Enqueue(tile);

                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    clusterCells.Add(current);

                    foreach (var neighborCoord in topology.GetNeighbors(current.Coordinates))
                    {
                        if (visited.Contains(neighborCoord))
                            continue;

                        if (!registry.TryGetTile(neighborCoord, out var neighbor))
                            continue;

                        if ((neighbor.Flags & HexDebugFlags.Road) == 0)
                            continue;

                        visited.Add(neighborCoord);
                        queue.Enqueue(neighbor);
                        clusterEdges.Add(new HexGraphEdge(current, neighbor));
                    }
                }

                var severity = clusterCells.Count > _maxRoadLength
                    ? DebugSeverity.Error
                    : DebugSeverity.Info;

                var cluster = new DebugCluster(clusterCells, clusterEdges)
                {
                    Name = $"Road {index:D2}",
                    Severity = severity,
                    Color = Color.white, // sovrascrivibile dal Scene Drawer in Fase 4
                };

                clusters.Add(cluster);
                index++;
            }

            return clusters.ToArray();
        }
    }
}
