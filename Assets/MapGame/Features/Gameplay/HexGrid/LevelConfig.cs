using System;
using UnityEngine;

namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    /// <summary>
    /// Coppia TileType/DifficultyLevel eleggibile per un ruolo di piazzamento
    /// (EventCluster, EventSingle, o StopSingle). Il piazzamento sceglie una entry
    /// a caso dalla lista appropriata; se la posizione non ha abbastanza vicini validi
    /// per il DifficultyLevel dell'entry scelta, il DifficultyLevel viene abbassato fino
    /// a raggiungere quello supportato dalla posizione (minimo 1), stesso tipo, vedi
    /// AestheticClusterMapGenerator.PickEntry.
    /// </summary>
    [Serializable]
    public struct LevelTileEntry
    {
        public TileType Type;
        [Range(1, 6)] public int DifficultyLevel;
    }

    /// <summary>
    /// Configurazione di contenuto per missione/livello. Revisione 2026-07-10, sostituisce
    /// il vecchio sistema Pool A / Pool B a due pool globali fissi — vedi Architecture
    /// Decisions Log e la Implementation Spec "Level Content System".
    ///
    /// Vive in una propria sottocartella insieme alle EventClusterShape autorate per quel
    /// livello (es. Assets/MapGame/Content/Levels/NomeLivello/).
    ///
    /// EventClusterTilesList e EventSingleTilesList possono restare vuote finche' non
    /// vengono popolate a mano: il generatore le tratta come "nessun contenuto eleggibile"
    /// e salta quella fase con un warning, non crasha.
    /// StopSingleTilesList vuota fa ripiegare il generatore sul vecchio comportamento
    /// piatto (TileType.Void, DifficultyLevel 0) per la separazione dei PathCluster, cosi'
    /// un LevelConfig parzialmente autorato resta comunque generabile.
    /// </summary>
    [CreateAssetMenu(menuName = "MapGame/Level Config", fileName = "LevelConfig")]
    public sealed class LevelConfig : ScriptableObject
    {
        [Tooltip("Bioma del livello. Testo libero finche' non esiste un catalogo/enum Bioma formale (Three-Axis Visual Model, design-only ad oggi).")]
        public string EnvType;

        [Header("Eleggibili per le tessere EventCluster (autoria manuale nello Shape Editor)")]
        public LevelTileEntry[] EventClusterTilesList;

        [Header("Eleggibili per le tessere singole isolate (1-tile EventCluster)")]
        public LevelTileEntry[] EventSingleTilesList;

        [Header("Eleggibili per le tessere che separano i PathCluster (ex Neutra piatta)")]
        public LevelTileEntry[] StopSingleTilesList;
    }
}
