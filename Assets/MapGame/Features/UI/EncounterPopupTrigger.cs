using System;
using System.Threading.Tasks;
using UnityEngine;
using hp55games.Mobile.Core;
using hp55games.Mobile.Core.Architecture;
using hp55games.Mobile.Core.UI;
using hp55games.MapGame.Features.Gameplay.HexGrid;

namespace hp55games.MapGame.Features.UI
{
    /// <summary>
    /// Ascolta EncounterStartedEvent sul bus e apre UIPopup_Encounter via IUIPopupService.
    /// Puro ascoltatore: zero campi Inspector, zero vincoli di posizionamento in scena
    /// (il riferimento al grid per i comandi Resolve viaggia nel payload dell'evento).
    /// Nessuna logica di gameplay qui, solo il collegamento evento -> apertura popup.
    /// </summary>
    public sealed class EncounterPopupTrigger : MonoBehaviour
    {
        private IUIPopupService _popupService;
        private IEventBus       _bus;
        private IDisposable     _encounterSub;

        private void Awake()
        {
            _popupService = ServiceRegistry.Resolve<IUIPopupService>();
            _bus          = ServiceRegistry.Resolve<IEventBus>();
        }

        private void OnEnable()
        {
            _encounterSub = _bus?.Subscribe<EncounterStartedEvent>(OnEncounterStarted);
        }

        private void OnDisable()
        {
            _encounterSub?.Dispose();
            _encounterSub = null;
        }

        private void OnEncounterStarted(EncounterStartedEvent evt)
        {
            AsyncUtils.FireAndForget(OpenPopupAsync(evt), context: nameof(EncounterPopupTrigger));
        }

        private async Task OpenPopupAsync(EncounterStartedEvent evt)
        {
            var popup = await _popupService.OpenAsync<UIPopup_Encounter>(Addr.Content.UI.Popups.Popup_Encounter);
            if (popup == null)
            {
                Debug.LogError("[EncounterPopupTrigger] Impossibile aprire UIPopup_Encounter (address non registrato o Addressables non buildate).");
                return;
            }

            popup.Open(evt.Tile, evt.Grid);
        }
    }
}
