using System;
using UnityEngine;

namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    [Serializable]
    public struct EventClusterTileSpec
    {
        public int RelativeQ;
        public int RelativeR;
        public TileType Type;
    }

    [CreateAssetMenu(menuName = "MapGame/Event Cluster Shape", fileName = "EventClusterShape")]
    public class EventClusterShape : ScriptableObject
    {
        public string ShapeName;
        public EventClusterTileSpec[] Tiles;
    }
}