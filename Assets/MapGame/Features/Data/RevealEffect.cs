namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    /// <summary>
    /// Governa cosa succede al reveal di una tile con Element (GDD, sezione "Reveal
    /// Effects", revisione 2026-07-23). Usato da ElementConfig per determinare quale
    /// popup (se esiste) aprire. Non aggiungere valori senza istruzione esplicita.
    ///
    /// Immediate — effetto applicato direttamente, nessun popup (Goods, Trap, Fountain,
    ///             Tree, Key). Vedi HexGridController.ApplyImmediateElement.
    /// Fight     — apre UIPopup_Encounter con scelta combatti/fuggi (Enemy, Miniboss).
    /// Talk      — popup dialogo NPC lineare, esito automatico finale. Flow non costruito.
    /// Trade     — apre UIPopup_Shop con inventario (Shop, NPC mercante).
    /// Choice    — popup con testo + 2-3 scelte a nodi, esito automatico. Non costruito.
    /// Loot      — popup con esito materiale: Item, risorse, potenziamenti (Chest).
    ///             Popup non ancora costruito: Chest oggi e' inerte al reveal.
    /// Info      — popup con hint narrativo (tier S/A/B/C dal LevelConfig). Non costruito.
    /// </summary>
    public enum RevealEffect
    {
        Immediate,
        Fight,
        Talk,
        Trade,
        Choice,
        Loot,
        Info
    }
}
