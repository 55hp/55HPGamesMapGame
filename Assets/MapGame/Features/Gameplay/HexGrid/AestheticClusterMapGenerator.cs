using System;
using System.Collections.Generic;
using hp55games.MapGame.Features.Configs;

namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    /// <summary>
    /// Generatore di mappa, pipeline "macchie di leopardo" (GDD, rev. 2026-07-07),
    /// revisione 2026-08-05 (composizione deterministica + aggancio ElementCatalog):
    /// 1. Piazzamento Start/End (weighted distance). Start diventa Road, End diventa
    ///    Enemy con IsObjective = true, DifficultyLevel 6 (placeholder "boss finale",
    ///    coerente con PlaceholderBossElementConfig — vedi Generate).
    /// 2. Piazzamento EventCluster (cluster da 7 tessere generati interamente a runtime
    ///    dalle entry ListType.EventCluster di LevelConfig — vedi TryBuildProceduralCluster;
    ///    il vecchio fallback su forme manuali/EventClusterCatalog e' stato rimosso
    ///    2026-08-07, un cluster fallito degrada direttamente a tessera singola) e tessere
    ///    singole via rejection sampling, con almeno una tessera di distacco tra due
    ///    piazzamenti. Un solo cluster per tipo di centro sull'intera mappa (2026-08-07,
    ///    regola Franci: il tipo di un cluster e' la coppia (Type, DifficultyLevel) del suo
    ///    centro — Enemy DL4 ed Enemy DL5 sono due tipi distinti, ciascuno piazzabile al
    ///    massimo una volta) — vedi _usedClusterCenterTypes, popolato in PlaceEventClusters
    ///    solo dopo un piazzamento validato, mai ripescato.
    /// 3. Mesh di PathCluster: parte da ogni bordo di ogni EventCluster/singola piazzata,
    ///    cresce a budget (15 tessere / 5 rami / rami 3-7) per ciascun PathCluster, si
    ///    ferma sui bordi degli EventCluster (fanno da muro, nessuna separazione
    ///    necessaria lì) e pesca dalle entry LevelConfig.Entries con ListType.StopSingle
    ///    dove tocca un PathCluster diverso già piazzato (per non farli fondere in
    ///    un'unica cascata).
    /// 4. Rammendo: ogni tessera ancora priva di contenuto diventa Strada extra su un
    ///    PathCluster confinante (massimo 2 per PathCluster, o una entry ListType.StopSingle
    ///    se confina anche con un PathCluster diverso) oppure, se non c'è un PathCluster a
    ///    cui attaccarsi, una tessera singola da un'entry ListType.EventSingle. Nessuna
    ///    tessera resta senza contenuto esplicito.
    /// 5. Vincolo DifficultyLevel: ogni tessera pescata porta con se' un DifficultyLevel
    ///    (1-6) dalla entry scelta; se la posizione finale non ha abbastanza vicini
    ///    validi in griglia per quel livello, il livello viene abbassato fino al valore
    ///    supportato (minimo 1), stesso TileType — vedi ResolveDifficulty.
    /// 6. Composizione fissa e deterministica (2026-08-05, decisione confermata
    ///    2026-07-24; struttura dati rivista 2026-08-07): le entry LevelConfig.Entries con
    ///    ListType.EventSingle o ListType.StopSingle NON sono piu' pool pesati con
    ///    reinserimento. Sono manifest: ogni entry rappresenta UNA istanza da piazzare (o
    ///    Amount istanze per EventSingle, vedi sotto), consumata quando viene usata.
    ///    All'inizio di Generate, BuildManifest filtra Entries per ListType e mescola
    ///    (Fisher-Yates) il risultato con lo stesso Random(seed) della run, poi TryDrawEntry
    ///    pesca dalla coda senza mai reinserire — stesso seed produce sempre la stessa
    ///    sequenza di pesche. Se il manifest si esaurisce prima che la griglia sia piena, il
    ///    generatore degrada come per un manifest vuoto (vedi AssignSingleTile/
    ///    AssignStopTile), non crasha. Le entry con ListType.EventCluster sono la palette
    ///    per la generazione procedurale dei cluster: TryBuildProceduralCluster le usa per
    ///    comporre centro (DL 4/5/6) e ring (DL 1/2) di ogni cluster generato a runtime.
    ///    Amount (LevelTileEntry.Amount): SOLO per ListType.EventSingle, BuildManifest
    ///    espande ogni entry in Amount copie PRIMA dello shuffle — un'entry con Amount=3
    ///    sostituisce 3 entry duplicate a mano, senza cambiare la garanzia di determinismo
    ///    (l'espansione e' deterministica, lo shuffle successivo e' comunque guidato solo
    ///    dal seed). ListType.StopSingle ignora Amount, resta un'entry = un'istanza.
    /// 7. ElementCatalog (2026-08-05): ogni tessera content (EventCluster, singola, o
    ///    stop) risolve una specie eleggibile per {TileType, DifficultyLevel finale} via
    ///    ElementCatalog.PickRandom, e ne copia FoodRestore/CoinReward sulla
    ///    HexTileData (HpRestore resta sempre 0: nessun path applica danno/cura tramite
    ///    questo campo oggi, vedi HexGridController). Nessuna specie eleggibile per la
    ///    combinazione richiesta → degrado silenzioso a valori neutri con un warning in
    ///    Console, stesso principio del manifest vuoto: il designer se ne accorge, il
    ///    gioco non crasha.
    /// 8. Rivelazione iniziale (Start ed End già Scoperte)
    /// 9. Vincolo adiacenza Strada (2026-08-07, regola Franci): ogni tile Strada deve
    ///    confinare con almeno un'altra Strada (mai isolata), e puo' confinarne due o
    ///    piu' ma MAI su due lati consecutivi dell'esagono (produrrebbe un tratto largo
    ///    2 celle). Applicato in modo costruttivo durante la crescita — vedi
    ///    WouldViolateConsecutiveSides, controllato in GrowBranch (ogni tessera, incluse
    ///    le origini dei rami) e in PatchResidualGaps (rammendo) — non come scarto post-
    ///    hoc su una griglia gia' generata. Fork a 3 vie restano sempre validi per
    ///    costruzione geometrica; fork a 5 vie non possono mai esserlo (5 vicini su 6
    ///    lati garantiscono coppie consecutive) e degradano organicamente invece di
    ///    creare un incrocio vietato. CollectClusterBorders semina anche i vicini di
    ///    Start (prima solo quelli degli EventCluster) per ridurre il rischio di una
    ///    Start isolata. ValidateRoadAdjacencyRule fa da rete di sicurezza a fine
    ///    Generate: solo log, nessuna mutazione.
    /// 10. Vincolo numero minimo di PathCluster (2026-08-07, regola Franci, MinPathClusters
    ///     = 3): a differenza di tutti gli altri vincoli di questo generatore, questo NON
    ///     degrada silenziosamente — sotto soglia la mappa non e' considerata giocabile.
    ///     Generate conta i PathCluster con almeno una tessera Strada (CountRealPathClusters,
    ///     un PathCluster demota interamente a Stop per separazione da un altro non conta),
    ///     e se sotto MinPathClusters marca MapGenerationResult.Success = false con un
    ///     Debug.LogError. Il chiamante (HexGridController.BuildGrid) e' responsabile di
    ///     NON avviare il livello quando Success e' false — Generate stessa non puo'
    ///     "fermare" nulla, produce solo il segnale.
    /// </summary>
    public sealed class AestheticClusterMapGenerator : IMapGenerator
    {
        /// <summary>DifficultyLevel della tile End/obiettivo. Coincide col DifficultyLevel di PlaceholderBossElementConfig (6) — se in futuro esistono piu' "boss" a livelli diversi, questo andra' reso configurabile invece che hardcoded.</summary>
        private const int ObjectiveDifficultyLevel = 6;

        /// <summary>
        /// Numero minimo di PathCluster "reali" (con almeno una tessera Strada, vedi
        /// CountRealPathClusters) perche' una mappa sia considerata giocabile (2026-08-07,
        /// regola Franci). A differenza degli altri vincoli del generatore, questo non
        /// degrada silenziosamente: sotto soglia Generate marca il risultato
        /// Success=false — vedi HexGridController.BuildGrid, che non avvia il livello.
        /// </summary>
        private const int MinPathClusters = 3;

        private readonly DistanceWeight[] _endDistanceWeights;
        private readonly int _startMinBorderDistance;
        private readonly int _clusterMinDistanceFromStartEnd;
        private readonly StradaNetworkSettings _strada;
        private readonly EventClusterPlacementSettings _eventClusters;
        private readonly LevelConfig _levelConfig;
        private readonly ElementCatalog _elementCatalog;

        // Manifest consumabili della run corrente, costruiti in Generate() e pescati da
        // TryDrawEntry. Campi di istanza invece di parametri passati a catena attraverso
        // mezza dozzina di metodi privati: sicuro perche' il generatore e' istanziato una
        // volta per singola chiamata a Generate (vedi MapGenerationService), mai riusato
        // ne' chiamato in parallelo su piu' thread.
        private List<LevelTileEntry> _eventSingleManifest;
        private List<LevelTileEntry> _stopSingleManifest;

        /// <summary>
        /// Combinazioni (Type, DifficultyLevel) del centro gia' usate come cluster in
        /// questa run (2026-08-07, regola Franci: un cluster e' definito dal suo centro —
        /// Enemy DL4 ed Enemy DL5 sono due "tipi di cluster" distinti — e ogni tipo puo'
        /// comparire al massimo una volta per mappa). Popolata in PlaceEventClusters SOLO
        /// dopo che il piazzamento e' stato validato (IsValidEventClusterPlacement), non
        /// alla scelta del centro in TryBuildProceduralCluster: un piazzamento respinto per
        /// posizione (rejection sampling) non deve bruciare il tipo, va ritentato altrove.
        /// TryBuildProceduralCluster filtra i centerCandidates escludendo le combinazioni
        /// gia' qui dentro — quando tutte sono esaurite ritorna null e PlaceEventClusters
        /// degrada a tessera singola, stesso fallback gia' esistente per LevelConfig con
        /// poche entry.
        /// </summary>
        private HashSet<(TileType Type, int DifficultyLevel)> _usedClusterCenterTypes;

        /// <summary>
        /// Combinazioni (Type, DifficultyLevel) del centro per cui TryBuildProceduralCluster
        /// ha gia' loggato un warning di composizione insufficiente in questa run (2026-08-08,
        /// trovato verificando LevelConfig_Test_1: Enemy come centro richiede 4 tipi non-Trap
        /// distinti a DL1/2 ma solo 3 erano disponibili, fallendo SEMPRE in silenzio). Un
        /// centro non ancora usato (vedi _usedClusterCenterTypes) puo' essere ripescato molte
        /// volte in una singola Generate finche' non ha successo o viene esaurito il pool —
        /// senza questa dedup un centro strutturalmente impossibile (come Enemy sopra)
        /// stamperebbe lo stesso warning decine di volte per run.
        /// </summary>
        private HashSet<(TileType Type, int DifficultyLevel)> _warnedInsufficientClusterComposition;

        // Contatori diagnostici (2026-08-07, richiesta Franci: piu' dettaglio nel log di
        // fallimento MinPathClusters). Solo per il report — nessuna logica dipende da
        // questi valori. Azzerati a inizio Generate, incrementati a ogni punto in cui la
        // crescita di un PathCluster viene effettivamente rifiutata, cosi' che il report
        // finale possa dire QUALE vincolo ha bloccato piu' crescita, non solo il risultato
        // finale. Vedi BuildInsufficientPathClustersReport per come vengono letti.
        private int _rejectedByBlocked;
        private int _rejectedByConsecutiveSides;
        private int _rejectedByShortBranch;
        private int _rejectedByBudget;
        private int _patchDemotedByOtherCluster;

        public AestheticClusterMapGenerator(
            DistanceWeight[] endDistanceWeights,
            int startMinBorderDistance,
            int clusterMinDistanceFromStartEnd,
            StradaNetworkSettings strada,
            EventClusterPlacementSettings eventClusters,
            LevelConfig levelConfig,
            ElementCatalog elementCatalog)
        {
            _endDistanceWeights = endDistanceWeights;
            _startMinBorderDistance = startMinBorderDistance;
            _clusterMinDistanceFromStartEnd = clusterMinDistanceFromStartEnd;
            _strada = strada;
            _eventClusters = eventClusters;
            _levelConfig = levelConfig;
            _elementCatalog = elementCatalog;
        }

        public MapGenerationResult Generate(int width, int height, int seed)
        {
            _rejectedByBlocked = 0;
            _rejectedByConsecutiveSides = 0;
            _rejectedByShortBranch = 0;
            _rejectedByBudget = 0;
            _patchDemotedByOtherCluster = 0;
            _usedClusterCenterTypes = new HashSet<(TileType, int)>();
            _warnedInsufficientClusterComposition = new HashSet<(TileType, int)>();

            var rng = new Random(seed);
            var tiles = new Dictionary<HexCoord, HexTileData>(width * height);

            for (int row = 0; row < height; row++)
            for (int col = 0; col < width; col++)
            {
                var coord = HexCoord.FromOffsetOddQ(col, row);
                tiles[coord] = new HexTileData(coord);
            }

            // Manifest per-run: filtrati per ListType da LevelConfig.Entries (2026-08-07,
            // sostituisce i tre array separati) e mescolati una volta sola qui, poi
            // consumati (mai reinseriti) da AssignSingleTile/AssignStopTile per tutta la
            // Generate. expandByAmount=true SOLO per TileListType.EventSingle (vedi
            // LevelTileEntry.Amount): StopSingle resta un'entry = un'istanza, come prima
            // dell'introduzione del campo.
            _eventSingleManifest = BuildManifest(_levelConfig?.Entries, TileListType.EventSingle, rng, expandByAmount: true);
            _stopSingleManifest  = BuildManifest(_levelConfig?.Entries, TileListType.StopSingle, rng, expandByAmount: false);

            var startCoord = PlaceStart(tiles, width, height, rng);
            var endCoord = PlaceEnd(tiles, startCoord, rng);

            ResetTile(tiles[startCoord], TileType.Road, hpRestore: 0, moneteGained: 0);
            tiles[startCoord].IsObjective = false;

            // Obiettivo: sempre Enemy con IsObjective = true (Boss/Miniboss come TileType
            // a se' sono obsoleti dal 2026-07-25, la distinzione e' via ElementConfig).
            // DifficultyLevel 6 come da placeholder "Dragon" — passa comunque da
            // ResolveDifficulty per coerenza con tutte le altre tessere content, nel caso
            // la posizione End non abbia 6 vicini validi in griglia.
            int objectiveLevel = ResolveDifficulty(ObjectiveDifficultyLevel, endCoord, tiles);
            ApplyElementStats(tiles[endCoord], TileType.Enemy, objectiveLevel, rng);
            tiles[endCoord].DifficultyLevel = objectiveLevel;
            tiles[endCoord].IsObjective = true;

            var eventOccupied = PlaceEventClusters(tiles, width, height, startCoord, endCoord, rng, out int nextEventId);
            GeneratePathClusterMesh(tiles, startCoord, endCoord, eventOccupied, rng, out var globalClaimed, out var tileOwner, out int originsCount, out int attemptedClusters);
            PatchResidualGaps(tiles, startCoord, endCoord, eventOccupied, globalClaimed, tileOwner, rng, nextEventId);

            RevealInitialTiles(tiles[startCoord], tiles[endCoord]);

            ValidateRoadAdjacencyRule(tiles);

            var result = new MapGenerationResult
            {
                Tiles = tiles,
                StartCoord = startCoord,
                ObjectiveCoord = endCoord,
                SeedUsed = seed,
            };

            int pathClusterCount = CountRealPathClusters(tiles);
            if (pathClusterCount < MinPathClusters)
            {
                result.Success = false;
                result.FailureReason = BuildInsufficientPathClustersReport(
                    pathClusterCount, originsCount, attemptedClusters, eventOccupied.Count, width, height, seed);
                UnityEngine.Debug.LogError($"[AestheticClusterMapGenerator] {result.FailureReason}");
            }

            return result;
        }

        /// <summary>
        /// Report diagnostico per il fallimento MinPathClusters (2026-08-07, arricchito su
        /// richiesta Franci — seconda passata: non piu' solo "possibili cause" in prosa,
        /// ma i contatori effettivi di QUANTE volte ciascun vincolo ha bloccato una
        /// crescita, misurati durante questa stessa Generate (vedi i campi _rejectedBy*/
        /// _patchDemotedByOtherCluster). Con i numeri in mano non serve piu' indovinare:
        /// - _rejectedByBlocked alto → la griglia/lo spazio libero e' il collo di
        ///   bottiglia (bordo griglia, EventCluster, tessere gia' reclamate).
        /// - _rejectedByConsecutiveSides alto (spesso il dominante da quando il vincolo
        ///   lati-non-consecutivi e' attivo, vedi WouldViolateConsecutiveSides) → e'
        ///   proprio quel vincolo a impedire ai PathCluster di crescere/collegarsi
        ///   abbastanza; su griglie piccole o dense puo' rendere impossibile MinPathClusters
        ///   anche con budget StradaNetwork generoso.
        /// - _rejectedByBudget/_rejectedByShortBranch alti → StradaNetwork.MinBranchLength/
        ///   MaxTotalTiles/MaxBranches troppo stretti per la griglia.
        /// - _patchDemotedByOtherCluster alto → troppa densita' di PathCluster ravvicinati
        ///   (rammendo che continua a scontrarsi con cluster vicini invece di estendere).
        /// </summary>
        private string BuildInsufficientPathClustersReport(
            int pathClusterCount, int originsCount, int attemptedClusters, int eventOccupiedCount,
            int width, int height, int seed)
        {
            int totalTiles = width * height;
            float occupiedPct = totalTiles > 0 ? 100f * eventOccupiedCount / totalTiles : 0f;

            return
                $"Mappa non giocabile: {pathClusterCount} PathCluster con Strada effettiva, minimo richiesto {MinPathClusters}.\n" +
                $"  Seed={seed}  Griglia={width}x{height} ({totalTiles} tile)\n" +
                $"  Pipeline: {originsCount} bordi disponibili (EventCluster + vicini di Start) → {attemptedClusters} PathCluster avviati (>=1 tessera cresciuta) → {pathClusterCount} con Strada effettiva dopo separazione/vincolo lati\n" +
                $"  EventCluster/tessere singole: {eventOccupiedCount} tile occupate ({occupiedPct:F0}% della griglia)\n" +
                $"  Rifiuti misurati durante la crescita (quante volte un tentativo di crescita e' stato bloccato):\n" +
                $"    Fuori griglia/occupato/gia' reclamato (IsBlocked): {_rejectedByBlocked}\n" +
                $"    Vincolo lati non consecutivi (WouldViolateConsecutiveSides): {_rejectedByConsecutiveSides}\n" +
                $"    Budget esaurito prima di poter crescere (MinBranchLength non raggiungibile): {_rejectedByBudget}\n" +
                $"    Ramo cresciuto ma scartato perche' troppo corto (< MinBranchLength): {_rejectedByShortBranch}\n" +
                $"    Rammendo (PatchResidualGaps) demotato a Stop per confinare con un PathCluster diverso: {_patchDemotedByOtherCluster}\n" +
                $"  StradaNetwork: MinBranchLength={_strada.MinBranchLength} MaxBranchLength={_strada.MaxBranchLength} MaxBranches={_strada.MaxBranches} MaxTotalTiles={_strada.MaxTotalTiles}  Pesi Stop/Fork3/Fork5={_strada.StopWeight}/{_strada.Fork3Weight}/{_strada.Fork5Weight}\n" +
                $"  EventClusters: MaxConsecutiveFailures={_eventClusters.MaxConsecutiveFailures} ClusterToSingleRatio={_eventClusters.ClusterToSingleRatioMin}-{_eventClusters.ClusterToSingleRatioMax}";
        }

        /// <summary>
        /// Numero di PathCluster distinti che hanno prodotto almeno una tessera Strada nel
        /// risultato finale. Un PathCluster puo' crescere (GrowOnePathCluster ritorna
        /// tessere) e finire comunque con zero Strada se ogni sua tessera viene demota a
        /// ListType.StopSingle/Void per separazione da un cluster diverso (vedi
        /// stopOverride in GeneratePathClusterMesh) — quel caso non conta come "percorso"
        /// per il vincolo MinPathClusters, da qui il filtro su Type invece che su un
        /// contatore grezzo di cluster avviati. Start (sempre Strada, PathClusterId = -1
        /// di default: nessun PathCluster lo reclama mai, IsBlocked lo esclude sempre) non
        /// e' mai incluso.
        /// </summary>
        private int CountRealPathClusters(Dictionary<HexCoord, HexTileData> tiles)
        {
            var seen = new HashSet<int>();
            foreach (var tile in tiles.Values)
            {
                if (tile.Type == TileType.Road && tile.PathClusterId >= 0)
                    seen.Add(tile.PathClusterId);
            }
            return seen.Count;
        }

        /// <summary>
        /// Filtra source per listType (2026-08-07: LevelConfig.Entries e' un'unica List,
        /// non piu' tre array separati — vedi TileListType) in una List mescolata con
        /// Fisher-Yates usando lo stesso rng della run (mai un rng separato: il seed resta
        /// l'unica fonte di casualita', e la stessa sequenza di draw deve ripetersi
        /// identica a parita' di seed). Lista vuota (mai null) se source e' null/vuoto o
        /// non contiene entry di quel listType — TryDrawEntry gestisce il caso senza
        /// bisogno di controlli aggiuntivi nei chiamanti.
        ///
        /// expandByAmount (vedi LevelTileEntry.Amount): se true, ogni entry (gia' filtrata
        /// per listType) viene ripetuta Amount volte PRIMA dello shuffle — un'entry con
        /// Amount=3 diventa 3 copie indipendenti nel manifest, ciascuna poi piazzata al
        /// massimo una volta come le altre (TryDrawEntry non le distingue). L'espansione
        /// avviene prima dello shuffle apposta: la sequenza di pesca resta comunque
        /// interamente derivata dal seed, nessuna sorgente di casualita' aggiuntiva.
        /// Amount <= 0 conta come 1 (mai zero copie: coerente con [Min(1)] sul campo, ma
        /// qui per sicurezza anche se il valore serializzato fosse "sporco"). Passare
        /// false (TileListType.StopSingle) ignora del tutto Amount, un'entry resta una
        /// singola istanza — vedi Generate.
        /// </summary>
        private List<LevelTileEntry> BuildManifest(List<LevelTileEntry> source, TileListType listType, Random rng, bool expandByAmount)
        {
            var manifest = new List<LevelTileEntry>();
            if (source != null)
            {
                foreach (var entry in source)
                {
                    if (entry.ListType != listType) continue;

                    int copies = expandByAmount ? Math.Max(1, entry.Amount) : 1;
                    for (int i = 0; i < copies; i++)
                        manifest.Add(entry);
                }
            }

            for (int i = manifest.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (manifest[i], manifest[j]) = (manifest[j], manifest[i]);
            }

            return manifest;
        }

        /// <summary>
        /// Pesca (e consuma) l'ultima entry di manifest. False se il manifest e' vuoto —
        /// il chiamante decide il fallback, stesso comportamento di quando LevelConfig
        /// non e' popolato. Rimuovere dalla coda invece che dalla testa e' solo una
        /// scelta di costo O(1); l'ordine e' gia' casuale per via dello shuffle in
        /// BuildManifest, quindi non ha alcun effetto sull'esito.
        /// </summary>
        private bool TryDrawEntry(List<LevelTileEntry> manifest, out LevelTileEntry entry)
        {
            if (manifest == null || manifest.Count == 0)
            {
                entry = default;
                return false;
            }

            int lastIndex = manifest.Count - 1;
            entry = manifest[lastIndex];
            manifest.RemoveAt(lastIndex);
            return true;
        }

        /// <summary>
        /// TileType strutturali/meccanici che non hanno mai avuto (ne' dovrebbero avere)
        /// una specie in ElementCatalog: il loro effetto e' una formula diretta su
        /// DifficultyLevel dentro HexGridController (Trap: HP -= DifficultyLevel via
        /// ApplyImmediateElement; Fountain: HP al massimo, DifficultyLevel ignorato;
        /// Key: attiva HasKeyDL4/5/6; Chest: inerte, RevealEffect Loot non costruito),
        /// mai da FoodRestore/CoinReward di una specie. Trovato 2026-08-05: senza questa
        /// esclusione ApplyElementStats logga un warning per OGNI Trap/Fountain/Key/Chest
        /// piazzata, sempre — rumore fuorviante, non un problema di configurazione da
        /// segnalare al designer.
        /// </summary>
        private static readonly HashSet<TileType> TypesWithoutSpecies = new HashSet<TileType>
        {
            TileType.Trap, TileType.Fountain, TileType.Key, TileType.Chest,
            TileType.Void, // strutturale: nessuna specie attesa, nessun warning da loggare
        };

        /// <summary>
        /// Risolve una specie eleggibile da ElementCatalog per {type, difficultyLevel} e
        /// ne copia FoodRestore/CoinReward sulla tile. HpRestore resta sempre 0: nessun
        /// path del gioco applica oggi danno o cura tramite questo campo (Trap ed Enemy
        /// agiscono direttamente su IGameContextService.Lives, non su HexTileData.HpRestore
        /// — vedi HexGridController.ApplyImmediateElement/ApplyEnemyDamage). Se non esiste
        /// nessuna specie eleggibile per la combinazione, la tile resta a valori neutri;
        /// viene loggato un warning solo se il tipo e' de facto "una creatura/risorsa con
        /// specie" (vedi TypesWithoutSpecies) — per Trap/Fountain/Key/Chest l'assenza di
        /// specie e' normale, non un problema di autoria da segnalare. Degrado sempre
        /// silenzioso lato gameplay, mai un crash.
        /// </summary>
        private void ApplyElementStats(HexTileData tile, TileType type, int difficultyLevel, Random rng)
        {
            tile.Type = type;
            tile.HpRestore = 0;

            var species = _elementCatalog != null ? _elementCatalog.PickRandom(type, difficultyLevel, rng) : null;
            if (species != null)
            {
                tile.FoodRestore = species.FoodRestore;
                tile.MoneteGained = species.CoinReward;
            }
            else
            {
                tile.FoodRestore = 0;
                tile.MoneteGained = 0;
                if (!TypesWithoutSpecies.Contains(type))
                    UnityEngine.Debug.LogWarning($"[AestheticClusterMapGenerator] Nessuna specie eleggibile in ElementCatalog per {type} DifficultyLevel {difficultyLevel}: tile lasciata a valori neutri (FoodRestore/CoinReward 0).");
            }
        }

        private void ResetTile(HexTileData tile, TileType type, int hpRestore, int moneteGained)
        {
            tile.Type = type;
            tile.HpRestore = hpRestore;
            tile.MoneteGained = moneteGained;
        }

        /// <summary>
        /// Conta quanti dei fino a 6 vicini esagonali di coord esistono davvero dentro i
        /// confini della griglia (sono presenti in tiles). Non richiede che siano liberi o
        /// occupati, solo che esistano geometricamente. Usato per il vincolo di
        /// piazzamento DifficultyLevel, vedi ResolveDifficulty.
        /// </summary>
        private int NeighborCount(HexCoord coord, Dictionary<HexCoord, HexTileData> tiles)
        {
            int count = 0;
            for (int dir = 0; dir < 6; dir++)
                if (tiles.ContainsKey(coord.GetNeighbor(dir))) count++;
            return count;
        }

        /// <summary>
        /// Applica il vincolo di piazzamento DifficultyLevel: una tessera con livello
        /// richiesto D e' piazzabile in coord solo se NeighborCount(coord) >= D. Se non lo
        /// e', il livello viene abbassato fino al valore supportato dalla posizione
        /// (minimo 1), stesso TileType — non viene mai scartato il piazzamento stesso,
        /// solo il livello effettivo si adegua alla posizione.
        /// </summary>
        private int ResolveDifficulty(int requestedLevel, HexCoord coord, Dictionary<HexCoord, HexTileData> tiles)
        {
            int neighborCount = NeighborCount(coord, tiles);
            return Math.Max(1, Math.Min(requestedLevel, neighborCount));
        }

        /// <summary>
        /// Assegna una tessera singola (EventCluster da 1 tessera, o rammendo senza
        /// PathCluster adiacente) pescando dal manifest ListType.EventSingle. Se il
        /// manifest e' esaurito (o il LevelConfig manca), ripiega su TileType.Void
        /// (DifficultyLevel 0) — stesso fallback di AssignStopTile. FIX 2026-08-06: prima
        /// non faceva nulla in questo caso, e "nulla" per una HexTileData appena
        /// costruita significa restare al default del costruttore (TileType.Road all'epoca
        /// — cambiato in Void col fix 2026-08-08, vedi HexTileData.Type) — su una griglia
        /// piu' grande del contenuto disponibile questo produceva un'unica macchia di
        /// decine di tessere Road silenziosamente fuse insieme, invece di tante Void
        /// separate. Degrado silenzioso, coerente con "LevelConfig puo' restare
        /// parzialmente autorato" descritto sul LevelConfig stesso.
        /// </summary>
        private void AssignSingleTile(HexTileData tile, HexCoord coord, Dictionary<HexCoord, HexTileData> tiles, Random rng)
        {
            if (TryDrawEntry(_eventSingleManifest, out var entry))
            {
                int level = ResolveDifficulty(entry.DifficultyLevel, coord, tiles);
                ApplyElementStats(tile, entry.Type, level, rng);
                tile.DifficultyLevel = level;
            }
            else
            {
                ResetTile(tile, TileType.Void, hpRestore: 0, moneteGained: 0);
                tile.DifficultyLevel = 0;
            }
        }

        /// <summary>
        /// Assegna una tessera di separazione PathCluster pescando dal manifest
        /// ListType.StopSingle (contenuto reale riskinnato, es. separatore visivo tra due
        /// percorsi). Se il manifest e' esaurito o il LevelConfig manca, ripiega su
        /// TileType.Void (vero no-op, DifficultyLevel 0). In entrambi i casi la separazione
        /// funziona identicamente: CascadeStrada si ferma su qualunque tessera non-Road,
        /// a prescindere da cosa faccia quella tessera.
        /// </summary>
        private void AssignStopTile(HexTileData tile, HexCoord coord, Dictionary<HexCoord, HexTileData> tiles, Random rng)
        {
            if (TryDrawEntry(_stopSingleManifest, out var entry))
            {
                int level = ResolveDifficulty(entry.DifficultyLevel, coord, tiles);
                ApplyElementStats(tile, entry.Type, level, rng);
                tile.DifficultyLevel = level;
            }
            else
            {
                ResetTile(tile, TileType.Void, hpRestore: 0, moneteGained: 0);
                tile.DifficultyLevel = 0;
            }
        }

        private HexCoord PlaceStart(Dictionary<HexCoord, HexTileData> tiles, int width, int height, Random rng)
        {
            var candidates = new List<HexCoord>();
            foreach (var coord in tiles.Keys)
            {
                coord.ToOffsetOddQ(out int col, out int row);
                if (col < _startMinBorderDistance || col >= width - _startMinBorderDistance) continue;
                if (row < _startMinBorderDistance || row >= height - _startMinBorderDistance) continue;
                candidates.Add(coord);
            }

            return candidates.Count == 0
                ? HexCoord.FromOffsetOddQ(width / 2, height / 2)
                : candidates[rng.Next(candidates.Count)];
        }

        private HexCoord PlaceEnd(Dictionary<HexCoord, HexTileData> tiles, HexCoord start, Random rng)
        {
            int targetDistance = WeightedPick(_endDistanceWeights, rng);

            HexCoord best = start;
            int bestDelta = int.MaxValue;
            foreach (var coord in tiles.Keys)
            {
                if (coord.Equals(start)) continue;
                int delta = Math.Abs(coord.DistanceTo(start) - targetDistance);
                if (delta < bestDelta) { bestDelta = delta; best = coord; }
            }

            return best;
        }

        private int WeightedPick(DistanceWeight[] table, Random rng)
        {
            int totalWeight = 0;
            foreach (var entry in table) totalWeight += entry.Weight;

            int roll = rng.Next(totalWeight);
            foreach (var entry in table)
            {
                if (roll < entry.Weight) return entry.Distance;
                roll -= entry.Weight;
            }

            return table[^1].Distance;
        }

        /// <summary>
        /// Piazza EventCluster e tessere singole via rejection sampling. Per ogni slot
        /// cluster tenta la generazione procedurale (TryBuildProceduralCluster, unica
        /// fonte di forme dal 2026-08-07 — il vecchio fallback sul catalogo di forme
        /// manuali/EventClusterCatalog e' stato rimosso, i cluster sono generati
        /// interamente a runtime); se fallisce (LevelConfig senza entry sufficienti)
        /// degrada a tessera singola. Ritorna l'insieme di tutte le tessere occupate;
        /// nextEventId è l'indice progressivo per i piazzamenti successivi (es.
        /// PatchResidualGaps) per garantire ID unici.
        /// </summary>
        private HashSet<HexCoord> PlaceEventClusters(
            Dictionary<HexCoord, HexTileData> tiles, int width, int height,
            HexCoord start, HexCoord end, Random rng, out int nextEventId)
        {
            var occupied = new HashSet<HexCoord>();

            int clustersUntilNextSingle = rng.Next(_eventClusters.ClusterToSingleRatioMin, _eventClusters.ClusterToSingleRatioMax + 1);
            int consecutiveFailures = 0;
            int eventPlacementIndex = 0;

            while (consecutiveFailures < _eventClusters.MaxConsecutiveFailures)
            {
                var origin = HexCoord.FromOffsetOddQ(rng.Next(width), rng.Next(height));

                // Tenta un cluster procedurale; se la generazione fallisce (entry
                // insufficienti) resta null e piu' sotto degrada a tessera singola.
                EventClusterTileSpec[] clusterSpecs = clustersUntilNextSingle > 0
                    ? TryBuildProceduralCluster(rng)
                    : null;

                var footprint = clusterSpecs != null
                    ? ComputeFootprint(origin, clusterSpecs)
                    : new List<HexCoord> { origin };

                if (!IsValidEventClusterPlacement(footprint, tiles, start, end, occupied))
                {
                    consecutiveFailures++;
                    continue;
                }

                if (clusterSpecs != null)
                {
                    ApplyEventClusterShape(tiles, origin, clusterSpecs, rng);
                    clustersUntilNextSingle--;

                    // Marca il tipo di centro come usato SOLO ora che il piazzamento e'
                    // confermato valido (2026-08-07, vedi doc su _usedClusterCenterTypes):
                    // clusterSpecs[0] e' sempre il centro (RelativeQ=0, RelativeR=0, vedi
                    // TryBuildProceduralCluster).
                    _usedClusterCenterTypes.Add((clusterSpecs[0].Type, clusterSpecs[0].DifficultyLevel));
                }
                else
                {
                    AssignSingleTile(tiles[origin], origin, tiles, rng);
                    clustersUntilNextSingle = rng.Next(_eventClusters.ClusterToSingleRatioMin, _eventClusters.ClusterToSingleRatioMax + 1);
                }

                foreach (var coord in footprint)
                {
                    occupied.Add(coord);
                    tiles[coord].EventPlacementId = eventPlacementIndex;
                }
                eventPlacementIndex++;
                consecutiveFailures = 0;
            }

            nextEventId = eventPlacementIndex;
            return occupied;
        }

        private List<HexCoord> ComputeFootprint(HexCoord origin, EventClusterTileSpec[] specs)
        {
            var footprint = new List<HexCoord>(specs.Length);
            foreach (var tileSpec in specs)
                footprint.Add(origin + new HexCoord(tileSpec.RelativeQ, tileSpec.RelativeR));
            return footprint;
        }

        private bool IsValidEventClusterPlacement(
            List<HexCoord> footprint, Dictionary<HexCoord, HexTileData> tiles,
            HexCoord start, HexCoord end, HashSet<HexCoord> occupied)
        {
            foreach (var coord in footprint)
            {
                if (!tiles.ContainsKey(coord)) return false;
                if (coord.Equals(start) || coord.Equals(end)) return false;
                if (coord.DistanceTo(start) < _clusterMinDistanceFromStartEnd) return false;
                if (coord.DistanceTo(end) < _clusterMinDistanceFromStartEnd) return false;
                if (occupied.Contains(coord)) return false;

                for (int dir = 0; dir < 6; dir++)
                {
                    var neighbor = coord.GetNeighbor(dir);
                    if (occupied.Contains(neighbor) && !footprint.Contains(neighbor)) return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Applica le spec di un EventCluster (procedurali o da catalogo) alla griglia:
        /// ogni spec porta Type e DifficultyLevel autorato/generato. Il DifficultyLevel
        /// passa per ResolveDifficulty perche' la posizione finale e' scelta a runtime
        /// (rejection sampling) e potrebbe non avere abbastanza vicini per il livello
        /// richiesto. La specie viene risolta via ElementCatalog come per ogni altra tessera.
        /// </summary>
        private void ApplyEventClusterShape(Dictionary<HexCoord, HexTileData> tiles, HexCoord origin, EventClusterTileSpec[] specs, Random rng)
        {
            foreach (var tileSpec in specs)
            {
                var coord = origin + new HexCoord(tileSpec.RelativeQ, tileSpec.RelativeR);
                int level = ResolveDifficulty(tileSpec.DifficultyLevel, coord, tiles);
                ApplyElementStats(tiles[coord], tileSpec.Type, level, rng);
                tiles[coord].DifficultyLevel = level;
            }
        }

        /// <summary>
        /// Genera proceduralmente le spec di un EventCluster da 7 tessere (centro + 6
        /// adiacenti) usando le entry ListType.EventCluster di LevelConfig come palette.
        ///
        /// Regole di composizione:
        /// - Centro: entry EventCluster con DifficultyLevel 4/5/6, scelta casualmente TRA
        ///   quelle il cui (Type, DifficultyLevel) non e' ancora in _usedClusterCenterTypes
        ///   (2026-08-07, regola Franci: un solo cluster per tipo di centro sull'intera
        ///   mappa — Enemy DL4 ed Enemy DL5 sono due tipi distinti). La combinazione scelta
        ///   qui NON viene marcata come usata subito: lo fa PlaceEventClusters, e solo dopo
        ///   che il piazzamento e' stato validato — vedi doc su _usedClusterCenterTypes sul
        ///   perche' non si fa qui.
        /// - Se il centro è Enemy: 2 Trap nel ring + 4 posizioni con tipi unici non-Trap.
        /// - Altrimenti: 3 Trap nel ring + 3 posizioni con tipi unici non-Trap.
        /// - Le Trap sono scelte casualmente tra le entry Trap DL 1/2.
        /// - Le posizioni non-Trap hanno ognuna un tipo diverso; per ogni tipo viene scelta
        ///   casualmente una entry tra quelle DL 1/2 disponibili.
        /// - Le posizioni nel ring vengono mescolate (Fisher-Yates).
        ///
        /// Ritorna null se LevelConfig manca, non ha entry sufficienti, o tutti i tipi di
        /// centro disponibili sono gia' stati usati — il chiamante (PlaceEventClusters)
        /// degrada a tessera singola.
        /// </summary>
        private EventClusterTileSpec[] TryBuildProceduralCluster(Random rng)
        {
            if (_levelConfig?.Entries == null || _levelConfig.Entries.Count == 0) return null;

            // Centro: entry EventCluster a DL 4/5/6, escluse le combinazioni (Type, DL) gia' usate.
            var centerCandidates = new List<LevelTileEntry>();
            foreach (var e in _levelConfig.Entries)
                if (e.ListType == TileListType.EventCluster && e.DifficultyLevel >= 4 && e.DifficultyLevel <= 6
                    && !_usedClusterCenterTypes.Contains((e.Type, e.DifficultyLevel)))
                    centerCandidates.Add(e);
            if (centerCandidates.Count == 0) return null;

            var center = centerCandidates[rng.Next(centerCandidates.Count)];
            int trapCount  = center.Type == TileType.Enemy ? 2 : 3;
            int otherCount = 6 - trapCount;

            // Trap circostanti: entry EventCluster Trap a DL 1/2
            var trapCandidates = new List<LevelTileEntry>();
            foreach (var e in _levelConfig.Entries)
                if (e.ListType == TileListType.EventCluster && e.Type == TileType.Trap
                    && e.DifficultyLevel >= 1 && e.DifficultyLevel <= 2)
                    trapCandidates.Add(e);
            if (trapCandidates.Count == 0)
            {
                WarnInsufficientComposition(center, "nessuna entry ListType.EventCluster Trap a DL1/2 in LevelConfig.Entries");
                return null;
            }

            // Non-Trap circostanti: tipi unici, EventCluster a DL 1/2
            var nonTrapByType = new Dictionary<TileType, List<LevelTileEntry>>();
            foreach (var e in _levelConfig.Entries)
            {
                if (e.ListType != TileListType.EventCluster) continue;
                if (e.Type == TileType.Trap) continue;
                if (e.DifficultyLevel < 1 || e.DifficultyLevel > 2) continue;
                if (!nonTrapByType.ContainsKey(e.Type))
                    nonTrapByType[e.Type] = new List<LevelTileEntry>();
                nonTrapByType[e.Type].Add(e);
            }

            var availableTypes = new List<TileType>(nonTrapByType.Keys);
            if (availableTypes.Count < otherCount)
            {
                WarnInsufficientComposition(center,
                    $"servono {otherCount} tipi non-Trap distinti a DL1/2 (ListType.EventCluster), disponibili solo {availableTypes.Count}");
                return null;
            }

            // Shuffle completo per selezione casuale senza ripetizione
            for (int i = availableTypes.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (availableTypes[i], availableTypes[j]) = (availableTypes[j], availableTypes[i]);
            }

            // Assembla il ring: Trap + non-Trap
            var ring = new List<EventClusterTileSpec>(6);

            for (int i = 0; i < trapCount; i++)
            {
                var e = trapCandidates[rng.Next(trapCandidates.Count)];
                ring.Add(new EventClusterTileSpec { Type = e.Type, DifficultyLevel = e.DifficultyLevel });
            }

            for (int i = 0; i < otherCount; i++)
            {
                var candidates = nonTrapByType[availableTypes[i]];
                var e = candidates[rng.Next(candidates.Count)];
                ring.Add(new EventClusterTileSpec { Type = e.Type, DifficultyLevel = e.DifficultyLevel });
            }

            // Mescola le posizioni nel ring
            for (int i = ring.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (ring[i], ring[j]) = (ring[j], ring[i]);
            }

            // Costruisci specs: centro (0,0) + 6 vicini
            var specs = new EventClusterTileSpec[7];
            specs[0] = new EventClusterTileSpec
            {
                RelativeQ = 0, RelativeR = 0,
                Type = center.Type, DifficultyLevel = center.DifficultyLevel,
            };
            for (int dir = 0; dir < 6; dir++)
            {
                var offset = new HexCoord(0, 0).GetNeighbor(dir);
                specs[dir + 1] = new EventClusterTileSpec
                {
                    RelativeQ = offset.Q, RelativeR = offset.R,
                    Type = ring[dir].Type, DifficultyLevel = ring[dir].DifficultyLevel,
                };
            }

            return specs;
        }

        /// <summary>
        /// Warning deduplicato (2026-08-08) per un centro la cui composizione ring non e'
        /// realizzabile con le entry ListType.EventCluster disponibili — vedi doc su
        /// _warnedInsufficientClusterComposition sul perche' della dedup. Un solo log per
        /// combinazione (Type, DifficultyLevel) per Generate, anche se TryBuildProceduralCluster
        /// la ripesca decine di volte prima di esaurire il pool o abbandonare il tentativo.
        /// </summary>
        private void WarnInsufficientComposition(LevelTileEntry center, string reason)
        {
            if (!_warnedInsufficientClusterComposition.Add((center.Type, center.DifficultyLevel))) return;

            UnityEngine.Debug.LogWarning(
                $"[AestheticClusterMapGenerator] Cluster con centro {center.Type} DL{center.DifficultyLevel} mai generabile in questa run: {reason}. " +
                "Aggiungi le entry mancanti a LevelConfig.Entries (ListType.EventCluster) o questo tipo di cluster degradera' sempre a tessera singola.");
        }

        // ===== Mesh PathCluster =====

        /// <summary>
        /// Punto di partenza: ogni tessera di bordo di ogni EventCluster/singola piazzata
        /// (adiacente a una tessera occupata, non occupata essa stessa, non Start/End),
        /// mescolate in ordine casuale. Da ognuna, se ancora libera al suo turno, cresce
        /// un intero PathCluster a budget. Nessuna soglia di tentativi da tarare: la lista
        /// di partenza è finita per costruzione, quindi il processo termina da sé.
        ///
        /// originsCount/attemptedClusters (2026-08-07) esistono solo per il messaggio
        /// diagnostico di Generate quando il vincolo MinPathClusters non e' soddisfatto:
        /// originsCount = quanti bordi disponibili aveva la mesh per partire (0 o molto
        /// basso → EventCluster/Start non offrono punti di aggancio, non e' un problema di
        /// budget); attemptedClusters = quanti PathCluster hanno effettivamente prodotto
        /// almeno una tessera (puo' essere piu' alto del conteggio "reale" post-filtro in
        /// CountRealPathClusters se alcuni sono stati demotati interamente a Stop per
        /// separazione — il divario tra i due numeri e' la spia di quel caso).
        /// </summary>
        private void GeneratePathClusterMesh(
            Dictionary<HexCoord, HexTileData> tiles, HexCoord start, HexCoord end,
            HashSet<HexCoord> eventOccupied, Random rng,
            out HashSet<HexCoord> globalClaimed, out Dictionary<HexCoord, int> tileOwner,
            out int originsCount, out int attemptedClusters)
        {
            var origins = CollectClusterBorders(tiles, eventOccupied, start, end, rng);
            originsCount = origins.Count;

            globalClaimed = new HashSet<HexCoord>();
            tileOwner = new Dictionary<HexCoord, int>();
            int pathClusterIndex = 0;

            foreach (var (originTile, originDir) in origins)
            {
                if (eventOccupied.Contains(originTile) || globalClaimed.Contains(originTile)) continue;

                var pathTiles = GrowOnePathCluster(tiles, originTile, originDir, start, end, eventOccupied, globalClaimed, rng);
                if (pathTiles.Count == 0) continue;

                // Separazione da un PathCluster diverso già piazzato: se una tessera di
                // questo PathCluster confina con una tessera di un altro, pesca da una
                // entry ListType.StopSingle (o Void) invece di restare Strada, così
                // CascadeStrada non li fonde in un'unica cascata.
                var stopOverride = new HashSet<HexCoord>();
                foreach (var coord in pathTiles)
                {
                    for (int dir = 0; dir < 6; dir++)
                    {
                        var neighbor = coord.GetNeighbor(dir);
                        if (tileOwner.TryGetValue(neighbor, out int ownerIdx) && ownerIdx != pathClusterIndex)
                        {
                            stopOverride.Add(coord);
                            break;
                        }
                    }
                }

                foreach (var coord in pathTiles)
                {
                    var tileData = tiles[coord];
                    if (stopOverride.Contains(coord))
                    {
                        AssignStopTile(tileData, coord, tiles, rng);
                    }
                    else
                    {
                        ResetTile(tileData, TileType.Road, hpRestore: 0, moneteGained: 0);
                        tileData.DifficultyLevel = 0;
                    }

                    globalClaimed.Add(coord);
                    tileOwner[coord] = pathClusterIndex;
                    tileData.PathClusterId = pathClusterIndex;
                }

                pathClusterIndex++;
            }

            attemptedClusters = pathClusterIndex;
        }

        /// <summary>
        /// Ogni tessera ancora priva di contenuto dopo EventCluster e mesh PathCluster
        /// (né Start, né End, né occupata, né già Strada/ListType.StopSingle) viene
        /// risolta qui: se confina con un PathCluster che non ha ancora esaurito la sua
        /// quota di 2 tessere extra, diventa Strada (o una entry ListType.StopSingle
        /// se confina anche con un PathCluster diverso) e si aggiunge a quel PathCluster;
        /// altrimenti diventa una tessera singola dal manifest ListType.EventSingle. Con
        /// questa passata nessuna tessera della griglia resta senza contenuto esplicito.
        /// </summary>
        private void PatchResidualGaps(
            Dictionary<HexCoord, HexTileData> tiles, HexCoord start, HexCoord end,
            HashSet<HexCoord> eventOccupied, HashSet<HexCoord> globalClaimed,
            Dictionary<HexCoord, int> tileOwner, Random rng, int nextEventId)
        {
            const int maxPatchPerPathCluster = 2;
            var patchCountPerPathCluster = new Dictionary<int, int>();

            foreach (var coord in tiles.Keys)
            {
                if (coord.Equals(start) || coord.Equals(end)) continue;
                if (eventOccupied.Contains(coord) || globalClaimed.Contains(coord)) continue;

                int? extendOwner = null;
                for (int dir = 0; dir < 6; dir++)
                {
                    var neighbor = coord.GetNeighbor(dir);
                    if (!tileOwner.TryGetValue(neighbor, out int ownerIdx)) continue;

                    int used = patchCountPerPathCluster.TryGetValue(ownerIdx, out int u) ? u : 0;
                    if (used < maxPatchPerPathCluster)
                    {
                        extendOwner = ownerIdx;
                        break;
                    }
                }

                if (extendOwner.HasValue)
                {
                    // Stessa logica di separazione di GeneratePathClusterMesh: se questa
                    // tessera confina su qualsiasi lato con un PathCluster diverso
                    // dall'extendOwner, pesca da una entry ListType.StopSingle per non fare
                    // da ponte tra i due cluster (CascadeStrada li fonderebbe in un'unica cascata).
                    bool touchesOtherCluster = false;
                    for (int dir = 0; dir < 6; dir++)
                    {
                        var nb = coord.GetNeighbor(dir);
                        if (tileOwner.TryGetValue(nb, out int nbOwner) && nbOwner != extendOwner.Value)
                        {
                            touchesOtherCluster = true;
                            break;
                        }
                    }

                    // Vincolo lati non consecutivi (2026-08-07, vedi WouldViolateConsecutiveSides):
                    // a differenza della crescita PathCluster, qui tutti i cluster sono gia'
                    // scritti su tiles, quindi alsoRoad=null basta.
                    bool wouldViolateSides = WouldViolateConsecutiveSides(coord, tiles, alsoRoad: null);

                    var tileData = tiles[coord];
                    if (touchesOtherCluster || wouldViolateSides)
                    {
                        if (touchesOtherCluster) _patchDemotedByOtherCluster++;
                        if (wouldViolateSides) _rejectedByConsecutiveSides++;
                        AssignStopTile(tileData, coord, tiles, rng);
                    }
                    else
                    {
                        ResetTile(tileData, TileType.Road, hpRestore: 0, moneteGained: 0);
                        tileData.DifficultyLevel = 0;
                    }

                    globalClaimed.Add(coord);
                    tileOwner[coord] = extendOwner.Value;
                    tileData.PathClusterId = extendOwner.Value;
                    patchCountPerPathCluster[extendOwner.Value] =
                        (patchCountPerPathCluster.TryGetValue(extendOwner.Value, out int u2) ? u2 : 0) + 1;
                }
                else
                {
                    AssignSingleTile(tiles[coord], coord, tiles, rng);
                    tiles[coord].EventPlacementId = nextEventId++;
                    eventOccupied.Add(coord);
                }
            }
        }

        private List<(HexCoord tile, int direction)> CollectClusterBorders(
            Dictionary<HexCoord, HexTileData> tiles, HashSet<HexCoord> eventOccupied,
            HexCoord start, HexCoord end, Random rng)
        {
            var seen = new HashSet<HexCoord>();
            var borders = new List<(HexCoord, int)>();

            foreach (var occupiedCoord in eventOccupied)
            {
                for (int dir = 0; dir < 6; dir++)
                {
                    var neighbor = occupiedCoord.GetNeighbor(dir);
                    if (!tiles.ContainsKey(neighbor)) continue;
                    if (eventOccupied.Contains(neighbor)) continue;
                    if (neighbor.Equals(start) || neighbor.Equals(end)) continue;
                    if (!seen.Add(neighbor)) continue;
                    borders.Add((neighbor, dir));
                }
            }

            // 2026-08-07: Start e' sempre Strada (vedi Generate) ma, a differenza degli
            // EventCluster, non offriva bordi da cui far nascere un PathCluster — poteva
            // restare Strada isolata (zero vicini Strada), contro la regola "ogni Strada
            // confina con almeno un'altra Strada". Stesso identico meccanismo di seeding
            // degli EventCluster sopra, non uno nuovo: aumenta la probabilita' che un
            // PathCluster nasca adiacente a Start, non e' una garanzia assoluta (rejection
            // sampling/budget possono comunque scartarlo) — vedi il controllo di
            // sicurezza, solo un warning, a fine Generate.
            for (int dir = 0; dir < 6; dir++)
            {
                var neighbor = start.GetNeighbor(dir);
                if (!tiles.ContainsKey(neighbor)) continue;
                if (eventOccupied.Contains(neighbor)) continue;
                if (neighbor.Equals(end)) continue;
                if (!seen.Add(neighbor)) continue;
                borders.Add((neighbor, dir));
            }

            // Fisher-Yates: ordine di elaborazione casuale, non l'ordine di scoperta.
            for (int i = borders.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (borders[i], borders[j]) = (borders[j], borders[i]);
            }

            return borders;
        }

        /// <summary>
        /// Cresce un intero PathCluster (budget 15 tessere / 5 rami / rami 3-7, gli stessi
        /// pesi STOP/fork-3/fork-5 e di deviazione di prima) a partire da una singola
        /// origine. Si blocca su: bordi griglia, Start/End, tessere di un EventCluster
        /// (muro fisso), tessere già usate da un altro PathCluster (muro che cresce).
        /// </summary>
        private List<HexCoord> GrowOnePathCluster(
            Dictionary<HexCoord, HexTileData> tiles, HexCoord originTile, int originDir,
            HexCoord start, HexCoord end, HashSet<HexCoord> eventOccupied, HashSet<HexCoord> globalClaimed, Random rng)
        {
            var localClaimed = new HashSet<HexCoord>();
            var allTiles = new List<HexCoord>();

            int tilesBudget = _strada.MaxTotalTiles;
            int branchBudget = _strada.MaxBranches;

            var queue = new Queue<(HexCoord origin, int dir, bool includeOrigin)>();
            queue.Enqueue((originTile, originDir, true));

            while (queue.Count > 0 && tilesBudget >= _strada.MinBranchLength && branchBudget > 0)
            {
                var (origin, dir, includeOrigin) = queue.Dequeue();
                if (includeOrigin && IsBlocked(origin, tiles, start, end, eventOccupied, globalClaimed, localClaimed)) { _rejectedByBlocked++; continue; }
                if (includeOrigin && WouldViolateConsecutiveSides(origin, tiles, localClaimed)) { _rejectedByConsecutiveSides++; continue; }

                int maxLenHere = Math.Min(_strada.MaxBranchLength, tilesBudget);
                if (maxLenHere < _strada.MinBranchLength) { _rejectedByBudget++; continue; }

                int length = rng.Next(_strada.MinBranchLength, maxLenHere + 1);
                var path = GrowBranch(tiles, origin, dir, length, includeOrigin, start, end, eventOccupied, globalClaimed, localClaimed, rng, out int finalDir);
                if (path.Count < _strada.MinBranchLength) { _rejectedByShortBranch++; continue; }

                allTiles.AddRange(path);
                tilesBudget -= path.Count;
                branchBudget -= 1;

                var endpoint = path[^1];
                var outcomes = new List<(int weight, int forkCount)> { (_strada.StopWeight, 0) };
                if (branchBudget >= 2 && tilesBudget >= _strada.MinBranchLength * 2)
                    outcomes.Add((_strada.Fork3Weight, 2));
                if (branchBudget >= 4 && tilesBudget >= _strada.MinBranchLength * 4)
                    outcomes.Add((_strada.Fork5Weight, 4));

                int forkCount = WeightedPickOutcome(outcomes, rng);
                if (forkCount > 0)
                {
                    // 2026-08-08: finalDir, non dir — vedi doc su GrowBranch/finalDir.
                    // EnqueueFork deve biforcare rispetto a come il ramo e' arrivato
                    // davvero all'endpoint, non a come e' partito.
                    EnqueueFork(queue, endpoint, finalDir, forkCount, rng);
                    branchBudget -= forkCount;
                }
            }

            return allTiles;
        }

        private bool IsBlocked(
            HexCoord coord, Dictionary<HexCoord, HexTileData> tiles, HexCoord start, HexCoord end,
            HashSet<HexCoord> eventOccupied, HashSet<HexCoord> globalClaimed, HashSet<HexCoord> localClaimed)
        {
            if (!tiles.ContainsKey(coord)) return true;
            if (coord.Equals(start) || coord.Equals(end)) return true;
            if (eventOccupied.Contains(coord)) return true;
            if (globalClaimed.Contains(coord)) return true;
            if (localClaimed.Contains(coord)) return true;
            return false;
        }

        // ===== Vincolo "lati non consecutivi" (2026-08-07) =====
        //
        // Regola confermata Franci: ogni tile Strada deve confinare con almeno un'altra
        // Strada (mai isolata) e puo' confinare con due o piu' Strada, ma MAI su due lati
        // consecutivi dell'esagono — due vicini su lati consecutivi sono a loro volta
        // vicini tra loro, il che produrrebbe un tratto largo 2 celle invece di 1 (un
        // "blob" invece di un percorso). Non vieta i fork: un fork a 3 vie (ramo in
        // entrata + 2 nuovi rami, vedi EnqueueFork con offset -1/+1) piazza per
        // costruzione i 3 vicini su lati alternati (differenza 2), sempre non
        // consecutivi — MA SOLO SE l'offset e' calcolato sulla direzione vera con cui il
        // ramo arriva al punto di fork: bug trovato 2026-08-08, EnqueueFork riceveva la
        // direzione di PARTENZA del ramo (stantia dopo le deviazioni di DeviateDirection),
        // non quella finale — corretto passando GrowBranch.finalDir invece di dir, vedi
        // GrowOnePathCluster. Prima del fix un fork su un ramo che aveva svoltato veniva
        // respinto da WouldViolateConsecutiveSides per un motivo che non era la geometria
        // reale ma la direzione sbagliata usata per calcolarla. Un fork a 5 vie invece
        // NON puo' mai rispettare la regola in ogni caso (5 vicini su 6 lati significano
        // che almeno 4 coppie sono consecutive per
        // pigeonhole), quindi con questo vincolo attivo i rami oltre il terzo di un
        // Fork5 vengono naturalmente scartati da WouldViolateConsecutiveSides invece che
        // creare un incrocio a 5 vie: degrado organico, stesso principio "mai crashare,
        // degrada silenziosamente" del resto del generatore (vedi ResolveDifficulty,
        // ApplyElementStats). Fork3Weight/Fork5Weight restano cosi' come sono in
        // MapGenerationConfig: non li ho toccati, la regola li rende semplicemente auto-
        // limitanti.

        /// <summary>
        /// Direzioni (0-5) verso vicini di coord che sono gia' Strada in tiles, o stanno
        /// per diventarlo nello stesso PathCluster in crescita (alsoRoad — le tile del
        /// cluster corrente non hanno ancora Type=Road scritto su tiles, vedi GrowBranch/
        /// GeneratePathClusterMesh: l'assegnazione avviene solo a fine cluster). Null per
        /// alsoRoad quando il chiamante non ha uno stato "in corso" da considerare (es.
        /// PatchResidualGaps, dove ogni cluster e' gia' stato scritto su tiles).
        /// </summary>
        private IEnumerable<int> RoadNeighborDirections(HexCoord coord, Dictionary<HexCoord, HexTileData> tiles, HashSet<HexCoord> alsoRoad)
        {
            for (int dir = 0; dir < 6; dir++)
            {
                var neighbor = coord.GetNeighbor(dir);
                bool isRoad = (tiles.TryGetValue(neighbor, out var data) && data.Type == TileType.Road)
                              || (alsoRoad != null && alsoRoad.Contains(neighbor));
                if (isRoad) yield return dir;
            }
        }

        /// <summary>True se dirA e dirB sono lati consecutivi dell'esagono (differiscono di 1, incluso il wraparound 5/0).</summary>
        private static bool AreConsecutiveSides(int dirA, int dirB)
        {
            int diff = Math.Abs(dirA - dirB) % 6;
            return diff == 1 || diff == 5;
        }

        /// <summary>
        /// True se rendere Strada la tile a coord violerebbe il vincolo "nessun lato
        /// consecutivo", da due prospettive: 1) coord stesso finirebbe con due vicini
        /// Strada su lati consecutivi; 2) un vicino gia' Strada di coord finirebbe con due
        /// vicini Strada su lati consecutivi (il vicino stesso, piu' coord che gli aggiunge
        /// un lato occupato in piu'). Entrambe le prospettive sono necessarie: la prima non
        /// basta a cogliere il caso in cui e' il VICINO a superare il limite per colpa
        /// della nuova tile, non coord stesso.
        /// </summary>
        private bool WouldViolateConsecutiveSides(HexCoord coord, Dictionary<HexCoord, HexTileData> tiles, HashSet<HexCoord> alsoRoad)
        {
            var ownDirs = new List<int>(RoadNeighborDirections(coord, tiles, alsoRoad));
            for (int i = 0; i < ownDirs.Count; i++)
                for (int j = i + 1; j < ownDirs.Count; j++)
                    if (AreConsecutiveSides(ownDirs[i], ownDirs[j])) return true;

            for (int dir = 0; dir < 6; dir++)
            {
                var neighbor = coord.GetNeighbor(dir);
                bool neighborIsRoad = (tiles.TryGetValue(neighbor, out var data) && data.Type == TileType.Road)
                                      || (alsoRoad != null && alsoRoad.Contains(neighbor));
                if (!neighborIsRoad) continue;

                int backDir = (dir + 3) % 6; // direzione da neighbor verso coord
                var neighborDirs = new List<int>(RoadNeighborDirections(neighbor, tiles, alsoRoad));
                if (!neighborDirs.Contains(backDir)) neighborDirs.Add(backDir);

                for (int i = 0; i < neighborDirs.Count; i++)
                    for (int j = i + 1; j < neighborDirs.Count; j++)
                        if (AreConsecutiveSides(neighborDirs[i], neighborDirs[j])) return true;
            }

            return false;
        }

        /// <summary>
        /// Rete di sicurezza a fine Generate, solo log — non modifica la griglia. La
        /// crescita costruttiva (WouldViolateConsecutiveSides in GrowBranch/
        /// PatchResidualGaps, seeding da Start in CollectClusterBorders) dovrebbe gia'
        /// impedire entrambe le violazioni; questo controllo esiste per accorgersi subito
        /// in Console se un caso non previsto le aggira, invece di scoprirlo a occhio su
        /// una mappa. Stesso principio "degrado silenzioso ma visibile" del resto del
        /// generatore (vedi ApplyElementStats) — qui non c'e' nulla da degradare a
        /// generazione ormai completa, quindi solo warning.
        /// </summary>
        private void ValidateRoadAdjacencyRule(Dictionary<HexCoord, HexTileData> tiles)
        {
            foreach (var kvp in tiles)
            {
                if (kvp.Value.Type != TileType.Road) continue;

                var roadDirs = new List<int>(RoadNeighborDirections(kvp.Key, tiles, alsoRoad: null));

                if (roadDirs.Count == 0)
                {
                    UnityEngine.Debug.LogWarning($"[AestheticClusterMapGenerator] Regola adiacenza Strada violata: {kvp.Key} e' Strada isolata (zero vicini Strada).");
                    continue;
                }

                for (int i = 0; i < roadDirs.Count; i++)
                for (int j = i + 1; j < roadDirs.Count; j++)
                    if (AreConsecutiveSides(roadDirs[i], roadDirs[j]))
                        UnityEngine.Debug.LogWarning($"[AestheticClusterMapGenerator] Regola adiacenza Strada violata: {kvp.Key} ha due vicini Strada su lati consecutivi ({roadDirs[i]}/{roadDirs[j]}).");
            }
        }

        /// <summary>
        /// finalDir (2026-08-08, bug trovato: EnqueueFork riceveva la direzione di PARTENZA
        /// del ramo, non quella con cui il ramo arriva davvero all'endpoint dopo le
        /// deviazioni di DeviateDirection). Senza restituirla, il chiamante non ha modo di
        /// sapere come il ramo ha effettivamente svoltato — e la dimostrazione che un fork a
        /// 3 vie e' sempre geometricamente valido rispetto al vincolo lati-non-consecutivi
        /// vale solo se il fork usa la direzione VERA di ingresso al punto di biforcazione,
        /// non quella di partenza. Ritorna heading invariata se il ramo e' vuoto/di sola
        /// origine (nessun passo fatto, nessuna deviazione da riportare).
        /// </summary>
        private List<HexCoord> GrowBranch(
            Dictionary<HexCoord, HexTileData> tiles, HexCoord origin, int heading, int length, bool includeOrigin,
            HexCoord start, HexCoord end, HashSet<HexCoord> eventOccupied, HashSet<HexCoord> globalClaimed,
            HashSet<HexCoord> localClaimed, Random rng, out int finalDir)
        {
            var path = new List<HexCoord>();
            var current = origin;
            int dir = heading;
            int steps = length;

            if (includeOrigin)
            {
                path.Add(origin);
                localClaimed.Add(origin);
                steps -= 1;
            }

            for (int i = 0; i < steps; i++)
            {
                dir = DeviateDirection(dir, rng);
                var next = current.GetNeighbor(dir);
                if (IsBlocked(next, tiles, start, end, eventOccupied, globalClaimed, localClaimed)) { _rejectedByBlocked++; break; }
                if (WouldViolateConsecutiveSides(next, tiles, localClaimed)) { _rejectedByConsecutiveSides++; break; }

                path.Add(next);
                localClaimed.Add(next);
                current = next;
            }

            finalDir = dir;

            return path;
        }

        private int DeviateDirection(int dir, Random rng)
        {
            int total = _strada.KeepHeadingWeight + _strada.TurnWeight;
            int roll = rng.Next(total);

            if (roll < _strada.KeepHeadingWeight) return dir;

            int turnRoll = roll - _strada.KeepHeadingWeight;
            bool turnRight = turnRoll < _strada.TurnWeight / 2;
            return turnRight ? (dir + 1) % 6 : (dir + 5) % 6;
        }

        private int WeightedPickOutcome(List<(int weight, int forkCount)> outcomes, Random rng)
        {
            int total = 0;
            foreach (var o in outcomes) total += o.weight;

            int roll = rng.Next(total);
            foreach (var o in outcomes)
            {
                if (roll < o.weight) return o.forkCount;
                roll -= o.weight;
            }

            return outcomes[^1].forkCount;
        }

        private void EnqueueFork(
            Queue<(HexCoord origin, int dir, bool includeOrigin)> queue,
            HexCoord origin, int parentDir, int count, Random rng)
        {
            int[] offsets = count >= 4
                ? new[] { -2, -1, 1, 2 }
                : new[] { -1, 1 };

            foreach (var offset in offsets)
                queue.Enqueue((origin, ((parentDir + offset) % 6 + 6) % 6, false));
        }

        private void RevealInitialTiles(HexTileData startTile, HexTileData endTile)
        {
            startTile.Exploration = ExplorationState.Explored; startTile.Spotting = SpottingState.Spotted;
            endTile.Exploration = ExplorationState.Unexplored; endTile.Spotting = SpottingState.Spotted;
        }
    }
}