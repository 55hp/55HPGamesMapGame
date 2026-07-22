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
    /// Monete correnti: _context.Score — unica valuta di progressione (Economia 2026-07-20).
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
    /// (HasPendingEncounter/PendingEncounterTile) e pubblica EncounterStartedEvent su
    /// IEventBus (vedi EncounterEvents.cs). Il popup di Combattimento deve poi chiamare
    /// ResolveEncounterFight() o
    /// ResolveEncounterFlee(). Mentre un incontro e' pending, TryRevealTile ignora ogni
    /// altro click. Fuga: costo fisso FleeFoodCost in Cibo (clamp a 0, mai HP), la tile
    /// resta/torna Conosciuta (non e' mai stata marcata Scoperta) e il giocatore non si
    /// sposta — "l'informazione va persa" e' un fatto di UI (popup chiuso), non di dati
    /// (HexTileData non cambia, un nuovo tentativo mostra le stesse info).
    ///
    /// Effetto Enemy al combattimento: HP -= DifficultyLevel (nessuna ricompensa: XP
    /// rimosso 2026-07-20, ricompensa in Monete da definire — vedi Franci Tasks). No-op per
    /// Miniboss finche' il suo bilanciamento non e' definito. Applicato in
    /// ResolveEncounterFight, non in generazione, perche' il DifficultyLevel finale e'
    /// noto solo dopo ResolveDifficulty.
    ///
    /// Economia (2026-07-20): la progressione passa da Monete (_context.Score) e dallo shop
    /// — vedi la sezione Shop in fondo (TryBuyMaxHpUpgrade/FoodSlotUpgrade/Heal/FoodRefill,
    /// prezzi in EconomyConfig). XP/Livello rimossi.
    ///
    /// Win Condition (2026-07-20): rivelare la tile IsObjective da vivi pubblica
    /// PlayerVictoryEvent; la morte nello stesso click prevale.
    ///
    /// Inizializzazione avviene in OnGameStarted (GameStartedEvent), non in BuildGrid, per
    /// evitare la race con GameplayState.ResetRun(). Fallback in Start() per standalone.
    /// </summary>
    public sealed class HexGridController : MonoBehaviour
    {
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
        private EconomyConfig       _economy;

        // Cap runtime della run, inizializzati dalla baseline SurvivalConfig e alzati dai
        // potenziamenti dello shop. Non modificano mai l'asset SurvivalConfig.
        private int _currentMaxHp;
        private int _currentMaxFood;

        /// <summary>Cap HP corrente della run (baseline SurvivalConfig + potenziamenti acquistati).</summary>
        public int CurrentMaxHp => _currentMaxHp;

        /// <summary>Cap Cibo corrente della run (baseline SurvivalConfig + potenziamenti acquistati).</summary>
        public int CurrentMaxFood => _currentMaxFood;

        /// <summary>Monete correnti della run (facciata su _context.Score per la UI dello shop).</summary>
        public int CurrentMonete => _context?.Score ?? 0;

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

        // Gli eventi di incontro (EncounterStartedEvent/EncounterResolvedEvent) NON sono
        // C# event locali come i tre qui sopra: quelli servono al rendering della griglia
        // (coupling 1:1 con la view), gli incontri sono segnali di gameplay trasversali
        // consumati da sistemi disaccoppiati (popup UI, in futuro audio/missioni) e
        // viaggiano su IEventBus come HpChangedEvent/PlayerDeathEvent. Vedi EncounterEvents.cs.

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
            _economy   = _configs.Get<EconomyConfig>();

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

            _bus?.Publish(new HpChangedEvent());
            _bus?.Publish(new FoodChangedEvent());
            _bus?.Publish(new ScoreChangedEvent());
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

            // Rigenerazione = nessun incontro può sopravvivere: la coordinata pending
            // apparterrebbe a una mappa che non esiste più e bloccherebbe ogni click.
            _pendingEncounterCoord = null;

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
            else if (tile.IsObjective)
            {
                // Win Condition (2026-07-20): rivelare la tile obiettivo (Boss, IsObjective
                // da generazione) da vivi = vittoria. La morte prevale: se il costo movimento
                // dell'ultimo click azzera gli HP proprio sull'obiettivo, e' una sconfitta.
                // UIResultsPage inferisce vittoria da Lives > 0, coerente con questa regola.
                _feedbackService?.Play("victory");
                _bus?.Publish(new PlayerVictoryEvent());
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
            _bus?.Publish(new EncounterStartedEvent(tile, this));
        }

        /// <summary>
        /// Il giocatore ha scelto di combattere l'incontro pending. Risolve come il vecchio
        /// reveal istantaneo: Scoperta, danno Enemy da DifficultyLevel (no-op per Miniboss
        /// finche' il suo bilanciamento non e' definito), guadagni della tessera,
        /// reachability, eventi. Ritorna false se non c'e' nessun incontro pending.
        /// Ricompensa del combattimento: nessuna per ora — l'XP e' stato rimosso (Economia,
        /// 2026-07-20) e la ricompensa in Monete e' una decisione di design aperta (vedi
        /// Franci Tasks).
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

            ApplyEnemyDamage(tile);
            AccumulateHp(tile);
            AccumulateFood(tile);
            bool moneteEarned = AccumulateMonete(tile);

            RecomputeReachability();

            var neighbors = GetNeighbors(target);
            TileRevealed?.Invoke(tile, neighbors);
            _bus?.Publish(new EncounterResolvedEvent(tile, wasFought: true));

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
            else if (tile.IsObjective)
            {
                // Stessa regola di TryRevealTile: l'obiettivo oggi non passa da qui (Boss
                // non e' nel ramo encounter), ma se in futuro il Boss diventera' un
                // incontro, la vittoria post-combattimento e' gia' coperta. Morte prevale.
                _feedbackService?.Play("victory");
                _bus?.Publish(new PlayerVictoryEvent());
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

            _bus?.Publish(new EncounterResolvedEvent(tile, wasFought: false));
            _bus?.Publish(new FoodChangedEvent());

            return true;
        }

        /// <summary>
        /// Effetto Enemy: perdita HP pari al DifficultyLevel della tessera. No-op per i tipi
        /// non-Enemy (Miniboss: bilanciamento non definito, vedi backlog). L'XP e' stato
        /// rimosso (Economia, 2026-07-20): la progressione passa dalle Monete e dallo shop.
        /// Non pubblica eventi — il publish e' centralizzato nel chiamante.
        /// </summary>
        private void ApplyEnemyDamage(HexTileData tile)
        {
            if (tile.Type != TileType.Enemy) return;

            int level = Mathf.Max(0, tile.DifficultyLevel);
            if (level <= 0) return;

            _context.Lives = Mathf.Clamp(_context.Lives - level, 0, _currentMaxHp);
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

        // ------------------------- Shop (Economia, 2026-07-20) -------------------------
        // Potenziamenti diretti acquistabili in Monete (_context.Score), ricomprabili nella
        // stessa run. Sostituiscono il level up XP (rimosso). Prezzi ed entita' degli
        // effetti in EconomyConfig (bilanciamento in editor, mai hardcoded qui).
        // ATK/DEF/chiavi/vision boost: deferiti, i sistemi che li consumano non esistono
        // ancora (Combattimento / Knowledge). Ogni TryBuy ritorna false senza effetti se
        // le Monete non bastano o l'acquisto sarebbe inutile (es. cura a HP pieni).

        /// <summary>Alza il cap HP della run di EconomyConfig.MaxHpUpgradeAmount (le Monete lo consentono sempre: mai "inutile").</summary>
        public bool TryBuyMaxHpUpgrade()
        {
            if (!CanSpend(_economy?.MaxHpUpgradeCost)) return false;

            Spend(_economy.MaxHpUpgradeCost);
            _currentMaxHp += _economy.MaxHpUpgradeAmount;

            _bus?.Publish(new ScoreChangedEvent());
            _bus?.Publish(new HpChangedEvent()); // il cap e' cambiato, la UI degli stack deve saperlo
            return true;
        }

        /// <summary>Alza il cap Cibo della run di EconomyConfig.FoodSlotUpgradeAmount.</summary>
        public bool TryBuyFoodSlotUpgrade()
        {
            if (!CanSpend(_economy?.FoodSlotUpgradeCost)) return false;

            Spend(_economy.FoodSlotUpgradeCost);
            _currentMaxFood += _economy.FoodSlotUpgradeAmount;

            _bus?.Publish(new ScoreChangedEvent());
            _bus?.Publish(new FoodChangedEvent());
            return true;
        }

        /// <summary>Cura EconomyConfig.HealAmount HP (clamp al cap). Rifiutato a HP gia' pieni.</summary>
        public bool TryBuyHeal()
        {
            if (_context.Lives >= _currentMaxHp) return false;
            if (!CanSpend(_economy?.HealCost)) return false;

            Spend(_economy.HealCost);
            _context.Lives = Mathf.Clamp(_context.Lives + _economy.HealAmount, 0, _currentMaxHp);

            _bus?.Publish(new ScoreChangedEvent());
            _bus?.Publish(new HpChangedEvent());
            return true;
        }

        /// <summary>Aggiunge EconomyConfig.FoodRefillAmount Cibo (clamp al cap). Rifiutato a Cibo gia' pieno.</summary>
        public bool TryBuyFoodRefill()
        {
            if (_context.Food >= _currentMaxFood) return false;
            if (!CanSpend(_economy?.FoodRefillCost)) return false;

            Spend(_economy.FoodRefillCost);
            _context.Food = Mathf.Clamp(_context.Food + _economy.FoodRefillAmount, 0, _currentMaxFood);

            _bus?.Publish(new ScoreChangedEvent());
            _bus?.Publish(new FoodChangedEvent());
            return true;
        }

        /// <summary>null-safe: false se EconomyConfig manca dal catalogo o le Monete non bastano.</summary>
        private bool CanSpend(int? cost)
        {
            if (!cost.HasValue)
            {
                Debug.LogError("[HexGridController] EconomyConfig non risolto dal catalogo: acquisto rifiutato.", this);
                return false;
            }
            return _context.Score >= cost.Value;
        }

        private void Spend(int cost) => _context.Score -= cost;

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
