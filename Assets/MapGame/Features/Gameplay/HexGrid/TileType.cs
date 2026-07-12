namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    /// <summary>
    /// Tipi di tile come da GDD (sezione "Tile Types"), revisione 2026-07-10.
    /// Tile-state-agnostico: ogni contenuto logico che una tile puo' avere e' un membro.
    /// Non aggiungere valori senza istruzione esplicita.
    ///
    /// Void e Strada non hanno DifficultyLevel (strutturali, non content).
    /// Tutti gli altri tipi ricevono un DifficultyLevel (1-6) per-istanza da LevelConfig,
    /// vedi HexTileData.DifficultyLevel e AestheticClusterMapGenerator.
    ///
    /// Trappola non e' piu' un tipo a se', confluisce in Mistery come uno dei possibili
    /// esiti (tabella esiti non ancora implementata, vedi Mistery in HexTileData/generator).
    /// Shop era un sottotipo di dialogo di NPC (Mercante), ora e' un TileType a se stante.
    /// Miniboss e' nuovo, bilanciamento non ancora definito.
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
        Chance
    }
}
