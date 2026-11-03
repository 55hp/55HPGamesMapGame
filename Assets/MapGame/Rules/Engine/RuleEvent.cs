namespace hp55games.MapGame.Rules
{
    public enum RuleEventType
    {
        MissionStarted,
        TileRevealed,
        EncounterWon,
        EncounterLost,      // il bersaglio fugge, il giocatore subisce danno
        TrapTriggered,
        TrapDisarmed,
        Healed,
        DamageTaken,
        CoinsGained,
        CoinsSpent,
        XpGained,
        LineCompleted,
        LevelUp,            // livello raggiunto (scelta aperta come decisione)
        LevelUpChosen,
        ItemEquipped,
        ItemDiscarded,      // oggetto vecchio perso (R-025) o nuovo rifiutato
        KeyObtained,
        ChestOpened,
        ChestVanished,      // R-054
        NpcOpened,
        DecisionResolved,   // ogni risposta valida a una decisione (anche "Vai via"), sempre presente
        NpcKilled,
        NpcFled,
        OfferBought,
        ClueReceived,
        ClueUsed,
        ClueLost,           // R-096
        TileTypeShown,      // Lanterna
        GameWon,
        GameLost,
    }

    /// <summary>
    /// Evento di regola piatto: i campi non pertinenti al tipo restano ai valori di default.
    /// La presentazione li traduce in feedback; non contengono testo per il giocatore.
    /// </summary>
    public sealed class RuleEvent
    {
        public RuleEventType Type { get; internal set; }
        public int TileIndex { get; internal set; } = -1;
        public ContentKind Kind { get; internal set; }
        public int Amount { get; internal set; }
        public int Attack { get; internal set; }
        public int Defense { get; internal set; }
        public string ItemId { get; internal set; }
        public string OfferId { get; internal set; }
        public KeyKind Key { get; internal set; }
        public ClueTarget ClueTarget { get; internal set; }
        public LineRef Line { get; internal set; }
        public LossReason LossReason { get; internal set; }

        public override string ToString() =>
            $"{Type} tile={TileIndex} kind={Kind} n={Amount} atk={Attack} def={Defense} item={ItemId} offer={OfferId} key={Key} clue={ClueTarget} line={Line} loss={LossReason}";
    }
}
