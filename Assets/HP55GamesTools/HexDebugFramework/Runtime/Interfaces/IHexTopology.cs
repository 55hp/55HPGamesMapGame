using System.Collections.Generic;
using UnityEngine;

namespace hp55games.Tools.HexDebugFramework
{
    public interface IHexTopology
    {
        IEnumerable<Vector2Int> GetNeighbors(Vector2Int coordinates);
        bool AreAdjacent(Vector2Int a, Vector2Int b);
        int Distance(Vector2Int a, Vector2Int b);
    }
}
