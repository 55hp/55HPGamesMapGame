using hp55games.Mobile.Core;
using hp55games.Mobile.Core.Architecture;
using hp55games.Mobile.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace hp55games.Mobile.Game.UI
{
    /// <summary>
    /// Pagina Crediti: contenuto statico nel prefab. Il bottone Back fa Pop dello
    /// stack di navigazione; la ScrollRect viene riportata in cima a ogni apertura.
    /// </summary>
    public sealed class UICreditsPage : UIPageBase
    {
        [Header("UI References")]
        [SerializeField] private Button backButton;
        [SerializeField] private ScrollRect scrollRect;

        private IUINavigationService _navigation;

        private void Awake()
        {
            if (!ServiceRegistry.TryResolve<IUINavigationService>(out _navigation))
            {
                Debug.LogWarning("[UICreditsPage] IUINavigationService not available. Back button will do nothing.");
            }

            Bind(backButton, OnBackClicked);
        }

        private void OnEnable()
        {
            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 1f;
        }

        private void OnBackClicked()
        {
            if (_navigation == null)
            {
                Debug.LogWarning("[UICreditsPage] Back clicked but IUINavigationService is null.");
                return;
            }

            AsyncUtils.FireAndForget(_navigation.PopAsync(), context: nameof(UICreditsPage));
        }
    }
}
