using System.Collections.Generic;
using System.Linq;
using hp55games.FranzTools.HexDebugFramework;
using hp55games.MapGame.Features.Gameplay.HexGrid;
using UnityEngine;

namespace hp55games.MapGame.Features.Debug
{
    /// <summary>
    /// Esegue una BFS a movimento libero adiacente dalla tile Start verso End e verso
    /// ogni Risorsa, NPC, Boss, per confermare che tutta la mappa generata sia
    /// effettivamente raggiungibile. Requisito diretto dei pillar P2 e P3.
    /// Accede a HexGridController dalla scena per recuperare le coordinate di Start.
    /// </summary>
    public sealed class ReachabilityAnalyzer : IHexAnalyzer
    {
        private static readonly HexDebugFlags KeyTileFlags =
            MapGameDebugFlags.Boss          |
            MapGameDebugFlags.Risorsa       |
            MapGameDebugFlags.Npc           |
            HexDebugFlags.PointOfInterest;  // IsObjective

        public string Name => "Reachability";

        public DebugCluster[] Analyze(HexGridRegistry registry, IHexTopology topology)
        {
            // Recupera il controller dalla scena per ottenere la posizione di Start
            var controller = Object.FindObjectOfType<HexGridController>();
            if (controller == null)
            {
                return new[] { new DebugCluster(new List<IHexCell>(), new List<HexGraphEdge>())
                    { Name = "Reachability: nessun HexGridController in scena", Severity = DebugSeverity.Error } };
            }

            var startCoord = new Vector2Int(controller.PlayerCoord.Q, controller.PlayerCoord.R);

            if (!registry.TryGetTile(startCoord, out _))
            {
                return new[] { new DebugCluster(new List<IHexCell>(), new List<HexGraphEdge>())
                    { Name = "Reachability: Start non trovato nel registry", Severity = DebugSeverity.Error } };
            }

            // BFS libera da Start (tutte le tile adiacenti, senza filtro tipo)
            var reachable = new HashSet<Vector2Int>();
            var queue = new Queue<Vector2Int>();
            reachable.Add(startCoord);
            queue.Enqueue(startCoord);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var neighborCoord in topology.GetNeighbors(current))
                {
                    if (reachable.Contains(neighborCoord)) continue;
                    if (!registry.TryGetTile(neighborCoord, out _)) continue;

                    reachable.Add(neighborCoord);
                    queue.Enqueue(neighborCoord);
                }
            }

            // Tile chiave non raggiungibili
            var unreachable = registry.AllTiles
                .Where(t => (t.Flags & KeyTileFlags) != 0 && !reachable.Contains(t.Coordinates))
                .Cast<IHexCell>()
                .ToList();

            if (unreachable.Count == 0)
                return new[] { new DebugCluster(new List<IHexCell>(), new List<HexGraphEdge>())
                    { Name = $"Reachability OK ({reachable.Count} tile raggiungibili)", Severity = DebugSeverity.Info } };

            return new[] { new DebugCluster(unreachable, new List<HexGraphEdge>())
                { Name = $"Reachability: {unreachable.Count} tile non raggiungibili", Severity = DebugSeverity.Error } };
        }
    }
}
