using System.Collections.Generic;
using hp55games.Tools.HexDebugFramework;
using hp55games.MapGame.Features.Gameplay.HexGrid;
using UnityEngine;

namespace hp55games.MapGame.Features.HexDebugAdapter
{
    /// <summary>
    /// Adapter IHexTopology per MapGame, schema Odd-Q flat-top.
    /// Delega interamente la matematica dei vicini a HexCoord per evitare
    /// qualsiasi disallineamento con la generazione mappa reale.
    /// Non modificare la matematica qui: se HexCoord cambia, questo cambia di conseguenza.
    /// </summary>
    public sealed class MapGameHexTopology : IHexTopology
    {
        public IEnumerable<Vector2Int> GetNeighbors(Vector2Int coordinates)
        {
            var coord = new HexCoord(coordinates.x, coordinates.y);
            for (int i = 0; i < 6; i++)
            {
                var neighbor = coord.GetNeighbor(i);
                yield return new Vector2Int(neighbor.Q, neighbor.R);
            }
        }

        public bool AreAdjacent(Vector2Int a, Vector2Int b)
        {
            var coordA = new HexCoord(a.x, a.y);
            var coordB = new HexCoord(b.x, b.y);
            return coordA.DistanceTo(coordB) == 1;
        }

        public int Distance(Vector2Int a, Vector2Int b)
        {
            var coordA = new HexCoord(a.x, a.y);
            var coordB = new HexCoord(b.x, b.y);
            return coordA.DistanceTo(coordB);
        }
    }
}
