using System;
using System.Collections.Generic;
using hp55games.MapGame.Features.Configs;
using hp55games.Mobile.Core.Architecture;
using hp55games.Mobile.Core.Context;
using hp55games.Mobile.Core.Gameplay.Events;
using hp55games.Mobile.Core.Juice;
using UnityEngine;

namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    /// <summary>
    /// Controller di griglia esagonale. Gestisce stato tile, reachability e reveal.
    /// Nessuna logica di rendering: notifica gli ascoltatori con eventi C# puri.
    ///
    /// HP correnti: _context.Lives (clamped 0.._maxHp).
    /// Monete correnti: _context.Score (accumulate per run).
    /// _maxHp è locale perché IGameContextService non ha il concetto di massimo.
    ///
    /// Dimensioni e seed provengono da MapGenerationConfig (ScriptableObject).
    /// Inizializzazione HP/Monete avviene in OnGameStarted (via GameStartedEvent),
    /// NON in BuildGrid(), per evitare la race con GameplayState.ResetRun().
    /// Fallback autonomo in Start() per sessioni standalone senza FSM attivo.
    /// </summary>
    public sealed class HexGridController : MonoBehaviour
    {
        [Header("Configurazione mappa")]
        [SerializeField] private MapGenerationConfig _config;

        [Header("Sopravvivenza")]
        [SerializeField] private int _maxHp = 20;

        private IMapGenerationService _mapGenerationService;
        private IGameContextService  _context;
        private IEventBus            _bus;
        private IFeedbackService     _feedbackService;
        private IDisposable          _gameStartedSub;

        private readonly Dictionary<HexCoord, HexTileData> _tiles = new();
        private HexCoord _playerCoord;
        private HexCoord _objectiveCoord;

        public HexCoord PlayerCoord    => _playerCoord;
        public HexCoord ObjectiveCoord => _objectiveCoord;
        public IReadOnlyDictionary<HexCoord, HexTileData> Tiles => _tiles;

        /// <summary>La griglia è stata generata e tutte le tile sono nello stato iniziale (Sconosciuta, o Conosciuta se adiacenti alla partenza). Setup iniziale o rigenerazione.</summary>
        public event Action GridInitialized;

        /// <summary>Una tile è passata a Scoperta. Include i vicini per aggiornarne gli hint.</summary>
        public event Action<HexTileData, IReadOnlyList<HexTileData>> TileRevealed;

        /// <summary>La reachability è stata ricalcolata: le tile Sconosciuta adiacenti a una Scoperta sono promosse a Conosciuta (IsClickable aggiornato di conseguenza).</summary>
        public event Action ReachabilityChanged;

        private void Awake()
        {
            _context      = ServiceRegistry.Resolve<IGameContextService>();
            _bus          = ServiceRegistry.Resolve<IEventBus>();
            _mapGenerationService = ServiceRegistry.Resolve<IMapGenerationService>();
            BuildGrid();
        }

        private void Start()
        {
            // IFeedbackService resolved in Start() to avoid ordering issues:
            // FeedbackService.Awake() may not have run yet if both live in the same scene.
            ServiceRegistry.TryResolve<IFeedbackService>(out _feedbackService);

            // Subscribe for FSM-driven sessions: GameplayState publishes GameStartedEvent
            // after ResetRun() completes, giving us the correct moment to set HP/Monete.
            _gameStartedSub = _bus.Subscribe<GameStartedEvent>(OnGameStarted);

            // Standalone / prototype fallback: if no FSM is running, Lives is still at its
            // default (-1) at this point, so initialize immediately.
            if (_context.Lives < 0)
                InitializeSession();
        }

        private void OnDestroy()
        {
            _gameStartedSub?.Dispose();
        }

        private void OnGameStarted(GameStartedEvent _) => InitializeSession();

        /// <summary>
        /// Imposta HP e Monete a inizio sessione e pubblica i relativi eventi.
        /// Chiamato sia da GameStartedEvent (FSM path) sia da Start() come fallback
        /// standalone. Sicuro da invocare più volte: l'ultimo a farlo vince.
        /// </summary>
        private void InitializeSession()
        {
            _context.Lives = _maxHp;
            _context.Score = 0;
            _bus?.Publish(new HpChangedEvent());
            _bus?.Publish(new ScoreChangedEvent());
        }

        /// <summary>
        /// Rigenera la griglia da zero.
        /// Seed: da IGameContextService.CurrentRunSeed se != 0, altrimenti da _config.Seed.
        /// </summary>
        public void BuildGrid()
        {
            if (_config == null)
            {
                Debug.LogError("[HexGridController] _config non assegnato. Assegna un MapGenerationConfig nell'Inspector.", this);
                return;
            }

            int seed = (_context?.CurrentRunSeed != 0) ? _context.CurrentRunSeed : _config.Seed;
            var result = _mapGenerationService.GenerateMap(_config, seed);

            _tiles.Clear();
            foreach (var kvp in result.Tiles)
                _tiles[kvp.Key] = kvp.Value;

            _playerCoord    = result.StartCoord;
            _objectiveCoord = result.ObjectiveCoord;

            RecomputeReachability();
            GridInitialized?.Invoke();
        }

        private void RecomputeReachability()
        {
            foreach (var tile in _tiles.Values)
            {
                // Knowledge never regresses: only promote Sconosciuta → Conosciuta.
                // Tiles already Conosciuta or Scoperta are never touched here.
                if (tile.State != TileState.Sconosciuta) continue;

                foreach (var neighbor in GetNeighbors(tile.Coord))
                {
                    if (neighbor.State == TileState.Scoperta)
                    {
                        tile.State = TileState.Conosciuta;
                        break;
                    }
                }
            }

            ReachabilityChanged?.Invoke();
        }

        /// <summary>
        /// A tile is clickable if it exists, has not yet been resolved
        /// (State != Scoperta), and has at least one Scoperta neighbor.
        /// </summary>
        public bool IsClickable(HexCoord coord)
        {
            if (!_tiles.TryGetValue(coord, out var tile)) return false;
            if (tile.State == TileState.Scoperta) return false;

            foreach (var neighbor in GetNeighbors(coord))
            {
                if (neighbor.State == TileState.Scoperta)
                    return true;
            }

            return false;
        }

        public IReadOnlyList<HexTileData> GetNeighbors(HexCoord coord)
        {
            var result = new List<HexTileData>(6);
            for (int dir = 0; dir < 6; dir++)
            {
                if (_tiles.TryGetValue(coord.GetNeighbor(dir), out var neighbor))
                    result.Add(neighbor);
            }
            return result;
        }

        /// <summary>
        /// Tenta di rivelare una tile. Ritorna false se non in griglia o non cliccabile (IsClickable).
        /// Se la tile è Strada, esegue un flood-fill BFS su tutte le Strada non ancora Scoperta connesse.
        /// HP e Monete aggiornati per ogni tile rivelata; HpChangedEvent e ScoreChangedEvent
        /// pubblicati una sola volta al termine dell'intera azione (tap + cascade).
        /// PlayerDeathEvent emesso una sola volta se HP raggiunge 0.
        /// </summary>
        public bool TryRevealTile(HexCoord target)
        {
            if (!_tiles.TryGetValue(target, out var tile)) return false;
            if (!IsClickable(target)) return false;

            tile.State   = TileState.Scoperta;
            _playerCoord = target;
            AccumulateHp(tile);
            bool moneteEarned = AccumulateMonete(tile);

            if (tile.Type == TileType.Strada)
                CascadeStrada(target);

            RecomputeReachability();

            var neighbors = GetNeighbors(target);
            TileRevealed?.Invoke(tile, neighbors);

            // Publish once after all HP and Monete changes from this player action.
            _bus?.Publish(new HpChangedEvent());
            _feedbackService?.Play("hp_changed");

            if (moneteEarned)
                _bus?.Publish(new ScoreChangedEvent());

            if (_context.Lives <= 0)
            {
                _feedbackService?.Play("game_over");
                _bus?.Publish(new PlayerDeathEvent());
            }

            return true;
        }

        /// <summary>
        /// Applica l'HpRestore di una tile appena rivelata a _context.Lives (clamped).
        /// Non pubblica eventi — il publish avviene una sola volta per azione in TryRevealTile.
        /// </summary>
        private void AccumulateHp(HexTileData tile)
        {
            _context.Lives = Mathf.Clamp(_context.Lives + tile.HpRestore, 0, _maxHp);
        }

        /// <summary>
        /// Aggiunge le Monete della tile a _context.Score.
        /// Ritorna true se il valore è cambiato (per decidere se pubblicare ScoreChangedEvent).
        /// </summary>
        private bool AccumulateMonete(HexTileData tile)
        {
            if (tile.MoneteGained <= 0) return false;
            _context.Score += tile.MoneteGained;
            return true;
        }

        /// <summary>
        /// BFS flood-fill: rivela automaticamente tutte le tile Strada non ancora Scoperta
        /// raggiungibili dalla coordinata di partenza, senza emettere eventi di reveal.
        /// Accumula HP per ogni tile rivelata; il publish è responsabilità di TryRevealTile.
        /// </summary>
        private void CascadeStrada(HexCoord origin)
        {
            var queue = new Queue<HexCoord>();
            queue.Enqueue(origin);

            while (queue.Count > 0)
            {
                var coord = queue.Dequeue();
                foreach (var neighbor in GetNeighbors(coord))
                {
                    if (neighbor.Type == TileType.Strada && neighbor.State != TileState.Scoperta)
                    {
                        neighbor.State = TileState.Scoperta;
                        AccumulateHp(neighbor);
                        queue.Enqueue(neighbor.Coord);
                    }
                }
            }
        }
    }
}
