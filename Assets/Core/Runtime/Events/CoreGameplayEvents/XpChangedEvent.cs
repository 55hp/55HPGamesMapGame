using hp55games.Mobile.Core.Architecture;

namespace hp55games.Mobile.Core.Gameplay.Events
{
    /// <summary>
    /// Pubblicato quando XP o Livello del personaggio cambiano (guadagno XP da Enemy,
    /// oppure level up). Ripristinato 2026-07-17 con la reintegrazione del sistema XP.
    /// </summary>
    public struct XpChangedEvent : IEvent { }
}
