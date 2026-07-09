using System.Collections.Generic;
using System.Linq;
using hp55games.FranzTools.HexDebugFramework;
using hp55games.MapGame.Features.Configs;
using UnityEngine;

namespace hp55games.MapGame.Features.Debug
{
    /// <summary>
    /// Verifica che ogni PathCluster (componente connessa di tile Strada) rispetti i budget
    /// di StradaNetworkSettings: tile totali, numero di rami, lunghezza per ramo.
    /// Segnala visivamente gli scostamenti tramite DebugSeverity per individuare
    /// immediatamente regressioni come il bug storico sul MinBranchLength.
    /// </summary>
    public sealed class PathClusterBudgetAnalyzer : IHexAnalyzer
    {
        private readonly StradaNetworkSettings _settings;

        public string Name => "Path Cluster Budget";

        public PathClusterBudgetAnalyzer(StradaNetworkSettings settings)
        {
            _settings = settings;
        }

        public DebugCluster[] Analyze(HexGridRegistry registry, IHexTopology topology)
        {
            // Trova le componenti connesse di tile Strada (Road flag)
            var visited = new HashSet<Vector2Int>();
            var results = new List<DebugCluster>();
            int clusterIndex = 0;

            foreach (var tile in registry.AllTiles)
            {
                if ((tile.Flags & HexDebugFlags.Road) == 0) continue;
                if (visited.Contains(tile.Coordinates)) continue;

                var cells = new List<IHexCell>();
                var edges = new List<HexGraphEdge>();
                BfsCluster(tile.Coordinates, registry, topology, HexDebugFlags.Road, visited, cells, edges);

                var violations = new List<string>();

                // Controlla tile totali
                if (cells.Count > _settings.MaxTotalTiles)
                    violations.Add($"tile {cells.Count} > max {_settings.MaxTotalTiles}");

                // Decomponi in rami e controlla lunghezze
                var branches = FindBranches(cells.Select(c => c.Coordinates).ToHashSet(), topology);
                if (branches.Count > _settings.MaxBranches)
                    violations.Add($"rami {branches.Count} > max {_settings.MaxBranches}");

                foreach (var branch in branches)
                {
                    int branchLen = branch.Count - 1; // esclude il nodo di giunzione condiviso
                    if (branchLen < _settings.MinBranchLength)
                        violations.Add($"ramo corto {branchLen} < min {_settings.MinBranchLength}");
                    else if (branchLen > _settings.MaxBranchLength)
                        violations.Add($"ramo lungo {branchLen} > max {_settings.MaxBranchLength}");
                }

                var severity = violations.Count > 0 ? DebugSeverity.Error : DebugSeverity.Info;
                var name = violations.Count > 0
                    ? $"Path {clusterIndex:D2} [{string.Join(", ", violations)}]"
                    : $"Path {clusterIndex:D2} OK ({cells.Count} tile, {branches.Count} rami)";

                results.Add(new DebugCluster(cells, edges) { Name = name, Severity = severity });
                clusterIndex++;
            }

            return results.ToArray();
        }

        // --- BFS helper (componente connessa per un dato flag) ---

        private static void BfsCluster(
            Vector2Int start,
            HexGridRegistry registry,
            IHexTopology topology,
            HexDebugFlags flag,
            HashSet<Vector2Int> visited,
            List<IHexCell> cells,
            List<HexGraphEdge> edges)
        {
            var queue = new Queue<IHexCell>();
            visited.Add(start);
            registry.TryGetTile(start, out var startTile);
            queue.Enqueue(startTile);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                cells.Add(current);

                foreach (var neighborCoord in topology.GetNeighbors(current.Coordinates))
                {
                    if (visited.Contains(neighborCoord)) continue;
                    if (!registry.TryGetTile(neighborCoord, out var neighbor)) continue;
                    if ((neighbor.Flags & flag) == 0) continue;

                    visited.Add(neighborCoord);
                    queue.Enqueue(neighbor);
                    edges.Add(new HexGraphEdge(current, neighbor));
                }
            }
        }

        // --- Decomposizione in rami tramite nodi critici (grado != 2) ---

        private static List<List<Vector2Int>> FindBranches(
            HashSet<Vector2Int> coordSet, IHexTopology topology)
        {
            // Gradi dei nodi all'interno del cluster
            var degree = new Dictionary<Vector2Int, int>();
            foreach (var c in coordSet)
                degree[c] = topology.GetNeighbors(c).Count(n => coordSet.Contains(n));

            bool IsCritical(Vector2Int c) => degree[c] != 2;

            // Caso degenere: anello o singola cella
            if (!coordSet.Any(IsCritical))
                return new List<List<Vector2Int>> { new List<Vector2Int>(coordSet) };

            var usedEdges = new HashSet<(Vector2Int, Vector2Int)>();
            var branches = new List<List<Vector2Int>>();

            foreach (var start in coordSet.Where(IsCritical))
            {
                foreach (var firstStep in topology.GetNeighbors(start))
                {
                    if (!coordSet.Contains(firstStep)) continue;
                    if (usedEdges.Contains((start, firstStep)) ||
                        usedEdges.Contains((firstStep, start))) continue;

                    var branch = new List<Vector2Int> { start };
                    var prev = start;
                    var curr = firstStep;

                    while (true)
                    {
                        usedEdges.Add((prev, curr));
                        usedEdges.Add((curr, prev));
                        branch.Add(curr);

                        if (IsCritical(curr)) break;

                        var next = topology.GetNeighbors(curr)
                            .FirstOrDefault(n => coordSet.Contains(n) && n != prev);
                        if (next == default) break;

                        prev = curr;
                        curr = next;
                    }

                    branches.Add(branch);
                }
            }

            return branches;
        }
    }
}
