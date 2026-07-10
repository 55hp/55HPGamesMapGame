using System.Collections.Generic;
using UnityEngine;

namespace hp55games.Tools.HexDebugFramework
{
    public sealed class DebugCluster
    {
        public string Name { get; set; }
        public Color Color { get; set; }
        public DebugSeverity Severity { get; set; }
        public IReadOnlyList<IHexCell> Cells { get; }
        public IReadOnlyCollection<HexGraphEdge> Edges { get; }

        public DebugCluster(IReadOnlyList<IHexCell> cells, IReadOnlyCollection<HexGraphEdge> edges)
        {
            Cells = cells;
            Edges = edges;
        }
    }
}
