using hp55games.Mobile.Core.Architecture;

namespace hp55games.Mobile.Core.Gameplay.Events
{
    /// <summary>
    /// Lo stato delle chiavi di sessione (HasKeyDL4/5/6 su IGameContextService) e'
    /// cambiato. Pubblicato da HexGridController al reveal di una tile Key; consumato
    /// dall'HUD per aggiornare gli slot on/off. Payload vuoto come gli altri eventi
    /// Changed: chi ascolta rilegge lo stato dal contesto.
    /// </summary>
    public struct KeysChangedEvent : IEvent { }
}
