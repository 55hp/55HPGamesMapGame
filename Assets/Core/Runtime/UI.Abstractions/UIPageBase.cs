using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace hp55games.Mobile.Core.UI
{
    /// <summary>
    /// Base per le pagine di navigazione (Main Menu, Results, Credits, ...). Per ora
    /// offre solo Bind() per i bottoni, che registra il listener e lo deregistra da
    /// solo in OnDestroy: le pagine risolvono servizi diversi tra loro (SceneFlow,
    /// Save, Navigation, Options...) quindi non c'e' altro da centralizzare qui senza
    /// forzare un accoppiamento che non esiste davvero.
    ///
    /// UIOptionsPage NON eredita da questa base (2026-07-20): usa Slider/Toggle/
    /// Dropdown oltre ai Button, con logica di apply/save che si intreccia con quei
    /// controlli. Bind() qui copre solo Button — farla ereditare oggi vorrebbe dire
    /// migrare solo 3 bottoni su una pagina che resta comunque piena di listener non
    /// coperti, un pulizia superficiale che non risolve il problema reale. Se in futuro
    /// serve davvero, aggiungere un overload Bind&lt;T&gt;(UnityEvent&lt;T&gt;, UnityAction&lt;T&gt;)
    /// qui e poi migrare anche quella pagina per intero.
    /// </summary>
    public abstract class UIPageBase : MonoBehaviour
    {
        private readonly List<(Button button, UnityAction action)> _boundButtons = new();

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
    }
}
