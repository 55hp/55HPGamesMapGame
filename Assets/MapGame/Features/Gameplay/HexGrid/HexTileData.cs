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
        public ExplorationState Exploration;
        public SpottingState Spotting;

        /// <summary>
        /// Default Void, non Road: una tile appena costruita non ha ancora contenuto
        /// esplicito (assegnato entro la fine di Generate). Se il default fosse Road,
        /// le tile ancora intoccate durante PlaceEventClusters/GeneratePathClusterMesh
        /// sembrerebbero gia' Strada a WouldViolateConsecutiveSides, bloccando quasi ogni
        /// crescita di PathCluster con un "blob" fantasma di Road. Void e' anche gia' il
        /// fallback esplicito di AssignSingleTile/AssignStopTile quando i manifest sono
        /// esauriti.
        /// </summary>
        public TileType Type;

        /// <summary>
        /// Delta HP applicato al reveal: negativo danneggia (Trap, Enemy via
        /// ApplyEnemyDamage). Fountain non usa questo campo: ripristina tutti gli HP via
        /// logica dedicata in HexGridController. Zero per gli altri tipi.
        /// </summary>
        public int HpRestore;

        /// <summary>
        /// Cibo guadagnato al reveal, aggiunto alla scorta (clamp 0..MaxFood). Assegnato
        /// ai tile di raccolta cibo (Bush/BeeHive/TurnipSprout/Tree), valore per specie da
        /// ElementConfig.FoodRestore (Bush=1, BeeHive=2, TurnipSprout=2, Tree=3). Zero per
        /// tutti gli altri tipi.
        /// </summary>
        public int FoodRestore;

        /// <summary>
        /// Monete guadagnate al reveal. Il combattimento (Enemy) calcola la sua ricompensa
        /// a parte in ResolveEncounterFight (Coins += DifficultyLevel), non da questo
        /// campo. Zero per tutti gli altri tipi salvo casi futuri.
        /// </summary>
        public int MoneteGained;

        /// <summary>
        /// True sulla tile obiettivo di missione (Enemy generato da PlaceEnd).
        /// </summary>
        public bool IsObjective;

        /// <summary>
        /// True se la tile Void ospita un ambiente Lake/Sea/Pond — richiesto dall'item
        /// Fishing Rod per determinare se puo' essere usato qui. Non ancora popolato da
        /// nessun generatore (env oggi e' solo estetico/SpriteRenderer); default false.
        /// </summary>
        public bool IsFishable;

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
        /// 0 per Road e Void, che non hanno DifficultyLevel (strutturali, non content).
        /// </summary>
        public int DifficultyLevel;

        public HexTileData(HexCoord coord)
        {
            Coord = coord;
            Exploration = ExplorationState.Unexplored;
            Spotting = SpottingState.Unspotted;
            Type = TileType.Void;
            HpRestore = 0;
            FoodRestore = 0;
            IsObjective = false;
            IsFishable = false;
            PathClusterId = -1;
            EventPlacementId = -1;
            DifficultyLevel = 0;
        }
    }
}