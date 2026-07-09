using hp55games.Tools.HexDebugFramework;
using hp55games.MapGame.Features.Gameplay.HexGrid;
using UnityEngine;

namespace hp55games.MapGame.Features.HexDebugAdapter
{
    /// <summary>
    /// Adapter IHexCell per HexTileData di MapGame.
    /// Codifica TileType e IsObjective in HexDebugFlags usando il mapping MapGameDebugFlags.
    /// Espone PathClusterId ed EventPlacementId direttamente da HexTileData — usati dagli
    /// analyzer MapGame tramite downcast, non fanno parte di IHexCell.
    /// </summary>
    public sealed class HexTileDebugCell : IHexCell
    {
        public Vector2Int Coordinates { get; }
        public Vector3 WorldPosition { get; }
        public HexDebugFlags Flags { get; }

        /// <summary>
        /// ID del PathCluster assegnato da AestheticClusterMapGenerator. -1 = nessun cluster.
        /// </summary>
        public int PathClusterId { get; }

        /// <summary>
        /// ID del piazzamento EventCluster assegnato da AestheticClusterMapGenerator. -1 = nessun placement.
        /// </summary>
        public int EventPlacementId { get; }

        public HexTileDebugCell(HexTileData data, Vector3 worldPosition, bool isStart = false)
        {
            Coordinates = new Vector2Int(data.Coord.Q, data.Coord.R);
            WorldPosition = worldPosition;

            var flags = MapGameDebugFlags.FromTileType(data.Type);
            if (data.IsObjective) flags |= HexDebugFlags.PointOfInterest;
            if (isStart)         flags |= MapGameDebugFlags.StartTile;

            Flags = flags;

            PathClusterId    = data.PathClusterId;
            EventPlacementId = data.EventPlacementId;
        }
    }
}
