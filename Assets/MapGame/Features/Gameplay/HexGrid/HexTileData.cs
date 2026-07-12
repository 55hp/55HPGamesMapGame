using System;

namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    /// <summary>
    /// Dati puri di una tile. Nessuna logica di rendering qui.
    /// </summary>
    [Serializable]
    public sealed class HexTileData
    {
        public HexCoord Coord;
        public TileState State;
        public TileType Type;

        /// <summary>
        /// Delta HP applicato al reveal: positivo cura (Risorsa), negativo danneggia (Enemy),
        /// zero per gli altri tipi. La tabella esiti di Mistery (che assorbe il vecchio
        /// Trappola come uno dei possibili risultati) non e' ancora implementata: Mistery
        /// resta a impatto zero finche' non viene definita — vedi nota in
        /// AestheticClusterMapGenerator.ApplyPlaceholderBalance.
        /// </summary>
        public int HpRestore;

        /// <summary>
        /// Monete guadagnate al reveal. Assegnate solo a Enemy (combattimento vinto).
        /// Zero per tutti gli altri tipi. Il valore balance è responsabilità del designer.
        /// </summary>
        public int MoneteGained;

        /// <summary>
        /// True sulla tile Boss, l'obiettivo di missione generato da PlaceEnd.
        /// </summary>
        public bool IsObjective;

        /// <summary>
        /// Indice del PathCluster a cui appartiene questa tile (assegnato da AestheticClusterMapGenerator).
        /// -1 significa che la tile non appartiene a nessun PathCluster.
        /// </summary>
        public int PathClusterId = -1;

        /// <summary>
        /// Indice del piazzamento EventCluster o tessera singola a cui appartiene questa tile.
        /// -1 significa che la tile non appartiene a nessun EventPlacement.
        /// </summary>
        public int EventPlacementId = -1;

        /// <summary>
        /// Livello di difficolta' 1-6, assegnato dalla entry LevelConfig scelta al momento
        /// del piazzamento. Fa doppio lavoro: vincolo di piazzamento (una tile con
        /// DifficultyLevel D richiede almeno D vicini validi in griglia, vedi
        /// AestheticClusterMapGenerator.NeighborCount) e soglia di rivelazione icona (vedi
        /// HexGridController.CountScopertaNeighbors / HexGridViewSpawner).
        /// 0 per Strada e Void, che non hanno DifficultyLevel (strutturali, non content).
        /// </summary>
        public int DifficultyLevel;

        public HexTileData(HexCoord coord)
        {
            Coord = coord;
            State = TileState.Sconosciuta;
            Type = TileType.Path;
            HpRestore = 0;
            IsObjective = false;
            PathClusterId = -1;
            EventPlacementId = -1;
            DifficultyLevel = 0;
        }
    }
}
