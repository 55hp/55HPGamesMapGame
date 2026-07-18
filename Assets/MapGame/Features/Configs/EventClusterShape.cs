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

        /// <summary>
        /// Aggiunto 2026-07-10. Livello di difficolta' 1-6 autorato per questa cella nello
        /// Shape Editor, scelto tra le entry eleggibili di LevelConfig.EventClusterTilesList
        /// per quel Type. Vedi AestheticClusterMapGenerator.ApplyEventClusterShape per come
        /// viene poi adattato alla posizione finale in griglia (ResolveDifficulty).
        /// </summary>
        [Range(1, 6)] public int DifficultyLevel;
    }

    [CreateAssetMenu(menuName = "MapGame/Event Cluster Shape", fileName = "EventClusterShape")]
    public class EventClusterShape : ScriptableObject
    {
        public string ShapeName;
        public EventClusterTileSpec[] Tiles;
    }
}
