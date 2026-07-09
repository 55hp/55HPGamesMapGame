using UnityEngine;

namespace hp55games.FranzTools.HexDebugFramework
{
    public sealed class HexDebugData : IHexCell
    {
        public Vector2Int Coordinates { get; }
        public Vector3 WorldPosition { get; }
        public HexDebugFlags Flags { get; }

        public HexDebugData(Vector2Int coordinates, Vector3 worldPosition, HexDebugFlags flags)
        {
            Coordinates = coordinates;
            WorldPosition = worldPosition;
            Flags = flags;
        }
    }
}
