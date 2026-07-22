using hp55games.Mobile.Core.Architecture;

namespace hp55games.Mobile.Core.Gameplay.Events
{
    /// <summary>
    /// Il giocatore ha raggiunto l'obiettivo della run da vivo (Win Condition,
    /// 2026-07-20). Gemello di PlayerDeathEvent: GameplayState lo ascolta e transita a
    /// Results; UIResultsPage inferisce vittoria da Lives > 0.
    /// </summary>
    public struct PlayerVictoryEvent : IEvent { }
}
