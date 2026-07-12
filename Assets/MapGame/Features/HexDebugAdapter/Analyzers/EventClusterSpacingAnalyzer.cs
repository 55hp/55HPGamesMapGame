using System.Collections.Generic;
using hp55games.Tools.HexDebugFramework;

namespace hp55games.MapGame.Features.HexDebugAdapter
{
    /// <summary>
    /// Verifica che tra due EventPlacement distinti esista almeno 1 tile di gap.
    /// Usa EventPlacementId assegnato da AestheticClusterMapGenerator — nessun BFS interno,
    /// nessun rischio di fondere placement prima del controllo.
    /// Segnala come Error ogni tile evento il cui vicino evento appartiene a un placement diverso.
    /// EventFlags aggiornato 2026-07-10 per il TileType refactor: Battaglia -> Enemy,
    /// Trappola rimosso (confluito in Mistery), aggiunti Shop e Miniboss.
    /// </summary>
    public sealed class EventClusterSpacingAnalyzer : IHexAnalyzer
    {
        private static readonly HexDebugFlags EventFlags =
            MapGameDebugFlags.Enemy    |
            MapGameDebugFlags.Shop     |
            MapGameDebugFlags.Risorsa  |
            MapGameDebugFlags.Npc      |
            MapGameDebugFlags.Mistery  |
            MapGameDebugFlags.Miniboss |
            MapGameDebugFlags.Boss;

        public string Name => "Event Cluster Spacing";

        public DebugCluster[] Analyze(HexGridRegistry registry, IHexTopology topology)
        {
            var violationCells = new List<IHexCell>();

            foreach (var tile in registry.AllTiles)
            {
                if ((tile.Flags & EventFlags) == 0) continue;
                if (!(tile is HexTileDebugCell own) || own.EventPlacementId < 0) continue;

                foreach (var neighborCoord in topology.GetNeighbors(tile.Coordinates))
                {
                    if (!registry.TryGetTile(neighborCoord, out var neighbor)) continue;
                    if ((neighbor.Flags & EventFlags) == 0) continue;
                    if (!(neighbor is HexTileDebugCell other) || other.EventPlacementId < 0) continue;

                    if (other.EventPlacementId != own.EventPlacementId && !violationCells.Contains(tile))
                        violationCells.Add(tile);
                }
            }

            if (violationCells.Count == 0)
                return new[] { new DebugCluster(new List<IHexCell>(), new List<HexGraphEdge>())
                    { Name = "Event Spacing OK", Severity = DebugSeverity.Info } };

            return new[] { new DebugCluster(violationCells, new List<HexGraphEdge>())
                { Name = $"Event Spacing: {violationCells.Count} violazioni", Severity = DebugSeverity.Error } };
        }
    }
}
