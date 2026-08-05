using System;

namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    /// <summary>
    /// Governa cosa succede al reveal di una tile con Element (GDD, sezione "Reveal
    /// Effects", revisione 2026-07-25). Usato da ElementConfig per determinare quale
    /// popup (se esiste) aprire. Non aggiungere valori senza istruzione esplicita.
    ///
    /// Immediate — effetto applicato direttamente, nessun popup (Trap, Fountain, Tree,
    ///             Bush, BeeHive, TurnipSprout, MoneyBag, Key). Vedi HexGridController.ApplyImmediateElement.
    /// Fight     — apre UIPopup_Encounter con scelta combatti/fuggi (Enemy, incluse le
    ///             varianti Miniboss/Boss via ElementConfig).
    /// Trade     — apre UIPopup_Shop con inventario (Trader).
    /// Choice    — popup con testo + scelte a nodi, esito finale automatico. Copre sia
    ///             dialoghi lineari (un solo nodo/risposta) sia scelte mutualmente
    ///             esclusive a piu' opzioni — vedi Tile Types per gli archetipi NPC
    ///             (Inn/Witch/Farmer/Hunter) che lo usano. Non costruito.
    /// Loot      — popup con esito materiale: Item, risorse, potenziamenti (Chest).
    ///             Popup non ancora costruito: Chest oggi e' inerte al reveal.
    /// Info      — popup con hint narrativo (tier S/A/B/C dal LevelConfig). Non costruito.
    /// </summary>
    public enum RevealEffect
    {
        Immediate = 0,
        Fight = 1,

        [Obsolete("Talk eliminato dal design (2026-07-25): Choice ora copre anche il caso di solo dialogo lineare (un nodo, una risposta/prosegui) — niente enum separato per quello. Mantenuto solo per compatibilita' con .asset serializzati esistenti.")]
        Talk = 2,

        Trade = 3,
        Choice = 4,
        Loot = 5,
        Info = 6
    }
}