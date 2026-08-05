using System;

namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    /// <summary>
    /// Tipi di tile come da GDD (sezione "Tile Types"), revisione 2026-07-25 (fonte: Map
    /// game Franci GDD, priorita' massima). Tile-state-agnostico: ogni contenuto logico
    /// che una tile puo' avere e' un membro. Valori int espliciti: non riordinare mai,
    /// non riusare un valore libero per un membro nuovo — gli asset .asset serializzati
    /// puntano all'int, non al nome.
    ///
    /// Void e Road non hanno DifficultyLevel (strutturali, non content). Tutti gli altri
    /// tipi ricevono un DifficultyLevel (1-6) per-istanza da LevelConfig, vedi
    /// HexTileData.DifficultyLevel e AestheticClusterMapGenerator.
    ///
    /// La run ha un numero fisso e determinato di Element per tipo, definito nel
    /// LevelConfig — niente casualita' a runtime sulla composizione. Chance resta
    /// [Obsolete] solo per compatibilita' editor/debug esistenti, non usarlo in codice
    /// nuovo.
    ///
    /// Revisione 2026-07-25: Path rinominato Road (stesso int, alias mantenuto
    /// [Obsolete] perche' altri file — AestheticClusterMapGenerator, MapGenerationConfigEditor,
    /// HP55_EventClusterShapeEditor, MapGameDebugFlags — referenziano ancora TileType.Path
    /// come simbolo C#: rimuoverlo del tutto avrebbe rotto la compilazione fuori dallo
    /// scope di questa consegna. Stesso trattamento per Shop→Trader.
    /// Miniboss/Boss/Npc/Goods eliminati come TileType a se stanti (Miniboss/Boss diventano
    /// varianti di Enemy via ElementConfig; Npc si divide in Inn/Trader/Witch/Farmer/Hunter;
    /// Goods si divide in Bush/BeeHive/TurnipSprout, Tree resta) — marcati [Obsolete] con
    /// int originali preservati, mai rimossi ne' riassegnati, per non corrompere gli .asset
    /// gia' serializzati che li referenziano.
    ///
    /// Key: attiva la chiave di sessione corrispondente al proprio DifficultyLevel
    /// (4/5/6). Chest: DifficultyLevel 1/2/3, aperto dalla chiave DL+3.
    /// </summary>
    public enum TileType
    {
        Void = 0,

        [Obsolete("Path e' stato rinominato Road (2026-07-25). Alias mantenuto solo per compatibilita' con codice non ancora aggiornato — non usarlo in codice nuovo, usa Road.")]
        Path = 1,
        Road = 1,

        [Obsolete("Boss eliminato dal design (2026-07-25): la tile obiettivo e' un Enemy con IsObjective = true, differenziato via ElementConfig. Mantenuto solo per compatibilita' con .asset serializzati esistenti.")]
        Boss = 2,

        [Obsolete("Npc eliminato dal design (2026-07-25): si divide in Inn/Trader/Witch/Farmer/Hunter. Mantenuto solo per compatibilita' con .asset serializzati esistenti.")]
        Npc = 3,

        [Obsolete("Shop e' stato rinominato Trader (2026-07-25). Alias mantenuto solo per compatibilita' con codice non ancora aggiornato — non usarlo in codice nuovo, usa Trader.")]
        Shop = 4,
        Trader = 4,

        Enemy = 5,

        [Obsolete("Miniboss eliminato dal design (2026-07-25): e' una variante di Enemy via ElementConfig (specie/estetica diverse, stesso Type=Enemy), bilanciamento non ancora definito. Mantenuto solo per compatibilita' con .asset serializzati esistenti.")]
        Miniboss = 6,

        [Obsolete("Goods eliminato dal design (2026-07-25): si divide in Bush/BeeHive/TurnipSprout (Tree gia' esisteva a parte). Mantenuto solo per compatibilita' con .asset serializzati esistenti.")]
        Goods = 7,

        [Obsolete("Chance e' stato eliminato dal design (2026-07-22): i suoi esiti sono ora Element propri (Trap, Fountain, Tree, Chest). Mantenuto solo per compatibilita' con editor/debug esistenti.")]
        Chance = 8,

        Trap = 9,
        Fountain = 10,
        Tree = 11,
        Key = 12,
        Chest = 13,

        Bush = 14,
        BeeHive = 15,
        TurnipSprout = 16,
        MoneyBag = 17,
        Inn = 18,
        Witch = 19,
        Farmer = 20,
        Hunter = 21
    }
}