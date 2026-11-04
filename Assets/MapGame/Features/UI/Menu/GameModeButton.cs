using System.Threading.Tasks;
using hp55games.MapGame.Features.Gameplay.Run;
using hp55games.MapGame.Rules;
using hp55games.Mobile.Core;
using hp55games.Mobile.Core.Architecture;
using hp55games.Mobile.Core.SceneFlow;
using hp55games.Mobile.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace hp55games.MapGame.Features.UI.Menu
{
    /// <summary>
    /// Pulsante del menu che avvia una partita in una modalità precisa (D-040: Nebbia, Esplorazione).
    /// Il pulsante GIOCA del template avvia la modalità classica: MenuRunSetup la ripristina all'apertura del menu.
    /// </summary>
    public sealed class GameModeButton : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private GameMode _mode = GameMode.Fog;

        private void Awake()
        {
            if (_button == null)
            {
                Debug.LogError("[GameModeButton] _button non assegnato.", this);
                enabled = false;
                return;
            }
            _button.onClick.AddListener(OnClicked);
        }

        private void OnDestroy()
        {
            if (_button != null) _button.onClick.RemoveListener(OnClicked);
        }

        private void OnClicked()
        {
            if (!enabled || !ServiceRegistry.TryResolve(out ISceneFlowService flow)) return;
            MenuRunSetup.Get().Mode = _mode;
            AsyncUtils.FireAndForget(StartAsync(flow), context: nameof(GameModeButton));
        }

        private static async Task StartAsync(ISceneFlowService flow)
        {
            if (ServiceRegistry.TryResolve(out IUINavigationService nav)) await nav.PopAsync();
            await flow.GoToGameplayAsync();
        }
    }
}
