using hp55games.Mobile.Core.Architecture;

namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    /// <summary>
    /// Evento di incontro Trader (RevealEffect.Trade) sul bus (IEventBus), stessa famiglia di
    /// EncounterStartedEvent (vedi EncounterEvents.cs) e stesso motivo per vivere qui e non in
    /// Core/Runtime/Events: Core non referenzia Game.Features.
    ///
    /// A differenza dell'incontro Enemy, il Trader non ha uno stato "pending" da risolvere:
    /// la tile e' gia' Scoperta quando questo evento viene pubblicato (TryRevealTile, dopo il
    /// reveal standard), il popup e' solo presentazione/acquisti (UIPopup_Shop, gia' completo)
    /// e non incide sullo stato della tile. Nessun evento "Resolved" necessario.
    /// </summary>
    public readonly struct TradeStartedEvent : IEvent
    {
        /// <summary>Controller su cui il popup shop invoca le TryBuy*/CurrentMonete.</summary>
        public HexGridController Grid { get; }

        public TradeStartedEvent(HexGridController grid)
        {
            Grid = grid;
        }
    }
}
