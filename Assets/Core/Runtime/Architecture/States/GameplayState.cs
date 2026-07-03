using System.Threading;
using System.Threading.Tasks;
using hp55games.Mobile.Core.Context;
using hp55games.Mobile.Core.Gameplay.Events;
using UnityEngine;
using hp55games.Mobile.Core.UI;

namespace hp55games.Mobile.Core.Architecture.States
{
    /// <summary>
    /// Gameplay state: reacts to gameplay scene already loaded by SceneFlowService.
    /// This state should only manage game logic, HUD, subscriptions, BGM, etc.
    /// </summary>
    public sealed class GameplayState : IGameState
    {
        private readonly bool _isResuming;
        private IMusicService _music;

        public GameplayState(bool isResuming = false)
        {
            _isResuming = isResuming;
        }

        public async Task EnterAsync(CancellationToken ct)
        {
            Debug.Log($"[GameplayState] Enter (isResuming: {_isResuming})");

            if (!_isResuming)
            {
                // Prima volta: setup completo
                if (ServiceRegistry.TryResolve<IMusicService>(out _music))
                {
                    await _music.CrossfadeToAsync(Addr.Content.Audio.Bgm.GameTheme, 0.5f);
                }

                IGameContextService context = null;
                ServiceRegistry.TryResolve(out context);
                context?.ResetRun();

                // Signal that a fresh run has started. Subscribers (e.g. HexGridController)
                // use this to initialize run-scoped data (HP, Monete) AFTER ResetRun() has
                // cleared them, avoiding the Awake/ResetRun ordering race.
                var bus = ServiceRegistry.Resolve<IEventBus>();
                bus.Publish(new GameStartedEvent());

                var navigation = ServiceRegistry.Resolve<IUINavigationService>();
                await navigation.ReplaceAsync(hp55games.Addr.Content.UI.Screens.GameplayHUD);
            }
            
            await Task.Yield();
        }

        public async Task ExitAsync(CancellationToken ct)
        {
            Debug.Log("[GameplayState] Exit");

            // Optional cleanup (HUD, listeners, etc.)
            await Task.Yield();
        }
    }
}