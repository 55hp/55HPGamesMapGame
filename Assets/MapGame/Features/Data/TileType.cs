using System;

namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    /// <summary>
    /// Tipi di tile come da GDD (sezione "Tile Types"). Tile-state-agnostico: ogni
    /// contenuto logico che una tile puo' avere e' un membro. Valori int espliciti: non
    /// riordinare mai, non riusare un valore libero per un membro nuovo — gli asset
    /// .asset serializzati puntano all'int, non al nome.
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
    /// Path e' l'alias [Obsolete] di Road (stesso int), mantenuto solo per compatibilita'
    /// con .asset serializzati esistenti e codice non ancora aggiornato — nessun file del
    /// progetto referenzia piu' TileType.Path come simbolo C# (MapGameDebugFlags e' stato
    /// allineato a TileType.Road). Stesso trattamento per Shop→Trader.
    /// Miniboss/Boss/Npc/Goods non sono piu' TileType a se stanti
    /// (Miniboss/Boss diventano varianti di Enemy con DifficultyLevel piu' alto; Npc si
    /// divide in Inn/Trader/Witch/Farmer/Hunter; Goods si divide in Bush/BeeHive/
    /// TurnipSprout, Tree resta) — marcati [Obsolete] con int originali preservati, mai
    /// rimossi ne' riassegnati, per non corrompere gli .asset gia' serializzati che li
    /// referenziano.
    ///
    /// Key: attiva la chiave di sessione corrispondente al proprio DifficultyLevel
    /// (4/5/6). Chest: DifficultyLevel 1/2/3, aperto dalla chiave DL+3.
    /// </summary>
    public enum TileType
    {
        Void = 0,

        Road = 1,
        [Obsolete("Path e' l'alias di Road. Mantenuto solo per compatibilita' con codice non ancora aggiornato — non usarlo in codice nuovo, usa Road. Dichiarato DOPO Road cosi' Unity mostra \"Road\" nell'Inspector per il valore 1, non \"Path\": C#/Unity risolvono il nome da un int all'ordine di dichiarazione, non il contrario — nessun valore e' cambiato, solo l'ordine.")]
        Path = 1,

        [Obsolete("Boss non e' piu' un TileType a se': la tile obiettivo e' un Enemy con IsObjective = true. Mantenuto solo per compatibilita' con .asset serializzati esistenti.")]
        Boss = 2,

        [Obsolete("Npc non e' piu' un TileType a se': si divide in Inn/Trader/Witch/Farmer/Hunter. Mantenuto solo per compatibilita' con .asset serializzati esistenti.")]
        Npc = 3,

        Trader = 4,
        [Obsolete("Shop e' l'alias di Trader. Mantenuto solo per compatibilita' con codice non ancora aggiornato — non usarlo in codice nuovo, usa Trader. Dichiarato DOPO Trader cosi' Unity mostra \"Trader\" nell'Inspector per il valore 4, non \"Shop\" — nessun valore e' cambiato, solo l'ordine.")]
        Shop = 4,

        Enemy = 5,

        [Obsolete("Miniboss non e' piu' un TileType a se': e' un Enemy con DifficultyLevel piu' alto (stesso Type=Enemy), bilanciamento non ancora definito. Mantenuto solo per compatibilita' con .asset serializzati esistenti.")]
        Miniboss = 6,

        [Obsolete("Goods non e' piu' un TileType a se': si divide in Bush/BeeHive/TurnipSprout (Tree gia' esisteva a parte). Mantenuto solo per compatibilita' con .asset serializzati esistenti.")]
        Goods = 7,

        [Obsolete("Chance non e' piu' un TileType a se': i suoi esiti sono ora Element propri (Trap, Fountain, Tree, Chest). Mantenuto solo per compatibilita' con editor/debug esistenti.")]
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