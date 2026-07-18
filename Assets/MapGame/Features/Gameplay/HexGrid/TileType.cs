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
    /// Chance assorbe il vecchio Trappola come uno dei suoi possibili esiti; la tabella
    /// esiti non e' ancora implementata, quindi oggi Chance ha impatto zero.
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
        Chance
    }
}
