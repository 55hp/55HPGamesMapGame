using UnityEngine;

namespace hp55games.FranzTools.HexDebugFramework
{
    public interface IHexCell
    {
        Vector2Int Coordinates { get; }
        Vector3 WorldPosition { get; }
        HexDebugFlags Flags { get; }
    }
}
