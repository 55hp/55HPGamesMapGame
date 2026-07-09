using hp55games.FranzTools.HexDebugFramework;
using hp55games.MapGame.Features.Gameplay.HexGrid;
using UnityEngine;

namespace hp55games.MapGame.Features.Debug
{
    /// <summary>
    /// Adapter IHexCell per HexTileData di MapGame.
    /// Codifica TileType e IsObjective in HexDebugFlags usando il mapping MapGameDebugFlags.
    /// Usato da MapGameRegistryPopulator per costruire HexGridRegistry da HexGridController.Tiles.
    /// </summary>
    public sealed class HexTileDebugCell : IHexCell
    {
        public Vector2Int Coordinates { get; }
        public Vector3 WorldPosition { get; }
        public HexDebugFlags Flags { get; }

        public HexTileDebugCell(HexTileData data, Vector3 worldPosition, bool isStart = false)
        {
            Coordinates = new Vector2Int(data.Coord.Q, data.Coord.R);
            WorldPosition = worldPosition;

            var flags = MapGameDebugFlags.FromTileType(data.Type);
            if (data.IsObjective) flags |= HexDebugFlags.PointOfInterest;
            if (isStart)         flags |= MapGameDebugFlags.StartTile;

            Flags = flags;
        }
    }
}
