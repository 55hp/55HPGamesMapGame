using System.Collections.Generic;
using hp55games.Tools.HexDebugFramework;

namespace hp55games.MapGame.Features.HexDebugAdapter
{
    /// <summary>
    /// Verifica che nessun PathCluster sia direttamente adiacente a un PathCluster diverso.
    /// Usa PathClusterId assegnato da AestheticClusterMapGenerator — nessun BFS interno,
    /// nessun rischio di fondere i cluster prima del controllo.
    /// Segnala come Error ogni tile Strada il cui vicino Strada appartiene a un cluster diverso.
    /// </summary>
    public sealed class NeutraSeparationAnalyzer : IHexAnalyzer
    {
        public string Name => "Neutra Separation";

        public DebugCluster[] Analyze(HexGridRegistry registry, IHexTopology topology)
        {
            var violationCells = new List<IHexCell>();

            foreach (var tile in registry.AllTiles)
            {
                if ((tile.Flags & HexDebugFlags.Road) == 0) continue;
                if (!(tile is HexTileDebugCell own) || own.PathClusterId < 0) continue;

                foreach (var neighborCoord in topology.GetNeighbors(tile.Coordinates))
                {
                    if (!registry.TryGetTile(neighborCoord, out var neighbor)) continue;
                    if ((neighbor.Flags & HexDebugFlags.Road) == 0) continue;
                    if (!(neighbor is HexTileDebugCell other) || other.PathClusterId < 0) continue;

                    if (other.PathClusterId != own.PathClusterId && !violationCells.Contains(tile))
                        violationCells.Add(tile);
                }
            }

            if (violationCells.Count == 0)
                return new[] { new DebugCluster(new List<IHexCell>(), new List<HexGraphEdge>())
                    { Name = "Neutra Separation OK", Severity = DebugSeverity.Info } };

            return new[] { new DebugCluster(violationCells, new List<HexGraphEdge>())
                { Name = $"Neutra Separation: {violationCells.Count} violazioni", Severity = DebugSeverity.Error } };
        }
    }
}
