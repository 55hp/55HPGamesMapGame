using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using hp55games.Mobile.Core.Architecture;

namespace hp55games.Mobile.Core.UI
{
    /// <summary>
    /// Base per i popup istanziati tramite IUIPopupService.OpenAsync. Risolve il
    /// servizio una sola volta in Awake (PopupService), espone ClosePopup() per
    /// chiudersi da soli passando sempre dal servizio — cosi' il bookkeeping interno
    /// (_opened) e lo scrim restano coerenti, cosa che non succedeva ovunque prima
    /// (UIPopup_Generic si chiudeva solo abbassando l'alpha del proprio CanvasGroup,
    /// senza mai rilasciare l'istanza tramite il servizio) — e Bind() per registrare
    /// listener sui bottoni senza ripetere onClick.AddListener/RemoveListener in ogni
    /// popup. Le classi derivate che sovrascrivono Awake/OnDestroy devono chiamare base.
    /// </summary>
    public abstract class UIPopupBase : MonoBehaviour
    {
        protected IUIPopupService PopupService { get; private set; }

        private readonly List<(Button button, UnityAction action)> _boundButtons = new();

        protected virtual void Awake()
        {
            PopupService = ServiceRegistry.Resolve<IUIPopupService>();
        }

        protected virtual void OnDestroy()
        {
            foreach (var (button, action) in _boundButtons)
                if (button != null) button.onClick.RemoveListener(action);
            _boundButtons.Clear();
        }

        /// <summary>Registra un listener sul bottone e lo deregistra automaticamente in OnDestroy.</summary>
        protected void Bind(Button button, UnityAction action)
        {
            if (button == null) return;
            button.onClick.AddListener(action);
            _boundButtons.Add((button, action));
        }

        /// <summary>Chiude questo popup passando dal servizio (non nasconde e basta: rilascia l'istanza).</summary>
        protected void ClosePopup()
        {
            PopupService?.Close(gameObject);
        }
    }
}
