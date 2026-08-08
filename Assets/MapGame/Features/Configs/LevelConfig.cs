using System;
using System.Collections.Generic;
using hp55games.Mobile.Core.Config;
using UnityEngine;

namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    /// <summary>
    /// A quale delle tre liste logiche del generatore appartiene una LevelTileEntry
    /// (2026-08-07, sostituisce i tre array separati EventClusterTilesList/
    /// EventSingleTilesList/StopSingleTilesList di LevelConfig — un'unica List
    /// Entries, filtrata per ListType invece che tenuta in contenitori diversi).
    /// </summary>
    public enum TileListType
    {
        /// <summary>Palette per lo Shape Editor (dipingere celle di una EventClusterShape). Mai ripescata a runtime dal generatore — vedi HP55_EventClusterShapeEditor.</summary>
        EventCluster,

        /// <summary>Istanze piazzabili come EventCluster a 1 tile o come ripiego del rammendo — vedi AestheticClusterMapGenerator.AssignSingleTile. Unico ListType dove Amount conta.</summary>
        EventSingle,

        /// <summary>Istanze piazzabili come separatori tra PathCluster diversi — vedi AestheticClusterMapGenerator.AssignStopTile. Amount ignorato.</summary>
        StopSingle,
    }

    /// <summary>
    /// Coppia TileType/DifficultyLevel con ListType (a quale delle tre liste logiche del
    /// generatore appartiene, vedi TileListType) e Amount (quante volte piazzarla — SOLO
    /// per ListType.EventSingle, vedi sotto).
    ///
    /// Per ListType.StopSingle, per piazzare 3 Enemy DifficultyLevel 2 in una run, servono
    /// ancora 3 entry identiche {Type=Enemy, DifficultyLevel=2, ListType=StopSingle} —
    /// ciascuna consumata una volta sola quando il generatore la piazza (vedi
    /// AestheticClusterMapGenerator.TryDrawEntry). Per ListType.EventSingle invece la
    /// stessa cosa si ottiene con UNA sola entry e Amount=3: BuildManifest la espande in 3
    /// copie prima dello shuffle — vedi AestheticClusterMapGenerator.BuildManifest.
    ///
    /// Se la posizione scelta a runtime non ha abbastanza vicini validi per il
    /// DifficultyLevel dell'entry, il DifficultyLevel viene abbassato fino a raggiungere
    /// quello supportato dalla posizione (minimo 1), stesso tipo — vedi
    /// AestheticClusterMapGenerator.ResolveDifficulty. Questo vale per ISTANZA (ogni copia
    /// espansa da Amount e' valutata separatamente, non in blocco).
    ///
    /// ListType.EventCluster e' un caso a parte: qui Type/DifficultyLevel sono solo la
    /// palette offerta all'autore nello Shape Editor per dipingere le celle di una
    /// EventClusterShape — quella entry non viene mai ripescata a runtime dal generatore,
    /// e Amount non ha alcun effetto li'.
    /// </summary>
    [Serializable]
    public struct LevelTileEntry
    {
        public TileType Type;

        [Tooltip("A quale delle tre liste logiche del generatore appartiene questa entry — vedi TileListType. Sostituisce la vecchia divisione in tre array separati su LevelConfig.")]
        public TileListType ListType;

        [Range(1, 6)] public int DifficultyLevel;

        [Min(1)]
        [Tooltip("Quante volte questa entry va piazzata in una run. Valido SOLO per ListType.EventSingle — su EventCluster e StopSingle il generatore lo ignora (li' resta un'entry = un'istanza, o nessuna istanza per la palette EventCluster). Default 1: comportamento identico a prima dell'introduzione del campo.")]
        public int Amount;
    }

    /// <summary>
    /// Configurazione di contenuto per missione/livello. Revisione 2026-07-10, sostituisce
    /// il vecchio sistema Pool A / Pool B a due pool globali fissi — vedi Architecture
    /// Decisions Log e la Implementation Spec "Level Content System". Revisione 2026-08-07:
    /// i tre array EventClusterTilesList/EventSingleTilesList/StopSingleTilesList sono
    /// stati sostituiti da un'unica List Entries, ciascuna entry marcata con ListType (vedi
    /// TileListType) invece che tenuta in un contenitore diverso per ciascuna lista — un
    /// solo posto dove aggiungere/rimuovere contenuto, il generatore filtra per ListType
    /// dove serve (vedi AestheticClusterMapGenerator.BuildManifest).
    ///
    /// Vive in una propria sottocartella insieme alle EventClusterShape autorate per quel
    /// livello (es. Assets/MapGame/Content/Levels/NomeLivello/).
    ///
    /// Entries puo' restare vuota finche' non viene popolata a mano: il generatore tratta
    /// ogni ListType senza entry come "nessuna istanza da piazzare" e degrada (warning per
    /// EventCluster/EventSingle, ripiego su TileType.Void per StopSingle — vedi
    /// AssignSingleTile/AssignStopTile), non crasha.
    /// Le entry con ListType.EventSingle o ListType.StopSingle vengono mescolate una volta
    /// a inizio generazione (stesso Random(seed) della run) e consumate: ciascuna e'
    /// piazzata al massimo una volta per run, mai ripescata — vedi LevelTileEntry. Per
    /// EventSingle l'espansione per Amount avviene PRIMA dello shuffle: un'entry con
    /// Amount=3 diventa 3 copie indipendenti nel manifest, ciascuna piazzata al massimo una
    /// volta come le altre — la sequenza resta comunque interamente derivata dal seed (vedi
    /// AestheticClusterMapGenerator.BuildManifest).
    /// </summary>
    [CreateAssetMenu(menuName = "MapGame/Level Config", fileName = "LevelConfig")]
    public sealed class LevelConfig : ScriptableObject, IConfigAsset
    {
        [Tooltip("Bioma del livello. Testo libero finche' non esiste un catalogo/enum Bioma formale (Three-Axis Visual Model, design-only ad oggi).")]
        public string EnvType;

        [Tooltip("Tutte le entry di contenuto del livello, di qualunque ListType (EventCluster/EventSingle/StopSingle — vedi TileListType). Il generatore filtra per ListType dove serve: HP55_EventClusterShapeEditor legge solo le entry EventCluster, AestheticClusterMapGenerator costruisce i manifest EventSingle/StopSingle dalle rispettive entry (vedi BuildManifest). Amount conta solo per le entry EventSingle.")]
        public List<LevelTileEntry> Entries;
    }
}
