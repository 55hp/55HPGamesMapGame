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
    /// Controller di griglia esagonale. Gestisce stato tile, reachability, reveal e la
    /// progressione XP/Livello del personaggio. Nessuna logica di rendering: notifica gli
    /// ascoltatori con eventi C# puri.
    ///
    /// HP correnti: _context.Lives (clamped 0.._currentMaxHp).
    /// Cibo corrente: _context.Food (clamped 0.._currentMaxFood).
    /// Monete correnti: _context.Score (in pausa: nessun tipo assegna Monete per ora).
    /// XP corrente: _context.Xp (0..XpPerLevel). Livello: _context.Level (parte da 1).
    ///
    /// Configurazione: nessun config serializzato in scena. In Awake risolve
    /// IConfigCatalogService e pesca MapGenerationConfig, SurvivalConfig e il LevelConfig
    /// attivo dal catalogo (Get&lt;LevelConfig&gt;() assume un solo livello autorato; con piu'
    /// livelli servira' una selezione esplicita, vedi backlog).
    ///
    /// Cap runtime vs baseline: SurvivalConfig.MaxHp/MaxFood sono la BASELINE. Il cap
    /// effettivo di questa run vive in _currentMaxHp/_currentMaxFood, inizializzati dalla
    /// baseline a inizio sessione e alzati dal level up — NON si tocca l'asset SurvivalConfig
    /// (condiviso, le modifiche persisterebbero tra sessioni). StartHp/StartFood restano il
    /// punto di partenza, distinto dal cap.
    ///
    /// Costo movimento (Dragonsweeper-style): ogni click costa _survival.FoodCostPerClick
    /// Cibo se disponibile, altrimenti _survival.NoFoodHpPenalty HP diretti. Una sola volta
    /// per click, PRIMA dell'effetto della tessera; la cascata Strada resta gratuita.
    ///
    /// Incontro Enemy/Miniboss (2026-07-20, revisione Combat & NPC 2026-07-19): NON piu'
    /// istantaneo. Il click su Enemy/Miniboss applica il costo movimento ma NON marca la
    /// tile Scoperta ne' risolve il combattimento: apre un incontro pending
    /// (HasPendingEncounter/PendingEncounterTile) e pubblica EncounterStarted. Il popup di
    /// Combattimento (feature Combattimento) deve poi chiamare ResolveEncounterFight() o
    /// ResolveEncounterFlee(). Mentre un incontro e' pending, TryRevealTile ignora ogni
    /// altro click. Fuga: costo fisso FleeFoodCost in Cibo (clamp a 0, mai HP), la tile
    /// resta/torna Conosciuta (non e' mai stata marcata Scoperta) e il giocatore non si
    /// sposta — "l'informazione va persa" e' un fatto di UI (popup chiuso), non di dati
    /// (HexTileData non cambia, un nuovo tentativo mostra le stesse info).
    ///
    /// Effetto Enemy al combattimento: HP -= DifficultyLevel e XP += DifficultyLevel
    /// (cappato a XpPerLevel, l'eccesso e' perso). No-op per Miniboss finche' il suo
    /// bilanciamento non e' definito (vedi Combattimento, backlog). Applicato in
    /// ResolveEncounterFight, non in generazione, perche' il DifficultyLevel finale e'
    /// noto solo dopo ResolveDifficulty.
    ///
    /// Level up (TryLevelUp): esplicito, non automatico. Serve XP == XpPerLevel; a ogni
    /// level up: Livello +1, cap HP +1 sempre, cap Cibo +1 ogni 4 livelli, HP rifornito al
    /// nuovo cap, XP azzerato.
    ///
    /// Inizializzazione avviene in OnGameStarted (GameStartedEvent), non in BuildGrid, per
    /// evitare la race con GameplayState.ResetRun(). Fallback in Start() per standalone.
    /// </summary>
    public sealed class HexGridController : MonoBehaviour
    {
        /// <summary>XP necessari per un level up. L'XP si cappa qui; l'eccesso è perso.</summary>
        public const int XpPerLevel = 10;

        /// <summary>Ogni quanti livelli il level up concede +1 al cap del Cibo.</summary>
        private const int FoodSlotEveryLevels = 4;

        /// <summary>
        /// Costo fisso in Cibo per fuggire da un incontro Enemy/Miniboss pending. Valore di
        /// design confermato 2026-07-19, distinto da FoodCostPerClick/NoFoodHpPenalty
        /// (SurvivalConfig): specifico dell'azione fuga, non un parametro di sopravvivenza
        /// generico. Se il Cibo residuo non copre il costo si clampa a 0 — a differenza del
        /// costo movimento normale, la fuga NON ricade mai su HP.
        /// </summary>
        private const int FleeFoodCost = 2;

        private IMapGenerationService _mapGenerationService;
        private IConfigCatalogService _configs;
        private IGameContextService   _context;
        private IEventBus             _bus;
        private IFeedbackService      _feedbackService;
        private IDisposable           _gameStartedSub;

        private MapGenerationConfig _mapConfig;
        private SurvivalConfig      _survival;
        private LevelConfig         _level;

        // Cap runtime della run, inizializzati dalla baseline SurvivalConfig e alzati dal
        // level up. Non modificano mai l'asset SurvivalConfig.
        private int _currentMaxHp;
        private int _currentMaxFood;

        private readonly Dictionary<HexCoord, HexTileData> _tiles = new();
        private HexCoord _playerCoord;
        private HexCoord _objectiveCoord;

        /// <summary>
        /// Coordinata dell'incontro Enemy/Miniboss in attesa di risoluzione (combatti/fuggi),
        /// null se nessun incontro e' pending. Vedi ResolveEncounterFight/ResolveEncounterFlee.
        /// </summary>
        private HexCoord? _pendingEncounterCoord;

        public HexCoord PlayerCoord    => _playerCoord;
        public HexCoord ObjectiveCoord => _objectiveCoord;
        public IReadOnlyDictionary<HexCoord, HexTileData> Tiles => _tiles;

        /// <summary>True se un incontro Enemy/Miniboss e' in attesa di combatti/fuggi. Mentre e' true, TryRevealTile ignora ogni click.</summary>
        public bool HasPendingEncounter => _pendingEncounterCoord.HasValue;

        /// <summary>Dati della tile dell'incontro pending, null se nessun incontro e' in corso.</summary>
        public HexTileData PendingEncounterTile =>
            _pendingEncounterCoord.HasValue && _tiles.TryGetValue(_pendingEncounterCoord.Value, out var tile) ? tile : null;

        /// <summary>La griglia è stata generata e tutte le tile sono nello stato iniziale (Sconosciuta, o Conosciuta se adiacenti alla partenza). Setup iniziale o rigenerazione.</summary>
        public event Action GridInitialized;

        /// <summary>Una tile è passata a Scoperta. Include i vicini per aggiornarne gli hint.</summary>
        public event Action<HexTileData, IReadOnlyList<HexTileData>> TileRevealed;

        /// <summary>La reachability è stata ricalcolata: le tile Sconosciuta adiacenti a una Scoperta sono promosse a Conosciuta (IsClickable aggiornato di conseguenza).</summary>
        public event Action ReachabilityChanged;

        /// <summary>
        /// Click su una tile Enemy/Miniboss: costo movimento gia' applicato, ma la tile NON
        /// e' ancora Scoperta e il combattimento non e' ancora risolto. Il popup di
        /// Combattimento deve ascoltare questo evento, mostrare le info nemico (tile.Type,
        /// tile.DifficultyLevel) e poi chiamare ResolveEncounterFight() o
        /// ResolveEncounterFlee() in base alla scelta del giocatore.
        /// </summary>
        public event Action<HexTileData> EncounterStarted;

        /// <summary>
        /// L'incontro pending e' stato risolto. wasFought: true se combattuto (tile ora
        /// Scoperta), false se si e' fuggiti (tile invariata). Il popup deve chiudersi a
        /// questo evento.
        /// </summary>
        public event Action<HexTileData, bool> EncounterResolved;

        private void Awake()
        {
            _context              = ServiceRegistry.Resolve<IGameContextService>();
            _bus                  = ServiceRegistry.Resolve<IEventBus>();
            _mapGenerationService = ServiceRegistry.Resolve<IMapGenerationService>();

            if (!ServiceRegistry.TryResolve(out _configs))
            {
                Debug.LogError("[HexGridController] IConfigCatalogService non risolto. Verifica che ConfigCatalogInstaller sia in una scena caricata prima di questa.", this);
                return;
            }

            _mapConfig = _configs.Get<MapGenerationConfig>();
            _survival  = _configs.Get<SurvivalConfig>();
            _level     = _configs.Get<LevelConfig>();

            BuildGrid();
        }

        private void Start()
        {
            // IFeedbackService resolved in Start() to avoid ordering issues:
            // FeedbackService.Awake() may not have run yet if both live in the same scene.
            ServiceRegistry.TryResolve<IFeedbackService>(out _feedbackService);

            // Subscribe for FSM-driven sessions: GameplayState publishes GameStartedEvent
            // after ResetRun() completes, giving us the correct moment to set HP/Cibo/XP.
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
        /// Imposta HP, Cibo, Monete, XP e Livello a inizio sessione, inizializza i cap
        /// runtime dalla baseline SurvivalConfig e pubblica i relativi eventi. Chiamato sia
        /// da GameStartedEvent (FSM) sia da Start() come fallback. Sicuro da invocare più
        /// volte: l'ultimo a farlo vince.
        /// </summary>
        private void InitializeSession()
        {
            if (_survival == null)
            {
                Debug.LogError("[HexGridController] SurvivalConfig non risolto dal catalogo. Impossibile inizializzare la sessione.", this);
                return;
            }

            _currentMaxHp   = _survival.MaxHp;
            _currentMaxFood = _survival.MaxFood;

            _context.Lives = _survival.StartHp;
            _context.Food  = _survival.StartFood;
            _context.Score = 0;
            _context.Xp    = 0;
            _context.Level = 1;

            _bus?.Publish(new HpChangedEvent());
            _bus?.Publish(new FoodChangedEvent());
            _bus?.Publish(new ScoreChangedEvent());
            _bus?.Publish(new XpChangedEvent());
        }

        /// <summary>
        /// Rigenera la griglia da zero.
        /// Seed: da IGameContextService.CurrentRunSeed se != 0, altrimenti da _mapConfig.Seed.
        /// </summary>
        public void BuildGrid()
        {
            if (_mapConfig == null)
            {
                Debug.LogError("[HexGridController] MapGenerationConfig non risolto dal catalogo. Impossibile generare la mappa.", this);
                return;
            }

            if (_mapGenerationService == null)
            {
                Debug.LogError("[HexGridController] IMapGenerationService non risolto dal ServiceRegistry. Verifica che MapGenerationServiceInstaller sia in scena e attivo.", this);
                return;
            }

            int seed = (_context?.CurrentRunSeed != 0) ? _context.CurrentRunSeed : _mapConfig.Seed;
            var result = _mapGenerationService.GenerateMap(_mapConfig, _level, seed);

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
        /// (State != Scoperta), and has at least one Scoperta neighbor. Independent from
        /// DifficultyLevel / icon visibility — clickability always uses the "at least 1" rule.
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
        /// rivelazione icona guidato da DifficultyLevel (vedi HexGridViewSpawner).
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
        /// Tenta di rivelare una tile. Ritorna false se non in griglia, non cliccabile, o se
        /// un incontro e' gia' pending. Per Enemy/Miniboss non risolve subito: applica il
        /// costo movimento e apre un incontro pending (vedi StartEncounter). Per tutti gli
        /// altri tipi risolve come sempre: costo movimento (una volta) → HpRestore/
        /// FoodRestore/Monete della tessera → cascata Strada gratuita se Strada. Eventi
        /// pubblicati una sola volta a fine azione. PlayerDeathEvent una sola volta se HP
        /// arriva a 0.
        /// </summary>
        public bool TryRevealTile(HexCoord target)
        {
            if (_pendingEncounterCoord.HasValue) return false;
            if (!_tiles.TryGetValue(target, out var tile)) return false;
            if (!IsClickable(target)) return false;

            if (tile.Type == TileType.Enemy || tile.Type == TileType.Miniboss)
            {
                StartEncounter(target, tile);
                return true;
            }

            tile.State   = TileState.Scoperta;
            _playerCoord = target;

            ApplyMovementCost();
            AccumulateHp(tile);
            AccumulateFood(tile);
            bool moneteEarned = AccumulateMonete(tile);

            if (tile.Type == TileType.Path)
                CascadeStrada(target);

            RecomputeReachability();

            var neighbors = GetNeighbors(target);
            TileRevealed?.Invoke(tile, neighbors);

            // Publish once after all changes from this player action.
            _bus?.Publish(new HpChangedEvent());
            _bus?.Publish(new FoodChangedEvent());
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
        /// Avvia un incontro Enemy/Miniboss: applica il costo movimento (una tantum, come
        /// ogni click) ma NON marca la tile Scoperta e NON risolve il combattimento — resta
        /// in attesa che il popup di Combattimento chiami ResolveEncounterFight/Flee. Non
        /// tocca _playerCoord: finche' l'incontro e' pending il giocatore non si e' "mosso"
        /// sulla tile (coerente con la fuga, che la lascia coperta). Se il costo movimento
        /// stesso azzera gli HP, l'incontro non si apre: si va dritti a PlayerDeathEvent.
        /// </summary>
        private void StartEncounter(HexCoord target, HexTileData tile)
        {
            ApplyMovementCost();
            _bus?.Publish(new HpChangedEvent());
            _bus?.Publish(new FoodChangedEvent());
            _feedbackService?.Play("hp_changed");

            if (_context.Lives <= 0)
            {
                _feedbackService?.Play("game_over");
                _bus?.Publish(new PlayerDeathEvent());
                return;
            }

            _pendingEncounterCoord = target;
            EncounterStarted?.Invoke(tile);
        }

        /// <summary>
        /// Il giocatore ha scelto di combattere l'incontro pending. Risolve come il vecchio
        /// reveal istantaneo: Scoperta, danno/XP Enemy da DifficultyLevel (no-op per
        /// Miniboss finche' il suo bilanciamento non e' definito), guadagni della tessera,
        /// reachability, eventi. Ritorna false se non c'e' nessun incontro pending.
        /// </summary>
        public bool ResolveEncounterFight()
        {
            if (!_pendingEncounterCoord.HasValue) return false;
            var target = _pendingEncounterCoord.Value;
            if (!_tiles.TryGetValue(target, out var tile))
            {
                _pendingEncounterCoord = null;
                return false;
            }

            _pendingEncounterCoord = null;

            tile.State   = TileState.Scoperta;
            _playerCoord = target;

            bool xpGained = ApplyEnemyOutcome(tile);
            AccumulateHp(tile);
            AccumulateFood(tile);
            bool moneteEarned = AccumulateMonete(tile);

            RecomputeReachability();

            var neighbors = GetNeighbors(target);
            TileRevealed?.Invoke(tile, neighbors);
            EncounterResolved?.Invoke(tile, true);

            _bus?.Publish(new HpChangedEvent());
            _bus?.Publish(new FoodChangedEvent());
            _feedbackService?.Play("hp_changed");

            if (moneteEarned)
                _bus?.Publish(new ScoreChangedEvent());

            if (xpGained)
                _bus?.Publish(new XpChangedEvent());

            if (_context.Lives <= 0)
            {
                _feedbackService?.Play("game_over");
                _bus?.Publish(new PlayerDeathEvent());
            }

            return true;
        }

        /// <summary>
        /// Il giocatore ha scelto di fuggire dall'incontro pending. Costo fisso
        /// FleeFoodCost in Cibo, clampato a 0 (mai HP, a differenza del costo movimento
        /// normale). La tile resta/torna Conosciuta — non e' mai stata marcata Scoperta —
        /// e _playerCoord non cambia: il giocatore non si e' mai spostato sulla tile.
        /// L'informazione sul nemico e' persa solo lato UI (il popup si chiude); i dati
        /// (HexTileData.Type/DifficultyLevel) non cambiano, un nuovo tentativo mostrera' le
        /// stesse info. Ritorna false se non c'e' nessun incontro pending.
        /// </summary>
        public bool ResolveEncounterFlee()
        {
            if (!_pendingEncounterCoord.HasValue) return false;
            var target = _pendingEncounterCoord.Value;
            if (!_tiles.TryGetValue(target, out var tile))
            {
                _pendingEncounterCoord = null;
                return false;
            }

            _pendingEncounterCoord = null;

            _context.Food = Mathf.Max(0, _context.Food - FleeFoodCost);

            EncounterResolved?.Invoke(tile, false);
            _bus?.Publish(new FoodChangedEvent());

            return true;
        }

        /// <summary>
        /// Effetto Enemy: perdita HP pari al DifficultyLevel della tessera e guadagno XP
        /// pari allo stesso valore (cappato a XpPerLevel, eccesso perso). No-op per i tipi
        /// non-Enemy. Ritorna true se l'XP è cambiato (per pubblicare XpChangedEvent).
        /// Non pubblica eventi direttamente — il publish è centralizzato in TryRevealTile.
        /// </summary>
        private bool ApplyEnemyOutcome(HexTileData tile)
        {
            if (tile.Type != TileType.Enemy) return false;

            int level = Mathf.Max(0, tile.DifficultyLevel);
            if (level <= 0) return false;

            _context.Lives = Mathf.Clamp(_context.Lives - level, 0, _currentMaxHp);

            int before = _context.Xp;
            _context.Xp = Mathf.Min(_context.Xp + level, XpPerLevel);
            return _context.Xp != before;
        }

        /// <summary>
        /// Costo movimento: _survival.FoodCostPerClick Cibo se disponibile, altrimenti
        /// _survival.NoFoodHpPenalty HP diretti. Se il Cibo residuo non copre l'intero
        /// costo, si paga in HP (nessun pagamento parziale in Cibo).
        /// </summary>
        private void ApplyMovementCost()
        {
            if (_context.Food >= _survival.FoodCostPerClick)
                _context.Food -= _survival.FoodCostPerClick;
            else
                _context.Lives = Mathf.Clamp(_context.Lives - _survival.NoFoodHpPenalty, 0, _currentMaxHp);
        }

        private void AccumulateHp(HexTileData tile)
        {
            _context.Lives = Mathf.Clamp(_context.Lives + tile.HpRestore, 0, _currentMaxHp);
        }

        private void AccumulateFood(HexTileData tile)
        {
            _context.Food = Mathf.Clamp(_context.Food + tile.FoodRestore, 0, _currentMaxFood);
        }

        private bool AccumulateMonete(HexTileData tile)
        {
            if (tile.MoneteGained <= 0) return false;
            _context.Score += tile.MoneteGained;
            return true;
        }

        /// <summary>
        /// Level up esplicito. Richiede XP == XpPerLevel; altrimenti ritorna false senza
        /// fare nulla (pensato per essere invocato da un bottone UI che si attiva solo
        /// quando l'XP è pieno). A ogni level up riuscito: Livello +1, cap HP +1 sempre,
        /// cap Cibo +1 ogni FoodSlotEveryLevels livelli, HP rifornito al nuovo cap, XP
        /// azzerato. Pubblica gli eventi rilevanti.
        /// </summary>
        public bool TryLevelUp()
        {
            if (_context.Xp < XpPerLevel) return false;

            _context.Level += 1;
            _currentMaxHp  += 1;

            bool foodCapRaised = (_context.Level % FoodSlotEveryLevels) == 0;
            if (foodCapRaised)
                _currentMaxFood += 1;

            _context.Lives = _currentMaxHp; // refill completo al nuovo cap
            _context.Xp    = 0;

            _bus?.Publish(new HpChangedEvent());
            if (foodCapRaised)
                _bus?.Publish(new FoodChangedEvent());
            _bus?.Publish(new XpChangedEvent());

            return true;
        }

        /// <summary>
        /// BFS flood-fill: rivela tutte le tile Strada non ancora Scoperta connesse, senza
        /// eventi di reveal e senza costo movimento (gratis, stessa azione). Accumula HP per
        /// ogni tile rivelata; il publish è responsabilità di TryRevealTile.
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
