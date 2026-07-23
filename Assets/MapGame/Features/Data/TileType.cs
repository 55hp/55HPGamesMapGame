using System;

namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    /// <summary>
    /// Tipi di tile come da GDD (sezione "Tile Types"), revisione 2026-07-23.
    /// Tile-state-agnostico: ogni contenuto logico che una tile puo' avere e' un membro.
    /// Non aggiungere valori senza istruzione esplicita.
    ///
    /// Void e Path non hanno DifficultyLevel (strutturali, non content).
    /// Tutti gli altri tipi ricevono un DifficultyLevel (1-6) per-istanza da LevelConfig,
    /// vedi HexTileData.DifficultyLevel e AestheticClusterMapGenerator.
    ///
    /// Revisione 2026-07-22/23: la run ha un numero fisso e determinato di Element per
    /// tipo, definito nel LevelConfig — niente casualita' a runtime sulla composizione.
    /// Questo elimina Chance come tipo intermedio: ogni esito che era nella sua tabella
    /// e' ora un Element proprio (Trap, Fountain, Tree, Chest). Chance resta come membro
    /// [Obsolete] solo per non rompere i riferimenti esistenti in editor/debug
    /// (MapGameDebugFlags, MapGenerationConfigEditor) — non usarlo in codice nuovo, non
    /// inserirlo in nessuna LevelTileEntry.
    ///
    /// Key: attiva la chiave di sessione corrispondente al proprio DifficultyLevel
    /// (4/5/6). Chest: DifficultyLevel 1/2/3, aperto dalla chiave DL+3.
    /// Shop era un sottotipo di dialogo di NPC (Mercante), ora e' un TileType a se stante.
    /// Miniboss: bilanciamento non ancora definito.
    /// </summary>
    public enum TileType
    {
        Void,
        Path,
        Boss,
        Npc,
        Shop,
        Enemy,
        Miniboss,
        Goods,

        [Obsolete("Chance e' stato eliminato dal design (2026-07-22): i suoi esiti sono ora Element propri (Trap, Fountain, Tree, Chest). Mantenuto solo per compatibilita' con editor/debug esistenti.")]
        Chance,

        Trap,
        Fountain,
        Tree,
        Key,
        Chest
    }
}
