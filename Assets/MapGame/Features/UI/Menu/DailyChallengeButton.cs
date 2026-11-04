using System;
using System.Threading.Tasks;
using hp55games.MapGame.Features.Gameplay.Run;
using hp55games.Mobile.Core;
using hp55games.Mobile.Core.Architecture;
using hp55games.Mobile.Core.SceneFlow;
using hp55games.Mobile.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace hp55games.MapGame.Features.UI.Menu
{
    /// <summary>
    /// Pulsante "Mappa del giorno" del menu: stessa mappa per tutti nello stesso giorno (UTC).
    /// Prenota il seed in RunSeedRequest (il contesto del template azzera CurrentRunSeed) e avvia la partita.
    /// </summary>
    public sealed class DailyChallengeButton : MonoBehaviour
    {
        [SerializeField] private Button _button;

        private void Awake()
        {
            if (_button == null)
            {
                Debug.LogError("[DailyChallengeButton] _button non assegnato.", this);
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
            var request = MenuRunSetup.Get();
            request.Mode = hp55games.MapGame.Rules.GameMode.Open;
            var today = DateTime.UtcNow.Date;
            request.RequestDaily(RunSeedRequest.DailySeed(today.Year, today.Month, today.Day));
            AsyncUtils.FireAndForget(StartAsync(flow), context: nameof(DailyChallengeButton));
        }

        private static async Task StartAsync(ISceneFlowService flow)
        {
            // Come il pulsante Gioca del menu: chiude la pagina e passa al gameplay.
            if (ServiceRegistry.TryResolve(out IUINavigationService nav)) await nav.PopAsync();
            await flow.GoToGameplayAsync();
        }
    }
}
