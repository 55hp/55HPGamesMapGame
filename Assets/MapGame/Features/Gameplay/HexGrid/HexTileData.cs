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
        /// Delta HP applicato al reveal: negativo danneggia (Enemy), zero per gli altri
        /// tipi. Goods non restituisce più HP direttamente dal 2026-07-10, vedi
        /// FoodRestore. La tabella esiti di Chance (che assorbe il vecchio Trappola come
        /// uno dei possibili risultati) non e' ancora implementata: Chance resta a
        /// impatto zero finche' non viene definita — vedi nota in
        /// AestheticClusterMapGenerator.ApplyPlaceholderBalance.
        /// </summary>
        public int HpRestore;

        /// <summary>
        /// Cibo guadagnato al reveal, aggiunto alla scorta (clamp 0..MaxFood). Assegnato
        /// solo a Goods, sostituisce la cura HP diretta che Goods dava prima del
        /// 2026-07-10 — ora Goods rifornisce la scorta di cibo che il costo movimento
        /// consuma, invece di curare sul colpo. Zero per tutti gli altri tipi.
        /// </summary>
        public int FoodRestore;

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
        /// 0 per Path e Void, che non hanno DifficultyLevel (strutturali, non content).
        /// </summary>
        public int DifficultyLevel;

        public HexTileData(HexCoord coord)
        {
            Coord = coord;
            State = TileState.Sconosciuta;
            Type = TileType.Path;
            HpRestore = 0;
            FoodRestore = 0;
            IsObjective = false;
            PathClusterId = -1;
            EventPlacementId = -1;
            DifficultyLevel = 0;
        }
    }
}
