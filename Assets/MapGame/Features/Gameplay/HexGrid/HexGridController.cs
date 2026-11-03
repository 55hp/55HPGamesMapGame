using System;
using System.Collections.Generic;
using hp55games.MapGame.Features.Configs;
using hp55games.MapGame.Features.Gameplay.Run;
using hp55games.MapGame.Rules;
using hp55games.Mobile.Core.Architecture;
using hp55games.Mobile.Core.Context;
using hp55games.Mobile.Core.Gameplay.Events;
using hp55games.Mobile.Core.Juice;
using UnityEngine;

namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    /// <summary>
    /// Controller della griglia v2 (spec 0005). È l'adapter tra la scena e il RulesEngine puro:
    /// non contiene regole. Ogni tap passa a RulesEngine.Reveal; ogni scelta (NPC, level up,
    /// indizio, oggetto) passa a Submit. Gli eventi di regola aggiornano HexTileData (per le viste
    /// esistenti), IGameContextService (HUD/risultati legacy) e vengono rilanciati come
    /// <see cref="RuleEventsRaised"/> per la UI v2.
    ///
    /// Interfaccia verso le viste invariata: Tiles, GridInitialized, TileRevealed, ReachabilityChanged,
    /// IsClickable, GetNeighbors, PlayerCoord, ObjectiveCoord, BuildGrid.
    /// Config: RulesConfigAsset dal ConfigCatalog se presente, altrimenti RulesConfigDefaults.Level1.
    /// Seed (D-024): IGameContextService.CurrentRunSeed se ≠ 0, altrimenti MapGenerationConfig.Seed.
    /// </summary>
    public sealed class HexGridController : MonoBehaviour
    {
        private IConfigCatalogService _configs;
        private IGameContextService _context;
        private IEventBus _bus;
        private IFeedbackService _feedbackService;
        private IDisposable _gameStartedSub;

        private MapGenerationConfig _mapConfig;
        private RulesConfig _rules;
        private RulesEngine _engine;
        private int _seed;

        private readonly Dictionary<HexCoord, HexTileData> _tiles = new Dictionary<HexCoord, HexTileData>();
        private readonly Dictionary<int, HexCoord> _indexToCoord = new Dictionary<int, HexCoord>();
        private HexCoord _playerCoord;
        private HexCoord _objectiveCoord;

        public HexCoord PlayerCoord => _playerCoord;
        public HexCoord ObjectiveCoord => _objectiveCoord;
        public IReadOnlyDictionary<HexCoord, HexTileData> Tiles => _tiles;

        /// <summary>Motore di regole della partita in corso (sola lettura per la presentazione).</summary>
        public RulesEngine Engine => _engine;
        public RunState Run => _engine?.State;
        public Decision CurrentDecision => _engine?.CurrentDecision;
        public int Seed => _seed;

        public event Action GridInitialized;
        public event Action<HexTileData, IReadOnlyList<HexTileData>> TileRevealed;
        public event Action ReachabilityChanged;

        /// <summary>Eventi di regola di ogni azione, nell'ordine del motore. La UI v2 li usa per feedback e pop-up.</summary>
        public event Action<IReadOnlyList<RuleEvent>> RuleEventsRaised;

        private void Awake()
        {
            _context = ServiceRegistry.Resolve<IGameContextService>();
            _bus = ServiceRegistry.Resolve<IEventBus>();

            if (!ServiceRegistry.TryResolve(out _configs))
            {
                Debug.LogError("[HexGridController] IConfigCatalogService non risolto. Verifica che ConfigCatalogInstaller sia in una scena caricata prima di questa.", this);
                enabled = false;
                return;
            }

            _mapConfig = _configs.Get<MapGenerationConfig>();
            var assets = _configs.GetAll<RulesConfigAsset>();
            if (assets.Count > 0) _rules = assets[0].Build();
            else
            {
                _rules = RulesConfigDefaults.Level1();
                Debug.Log("[HexGridController] Nessun RulesConfigAsset nel catalogo: uso RulesConfigDefaults.Level1.");
            }

            var errors = _rules.Validate();
            if (errors.Count > 0)
            {
                Debug.LogError("[HexGridController] RulesConfig non valida: " + string.Join("; ", errors), this);
                enabled = false;
            }
        }

        private void Start()
        {
            ServiceRegistry.TryResolve(out _feedbackService);
            if (_bus != null) _gameStartedSub = _bus.Subscribe<GameStartedEvent>(_ => InitializeSession());
            // Fallback standalone: senza FSM Lives resta al default (-1).
            if (_context != null && _context.Lives < 0) InitializeSession();
        }

        private void OnDestroy() => _gameStartedSub?.Dispose();

        private void InitializeSession()
        {
            if (!enabled) return;
            if (!BuildGrid())
            {
                Debug.LogError("[HexGridController] InitializeSession interrotta: generazione mappa fallita.", this);
                return;
            }
            var events = _engine.Start();
            Dispatch(events);
        }

        /// <summary>Genera una nuova partita (mappa + motore). Non avvia il motore: lo fa InitializeSession.</summary>
        public bool BuildGrid()
        {
            if (_rules == null)
            {
                Debug.LogError("[HexGridController] RulesConfig assente.", this);
                return false;
            }

            if (_mapConfig != null)
            {
                _rules.StartMinBorderDistance = _mapConfig.StartMinBorderDistance;
                if (_mapConfig.EndDistanceWeights != null && _mapConfig.EndDistanceWeights.Length > 0)
                {
                    _rules.EndDistanceWeights.Clear();
                    foreach (var w in _mapConfig.EndDistanceWeights) _rules.EndDistanceWeights.Add(new Rules.DistanceWeight(w.Distance, w.Weight));
                }
            }

            int fallbackSeed = _mapConfig != null ? _mapConfig.Seed : 12345;
            _seed = _context != null && _context.CurrentRunSeed != 0 ? _context.CurrentRunSeed : fallbackSeed;

            _engine = new RulesEngine(_rules, unchecked((ulong)_seed));
            if (!_engine.Layout.Success)
            {
                Debug.LogError("[HexGridController] Generazione mappa fallita: " + _engine.Layout.FailureReason, this);
                _engine = null;
                return false;
            }

            var result = CensusMapAdapter.FromLayout(_rules, _engine.Layout, _seed);
            _tiles.Clear();
            _indexToCoord.Clear();
            foreach (var kv in result.Tiles)
            {
                _tiles[kv.Key] = kv.Value;
                _indexToCoord[kv.Value.RulesIndex] = kv.Key;
            }
            _playerCoord = result.StartCoord;
            _objectiveCoord = result.ObjectiveCoord;

            GridInitialized?.Invoke();
            return true;
        }

        // ---------------------------------------------------------------- query per le viste

        /// <summary>R-002/R-005: cliccabile = coperta, nessuna decisione in attesa, partita in corso.</summary>
        public bool IsClickable(HexCoord coord) =>
            _engine != null && _tiles.TryGetValue(coord, out var t) && _engine.IsRevealable(t.RulesIndex);

        public IReadOnlyList<HexTileData> GetNeighbors(HexCoord coord)
        {
            var result = new List<HexTileData>(6);
            for (int dir = 0; dir < 6; dir++)
                if (_tiles.TryGetValue(coord.GetNeighbor(dir), out var n)) result.Add(n);
            return result;
        }

        /// <summary>Legacy (gate icone v1): numero di vicini scoperti. Non usato dalle regole v2.</summary>
        public int CountScopertaNeighbors(HexCoord coord)
        {
            int count = 0;
            foreach (var n in GetNeighbors(coord))
                if (n.Exploration == ExplorationState.Explored) count++;
            return count;
        }

        public HexCoord CoordOf(int rulesIndex) => _indexToCoord[rulesIndex];

        // ---------------------------------------------------------------- comandi

        /// <summary>
        /// La UI lo alza quando un tocco appartiene a un suo pulsante: IInputService legge l'input grezzo
        /// e altrimenti lo stesso tocco scoprirebbe anche la tile sottostante.
        /// </summary>
        public int InputBlockedUntilFrame { get; set; }

        public bool TryRevealTile(HexCoord target)
        {
            if (Time.frameCount <= InputBlockedUntilFrame) return false;
            if (!IsClickable(target)) return false;
            var events = _engine.Reveal(_tiles[target].RulesIndex);
            if (events.Count == 0) return false;
            Dispatch(events);
            return true;
        }

        /// <summary>Risponde alla decisione corrente del motore (NPC, level up, indizio, oggetto).</summary>
        public bool Submit(string optionId)
        {
            if (_engine == null) return false;
            var events = _engine.Submit(optionId);
            if (events.Count == 0) return false;
            Dispatch(events);
            return true;
        }

        // ---------------------------------------------------------------- traduzione eventi

        private void Dispatch(IReadOnlyList<RuleEvent> events)
        {
            bool hp = false, coins = false, keys = false, won = false, lost = false;
            foreach (var e in events)
            {
                switch (e.Type)
                {
                    case RuleEventType.TileRevealed:
                        if (_indexToCoord.TryGetValue(e.TileIndex, out var c) && _tiles.TryGetValue(c, out var tile))
                        {
                            tile.Exploration = ExplorationState.Explored;
                            tile.Spotting = SpottingState.Spotted;
                            TileRevealed?.Invoke(tile, GetNeighbors(c));
                        }
                        break;
                    case RuleEventType.TileTypeShown:
                        if (_indexToCoord.TryGetValue(e.TileIndex, out var sc) && _tiles.TryGetValue(sc, out var st))
                            st.TypeShown = true;
                        break;
                    case RuleEventType.DamageTaken:
                    case RuleEventType.Healed:
                    case RuleEventType.LevelUpChosen:
                        hp = true; break;
                    case RuleEventType.CoinsGained:
                    case RuleEventType.CoinsSpent:
                        coins = true; break;
                    case RuleEventType.KeyObtained:
                        keys = true; break;
                    case RuleEventType.GameWon: won = true; break;
                    case RuleEventType.GameLost: lost = true; break;
                }
            }

            SyncContext();
            ReachabilityChanged?.Invoke();
            RuleEventsRaised?.Invoke(events);

            if (hp) _bus?.Publish(new HpChangedEvent());
            if (coins) _bus?.Publish(new ScoreChangedEvent());
            if (keys) _bus?.Publish(new KeysChangedEvent());
            if (won)
            {
                _feedbackService?.Play("victory");
                _bus?.Publish(new PlayerVictoryEvent());
            }
            if (lost) _bus?.Publish(new PlayerDeathEvent());
        }

        /// <summary>Specchia RunState nei campi legacy letti da HUD e pagina risultati.</summary>
        private void SyncContext()
        {
            if (_context == null || _engine == null) return;
            var s = _engine.State;
            // UIResultsPage deduce la sconfitta da Lives <= 0: anche "Boss fuggito" (R-039) deve risultare sconfitta.
            _context.Lives = s.Status == RunStatus.Lost ? Mathf.Min(0, s.Hp) : s.Hp;
            _context.Score = s.Coins;
            _context.Food = -1; // R-027: il Cibo non esiste
            _context.HasKeyDL4 = s.HasSilverKey;
            _context.HasKeyDL5 = s.HasGoldKey;
            _context.HasKeyDL6 = false;
        }

        // ---------------------------------------------------------------- legacy (rimozione: spec 0012)

        [Obsolete("v1: la fuga non esiste più (R-035).")] public bool CanFlee => false;
        [Obsolete("v1: lo scambio si risolve al click (R-031).")] public bool ResolveEncounterFight() => false;
        [Obsolete("v1: la fuga non esiste più (R-035).")] public bool ResolveEncounterFlee() => false;
        [Obsolete("v1: lo shop è sostituito dagli NPC.")] public void ResolveTrade() { }
        [Obsolete("v1: lo shop è sostituito dagli NPC.")] public bool TryBuyMaxHpUpgrade() => false;
        [Obsolete("v1: lo shop è sostituito dagli NPC.")] public bool TryBuyFoodSlotUpgrade() => false;
        [Obsolete("v1: lo shop è sostituito dagli NPC.")] public bool TryBuyHeal() => false;
        [Obsolete("v1: lo shop è sostituito dagli NPC.")] public bool TryBuyFoodRefill() => false;
        [Obsolete("v1")] public int CurrentMonete => _engine?.State.Coins ?? 0;
        [Obsolete("v1")] public int CurrentMaxHp => _engine?.State.MaxHp ?? 0;
        [Obsolete("v1")] public bool HasPendingEncounter => false;
        [Obsolete("v1")] public HexTileData PendingEncounterTile => null;
    }
}
