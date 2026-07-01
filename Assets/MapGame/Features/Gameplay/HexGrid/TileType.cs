namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    /// <summary>
    /// Tipi di tile come da GDD (sezione "Tile Types").
    /// Non aggiungere valori senza istruzione esplicita.
    /// </summary>
    public enum TileType
    {
        /// <summary>Nessun contenuto. Corrisponde a "Grey = Empty" nei hint di bordo (V2/V3).</summary>
        None,
        Battaglia,
        NPC,
        Mistery,
        Risorsa
    }
}
