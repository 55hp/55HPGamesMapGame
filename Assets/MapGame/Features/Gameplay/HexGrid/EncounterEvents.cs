using hp55games.Mobile.Core.Architecture;

namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    /// <summary>
    /// Eventi di incontro Enemy/Miniboss sul bus (IEventBus), famiglia dei segnali di
    /// gameplay trasversali (HpChangedEvent, PlayerDeathEvent, ...). Vivono qui e NON in
    /// Core/Runtime/Events perche' il payload e' HexTileData, tipo di Game.Features: Core
    /// non referenzia Game.Features (la dipendenza va nell'altro verso), quindi un evento
    /// con questo payload non puo' stare in Core. IEvent (Core) resta il contratto comune.
    /// </summary>
    public readonly struct EncounterStartedEvent : IEvent
    {
        /// <summary>Tile dell'incontro (Type e DifficultyLevel per la UI).</summary>
        public HexTileData Tile { get; }

        /// <summary>
        /// Controller su cui invocare ResolveEncounterFight()/ResolveEncounterFlee().
        /// I resolve restano comandi diretti (non eventi): includere la sorgente nel
        /// payload evita a chi ascolta di dover trovare il controller in scena.
        /// </summary>
        public HexGridController Grid { get; }

        public EncounterStartedEvent(HexTileData tile, HexGridController grid)
        {
            Tile = tile;
            Grid = grid;
        }
    }

    /// <summary>
    /// L'incontro pending e' stato risolto. WasFought: true se combattuto (tile ora
    /// Scoperta), false se fuggiti (tile invariata). Oggi nessuno lo consuma (il popup si
    /// chiude da solo alla pressione del bottone); pubblicato comunque per i sistemi
    /// futuri che ne avranno bisogno (audio, missioni, statistiche di run).
    /// </summary>
    public readonly struct EncounterResolvedEvent : IEvent
    {
        public HexTileData Tile { get; }
        public bool WasFought { get; }

        public EncounterResolvedEvent(HexTileData tile, bool wasFought)
        {
            Tile = tile;
            WasFought = wasFought;
        }
    }
}
