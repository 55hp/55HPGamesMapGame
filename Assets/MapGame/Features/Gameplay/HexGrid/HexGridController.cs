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
    /// progressione del personaggio. Nessuna logica di rendering: notifica gli ascoltatori
    /// con eventi C# puri.
    ///
    /// HP correnti: _context.Lives (clamped 0.._currentMaxHp).
    /// Cibo corrente: _context.Food (clamped 0.._currentMaxFood).
    /// Monete correnti: _context.Score — unica valuta di progressione.
    ///
    /// Configurazione: nessun config serializzato in scena. In Awake risolve
    /// IConfigCatalogService e pesca MapGenerationConfig, SurvivalConfig e il LevelConfig
    /// attivo dal catalogo (Get&lt;LevelConfig&gt;() assume un solo livello autorato; con piu'
    /// livelli servira' una selezione esplicita, vedi backlog).
    ///
    /// Cap runtime vs baseline: SurvivalConfig.MaxHp/MaxFood sono la BASELINE. Il cap
    /// effettivo di questa run vive in _currentMaxHp/_currentMaxFood, inizializzati dalla
    /// baseline a inizio sessione e alzati dagli acquisti shop — NON si tocca l'asset
    /// SurvivalConfig (condiviso, le modifiche persisterebbero tra sessioni). StartHp/
    /// StartFood restano il punto di partenza, distinto dal cap.
    ///
    /// Costo movimento (Dragonsweeper-style): ogni click costa _survival.FoodCostPerClick
    /// Cibo se disponibile, altrimenti _survival.NoFoodHpPenalty HP diretti. Una sola volta
    /// per click, PRIMA dell'effetto della tessera; la cascata Strada resta gratuita.
    ///
    /// Incontro Enemy/Miniboss: NON istantaneo. Il click su Enemy/Miniboss applica il
    /// costo movimento ma NON marca la tile Scoperta ne' risolve il combattimento: apre un
    /// incontro pending (HasPendingEncounter/PendingEncounterTile) e pubblica
    /// EncounterStartedEvent su IEventBus (vedi EncounterEvents.cs). Il popup di
    /// Combattimento deve poi chiamare ResolveEncounterFight() o ResolveEncounterFlee().
    /// Mentre un incontro e' pending, TryRevealTile ignora ogni altro click. Fuga: richiede
    /// Cibo >= FleeFoodCost — CanFlee espone il gate alla UI (bottone disabilitato),
    /// ResolveEncounterFlee lo rifiuta come difesa in profondita'. Con Cibo insufficiente
    /// l'unica opzione e' combattere. A fuga riuscita la tile resta/torna Conosciuta (non
    /// e' mai stata marcata Scoperta) e il giocatore non si sposta — nessuno snapshot da
    /// ripristinare, perche' il reveal non era mai stato scritto. "L'informazione va persa"
    /// e' un fatto di UI (popup chiuso) e di memoria del giocatore, non di dati (HexTileData
    /// non cambia, un nuovo tentativo mostra le stesse info).
    ///
    /// Element a effetto Immediate (vedi ApplyImmediateElement): Trap (HP -= DifficultyLevel),
    /// Fountain (HP al massimo), Tree (Cibo al massimo), Key (attiva HasKeyDL4/5/6 sul
    /// contesto + KeysChangedEvent). Chest e' inerte al reveal finche' il popup Loot non
    /// esiste.
    ///
    /// Effetto Enemy al combattimento: HP -= DifficultyLevel, ricompensa in Monete pari al
    /// DifficultyLevel. Applicato in ResolveEncounterFight, non in generazione, perche' il
    /// DifficultyLevel finale e' noto solo dopo ResolveDifficulty.
    ///
    /// Economia: la progressione passa da Monete (_context.Score) e dallo shop — vedi la
    /// sezione Shop in fondo (TryBuyMaxHpUpgrade/FoodSlotUpgrade/Heal/FoodRefill, prezzi in
    /// EconomyConfig).
    ///
    /// Win Condition: rivelare la tile IsObjective da vivi pubblica PlayerVictoryEvent; la
    /// morte nello stesso click prevale.
    ///
    /// Trader (RevealEffect.Trade): a differenza di Enemy la tile si rivela subito come
    /// qualunque altra (nessun effetto economico) — non c'e' una scelta combatti/fuggi che
    /// ne condizioni lo stato. In coda a TryRevealTile pubblica TradeStartedEvent (vedi
    /// TradeEvents.cs), consumato da RevealEffectPopupDispatcher che apre UIPopup_Shop.
    ///
    /// _pendingTradeCoord: pur non avendo una scelta da risolvere, il Trader USA comunque un
    /// gate pending come Enemy, per tutta la durata del popup Shop (non solo il caricamento
    /// Addressables) — vedi doc sul campo. Necessario perche' HexTileTapController legge
    /// input grezzo (IInputService.Tap), non bloccato dallo scrim del popup: senza il gate,
    /// un tap letale su un'altra tile mentre lo Shop e' aperto apriva/lasciava aperto il
    /// popup su un giocatore gia' morto. Liberato da ResolveTrade(), chiamato da
    /// UIPopup_Shop.OnDestroy qualunque sia la via di chiusura (bottone Chiudi, tap sullo
    /// scrim, CloseAll) — legarlo al solo bottone avrebbe lasciato TryRevealTile bloccato
    /// per il resto della run se il giocatore chiudeva lo shop toccando fuori.
    ///
    /// Inizializzazione avviene in OnGameStarted (GameStartedEvent), non in BuildGrid, per
    /// evitare la race con GameplayState.ResetRun(). Fallback in Start() per standalone.
    /// </summary>
    public sealed class HexGridController : MonoBehaviour
    {
        /// <summary>
        /// Costo fisso in Cibo per fuggire da un incontro Enemy/Miniboss pending, distinto
        /// da FoodCostPerClick/NoFoodHpPenalty (SurvivalConfig): specifico dell'azione fuga,
        /// non un parametro di sopravvivenza generico. E' anche un GATE: la fuga richiede
        /// Cibo >= FleeFoodCost (vedi CanFlee) — con Cibo insufficiente la fuga non e'
        /// permessa e l'unica opzione e' combattere. La fuga non ricade mai su HP.
        /// </summary>
        private const int FleeFoodCost = 2;

        /// <summary>
        /// True se il giocatore ha abbastanza Cibo per fuggire dall'incontro pending E la
        /// tile pending non e' l'obiettivo (niente fuga sulla tile finale, il player deve
        /// combattere). La UI del popup lo usa per disabilitare il bottone Fuggi;
        /// ResolveEncounterFlee applica lo stesso gate come difesa in profondita'.
        /// </summary>
        public bool CanFlee => _context != null
                               && _context.Food >= FleeFoodCost
                               && (PendingEncounterTile == null || !PendingEncounterTile.IsObjective);

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

        /// <summary>
        /// Coordinata del Trader in attesa di chiusura del popup Shop, null se nessun trade
        /// e' pending. Stesso precedente di _pendingEncounterCoord: TryRevealTile blocca
        /// ogni altro click finche' resta impostato. Necessario perche' HexTileTapController
        /// legge IInputService.Tap grezzo (Input.GetTouch/GetMouseButton diretti, vedi
        /// InputService.Tick) — lo scrim
        /// del popup blocca solo i raycast di Unity UI, NON questo input, quindi senza
        /// questo gate un tap letale su un'altra tile mentre lo Shop e' ancora aperto (anche
        /// durante il solo caricamento Addressables del popup) uccideva il giocatore mentre
        /// il popup restava/veniva comunque mostrato. Impostata alla pubblicazione di
        /// TradeStartedEvent, liberata da ResolveTrade() (chiamato da UIPopup_Shop.OnDestroy,
        /// qualunque sia la via di chiusura del popup — bottone, scrim, CloseAll).
        /// </summary>
        private HexCoord? _pendingTradeCoord;

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

            // BuildGrid() NON viene chiamato qui (vedi InitializeSession): farlo in Awake
            // pubblica GridInitialized troppo presto — Unity non garantisce che l'Awake/
            // OnEnable di QUESTO componente vada prima di quelli di ALTRI componenti sulla
            // stessa scena (HexGridViewSpawner, MapCameraController), solo che Awake preceda
            // OnEnable per lo STESSO oggetto. Se il loro OnEnable (dove si iscrivono a
            // GridInitialized) capitava dopo questo Awake, perdevano il primo GridInitialized
            // della run senza errori visibili: nessuna vista ridisegnata, nessuna camera
            // inquadrata sulla griglia nuova.
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

            _currentMaxHp   = Mathf.Max(1, _survival.MaxHp);
            _currentMaxFood = Mathf.Max(0, _survival.MaxFood);

            _context.Lives = Mathf.Clamp(_survival.StartHp, 1, _currentMaxHp);
            _context.Food  = Mathf.Clamp(_survival.StartFood, 0, _currentMaxFood);
            _context.Score = 0;

            // Chiamato da qui (non da Awake): sia GameStartedEvent (pubblicato da
            // GameplayState.EnterAsync, sempre dopo che l'intera scena ha finito
            // Awake/OnEnable/Start) sia il fallback qui sotto in Start() garantiscono che
            // ogni altro componente si sia gia' iscritto a GridInitialized prima che questa
            // venga chiamata — niente corsa contro HexGridViewSpawner/MapCameraController.
            if (!BuildGrid())
            {
                // BuildGrid ha gia' loggato il motivo. Niente eventi Hp/Food/Score —
                // pubblicarli implicherebbe una run attiva che l'HUD dovrebbe mostrare, ma
                // senza una griglia non c'e' nessuna run: resterebbe un HUD con numeri
                // validi sopra una scena vuota/non interagibile.
                Debug.LogError("[HexGridController] InitializeSession interrotta: generazione mappa fallita, livello non avviato.", this);
                return;
            }

            _bus?.Publish(new HpChangedEvent());
            _bus?.Publish(new FoodChangedEvent());
            _bus?.Publish(new ScoreChangedEvent());
        }

        /// <summary>
        /// Rigenera la griglia da zero. Ritorna false se il livello non deve avviarsi:
        /// config/servizio mancante, o MapGenerationResult.Success = false — il
        /// generatore ha gia' loggato un Debug.LogError col motivo tecnico (vincolo
        /// MinPathClusters, vedi AestheticClusterMapGenerator) in quel caso, qui si logga
        /// solo la conseguenza. Su fallimento _tiles/_playerCoord/_objectiveCoord NON
        /// vengono toccati (restano quello che erano prima, vuoti al primo avvio) e
        /// GridInitialized non viene pubblicato: nessuna vista si popola, nessuna camera
        /// si posiziona — il gioco resta fermo invece di mostrare una mappa non valida.
        /// Seed: da IGameContextService.CurrentRunSeed se != 0, altrimenti da _mapConfig.Seed.
        /// </summary>
        public bool BuildGrid()
        {
            if (_mapConfig == null)
            {
                Debug.LogError("[HexGridController] MapGenerationConfig non risolto dal catalogo. Impossibile generare la mappa.", this);
                return false;
            }

            if (_mapGenerationService == null)
            {
                Debug.LogError("[HexGridController] IMapGenerationService non risolto dal ServiceRegistry. Verifica che MapGenerationServiceInstaller sia in scena e attivo.", this);
                return false;
            }

            int seed = (_context?.CurrentRunSeed != 0) ? _context.CurrentRunSeed : _mapConfig.Seed;
            var result = _mapGenerationService.GenerateMap(_mapConfig, _level, seed);

            if (!result.Success)
            {
                Debug.LogError($"[HexGridController] Generazione mappa fallita, livello non avviato: {result.FailureReason}", this);
                return false;
            }

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
            return true;
        }

        private void RecomputeReachability()
        {
            foreach (var tile in _tiles.Values)
            {
                // Knowledge never regresses: only promote Sconosciuta → Conosciuta.
                if (tile.Spotting == SpottingState.Spotted) continue;

                foreach (var neighbor in GetNeighbors(tile.Coord))
                {
                    if ((neighbor.Exploration == ExplorationState.Explored && neighbor.Spotting == SpottingState.Spotted))
                    {
                        tile.Exploration = ExplorationState.Unexplored; tile.Spotting = SpottingState.Spotted;
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
            if ((tile.Exploration == ExplorationState.Explored && tile.Spotting == SpottingState.Spotted)) return false;

            foreach (var neighbor in GetNeighbors(coord))
            {
                if ((neighbor.Exploration == ExplorationState.Explored && neighbor.Spotting == SpottingState.Spotted))
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
                if ((neighbor.Exploration == ExplorationState.Explored && neighbor.Spotting == SpottingState.Spotted))
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
        /// un incontro o un trade sono gia' pending (vedi _pendingEncounterCoord/
        /// _pendingTradeCoord). Per Enemy/Miniboss non risolve subito: applica il costo
        /// movimento e apre un incontro pending (vedi StartEncounter). Per tutti gli altri
        /// tipi risolve come sempre: costo movimento (una volta) → HpRestore/FoodRestore/
        /// Monete della tessera → cascata Strada gratuita se Strada; se la tile e' un Trader
        /// resta pending fino alla chiusura del popup Shop. Eventi pubblicati una sola volta
        /// a fine azione. PlayerDeathEvent una sola volta se HP arriva a 0.
        /// </summary>
        public bool TryRevealTile(HexCoord target)
        {
            Debug.Log($"[HexGrid] TryRevealTile → target={target}  playerCoord={_playerCoord}  HP={_context?.Lives}/{_currentMaxHp}  Food={_context?.Food}/{_currentMaxFood}");

            if (_pendingEncounterCoord.HasValue)
            {
                Debug.Log($"[HexGrid] TryRevealTile BLOCKED — encounter already pending at {_pendingEncounterCoord.Value}");
                return false;
            }
            if (_pendingTradeCoord.HasValue)
            {
                Debug.Log($"[HexGrid] TryRevealTile BLOCKED — trade already pending at {_pendingTradeCoord.Value}");
                return false;
            }
            if (!_tiles.TryGetValue(target, out var tile))
            {
                Debug.Log($"[HexGrid] TryRevealTile BLOCKED — coordinate {target} not found in grid ({_tiles.Count} tiles)");
                return false;
            }
            if (!IsClickable(target))
            {
                Debug.Log($"[HexGrid] TryRevealTile BLOCKED — tile {target} not clickable (Exploration={tile.Exploration}, Spotting={tile.Spotting}, Type={tile.Type})");
                return false;
            }

            Debug.Log($"[HexGrid] TryRevealTile ACCEPTED — tile={target}  Type={tile.Type}  DL={tile.DifficultyLevel}  Exploration={tile.Exploration}  IsObjective={tile.IsObjective}");

            if (tile.Type == TileType.Enemy)
            {
                // Miniboss/Boss non sono piu' usati come TileType a se' stanti dal design:
                // un nemico di fascia alta e' semplicemente un Enemy con DifficultyLevel
                // piu' alto, quindi passano tutte da qui.
                Debug.Log($"[HexGrid] TryRevealTile → Enemy tile, starting encounter (DL={tile.DifficultyLevel}, IsObjective={tile.IsObjective})");
                StartEncounter(target, tile);
                return true;
            }

            tile.Exploration = ExplorationState.Explored; tile.Spotting = SpottingState.Spotted;
            _playerCoord = target;

            int hpBefore = _context.Lives;
            int foodBefore = _context.Food;
            int scoreBefore = _context.Score;

            ApplyMovementCost();
            AccumulateHp(tile);
            AccumulateFood(tile);
            bool moneteEarned = AccumulateMonete(tile);
            var (keysChanged, coinsFromElement) = ApplyImmediateElement(tile);
            moneteEarned |= coinsFromElement;

            Debug.Log($"[HexGrid] TryRevealTile effects — HP {hpBefore}→{_context.Lives}  Food {foodBefore}→{_context.Food}  Score {scoreBefore}→{_context.Score}  moneteEarned={moneteEarned}  keysChanged={keysChanged}");

            if (tile.Type == TileType.Road)
            {
                Debug.Log($"[HexGrid] TryRevealTile → Road tile, cascading strada from {target}");
                CascadeStrada(target);
            }

            RecomputeReachability();

            var neighbors = GetNeighbors(target);
            TileRevealed?.Invoke(tile, neighbors);

            // Publish once after all changes from this player action.
            _bus?.Publish(new HpChangedEvent());
            _bus?.Publish(new FoodChangedEvent());
            _feedbackService?.Play("hp_changed");

            if (moneteEarned)
                _bus?.Publish(new ScoreChangedEvent());

            if (keysChanged)
                _bus?.Publish(new KeysChangedEvent());

            if (_context.Lives <= 0)
            {
                Debug.Log($"[HexGrid] TryRevealTile → HP reached 0, publishing PlayerDeathEvent");
                _feedbackService?.Play("game_over");
                _bus?.Publish(new PlayerDeathEvent());
            }
            else if (tile.Type == TileType.Trader)
            {
                // Trader (RevealEffect.Trade, vedi TradeEvents.cs): la tile e' gia' Scoperta a
                // questo punto (a differenza di Enemy non c'e' scelta combatti/fuggi che ne
                // condizioni lo stato). _pendingTradeCoord pero' resta impostato per tutta la
                // durata del popup Shop — stesso precedente di _pendingEncounterCoord — perche'
                // altrimenti un tap letale su un'altra tile mentre lo Shop e' ancora aperto
                // (HexTileTapController non e' bloccato dallo scrim UI, vedi doc su
                // _pendingTradeCoord) aprirebbe/lascerebbe aperto il popup su un giocatore
                // gia' morto. La morte per costo movimento di QUESTO click prevale comunque
                // (stesso pattern del branch IsObjective in ResolveEncounterFight): niente
                // trade pending se il click che rivela il Trader e' gia' quello fatale.
                _pendingTradeCoord = target;
                _bus?.Publish(new TradeStartedEvent(this));
            }
            // Nessun controllo IsObjective qui: la tile obiettivo e' sempre un Enemy, quindi
            // passa sempre da StartEncounter sopra prima di arrivare a questo punto — la
            // vittoria si risolve in ResolveEncounterFight, non qui.

            return true;
        }

        /// <summary>
        /// Avvia un incontro Enemy: applica il costo movimento (una tantum, come
        /// ogni click) ma NON marca la tile Scoperta e NON risolve il combattimento — resta
        /// in attesa che il popup di Combattimento chiami ResolveEncounterFight/Flee. Non
        /// tocca _playerCoord: finche' l'incontro e' pending il giocatore non si e' "mosso"
        /// sulla tile (coerente con la fuga, che la lascia coperta). Se il costo movimento
        /// stesso azzera gli HP, l'incontro non si apre: si va dritti a PlayerDeathEvent.
        /// </summary>
        private void StartEncounter(HexCoord target, HexTileData tile)
        {
            int hpBefore = _context.Lives;
            int foodBefore = _context.Food;

            ApplyMovementCost();

            Debug.Log($"[HexGrid] StartEncounter — movement cost applied: HP {hpBefore}→{_context.Lives}  Food {foodBefore}→{_context.Food}");

            _bus?.Publish(new HpChangedEvent());
            _bus?.Publish(new FoodChangedEvent());
            _feedbackService?.Play("hp_changed");

            if (_context.Lives <= 0)
            {
                Debug.Log($"[HexGrid] StartEncounter → HP reached 0 during movement cost, PlayerDeathEvent (no popup)");
                _feedbackService?.Play("game_over");
                _bus?.Publish(new PlayerDeathEvent());
                return;
            }

            Debug.Log($"[HexGrid] StartEncounter → opening encounter popup (CanFlee={CanFlee})");
            _pendingEncounterCoord = target;
            _bus?.Publish(new EncounterStartedEvent(tile, this));
        }
        /// <summary>
        /// Il giocatore ha scelto di combattere l'incontro pending. Risolve: Scoperta,
        /// danno Enemy da DifficultyLevel, guadagni della tessera, ricompensa in Monete
        /// (Coins += DifficultyLevel, modificatori da Item posseduti deferiti), reachability,
        /// eventi. Ritorna false se non c'e' nessun incontro pending.
        /// Win Condition: la tile obiettivo e' sempre un Enemy con IsObjective = true, e
        /// passa sempre da qui — mai da un reveal diretto — quindi questo e' l'unico punto
        /// che puo' davvero far scattare la vittoria. La morte nello stesso combattimento
        /// prevale sempre sulla vittoria.
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

            tile.Exploration = ExplorationState.Explored; tile.Spotting = SpottingState.Spotted;
            _playerCoord = target;

            ApplyEnemyDamage(tile);
            AccumulateHp(tile);
            AccumulateFood(tile);
            bool moneteEarned = AccumulateMonete(tile);

            // Ricompensa combattimento (GDD, Resources — Coins): DifficultyLevel
            // dell'Enemy * EconomyConfig.EnemyKillCoinMultiplier, arrotondato all'intero
            // piu' vicino. Moltiplicatore 1 se EconomyConfig non e' risolto dal catalogo.
            // Modificatori da Item posseduti deferiti.
            int combatReward = Mathf.Max(0, Mathf.RoundToInt(tile.DifficultyLevel * (_economy?.EnemyKillCoinMultiplier ?? 1f)));
            if (combatReward > 0)
            {
                _context.Score += combatReward;
                moneteEarned = true;
            }

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
                // Unico punto raggiungibile per la vittoria (vedi doc comment sopra).
                _feedbackService?.Play("victory");
                _bus?.Publish(new PlayerVictoryEvent());
            }

            return true;
        }

        /// <summary>
        /// Il giocatore ha scelto di fuggire dall'incontro pending. Gate: richiede
        /// Cibo >= FleeFoodCost, altrimenti ritorna false senza effetti — la UI
        /// dovrebbe aver gia' disabilitato il bottone (CanFlee), questo e' il controllo di
        /// difesa in profondita'. A fuga riuscita: costo FleeFoodCost in Cibo (mai HP), la
        /// tile resta/torna Conosciuta — non e' mai stata marcata Scoperta — e _playerCoord
        /// non cambia: il giocatore non si e' mai spostato sulla tile. L'informazione sul
        /// nemico e' persa solo lato UI e nella memoria del giocatore (il popup si chiude);
        /// i dati (HexTileData.Type/DifficultyLevel) non cambiano, un nuovo tentativo
        /// mostrera' le stesse info. Ritorna false se non c'e' nessun incontro pending.
        /// </summary>
        public bool ResolveEncounterFlee()
        {
            if (!_pendingEncounterCoord.HasValue) return false;
            if (!CanFlee) return false;
            var target = _pendingEncounterCoord.Value;
            if (!_tiles.TryGetValue(target, out var tile))
            {
                _pendingEncounterCoord = null;
                return false;
            }

            _pendingEncounterCoord = null;

            _context.Food -= FleeFoodCost;

            _bus?.Publish(new EncounterResolvedEvent(tile, wasFought: false));
            _bus?.Publish(new FoodChangedEvent());

            return true;
        }

        /// <summary>
        /// Il popup Shop si e' chiuso, per qualunque via (bottone Chiudi, tap sullo scrim,
        /// CloseAll/teardown scena — vedi UIPopup_Shop.OnDestroy, l'unico punto comune a
        /// tutte). Libera il gate di Trade pending (vedi doc su _pendingTradeCoord): finche'
        /// non viene chiamato, TryRevealTile blocca ogni altro click. Nessun altro effetto —
        /// a differenza di ResolveEncounterFlee non c'e' un costo da pagare per "uscire": lo
        /// shop non e' un incontro da risolvere, solo un popup che va richiuso. Sicura da
        /// chiamare anche senza trade pending (no-op).
        /// </summary>
        public void ResolveTrade() => _pendingTradeCoord = null;

        /// <summary>
        /// Effetto Enemy: perdita HP pari al DifficultyLevel della tessera. Vale anche per
        /// le varianti Miniboss/Boss (non sono TileType separati, sono Enemy con
        /// DifficultyLevel piu' alto, stesso Type=Enemy). Non pubblica eventi — il publish
        /// e' centralizzato nel chiamante.
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
            var foodCost = Mathf.Max(0, _survival.FoodCostPerClick);
            var noFoodPenalty = Mathf.Max(0, _survival.NoFoodHpPenalty);
            if (_context.Food >= foodCost)
                _context.Food -= foodCost;
            else
                _context.Lives = Mathf.Clamp(_context.Lives - noFoodPenalty, 0, _currentMaxHp);
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
        /// Effetti degli Element a RevealEffect Immediate (GDD, Tile Types), applicati al
        /// reveal senza popup. Ritorna (keysChanged, coinsChanged): il chiamante pubblica
        /// KeysChangedEvent/ScoreChangedEvent di conseguenza — il publish resta
        /// centralizzato in TryRevealTile come per gli altri eventi.
        ///
        /// Trap:     HP -= DifficultyLevel (stessa formula placeholder del danno Enemy,
        ///           vedi ApplyEnemyDamage — regola autorevole in GDD, Tile Types → Enemy).
        /// Fountain: ripristina tutti gli HP al cap runtime corrente.
        /// Key:      attiva la chiave di sessione per il proprio DifficultyLevel (4/5/6);
        ///           idempotente, le chiavi non si consumano ne' si disattivano.
        /// MoneyBag: Coins += DifficultyLevel.
        /// Chest:    inerte — RevealEffect Loot, il popup non esiste ancora (vedi GDD,
        ///           Reveal Effects). Nessun effetto qui finche' il flusso Loot non c'e'.
        /// Tree/Bush/BeeHive/TurnipSprout: NON gestiti qui — il loro +Food viaggia gia' su
        ///           HexTileData.FoodRestore (AccumulateFood, chiamata incondizionatamente
        ///           per ogni reveal), calcolo fisso = DifficultyLevel assegnato dal
        ///           generatore (vedi MapClusterGenerator.ApplyElementStats). Non duplicare
        ///           l'effetto reintroducendo un case qui.
        /// </summary>
        private (bool keysChanged, bool coinsChanged) ApplyImmediateElement(HexTileData tile)
        {
            switch (tile.Type)
            {
                case TileType.Trap:
                {
                    int level = Mathf.Max(0, tile.DifficultyLevel);
                    if (level > 0)
                        _context.Lives = Mathf.Clamp(_context.Lives - level, 0, _currentMaxHp);
                    return (false, false);
                }

                case TileType.Fountain:
                    _context.Lives = _currentMaxHp;
                    return (false, false);

                case TileType.Key:
                    switch (tile.DifficultyLevel)
                    {
                        case 4: _context.HasKeyDL4 = true; return (true, false);
                        case 5: _context.HasKeyDL5 = true; return (true, false);
                        case 6: _context.HasKeyDL6 = true; return (true, false);
                        default:
                            Debug.LogWarning($"[HexGridController] Tile Key con DifficultyLevel {tile.DifficultyLevel} fuori dal set 4/5/6: nessuna chiave attivata. Verifica le entry Key del LevelConfig.", this);
                            return (false, false);
                    }

                case TileType.MoneyBag:
                {
                    int coins = Mathf.Max(0, tile.DifficultyLevel);
                    if (coins > 0)
                    {
                        _context.Score += coins;
                        return (false, true);
                    }
                    return (false, false);
                }

                default:
                    return (false, false);
            }
        }

        // ------------------------------------ Shop ---------------------------------------
        // Potenziamenti diretti acquistabili in Monete (_context.Score), ricomprabili nella
        // stessa run. Prezzi ed entita' degli effetti in EconomyConfig (bilanciamento in
        // editor, mai hardcoded qui). ATK/DEF/chiavi/vision boost: deferiti, i sistemi che
        // li consumano non esistono ancora (Combattimento / Knowledge). Ogni TryBuy ritorna
        // false senza effetti se le Monete non bastano o l'acquisto sarebbe inutile (es.
        // cura a HP pieni).

        /// <summary>Alza il cap HP della run di EconomyConfig.MaxHpUpgradeAmount (le Monete lo consentono sempre: mai "inutile").</summary>
        public bool TryBuyMaxHpUpgrade()
        {
            if (!CanSpend(_economy?.MaxHpUpgradeCost)) return false;

            Spend(_economy.MaxHpUpgradeCost);
            _currentMaxHp += Mathf.Max(0, _economy.MaxHpUpgradeAmount);

            _bus?.Publish(new ScoreChangedEvent());
            _bus?.Publish(new HpChangedEvent()); // il cap e' cambiato, la UI degli stack deve saperlo
            return true;
        }

        /// <summary>Alza il cap Cibo della run di EconomyConfig.FoodSlotUpgradeAmount.</summary>
        public bool TryBuyFoodSlotUpgrade()
        {
            if (!CanSpend(_economy?.FoodSlotUpgradeCost)) return false;

            Spend(_economy.FoodSlotUpgradeCost);
            _currentMaxFood += Mathf.Max(0, _economy.FoodSlotUpgradeAmount);

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
            _context.Lives = Mathf.Clamp(_context.Lives + Mathf.Max(0, _economy.HealAmount), 0, _currentMaxHp);

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
            _context.Food = Mathf.Clamp(_context.Food + Mathf.Max(0, _economy.FoodRefillAmount), 0, _currentMaxFood);

            _bus?.Publish(new ScoreChangedEvent());
            _bus?.Publish(new FoodChangedEvent());
            return true;
        }

        /// <summary>null-safe: false se EconomyConfig manca dal catalogo o le Monete non bastano.</summary>
        private bool CanSpend(int? cost)
        {
            if (!cost.HasValue || cost.Value < 1)
            {
                Debug.LogError("[HexGridController] EconomyConfig contiene un costo non valido: acquisto rifiutato.", this);
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
                    if (neighbor.Type == TileType.Road && !(neighbor.Exploration == ExplorationState.Explored && neighbor.Spotting == SpottingState.Spotted))
                    {
                        neighbor.Exploration = ExplorationState.Explored; neighbor.Spotting = SpottingState.Spotted;
                        AccumulateHp(neighbor);
                        queue.Enqueue(neighbor.Coord);
                    }
                }
            }
        }

    }
}
