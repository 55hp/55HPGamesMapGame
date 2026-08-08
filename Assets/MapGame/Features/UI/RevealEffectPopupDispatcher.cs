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
    /// Dispatcher unico per i popup di RevealEffect (GDD, sezione "Reveal Effects"): ascolta
    /// sul bus l'evento "Started" di ogni RevealEffect che apre un popup e lo istanzia via
    /// IUIPopupService — un solo componente per Fight (-> UIPopup_Encounter) e Trade
    /// (-> UIPopup_Shop), che scala meglio sui prossimi RevealEffect con popup (Choice,
    /// Loot, Info — vedi RevealEffect.cs, non ancora costruiti in HexGridController). Puro
    /// ascoltatore: zero campi Inspector, zero vincoli di posizionamento in scena (il
    /// riferimento a grid/tile viaggia nel payload dell'evento).
    ///
    /// Per aggiungere un nuovo RevealEffect con popup:
    /// 1) l'evento "XStartedEvent" (Grid, e Tile se il popup ne ha bisogno) accanto agli
    ///    altri in HexGrid/*Events.cs — invariato rispetto a prima.
    /// 2) qui: un campo IDisposable, una riga Subscribe in OnEnable, il Dispose in
    ///    OnDisable, un OnXStarted che chiama OpenPopupAsync&lt;TPopup&gt;. Nessuna modifica
    ///    alla forma del dispatcher stesso o a OpenPopupAsync.
    /// </summary>
    public sealed class RevealEffectPopupDispatcher : MonoBehaviour
    {
        private IUIPopupService _popupService;
        private IEventBus       _bus;

        private IDisposable _encounterSub;
        private IDisposable _tradeSub;

        private void Awake()
        {
            _popupService = ServiceRegistry.Resolve<IUIPopupService>();
            _bus          = ServiceRegistry.Resolve<IEventBus>();
        }

        private void OnEnable()
        {
            _encounterSub = _bus?.Subscribe<EncounterStartedEvent>(OnEncounterStarted);
            _tradeSub     = _bus?.Subscribe<TradeStartedEvent>(OnTradeStarted);
        }

        private void OnDisable()
        {
            _encounterSub?.Dispose();
            _encounterSub = null;

            _tradeSub?.Dispose();
            _tradeSub = null;
        }

        // ── Handlers — un caso per RevealEffect, tutti dello stesso identico shape ──────

        private void OnEncounterStarted(EncounterStartedEvent evt) =>
            AsyncUtils.FireAndForget(
                OpenPopupAsync<UIPopup_Encounter>(Addr.Content.UI.Popups.Popup_Encounter,
                    popup => popup.Open(evt.Tile, evt.Grid)),
                context: nameof(RevealEffectPopupDispatcher));

        private void OnTradeStarted(TradeStartedEvent evt) =>
            AsyncUtils.FireAndForget(
                OpenPopupAsync<UIPopup_Shop>(Addr.Content.UI.Popups.Popup_Shop,
                    popup => popup.Open(evt.Grid)),
                context: nameof(RevealEffectPopupDispatcher));

        /// <summary>
        /// Apre un popup Addressable via IUIPopupService e lo configura. Comune a tutti gli
        /// handler sopra: solo l'address e il configure cambiano da un RevealEffect all'altro.
        /// </summary>
        private async Task OpenPopupAsync<TPopup>(string address, Action<TPopup> configure) where TPopup : Component
        {
            var popup = await _popupService.OpenAsync<TPopup>(address);
            if (popup == null)
            {
                Debug.LogError($"[RevealEffectPopupDispatcher] Impossibile aprire {typeof(TPopup).Name} (address '{address}' non registrato o Addressables non buildate).");
                return;
            }

            configure(popup);
        }
    }
}
