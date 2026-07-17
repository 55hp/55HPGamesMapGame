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
    /// Cibo corrente: _context.Food (clamped 0.._maxFood).
    /// Monete correnti: _context.Score (accumulate per run, oggi sempre 0 — economia Shop
    /// in pausa dal 2026-07-10, vedi Architecture Decisions Log).
    /// _maxHp e _maxFood sono locali perché IGameContextService non ha il concetto di
    /// massimo, e ora crescono a runtime col level up (non più costanti).
    ///
    /// Costo movimento (2026-07-10, Dragonsweeper-style): ogni click costa 1 Cibo se
    /// disponibile, altrimenti NoFoodHpPenalty (2) HP diretti. Si applica una sola volta
    /// per click, PRIMA dell'effetto della tessera — la cascata Strada che segue un click
    /// resta gratuita, non paga il costo per ogni tessera che rivela.
    ///
    /// Sistema XP/livello (2026-07-10): Enemy da' XP = proprio DifficultyLevel invece di
    /// Monete. L'XP si accumula ma non supera mai XpPerLevel (10) — l'eccesso oltre la
    /// soglia e' perso, non fa da riserva. Il level up NON e' automatico: richiede una
    /// chiamata esplicita a TryLevelUp() da un bottone UI (vedi MapGameGameplayHud).
    /// Al level up: +1 HP massimo sempre, +1 Cibo massimo ogni 4 livelli, HP rifornito
    /// completamente (puo' sovrascrivere nello stesso click il danno appena subito
    /// dall'Enemy che ha dato l'XP del level up — comportamento voluto, non un bug).
    ///
    /// Si auto-registra in ServiceRegistry al proprio Awake() (stesso pattern di
    /// FeedbackService), cosi' componenti caricati dopo a runtime — come l'HUD, che
    /// arriva via IUINavigationService e non e' presente in scena all'edit time — possono
    /// risolverlo senza bisogno di un riferimento serializzato in Inspector.
    ///
    /// Dimensioni e seed provengono da MapGenerationConfig (ScriptableObject).
    /// Inizializzazione HP/Cibo/Monete/Livello avviene in OnGameStarted (via
    /// GameStartedEvent), NON in BuildGrid(), per evitare la race con
    /// GameplayState.ResetRun(). Fallback autonomo in Start() per sessioni standalone
    /// senza FSM attivo.
    /// </summary>
    public sealed class HexGridController : MonoBehaviour
    {
        public const int XpPerLevel = 10;

        [Header("Configurazione mappa")]
        [SerializeField] private MapGenerationConfig _config;

        [Header("Sopravvivenza (valori di partenza, crescono col level up)")]
        [SerializeField] private int _maxHp = 3;
        [SerializeField] private int _maxFood = 3;

        [Tooltip("HP persi in un click quando il Cibo è già a 0.")]
        [SerializeField] private int _noFoodHpPenalty = 2;

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
        public int MaxHp   => _maxHp;
        public int MaxFood => _maxFood;

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
            ServiceRegistry.Register<HexGridController>(this);
            BuildGrid();
        }

        private void Start()
        {
            // IFeedbackService resolved in Start() to avoid ordering issues:
            // FeedbackService.Awake() may not have run yet if both live in the same scene.
            ServiceRegistry.TryResolve<IFeedbackService>(out _feedbackService);

            // Subscribe for FSM-driven sessions: GameplayState publishes GameStartedEvent
            // after ResetRun() completes, giving us the correct moment to set HP/Cibo/Monete.
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
        /// Imposta HP, Cibo, Livello, XP e Monete a inizio sessione e pubblica i relativi
        /// eventi. Chiamato sia da GameStartedEvent (FSM path) sia da Start() come
        /// fallback standalone. Sicuro da invocare più volte: l'ultimo a farlo vince.
        /// </summary>
        private void InitializeSession()
        {
            _context.Lives = _maxHp;
            _context.Food  = _maxFood;
            _context.PlayerLevel = 1;
            _context.Xp    = 0;
            _context.Score = 0;
            _bus?.Publish(new HpChangedEvent());
            _bus?.Publish(new FoodChangedEvent());
            _bus?.Publish(new XpChangedEvent());
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
        /// Independent from DifficultyLevel / icon visibility (see CountScopertaNeighbors) —
        /// clickability always uses the "at least 1" rule, unchanged by the 2026-07-10 revision.
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

        /// <summary>
        /// Conta quanti vicini di coord sono attualmente Scoperta. Usato per il gate di
        /// rivelazione icona guidato da DifficultyLevel (vedi HexGridViewSpawner): l'icona
        /// di una tile Conosciuta diventa visibile solo quando questo conteggio raggiunge
        /// il DifficultyLevel della tile. Indipendente da IsClickable, che resta a soglia 1.
        /// </summary>
        public int CountScopertaNeighbors(HexCoord coord)
        {
            int count = 0;
            foreach (var neighbor in GetNeighbors(coord))
            {
                if (neighbor.State == TileState.Scoperta)
                    count++;
            }
            return count;
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
        /// Applica il costo movimento una sola volta (ApplyMovementCost), poi l'effetto
        /// della tessera stessa (HP/Cibo/Monete/XP). Se la tile è Strada, esegue un
        /// flood-fill BFS su tutte le Strada non ancora Scoperta connesse — la cascata
        /// non paga il costo movimento, solo il click che l'ha innescata.
        /// HpChangedEvent/FoodChangedEvent/XpChangedEvent/ScoreChangedEvent pubblicati una
        /// sola volta al termine dell'intera azione (tap + cascade). PlayerDeathEvent
        /// emesso una sola volta se HP raggiunge 0, indipendentemente dal fatto che sia
        /// stato il costo movimento o l'effetto della tessera a portarlo a 0.
        /// </summary>
        public bool TryRevealTile(HexCoord target)
        {
            if (!_tiles.TryGetValue(target, out var tile)) return false;
            if (!IsClickable(target)) return false;

            tile.State   = TileState.Scoperta;
            _playerCoord = target;

            ApplyMovementCost();
            AccumulateHp(tile);
            AccumulateFood(tile);
            bool moneteEarned = AccumulateMonete(tile);
            AccumulateXp(tile);

            if (tile.Type == TileType.Path)
                CascadeStrada(target);

            RecomputeReachability();

            var neighbors = GetNeighbors(target);
            TileRevealed?.Invoke(tile, neighbors);

            // Publish once after all HP, Cibo, XP and Monete changes from this player action.
            _bus?.Publish(new HpChangedEvent());
            _bus?.Publish(new FoodChangedEvent());
            _bus?.Publish(new XpChangedEvent());
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
        /// Costo movimento: 1 Cibo se disponibile, altrimenti _noFoodHpPenalty HP diretti.
        /// Si applica una sola volta per click, prima dell'effetto della tessera.
        /// </summary>
        private void ApplyMovementCost()
        {
            if (_context.Food > 0)
                _context.Food -= 1;
            else
                _context.Lives = Mathf.Clamp(_context.Lives - _noFoodHpPenalty, 0, _maxHp);
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
        /// Applica il FoodRestore di una tile appena rivelata a _context.Food (clamped).
        /// Non pubblica eventi — il publish avviene una sola volta per azione in TryRevealTile.
        /// </summary>
        private void AccumulateFood(HexTileData tile)
        {
            _context.Food = Mathf.Clamp(_context.Food + tile.FoodRestore, 0, _maxFood);
        }

        /// <summary>
        /// Aggiunge le Monete della tile a _context.Score.
        /// Ritorna true se il valore è cambiato (per decidere se pubblicare ScoreChangedEvent).
        /// Oggi MoneteGained è sempre 0 (economia Shop in pausa, 2026-07-10) — il metodo
        /// resta comunque per non dover ricollegare nulla se l'economia torna in gioco.
        /// </summary>
        private bool AccumulateMonete(HexTileData tile)
        {
            if (tile.MoneteGained <= 0) return false;
            _context.Score += tile.MoneteGained;
            return true;
        }

        /// <summary>
        /// Aggiunge XP solo per Enemy, pari al DifficultyLevel della tile appena
        /// sconfitta. L'XP non supera mai XpPerLevel: l'eccesso oltre la soglia è perso
        /// per design, non fa da riserva per il livello successivo — vedi TryLevelUp.
        /// </summary>
        private void AccumulateXp(HexTileData tile)
        {
            if (tile.Type != TileType.Enemy) return;
            _context.Xp = Mathf.Min(_context.Xp + tile.DifficultyLevel, XpPerLevel);
        }

        /// <summary>
        /// Level up esplicito, non automatico: va chiamato da un'azione del giocatore
        /// (bottone UI, vedi MapGameGameplayHud) quando l'XP è pieno. Ritorna false senza
        /// fare nulla se l'XP non ha ancora raggiunto XpPerLevel.
        /// Effetti: +1 HP massimo sempre, +1 Cibo massimo ogni 4 livelli, HP rifornito
        /// completamente (Lives = nuovo _maxHp).
        /// </summary>
        public bool TryLevelUp()
        {
            if (_context.Xp < XpPerLevel) return false;

            _context.Xp = 0;
            _context.PlayerLevel += 1;
            _maxHp += 1;

            if (_context.PlayerLevel % 4 == 0)
                _maxFood += 1;

            _context.Lives = _maxHp;

            _bus?.Publish(new HpChangedEvent());
            _bus?.Publish(new XpChangedEvent());
            _feedbackService?.Play("hp_changed");

            return true;
        }

        /// <summary>
        /// BFS flood-fill: rivela automaticamente tutte le tile Strada non ancora Scoperta
        /// raggiungibili dalla coordinata di partenza, senza emettere eventi di reveal e
        /// senza applicare il costo movimento (gratis, fa parte della stessa azione).
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
                    if (neighbor.Type == TileType.Path && neighbor.State != TileState.Scoperta)
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