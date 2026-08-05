using System;
using hp55games.Mobile.Core.Config;
using UnityEngine;

namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    /// <summary>
    /// Coppia TileType/DifficultyLevel. Dentro EventSingleTilesList e StopSingleTilesList
    /// e' un'ISTANZA da piazzare, non un tipo eleggibile per un draw ripetuto (revisione
    /// 2026-08-05: composizione fissa e deterministica, decisione confermata 2026-07-24).
    /// Per piazzare 3 Enemy DifficultyLevel 2 in una run, l'array contiene 3 entry
    /// identiche {Type = Enemy, DifficultyLevel = 2} — ciascuna viene consumata una volta
    /// sola quando il generatore la piazza (vedi AestheticClusterMapGenerator.TryDrawEntry).
    /// Se la posizione scelta a runtime non ha abbastanza vicini validi per il
    /// DifficultyLevel dell'entry, il DifficultyLevel viene abbassato fino a raggiungere
    /// quello supportato dalla posizione (minimo 1), stesso tipo — vedi
    /// AestheticClusterMapGenerator.ResolveDifficulty.
    /// Dentro EventClusterTilesList il significato resta diverso: e' solo la palette
    /// offerta all'autore nello Shape Editor per dipingere le celle di una
    /// EventClusterShape — quella lista non viene mai ripescata a runtime dal generatore.
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
    /// vengono popolate a mano: il generatore le tratta come "nessuna istanza da piazzare"
    /// e salta quella fase con un warning, non crasha.
    /// StopSingleTilesList vuota fa ripiegare il generatore sul vecchio comportamento
    /// piatto (TileType.Void, DifficultyLevel 0) per la separazione dei PathCluster, cosi'
    /// un LevelConfig parzialmente autorato resta comunque generabile.
    /// EventSingleTilesList e StopSingleTilesList vengono mescolate una volta a inizio
    /// generazione (stesso Random(seed) della run) e consumate: ogni entry e' piazzata al
    /// massimo una volta per run, mai ripescata — vedi LevelTileEntry.
    /// </summary>
    [CreateAssetMenu(menuName = "MapGame/Level Config", fileName = "LevelConfig")]
    public sealed class LevelConfig : ScriptableObject, IConfigAsset
    {
        [Tooltip("Bioma del livello. Testo libero finche' non esiste un catalogo/enum Bioma formale (Three-Axis Visual Model, design-only ad oggi).")]
        public string EnvType;

        [Header("Eleggibili per le tessere EventCluster (autoria manuale nello Shape Editor)")]
        public LevelTileEntry[] EventClusterTilesList;

        [Header("Istanze da piazzare come tessere singole isolate (1-tile EventCluster) — una entry = una tile, consumata al piazzamento")]
        public LevelTileEntry[] EventSingleTilesList;

        [Header("Istanze da piazzare come separatori tra PathCluster (ex Neutra piatta) — una entry = una tile, consumata al piazzamento")]
        public LevelTileEntry[] StopSingleTilesList;
    }
}
