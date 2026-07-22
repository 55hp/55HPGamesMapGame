using System;
using System.Threading;
using System.Threading.Tasks;
using hp55games.Mobile.Core.Context;
using hp55games.Mobile.Core.Gameplay.Events;
using hp55games.Mobile.Core.SceneFlow;
using UnityEngine;
using hp55games.Mobile.Core.UI;
using hp55games.Mobile.Core; // AsyncUtils.FireAndForget

namespace hp55games.Mobile.Core.Architecture.States
{
    /// <summary>
    /// Gameplay state: reacts to gameplay scene already loaded by SceneFlowService.
    /// This state should only manage game logic, HUD, subscriptions, BGM, etc.
    ///
    /// Lose condition (2026-07-10): sottoscrive PlayerDeathEvent per tutta la durata
    /// dello stato (sia al primo Enter sia dopo un resume da Pause, non solo al primo
    /// avvio — per questo la subscribe/unsubscribe sta fuori dal blocco "prima volta")
    /// e passa a Results via ISceneFlowService.GoToResultsAsync() quando l'HP arriva a 0.
    /// Win condition (2026-07-20): stessa meccanica con PlayerVictoryEvent, pubblicato al
    /// reveal della tile IsObjective da vivi. UIResultsPage distingue i due esiti
    /// inferendo da Lives (0 = sconfitta, > 0 = vittoria).
    /// </summary>
    public sealed class GameplayState : IGameState
    {
        private readonly bool _isResuming;
        private IMusicService _music;
        private IEventBus _bus;
        private ISceneFlowService _sceneFlow;
        private IDisposable _deathSub;
        private IDisposable _victorySub;

        public GameplayState(bool isResuming = false)
        {
            _isResuming = isResuming;
        }

        public async Task EnterAsync(CancellationToken ct)
        {
            Debug.Log($"[GameplayState] Enter (isResuming: {_isResuming})");

            // Fuori dal blocco "prima volta": deve restare attiva anche dopo un resume da
            // Pause, altrimenti morire subito dopo aver messo pausa non farebbe nulla.
            _bus = ServiceRegistry.Resolve<IEventBus>();
            ServiceRegistry.TryResolve<ISceneFlowService>(out _sceneFlow);
            _deathSub   = _bus?.Subscribe<PlayerDeathEvent>(OnPlayerDeath);
            _victorySub = _bus?.Subscribe<PlayerVictoryEvent>(OnPlayerVictory);

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
                // use this to initialize run-scoped data (HP, Cibo, Monete) AFTER ResetRun()
                // has cleared them, avoiding the Awake/ResetRun ordering race.
                _bus.Publish(new GameStartedEvent());

                var navigation = ServiceRegistry.Resolve<IUINavigationService>();
                await navigation.ReplaceAsync(hp55games.Addr.Content.UI.Screens.GameplayHUD);
            }

            await Task.Yield();
        }

        public async Task ExitAsync(CancellationToken ct)
        {
            Debug.Log("[GameplayState] Exit");

            _deathSub?.Dispose();
            _deathSub = null;

            _victorySub?.Dispose();
            _victorySub = null;

            // Optional cleanup (HUD, listeners, etc.)
            await Task.Yield();
        }

        /// <summary>
        /// Fire-and-forget: OnPlayerDeath è un handler void chiamato da IEventBus, non può
        /// essere await-ato direttamente. GoToResultsAsync() gestisce già da sé transizione
        /// FSM, cambio scena e fade — non serve altro qui. Eventuali eccezioni finiscono
        /// comunque loggate da GoToResultsAsync/RunWithOverlay internamente.
        /// </summary>
        private void OnPlayerDeath(PlayerDeathEvent _)
        {
            GoToResults("PlayerDeathEvent");
        }

        private void OnPlayerVictory(PlayerVictoryEvent _)
        {
            GoToResults("PlayerVictoryEvent");
        }

        private void GoToResults(string reason)
        {
            if (_sceneFlow == null)
            {
                Debug.LogWarning($"[GameplayState] {reason} ricevuto ma ISceneFlowService non disponibile, nessuna transizione.");
                return;
            }

            AsyncUtils.FireAndForget(_sceneFlow.GoToResultsAsync(), context: nameof(GameplayState));
        }
    }
}
