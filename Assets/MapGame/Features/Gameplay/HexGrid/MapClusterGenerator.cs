using System;
using System.Collections.Generic;
using hp55games.MapGame.Features.Configs;

namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    /// <summary>
    /// Generatore di mappa, pipeline "macchie di leopardo" (GDD):
    /// 1. Piazzamento Start/End (weighted distance). Start diventa Road, End diventa
    ///    Enemy con IsObjective = true, DifficultyLevel 6 (placeholder "boss finale" —
    ///    vedi Generate).
    /// 2. Piazzamento EventCluster (cluster da 7 tessere generati interamente a runtime
    ///    dalle entry LevelConfig.ClusterMainTileEntries/ClusterFillerTileEntries — vedi
    ///    TryBuildProceduralCluster) e tessere singole, in due fasi separate via rejection
    ///    sampling (vedi
    ///    PlaceEventClusters), con almeno una tessera di distacco tra due piazzamenti: prima
    ///    tutti i cluster disponibili, poi tutte le singole del manifest — nessun rapporto
    ///    cluster:singola, ogni tipo di cluster compare al massimo una volta e ogni singola
    ///    ha gia' il proprio Amount esplicito, quantita' note in anticipo. Un solo cluster
    ///    per tipo di centro sull'intera mappa: il tipo di un cluster e' la coppia (Type,
    ///    DifficultyLevel) del suo centro — Enemy DL4 ed Enemy DL5 sono due tipi distinti,
    ///    ciascuno piazzabile al massimo una volta — vedi _usedClusterCenterTypes, popolato
    ///    in PlaceEventClusters solo dopo un piazzamento validato, mai ripescato.
    /// 3. Mesh di PathCluster: parte da ogni bordo di ogni EventCluster/singola piazzata
    ///    (i semi Road di ogni cluster per primi, vedi punto 12), cresce a budget (15
    ///    tessere / 5 rami / rami 3-7) per ciascun PathCluster, si ferma sui bordi degli
    ///    EventCluster (fanno da muro, nessuna separazione necessaria lì) e pesca dalle
    ///    entry LevelConfig.StopTileEntries dove tocca un PathCluster diverso già piazzato
    ///    (per non farli fondere in un'unica cascata).
    /// 4. Rammendo: ogni tessera ancora priva di contenuto diventa Strada extra su un
    ///    PathCluster confinante (massimo 2 per PathCluster, o una entry StopTileEntries
    ///    se confina anche con un PathCluster diverso) oppure, se non c'è un PathCluster a
    ///    cui attaccarsi, una tessera singola da un'entry SingleTileEntries. Nessuna
    ///    tessera resta senza contenuto esplicito.
    /// 5. Vincolo DifficultyLevel: ogni tessera pescata porta con se' un DifficultyLevel
    ///    (1-6) dalla entry scelta; se la posizione finale non ha abbastanza vicini
    ///    validi in griglia per quel livello, il livello viene abbassato fino al valore
    ///    supportato (minimo 1), stesso TileType — vedi ResolveDifficulty.
    /// 6. Composizione fissa e deterministica: le entry di LevelConfig.SingleTileEntries e
    ///    StopTileEntries NON sono pool pesati con reinserimento. Sono manifest: ogni entry
    ///    rappresenta UNA istanza da piazzare (o Amount istanze per SingleTileEntries, vedi
    ///    sotto), consumata quando viene usata. All'inizio di Generate, BuildManifest
    ///    mescola (Fisher-Yates) ciascuna lista con lo stesso Random(seed) della run, poi
    ///    TryDrawEntry pesca dalla coda senza mai reinserire — stesso seed produce sempre la
    ///    stessa sequenza di pesche. Se il manifest si esaurisce prima che la griglia sia
    ///    piena, il generatore degrada come per un manifest vuoto (vedi AssignSingleTile/
    ///    AssignStopTile), non crasha. LevelConfig.ClusterMainTileEntries/
    ///    ClusterFillerTileEntries sono la palette per la generazione procedurale dei
    ///    cluster: TryBuildProceduralCluster le usa per comporre centro e ring di ogni
    ///    cluster generato a runtime, Amount ignorato li'. Amount conta SOLO per
    ///    SingleTileEntries: BuildManifest espande ogni entry in Amount copie PRIMA dello
    ///    shuffle — un'entry con Amount=3 sostituisce 3 entry duplicate a mano, senza
    ///    cambiare la garanzia di determinismo (l'espansione e' deterministica, lo shuffle
    ///    successivo e' comunque guidato solo dal seed). StopTileEntries ignora Amount,
    ///    resta un'entry = un'istanza.
    /// 7. Calcoli fissi (ApplyElementStats): ogni tessera content ha Type e DifficultyLevel
    ///    gia' risolti, e da questi soli deriva HexTileData.FoodRestore — fisso =
    ///    DifficultyLevel per i tipi che concedono Cibo (vedi FoodGrantingTypes), 0 per
    ///    tutti gli altri. HpRestore e MoneteGained restano sempre 0 qui: Trap/Fountain/
    ///    Key/MoneyBag/Enemy calcolano i propri effetti direttamente da DifficultyLevel a
    ///    runtime in HexGridController, non da questi campi. Nessun catalogo da
    ///    interrogare, nessuna specie da scegliere a caso: la relazione TileType-
    ///    comportamento e' 1:1 stabile (vedi HexTileConfig.Reveal), quindi non serve piu'
    ///    risolvere nulla durante la generazione.
    /// 8. Rivelazione iniziale (Start ed End già Scoperte)
    /// 9. Vincolo adiacenza Strada: ogni tile Strada deve confinare con almeno un'altra
    ///    Strada (mai isolata), e puo' confinarne due o piu' ma MAI su due lati
    ///    consecutivi dell'esagono (produrrebbe un tratto largo 2 celle). Il vincolo "MAI
    ///    due lati consecutivi" e' applicato in modo costruttivo durante la crescita —
    ///    vedi WouldViolateConsecutiveSides, controllato in GrowBranch (ogni tessera,
    ///    incluse le origini dei rami) e in PatchResidualGaps (rammendo) — non come scarto
    ///    post-hoc su una griglia gia' generata. Fork a 3 vie restano sempre validi per
    ///    costruzione geometrica; fork a 5 vie non possono mai esserlo (5 vicini su 6 lati
    ///    garantiscono coppie consecutive) e degradano organicamente invece di creare un
    ///    incrocio vietato. Il vincolo "almeno 1 vicino Strada" e' garantito per
    ///    costruzione durante la crescita (ogni tessera nasce adiacente alla precedente)
    ///    ma va verificato esplicitamente in PatchResidualGaps: una tessera di rammendo si
    ///    aggancia a un PathCluster tramite un vicino con proprietario noto (tileOwner),
    ///    che pero' potrebbe essere una tessera dello stesso cluster gia' demotata a Stop
    ///    (da stopOverride o da un patch precedente) — senza un controllo esplicito
    ///    (RoadNeighborDirections) diventerebbe Strada isolata pur avendo un "proprietario"
    ///    valido. CollectClusterBorders semina anche i vicini di Start (oltre a quelli
    ///    degli EventCluster) per ridurre il rischio di una Start isolata.
    ///    ValidateRoadAdjacencyRule fa da rete di sicurezza a fine Generate: solo log,
    ///    nessuna mutazione.
    /// 10. Vincolo numero minimo di PathCluster (MinPathClusters = 3): a differenza di
    ///     tutti gli altri vincoli di questo generatore, questo NON degrada
    ///     silenziosamente — sotto soglia la mappa non e' considerata giocabile.
    ///     Generate conta i PathCluster con almeno una tessera Strada (CountRealPathClusters,
    ///     un PathCluster demota interamente a Stop per separazione da un altro non conta),
    ///     e se sotto MinPathClusters marca MapGenerationResult.Success = false con un
    ///     Debug.LogError. Il chiamante (HexGridController.BuildGrid) e' responsabile di
    ///     NON avviare il livello quando Success e' false — Generate stessa non puo'
    ///     "fermare" nulla, produce solo il segnale.
    /// 11. MinBranchLength vale sul risultato finale, non solo sulla crescita grezza, e per
    ///     FRAMMENTO CONNESSO, non sul totale grezzo del cluster: un cluster puo' nascere
    ///     con abbastanza tile ma perderne alcune per stopOverride (punto 3, separazione da
    ///     un cluster diverso) — e stopOverride puo' colpire una tessera IN MEZZO a una
    ///     catena o in un nodo di fork, non solo alle estremita', spezzando il cluster in
    ///     piu' isole disconnesse la cui SOMMA supera MinBranchLength pur essendo,
    ///     singolarmente, troppo corte. GeneratePathClusterMesh decompone quindi le tessere
    ///     sopravvissute in componenti connesse: le componenti sotto soglia vengono
    ///     aggiunte a stopOverride (demotate a Stop, _rejectedComponentsTooShort), e solo
    ///     se NESSUNA componente raggiunge MinBranchLength l'intero cluster viene scartato
    ///     SENZA commit (_rejectedByPostSeparationTooShort). Le tile di un cluster scartato
    ///     restano libere per un altro origine o per il rammendo. Non tocca
    ///     PatchResidualGaps: le sue patch estendono un cluster GIA' validato al punto 11,
    ///     non crescono un ramo nuovo con un proprio requisito di lunghezza minima — devono
    ///     pero' rispettare "almeno 1 vicino Strada" (punto 9, _patchDemotedNoRoadNeighbor).
    /// 12. Seme Road per cluster: ogni EventCluster generato procedurale ha esattamente una
    ///     posizione del ring forzata a Road (vedi TryBuildProceduralCluster), da cui la
    ///     mesh PathCluster deve nascere una strada vera con le stesse identiche regole di
    ///     qualunque altro PathCluster (budget, MinBranchLength, vincolo lati non
    ///     consecutivi — nessuna eccezione o scorciatoia). "Deve nascere" e' un tentativo
    ///     con PRIORITA' assoluta, non una garanzia matematica: CollectClusterBorders
    ///     elabora i vicini dei semi Road PRIMA di ogni altro bordo (propria coda
    ///     mescolata, concatenata davanti al resto), ma la crescita puo' comunque fallire
    ///     per motivi geometrici (seme incastrato contro il bordo griglia o un altro
    ///     cluster) — stesso degrado silenzioso del resto del generatore, la tile Road
    ///     resta comunque parte del suo EventCluster anche se nessun PathCluster nasce da
    ///     li'. La generazione dei cluster (punto 2) e' sempre completa PRIMA che la mesh
    ///     PathCluster (punto 3) inizi: i semi Road sono raccolti durante PlaceEventClusters
    ///     e passati a GeneratePathClusterMesh solo alla fine di quella fase, mai durante.
    /// </summary>
    public sealed class MapClusterGenerator : IMapGenerator
    {
        /// <summary>DifficultyLevel della tile End/obiettivo (placeholder "boss finale", 6) — se in futuro esistono piu' "boss" a livelli diversi, questo andra' reso configurabile invece che hardcoded.</summary>
        private const int ObjectiveDifficultyLevel = 6;

        /// <summary>
        /// Numero minimo di PathCluster "reali" (con almeno una tessera Strada, vedi
        /// CountRealPathClusters) perche' una mappa sia considerata giocabile. A
        /// differenza degli altri vincoli del generatore, questo non degrada
        /// silenziosamente: sotto soglia Generate marca il risultato Success=false — vedi
        /// HexGridController.BuildGrid, che non avvia il livello.
        /// </summary>
        private const int MinPathClusters = 3;

        private readonly DistanceWeight[] _endDistanceWeights;
        private readonly int _startMinBorderDistance;
        private readonly int _clusterMinDistanceFromStartEnd;
        private readonly StradaNetworkSettings _strada;
        private readonly EventClusterPlacementSettings _eventClusters;
        private readonly LevelConfig _levelConfig;

        // Manifest consumabili della run corrente, costruiti in Generate() e pescati da
        // TryDrawEntry. Campi di istanza invece di parametri passati a catena attraverso
        // mezza dozzina di metodi privati: sicuro perche' il generatore e' istanziato una
        // volta per singola chiamata a Generate (vedi MapGenerationService), mai riusato
        // ne' chiamato in parallelo su piu' thread.
        private List<SingleTile> _singleManifest;
        private List<StopTile> _stopManifest;

        /// <summary>
        /// Combinazioni (Type, DifficultyLevel) del centro gia' usate come cluster in
        /// questa run: un cluster e' definito dal suo centro — Enemy DL4 ed Enemy DL5 sono
        /// due "tipi di cluster" distinti — e ogni tipo puo' comparire al massimo una
        /// volta per mappa. Popolata in PlaceEventClusters SOLO
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
        /// ha gia' loggato un warning di composizione insufficiente in questa run (es. un
        /// centro Enemy richiede 4 tipi non-Trap distinti a DL1/2: se LevelConfig ne offre
        /// meno, quel centro fallisce SEMPRE). Un centro non ancora usato (vedi
        /// _usedClusterCenterTypes) puo' essere ripescato molte volte in una singola
        /// Generate finche' non ha successo o viene esaurito il pool — senza questa dedup
        /// un centro strutturalmente impossibile stamperebbe lo stesso warning decine di
        /// volte per run.
        /// </summary>
        private HashSet<(TileType Type, int DifficultyLevel)> _warnedInsufficientClusterComposition;

        // Contatori diagnostici per il log di fallimento MinPathClusters. Solo per il
        // report — nessuna logica dipende da questi valori. Azzerati a inizio Generate,
        // incrementati a ogni punto in cui la
        // crescita di un PathCluster viene effettivamente rifiutata, cosi' che il report
        // finale possa dire QUALE vincolo ha bloccato piu' crescita, non solo il risultato
        // finale. Vedi BuildInsufficientPathClustersReport per come vengono letti.
        private int _rejectedByBlocked;
        private int _rejectedByConsecutiveSides;
        private int _rejectedByShortBranch;
        private int _rejectedByBudget;
        private int _patchDemotedByOtherCluster;

        /// <summary>
        /// Cluster scartati INTERAMENTE dopo la crescita perche', anche decomponendo le
        /// tessere sopravvissute alla separazione (stopOverride, vedi
        /// GeneratePathClusterMesh) in frammenti connessi, NESSUN frammento raggiunge
        /// MinBranchLength — il cluster non produce nessun percorso valido. Distinto da
        /// _rejectedByShortBranch, che conta i rami mai arrivati a MinBranchLength durante
        /// la crescita stessa — qui invece il cluster ERA abbastanza lungo, e' la
        /// separazione a rovinarlo dopo. Distinto da _rejectedComponentsTooShort, che
        /// conta i singoli frammenti troppo corti scartati DENTRO un cluster che nel
        /// complesso viene comunque committato (perche' ha almeno un altro frammento
        /// valido).
        /// </summary>
        private int _rejectedByPostSeparationTooShort;

        /// <summary>
        /// Frammenti connessi troppo corti (&lt; MinBranchLength) demotati a Stop dentro un
        /// PathCluster che nel complesso viene comunque committato — vedi
        /// GeneratePathClusterMesh. La separazione da un cluster diverso (stopOverride)
        /// puo' colpire una tessera IN MEZZO a una catena, non solo alle estremita':
        /// contare solo il totale di tessere sopravvissute (come prima) non basta, perche'
        /// due o piu' frammenti disconnessi possono sommare abbastanza tessere da superare
        /// MinBranchLength pur essendo, singolarmente, isole da 1-2 Strade isolate — la
        /// stessa situazione che _rejectedByPostSeparationTooShort previene a livello di
        /// cluster intero, qui applicata a livello di singolo frammento.
        /// </summary>
        private int _rejectedComponentsTooShort;

        /// <summary>
        /// Tessere di rammendo (PatchResidualGaps) che avrebbero esteso un PathCluster
        /// esistente ma il vicino usato per l'aggancio (extendOwner) non era esso stesso
        /// Strada — era una tessera dello stesso cluster gia' demotata a Stop (da
        /// stopOverride o da un patch precedente). Demotate a Stop anche loro invece di
        /// diventare Strada isolata (zero vicini Strada reali) — vedi la regola
        /// "ogni Strada confina con almeno un'altra Strada" (punto 9 della doc di classe).
        /// </summary>
        private int _patchDemotedNoRoadNeighbor;

        public MapClusterGenerator(
            DistanceWeight[] endDistanceWeights,
            int startMinBorderDistance,
            int clusterMinDistanceFromStartEnd,
            StradaNetworkSettings strada,
            EventClusterPlacementSettings eventClusters,
            LevelConfig levelConfig)
        {
            _endDistanceWeights = endDistanceWeights;
            _startMinBorderDistance = startMinBorderDistance;
            _clusterMinDistanceFromStartEnd = clusterMinDistanceFromStartEnd;
            _strada = strada;
            _eventClusters = eventClusters;
            _levelConfig = levelConfig;
        }

        public MapGenerationResult Generate(int width, int height, int seed)
        {
            _rejectedByBlocked = 0;
            _rejectedByConsecutiveSides = 0;
            _rejectedByShortBranch = 0;
            _rejectedByBudget = 0;
            _patchDemotedByOtherCluster = 0;
            _rejectedByPostSeparationTooShort = 0;
            _rejectedComponentsTooShort = 0;
            _patchDemotedNoRoadNeighbor = 0;
            _usedClusterCenterTypes = new HashSet<(TileType, int)>();
            _warnedInsufficientClusterComposition = new HashSet<(TileType, int)>();

            UnityEngine.Debug.Log($"[MapCluster] Generate avviata: seed={seed} griglia={width}x{height}");

            var rng = new Random(seed);
            var tiles = new Dictionary<HexCoord, HexTileData>(width * height);

            for (int row = 0; row < height; row++)
            for (int col = 0; col < width; col++)
            {
                var coord = HexCoord.FromOffsetOddQ(col, row);
                tiles[coord] = new HexTileData(coord);
            }

            // Manifest per-run: uno per LevelConfig.SingleTileEntries e uno per
            // StopTileEntries, mescolati una volta sola qui, poi consumati (mai reinseriti)
            // da AssignSingleTile/AssignStopTile per tutta la Generate. expandByAmount=true
            // SOLO per SingleTileEntries (vedi LevelTile.Amount su SingleTile): StopTyle
            // forza Amount a 1 nel costruttore e resta comunque un'entry = un'istanza.
            _singleManifest = BuildManifest(_levelConfig?.SingleTileEntries, rng, expandByAmount: true);
            _stopManifest   = BuildManifest(_levelConfig?.StopTileEntries, rng, expandByAmount: false);

            UnityEngine.Debug.Log($"[MapCluster] Manifest costruiti: SingleTileEntries={_singleManifest.Count} tessere (da {_levelConfig?.SingleTileEntries?.Count ?? 0} entry autorate), StopTileEntries={_stopManifest.Count} tessere");

            var startCoord = PlaceStart(tiles, width, height, rng);
            var endCoord = PlaceEnd(tiles, startCoord, rng);

            ResetTile(tiles[startCoord], TileType.Road, hpRestore: 0, moneteGained: 0);
            tiles[startCoord].IsObjective = false;

            UnityEngine.Debug.Log($"[MapCluster] Start piazzato a {startCoord}, End piazzato a {endCoord} (distanza={startCoord.DistanceTo(endCoord)})");

            // Obiettivo: sempre Enemy con IsObjective = true (Boss/Miniboss non sono
            // TileType a se', sono Enemy con DifficultyLevel piu' alto). DifficultyLevel 6
            // come da placeholder "Dragon" — passa comunque da ResolveDifficulty per
            // coerenza con tutte le altre tessere content, nel caso la posizione End non
            // abbia 6 vicini validi in griglia.
            int objectiveLevel = ResolveDifficulty(ObjectiveDifficultyLevel, endCoord, tiles);
            ApplyElementStats(tiles[endCoord], TileType.Enemy, objectiveLevel);
            tiles[endCoord].DifficultyLevel = objectiveLevel;
            tiles[endCoord].IsObjective = true;

            UnityEngine.Debug.Log($"[MapCluster] End risolto come Enemy obiettivo DL={objectiveLevel} (richiesto {ObjectiveDifficultyLevel})");

            var eventOccupied = PlaceEventClusters(tiles, width, height, startCoord, endCoord, rng, out int nextEventId, out var clusterRoadSeeds);

            UnityEngine.Debug.Log($"[MapCluster] Fase EventCluster/EventSingle completata: {_usedClusterCenterTypes.Count} cluster piazzati ({clusterRoadSeeds.Count} semi Road), {eventOccupied.Count} tessere totali occupate");

            GeneratePathClusterMesh(tiles, startCoord, endCoord, eventOccupied, clusterRoadSeeds, rng, out var globalClaimed, out var tileOwner, out int originsCount, out int attemptedClusters);

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
                UnityEngine.Debug.LogError($"[MapCluster] {result.FailureReason}");
            }
            else
            {
                UnityEngine.Debug.Log($"[MapCluster] Generate completata con successo: {pathClusterCount} PathCluster reali (minimo richiesto {MinPathClusters})");
            }

            return result;
        }

        /// <summary>
        /// Report diagnostico per il fallimento MinPathClusters: i contatori effettivi di
        /// QUANTE volte ciascun vincolo ha bloccato una crescita, misurati durante questa
        /// stessa Generate (vedi i campi _rejectedBy*/_patchDemotedByOtherCluster). Con i
        /// numeri in mano non serve indovinare:
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
        /// - _rejectedByPostSeparationTooShort alto → i cluster nascono abbastanza
        ///   lunghi ma la separazione da cluster vicini (stopOverride) li erode sotto
        ///   MinBranchLength cosi' spesso da scartarli per intero — stessa causa di
        ///   _patchDemotedByOtherCluster ma sui rami appena cresciuti invece che sul
        ///   rammendo: troppa densita' di PathCluster ravvicinati.
        /// - _rejectedComponentsTooShort alto → la separazione spezza spesso un cluster in
        ///   piu' frammenti disconnessi invece di erodere l'intero cluster: il cluster nel
        ///   complesso sopravvive (ha almeno un frammento valido) ma parte delle sue tile
        ///   viene comunque persa a Stop — stessa densita' eccessiva di _patchDemotedByOtherCluster/
        ///   _rejectedByPostSeparationTooShort, misurata a grana piu' fine.
        /// - _patchDemotedNoRoadNeighbor alto → il rammendo agganciava spesso tessere il cui
        ///   unico vicino con proprietario era gia' uno Stop dello stesso cluster (non una
        ///   Strada vera): sintomo della stessa densita' eccessiva di PathCluster vicini.
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
                $"    Cluster cresciuto abbastanza lungo ma scartato per intero dopo la separazione (nessun frammento >= MinBranchLength): {_rejectedByPostSeparationTooShort}\n" +
                $"    Frammenti disconnessi troppo corti demotati a Stop dentro un cluster comunque committato: {_rejectedComponentsTooShort}\n" +
                $"    Rammendo (PatchResidualGaps) demotato a Stop per confinare con un PathCluster diverso: {_patchDemotedByOtherCluster}\n" +
                $"    Rammendo (PatchResidualGaps) demotato a Stop perche' il vicino agganciato non era Strada vera: {_patchDemotedNoRoadNeighbor}\n" +
                $"  StradaNetwork: MinBranchLength={_strada.MinBranchLength} MaxBranchLength={_strada.MaxBranchLength} MaxBranches={_strada.MaxBranches} MaxTotalTiles={_strada.MaxTotalTiles}  Pesi Stop/Fork3/Fork5={_strada.StopWeight}/{_strada.Fork3Weight}/{_strada.Fork5Weight}\n" +
                $"  EventClusters: MaxConsecutiveFailures={_eventClusters.MaxConsecutiveFailures}";
        }

        /// <summary>
        /// Numero di PathCluster distinti che hanno prodotto almeno una tessera Strada nel
        /// risultato finale. Un PathCluster puo' crescere (GrowOnePathCluster ritorna
        /// tessere) e finire comunque con zero Strada se ogni sua tessera viene demota a
        /// Stop/Void per separazione da un cluster diverso (vedi
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
        /// Copia source (gia' la lista giusta — SingleTileEntries o StopTileEntries, nessun
        /// filtro da applicare) in una List mescolata con Fisher-Yates usando lo stesso rng
        /// della run (mai un rng separato: il seed resta l'unica fonte di casualita', e la
        /// stessa sequenza di draw deve ripetersi identica a parita' di seed). Lista vuota
        /// (mai null) se source e' null/vuoto — TryDrawEntry gestisce il caso senza bisogno
        /// di controlli aggiuntivi nei chiamanti.
        ///
        /// expandByAmount (vedi LevelTile.Amount): se true, ogni entry viene ripetuta
        /// Amount volte PRIMA dello shuffle — un'entry con Amount=3 diventa 3 copie
        /// indipendenti nel manifest, ciascuna poi piazzata al massimo una volta come le
        /// altre (TryDrawEntry non le distingue). L'espansione avviene prima dello shuffle
        /// apposta: la sequenza di pesca resta comunque interamente derivata dal seed,
        /// nessuna sorgente di casualita' aggiuntiva. Amount <= 0 conta come 1 (mai zero
        /// copie: coerente con [Min(1)] sul campo, ma qui per sicurezza anche se il valore
        /// serializzato fosse "sporco"). Passare false (StopTileEntries, dove StopTyle forza
        /// gia' Amount a 1 nel costruttore) ignora comunque Amount, un'entry resta una
        /// singola istanza — vedi Generate.
        /// </summary>
        private List<T> BuildManifest<T>(List<T> source, Random rng, bool expandByAmount) where T : LevelTile
        {
            var manifest = new List<T>();
            if (source != null)
            {
                foreach (var entry in source)
                {
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
        private bool TryDrawEntry<T>(List<T> manifest, out T entry) where T : LevelTile
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
        /// TileType che restituiscono Cibo al reveal. Valore fisso = DifficultyLevel (vedi
        /// ApplyElementStats) — non piu' un numero autorato per specie, "calcoli fissi" al
        /// posto del bilanciamento manuale.
        /// </summary>
        private static readonly HashSet<TileType> FoodGrantingTypes = new HashSet<TileType>
        {
            TileType.Bush, TileType.BeeHive, TileType.TurnipSprout, TileType.Tree,
        };

        /// <summary>
        /// Imposta Type e i valori derivati sulla tile con calcoli fissi basati su
        /// DifficultyLevel — niente specie multiple da scegliere a runtime ne' numeri
        /// autorati da copiare: la relazione TileType-comportamento e' 1:1 stabile (vedi
        /// HexTileConfig.Reveal). HpRestore e MoneteGained restano sempre 0 qui: Trap/
        /// Fountain/Key/MoneyBag/Enemy calcolano i propri effetti direttamente da
        /// DifficultyLevel a runtime in HexGridController (ApplyImmediateElement/
        /// ApplyEnemyDamage/ResolveEncounterFight), non da questi campi. FoodRestore e'
        /// l'unico valore ancora calcolato qui, fisso = DifficultyLevel per i tipi che
        /// concedono Cibo (vedi FoodGrantingTypes).
        /// </summary>
        private void ApplyElementStats(HexTileData tile, TileType type, int difficultyLevel)
        {
            tile.Type = type;
            tile.HpRestore = 0;
            tile.MoneteGained = 0;
            tile.FoodRestore = FoodGrantingTypes.Contains(type) ? difficultyLevel : 0;
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
        /// PathCluster adiacente) pescando dal manifest SingleTileEntries. Se il
        /// manifest e' esaurito (o il LevelConfig manca), ripiega su TileType.Void
        /// (DifficultyLevel 0) — stesso fallback di AssignStopTile, e stesso default del
        /// costruttore di HexTileData (vedi HexTileData.Type): senza questo ripiego
        /// esplicito una griglia piu' grande del contenuto disponibile produrrebbe
        /// tessere lasciate a un default ambiguo invece di Void separate. Degrado
        /// silenzioso, coerente con "LevelConfig puo' restare parzialmente autorato"
        /// descritto sul LevelConfig stesso.
        /// </summary>
        private void AssignSingleTile(HexTileData tile, HexCoord coord, Dictionary<HexCoord, HexTileData> tiles)
        {
            if (TryDrawEntry(_singleManifest, out var entry))
            {
                int level = ResolveDifficulty(entry.DifficultyLevel, coord, tiles);
                ApplyElementStats(tile, entry.Type, level);
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
        /// StopTileEntries (contenuto reale riskinnato, es. separatore visivo tra due
        /// percorsi). Se il manifest e' esaurito o il LevelConfig manca, ripiega su
        /// TileType.Void (vero no-op, DifficultyLevel 0). In entrambi i casi la separazione
        /// funziona identicamente: CascadeStrada si ferma su qualunque tessera non-Road,
        /// a prescindere da cosa faccia quella tessera.
        /// </summary>
        private void AssignStopTile(HexTileData tile, HexCoord coord, Dictionary<HexCoord, HexTileData> tiles)
        {
            if (TryDrawEntry(_stopManifest, out var entry))
            {
                int level = ResolveDifficulty(entry.DifficultyLevel, coord, tiles);
                ApplyElementStats(tile, entry.Type, level);
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
        /// Piazza EventCluster e tessere singole via rejection sampling, in due fasi
        /// separate — non piu' un rapporto casuale che alterna l'uno e l'altro: ogni tipo
        /// di cluster (coppia Type+DifficultyLevel del centro) compare al massimo una
        /// volta per mappa (vedi _usedClusterCenterTypes), e ogni entry EventSingle ha gia'
        /// il proprio Amount esplicito (espanso in copie da BuildManifest) — un rapporto
        /// cluster:singola non ha piu' senso quando entrambi i lati sono gia' quantita'
        /// fisse note in anticipo, non pool da bilanciare a runtime.
        ///
        /// Fase 1: un tentativo di piazzamento per ogni tipo di centro ancora disponibile
        /// (TryBuildProceduralCluster ritorna null quando i tipi sono esauriti o non
        /// costruibili, fermando la fase). Fase 2: un tentativo di piazzamento per ogni
        /// entry rimasta nel manifest SingleTileEntries (si ferma da sola quando il
        /// manifest si esaurisce). Entrambe le fasi condividono lo stesso rejection sampling
        /// (IsValidEventClusterPlacement, MaxConsecutiveFailures) e lo stesso spazio
        /// occupato, cosi' i cluster restano sempre "primi" nell'ordine di piazzamento
        /// (nessun cambiamento di comportamento su quello) ma senza piu' l'alternanza
        /// artificiale imposta dal rapporto. Ritorna l'insieme di tutte le tessere
        /// occupate; nextEventId è l'indice progressivo per i piazzamenti successivi (es.
        /// PatchResidualGaps) per garantire ID unici. clusterRoadSeeds raccoglie la
        /// coordinata della spec Road di ogni cluster piazzato con successo (sempre
        /// esattamente una, vedi TryBuildProceduralCluster) — GeneratePathClusterMesh la
        /// usa per far nascere la mesh PathCluster con priorita' da li'.
        /// </summary>
        private HashSet<HexCoord> PlaceEventClusters(
            Dictionary<HexCoord, HexTileData> tiles, int width, int height,
            HexCoord start, HexCoord end, Random rng, out int nextEventId, out HashSet<HexCoord> clusterRoadSeeds)
        {
            var occupied = new HashSet<HexCoord>();
            clusterRoadSeeds = new HashSet<HexCoord>();
            int eventPlacementIndex = 0;

            // Fase 1: un piazzamento per ogni tipo di centro disponibile.
            int consecutiveFailures = 0;
            while (consecutiveFailures < _eventClusters.MaxConsecutiveFailures)
            {
                var clusterSpecs = TryBuildProceduralCluster(rng);
                if (clusterSpecs == null)
                {
                    UnityEngine.Debug.Log($"[MapCluster] Fase 1 EventCluster conclusa: nessun tipo di centro ancora disponibile/costruibile ({_usedClusterCenterTypes.Count} cluster piazzati finora)");
                    break; // nessun tipo di centro ancora disponibile/costruibile
                }

                var origin = HexCoord.FromOffsetOddQ(rng.Next(width), rng.Next(height));
                var footprint = ComputeFootprint(origin, clusterSpecs);

                if (!IsValidEventClusterPlacement(footprint, tiles, start, end, occupied))
                {
                    consecutiveFailures++;
                    continue;
                }

                ApplyEventClusterShape(tiles, origin, clusterSpecs);

                // Marca il tipo di centro come usato SOLO ora che il piazzamento e'
                // confermato valido (vedi doc su _usedClusterCenterTypes): clusterSpecs[0]
                // e' sempre il centro (RelativeQ=0, RelativeR=0, vedi TryBuildProceduralCluster).
                _usedClusterCenterTypes.Add((clusterSpecs[0].Type, clusterSpecs[0].DifficultyLevel));

                // Seme Road del cluster (vedi TryBuildProceduralCluster): esattamente una
                // spec del ring ha Type=Road, la sua coordinata assoluta diventa un'origine
                // prioritaria per la mesh PathCluster (vedi GeneratePathClusterMesh/
                // CollectClusterBorders).
                for (int i = 1; i < clusterSpecs.Length; i++)
                {
                    if (clusterSpecs[i].Type != TileType.Road) continue;
                    clusterRoadSeeds.Add(origin + new HexCoord(clusterSpecs[i].RelativeQ, clusterSpecs[i].RelativeR));
                    break;
                }

                UnityEngine.Debug.Log($"[MapCluster] EventCluster piazzato: centro={clusterSpecs[0].Type} DL={clusterSpecs[0].DifficultyLevel} origine={origin} (7 tessere, id={eventPlacementIndex})");

                foreach (var coord in footprint)
                {
                    occupied.Add(coord);
                    tiles[coord].EventPlacementId = eventPlacementIndex;
                }
                eventPlacementIndex++;
                consecutiveFailures = 0;
            }

            if (consecutiveFailures >= _eventClusters.MaxConsecutiveFailures)
                UnityEngine.Debug.Log($"[MapCluster] Fase 1 EventCluster interrotta: MaxConsecutiveFailures ({_eventClusters.MaxConsecutiveFailures}) raggiunto con ancora tipi di centro disponibili — griglia troppo occupata per piazzarli");

            // Fase 2: un piazzamento per ogni entry rimasta nel manifest SingleTileEntries.
            int singlesPlaced = 0;
            consecutiveFailures = 0;
            while (consecutiveFailures < _eventClusters.MaxConsecutiveFailures && _singleManifest.Count > 0)
            {
                var origin = HexCoord.FromOffsetOddQ(rng.Next(width), rng.Next(height));
                var footprint = new List<HexCoord> { origin };

                if (!IsValidEventClusterPlacement(footprint, tiles, start, end, occupied))
                {
                    consecutiveFailures++;
                    continue;
                }

                AssignSingleTile(tiles[origin], origin, tiles);
                singlesPlaced++;

                occupied.Add(origin);
                tiles[origin].EventPlacementId = eventPlacementIndex;
                eventPlacementIndex++;
                consecutiveFailures = 0;
            }

            UnityEngine.Debug.Log($"[MapCluster] Fase 2 EventSingle conclusa: {singlesPlaced} tessere singole piazzate, {_singleManifest.Count} rimaste nel manifest (griglia piena o manifest esaurito)");

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
        /// richiesto — TRANNE per la spec Road (sempre esattamente una, vedi
        /// TryBuildProceduralCluster): ResolveDifficulty clampa un minimo di 1 per
        /// qualunque content reale, sbagliato per una tile strutturale che non ne ha
        /// bisogno (vedi HexTileData.DifficultyLevel — Road resta sempre 0).
        /// </summary>
        private void ApplyEventClusterShape(Dictionary<HexCoord, HexTileData> tiles, HexCoord origin, EventClusterTileSpec[] specs)
        {
            foreach (var tileSpec in specs)
            {
                var coord = origin + new HexCoord(tileSpec.RelativeQ, tileSpec.RelativeR);

                if (tileSpec.Type == TileType.Road)
                {
                    ApplyElementStats(tiles[coord], TileType.Road, 0);
                    tiles[coord].DifficultyLevel = 0;
                    continue;
                }

                int level = ResolveDifficulty(tileSpec.DifficultyLevel, coord, tiles);
                ApplyElementStats(tiles[coord], tileSpec.Type, level);
                tiles[coord].DifficultyLevel = level;
            }
        }

        /// <summary>
        /// Genera proceduralmente le spec di un EventCluster da 7 tessere (centro + 6
        /// adiacenti) usando LevelConfig.ClusterMainTileEntries come palette del centro e
        /// LevelConfig.ClusterFillerTileEntries come palette del ring.
        ///
        /// Regole di composizione:
        /// - Centro: entry da ClusterMainTileEntries, scelta casualmente TRA quelle il cui
        ///   (Type, DifficultyLevel) non e' ancora in _usedClusterCenterTypes (un solo
        ///   cluster per tipo di centro sull'intera mappa — Enemy DL4 ed Enemy DL5 sono due
        ///   tipi distinti). La combinazione scelta qui NON viene marcata come usata subito:
        ///   lo fa PlaceEventClusters, e solo dopo che il piazzamento e' stato validato —
        ///   vedi doc su _usedClusterCenterTypes sul perche' non si fa qui.
        /// - Esattamente 1 posizione del ring e' sempre Road: seme obbligatorio da cui la
        ///   mesh PathCluster fa nascere una strada vera, con priorita' su ogni altro
        ///   bordo (vedi PlaceEventClusters/CollectClusterBorders/GeneratePathClusterMesh).
        ///   DifficultyLevel forzato a 0 (Road e' strutturale, vedi ApplyEventClusterShape),
        ///   l'entry autorata conta solo per la scelta del Type.
        /// - Se il centro è Enemy: 2 Trap nel ring + 3 posizioni con tipi unici non-Trap/non-Road.
        /// - Altrimenti: 3 Trap nel ring + 2 posizioni con tipi unici non-Trap/non-Road.
        /// - Le Trap sono scelte casualmente tra le entry Trap DL 1/2 di ClusterFillerTileEntries.
        /// - Le posizioni non-Trap/non-Road hanno ognuna un tipo diverso; per ogni tipo
        ///   viene scelta casualmente una entry tra quelle DL 1/2 disponibili.
        /// - Le posizioni nel ring vengono mescolate (Fisher-Yates).
        ///
        /// Ritorna null se LevelConfig manca, ClusterMainTileEntries/ClusterFillerTileEntries
        /// non hanno entry sufficienti (Road inclusa), o tutti i tipi di centro disponibili
        /// sono gia' stati usati — il chiamante (PlaceEventClusters) degrada a tessera singola.
        /// </summary>
        private EventClusterTileSpec[] TryBuildProceduralCluster(Random rng)
        {
            if (_levelConfig.ClusterMainTileEntries == null || _levelConfig.ClusterMainTileEntries.Count == 0)
            {
                UnityEngine.Debug.Log("[MapCluster] TryBuildProceduralCluster: ClusterMainTileEntries vuoto/assente, nessun cluster costruibile");
                return null;
            }
            if (_levelConfig.ClusterFillerTileEntries == null || _levelConfig.ClusterFillerTileEntries.Count < 4)
            {
                UnityEngine.Debug.Log($"[MapCluster] TryBuildProceduralCluster: ClusterFillerTileEntries insufficiente ({_levelConfig.ClusterFillerTileEntries?.Count ?? 0} entry, minimo 4), nessun cluster costruibile");
                return null;
            }

            // Centro: escluse le combinazioni (Type, DL) gia' usate.
            var centerCandidates = new List<ClusterTile>();
            foreach (var e in _levelConfig.ClusterMainTileEntries)
                if (!_usedClusterCenterTypes.Contains((e.Type, e.DifficultyLevel)))
                    centerCandidates.Add(e);
            if (centerCandidates.Count == 0)
            {
                UnityEngine.Debug.Log("[MapCluster] TryBuildProceduralCluster: tutti i tipi di centro ClusterMainTileEntries gia' usati in questa run");
                return null;
            }

            var center = centerCandidates[rng.Next(centerCandidates.Count)];
            int trapCount  = center.Type == TileType.Enemy ? 2 : 3;
            int otherCount = 6 - trapCount;
            int nonRoadOtherCount = otherCount - 1; // una posizione e' sempre riservata a Road

            UnityEngine.Debug.Log($"[MapCluster] Centro candidato: {center.Type} DL{center.DifficultyLevel} (trapCount={trapCount}, otherCount={otherCount} inclusa 1 Road, {centerCandidates.Count} candidati disponibili)");

            // Road: esattamente una posizione del ring, seme obbligatorio per un
            // PathCluster (vedi doc sopra). Nessun filtro su DifficultyLevel: forzato a 0
            // nell'assemblaggio del ring, l'entry autorata serve solo a validare che il
            // Type Road sia presente in palette.
            var roadCandidates = new List<ClusterTile>();
            foreach (var e in _levelConfig.ClusterFillerTileEntries)
                if (e.Type == TileType.Road)
                    roadCandidates.Add(e);
            if (roadCandidates.Count == 0)
            {
                WarnInsufficientComposition(center, "nessuna entry Road in LevelConfig.ClusterFillerTileEntries — richiesta come seme PathCluster del cluster");
                return null;
            }

            // Trap circostanti: entry Trap a DL 1/2
            var trapCandidates = new List<ClusterTile>();
            foreach (var e in _levelConfig.ClusterFillerTileEntries)
                if (e.Type == TileType.Trap && e.DifficultyLevel >= 1 && e.DifficultyLevel <= 2)
                    trapCandidates.Add(e);
            if (trapCandidates.Count == 0)
            {
                WarnInsufficientComposition(center, "nessuna entry Trap a DL1/2 in LevelConfig.ClusterFillerTileEntries");
                return null;
            }

            // Non-Trap/non-Road circostanti: tipi unici a DL 1/2
            var nonTrapByType = new Dictionary<TileType, List<ClusterTile>>();
            foreach (var e in _levelConfig.ClusterFillerTileEntries)
            {
                if (e.Type == TileType.Trap || e.Type == TileType.Road) continue;
                if (e.DifficultyLevel < 1 || e.DifficultyLevel > 2) continue;
                if (!nonTrapByType.ContainsKey(e.Type))
                    nonTrapByType[e.Type] = new List<ClusterTile>();
                nonTrapByType[e.Type].Add(e);
            }

            var availableTypes = new List<TileType>(nonTrapByType.Keys);
            if (availableTypes.Count < nonRoadOtherCount)
            {
                WarnInsufficientComposition(center,
                    $"servono {nonRoadOtherCount} tipi non-Trap/non-Road distinti a DL1/2 (ClusterFillerTileEntries), disponibili solo {availableTypes.Count}");
                return null;
            }

            // Shuffle completo per selezione casuale senza ripetizione
            for (int i = availableTypes.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (availableTypes[i], availableTypes[j]) = (availableTypes[j], availableTypes[i]);
            }

            // Assembla il ring: Trap + 1 Road + non-Trap/non-Road
            var ring = new List<EventClusterTileSpec>(6);

            for (int i = 0; i < trapCount; i++)
            {
                var e = trapCandidates[rng.Next(trapCandidates.Count)];
                ring.Add(new EventClusterTileSpec { Type = e.Type, DifficultyLevel = e.DifficultyLevel });
            }

            ring.Add(new EventClusterTileSpec { Type = TileType.Road, DifficultyLevel = 0 });

            for (int i = 0; i < nonRoadOtherCount; i++)
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

            UnityEngine.Debug.Log($"[MapCluster] Ring assemblato per centro {center.Type} DL{center.DifficultyLevel}: {trapCount}x Trap + 1x Road + {string.Join(",", availableTypes.GetRange(0, nonRoadOtherCount))}");

            return specs;
        }

        /// <summary>
        /// Warning deduplicato per un centro la cui composizione ring non e'
        /// realizzabile con le entry ClusterFillerTileEntries disponibili — vedi doc su
        /// _warnedInsufficientClusterComposition sul perche' della dedup. Un solo log per
        /// combinazione (Type, DifficultyLevel) per Generate, anche se TryBuildProceduralCluster
        /// la ripesca decine di volte prima di esaurire il pool o abbandonare il tentativo.
        /// </summary>
        private void WarnInsufficientComposition(ClusterTile center, string reason)
        {
            if (!_warnedInsufficientClusterComposition.Add((center.Type, center.DifficultyLevel))) return;

            UnityEngine.Debug.LogWarning(
                $"[MapCluster] Cluster con centro {center.Type} DL{center.DifficultyLevel} mai generabile in questa run: {reason}. " +
                "Aggiungi le entry mancanti a LevelConfig.ClusterFillerTileEntries o questo tipo di cluster degradera' sempre a tessera singola.");
        }

        // ===== Mesh PathCluster =====

        /// <summary>
        /// Punto di partenza: ogni tessera di bordo di ogni EventCluster/singola piazzata
        /// (adiacente a una tessera occupata, non occupata essa stessa, non Start/End),
        /// piu' i vicini dei semi Road di ogni cluster (clusterRoadSeeds — vedi
        /// TryBuildProceduralCluster/PlaceEventClusters), mescolati in ordine casuale
        /// DENTRO ciascun gruppo ma con i semi Road sempre elaborati per primi (vedi
        /// CollectClusterBorders). Da ognuno, se ancora libero al suo turno, cresce un
        /// intero PathCluster a budget — stesse identiche regole (MinBranchLength,
        /// vincolo lati non consecutivi, ecc.) per qualunque origine, semi Road inclusi.
        /// Nessuna soglia di tentativi da tarare: la lista di partenza è finita per
        /// costruzione, quindi il processo termina da sé.
        ///
        /// originsCount/attemptedClusters esistono solo per il messaggio diagnostico di
        /// Generate quando il vincolo MinPathClusters non e' soddisfatto: originsCount =
        /// quanti bordi disponibili aveva la mesh per partire (0 o molto basso →
        /// EventCluster/Start non offrono punti di aggancio, non e' un problema di
        /// budget); attemptedClusters = quanti PathCluster sono stati effettivamente
        /// COMMITTATI (superata sia la crescita sia il controllo MinBranchLength post-
        /// separazione — non basta "ha prodotto almeno una tessera", vedi
        /// _rejectedByPostSeparationTooShort per i tentativi scartati dopo la crescita).
        /// Puo' essere piu' alto del conteggio "reale" post-filtro in
        /// CountRealPathClusters se un cluster committato e' stato comunque demotato
        /// interamente a Stop per separazione — il divario tra i due numeri e' la spia di
        /// quel caso residuo (tutte le sue tile toccavano un cluster diverso, non solo
        /// abbastanza da scendere sotto MinBranchLength).
        /// </summary>
        private void GeneratePathClusterMesh(
            Dictionary<HexCoord, HexTileData> tiles, HexCoord start, HexCoord end,
            HashSet<HexCoord> eventOccupied, HashSet<HexCoord> clusterRoadSeeds, Random rng,
            out HashSet<HexCoord> globalClaimed, out Dictionary<HexCoord, int> tileOwner,
            out int originsCount, out int attemptedClusters)
        {
            var origins = CollectClusterBorders(tiles, eventOccupied, clusterRoadSeeds, start, end, rng);
            originsCount = origins.Count;

            UnityEngine.Debug.Log($"[MapCluster] PathCluster: {originsCount} bordi di partenza raccolti ({clusterRoadSeeds.Count} semi Road prioritari + EventCluster + vicini di Start)");

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
                // entry StopTileEntries (o Void) invece di restare Strada, così
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

                // MinBranchLength vale sul risultato FINALE, non solo sulla crescita
                // grezza — un ramo nato lungo abbastanza ma ridotto sotto soglia dalla
                // separazione qui sopra non deve sopravvivere come frammento isolato da
                // 1-2 Strade. Non basta pero' sommare le tessere sopravvissute: stopOverride
                // puo' colpire una tessera IN MEZZO a una catena (o in un nodo di fork), non
                // solo alle estremita', spezzando il cluster in piu' isole disconnesse la
                // cui SOMMA supera MinBranchLength pur essendo, singolarmente, troppo corte
                // — da qui la decomposizione in componenti connesse: solo le componenti che
                // raggiungono MinBranchLength restano Strada, le altre vengono aggiunte a
                // stopOverride (demotate a Stop insieme al resto). Se nessuna componente
                // qualifica, l'intero cluster viene scartato senza commit: le tile restano
                // libere per un altro origine o per il rammendo (PatchResidualGaps), stesso
                // trattamento di un ramo troppo corto in GrowBranch.
                var survivingSet = new HashSet<HexCoord>();
                foreach (var coord in pathTiles)
                    if (!stopOverride.Contains(coord)) survivingSet.Add(coord);

                var visited = new HashSet<HexCoord>();
                bool anyComponentQualifies = false;
                foreach (var coord in survivingSet)
                {
                    if (visited.Contains(coord)) continue;

                    var component = new List<HexCoord>();
                    var componentQueue = new Queue<HexCoord>();
                    componentQueue.Enqueue(coord);
                    visited.Add(coord);
                    while (componentQueue.Count > 0)
                    {
                        var cur = componentQueue.Dequeue();
                        component.Add(cur);
                        for (int dir = 0; dir < 6; dir++)
                        {
                            var nb = cur.GetNeighbor(dir);
                            if (survivingSet.Contains(nb) && visited.Add(nb))
                                componentQueue.Enqueue(nb);
                        }
                    }

                    if (component.Count < _strada.MinBranchLength)
                    {
                        _rejectedComponentsTooShort++;
                        foreach (var c in component) stopOverride.Add(c);
                    }
                    else
                    {
                        anyComponentQualifies = true;
                    }
                }

                if (!anyComponentQualifies)
                {
                    _rejectedByPostSeparationTooShort++;
                    UnityEngine.Debug.Log($"[MapCluster] PathCluster scartato: {pathTiles.Count} tessere cresciute da {originTile} ma nessun frammento connesso raggiunge MinBranchLength ({_strada.MinBranchLength}) dopo la separazione");
                    continue;
                }

                foreach (var coord in pathTiles)
                {
                    var tileData = tiles[coord];
                    if (stopOverride.Contains(coord))
                    {
                        AssignStopTile(tileData, coord, tiles);
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

                UnityEngine.Debug.Log($"[MapCluster] PathCluster #{pathClusterIndex} committato: origine={originTile}, {pathTiles.Count - stopOverride.Count} Strada, {stopOverride.Count} Stop (demote per separazione)");

                pathClusterIndex++;
            }

            attemptedClusters = pathClusterIndex;

            UnityEngine.Debug.Log($"[MapCluster] Mesh PathCluster: {attemptedClusters} cluster committati su {originsCount} origini tentate");
        }

        /// <summary>
        /// Ogni tessera ancora priva di contenuto dopo EventCluster e mesh PathCluster
        /// (né Start, né End, né occupata, né già Strada/Stop) viene risolta qui: se
        /// confina con un PathCluster che non ha ancora esaurito la sua quota di 2 tessere
        /// extra, diventa Strada (o una entry StopTileEntries se confina anche con un
        /// PathCluster diverso) e si aggiunge a quel PathCluster; altrimenti diventa una
        /// tessera singola dal manifest SingleTileEntries. Con questa passata nessuna
        /// tessera della griglia resta senza contenuto esplicito.
        /// </summary>
        private void PatchResidualGaps(
            Dictionary<HexCoord, HexTileData> tiles, HexCoord start, HexCoord end,
            HashSet<HexCoord> eventOccupied, HashSet<HexCoord> globalClaimed,
            Dictionary<HexCoord, int> tileOwner, Random rng, int nextEventId)
        {
            const int maxPatchPerPathCluster = 2;
            var patchCountPerPathCluster = new Dictionary<int, int>();

            int roadExtended = 0, stopDemoted = 0, singleFallback = 0;

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
                    // dall'extendOwner, pesca da una entry StopTileEntries per non fare
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

                    // Vincolo lati non consecutivi (vedi WouldViolateConsecutiveSides):
                    // a differenza della crescita PathCluster, qui tutti i cluster sono gia'
                    // scritti su tiles, quindi alsoRoad=null basta.
                    bool wouldViolateSides = WouldViolateConsecutiveSides(coord, tiles, alsoRoad: null);

                    // extendOwner e' scelto per PROPRIETA' (tileOwner), non per tipo: il
                    // vicino che ha giustificato l'estensione potrebbe essere una tessera
                    // dello stesso cluster gia' demotata a Stop (da stopOverride in
                    // GeneratePathClusterMesh, o da un patch precedente in questo stesso
                    // ciclo). Senza questo controllo la tessera diventerebbe Strada isolata
                    // (zero vicini Strada reali), violando la regola "ogni Strada confina
                    // con almeno un'altra Strada" (punto 9 della doc di classe).
                    bool hasRoadNeighbor = false;
                    foreach (var _ in RoadNeighborDirections(coord, tiles, alsoRoad: null)) { hasRoadNeighbor = true; break; }

                    var tileData = tiles[coord];
                    if (touchesOtherCluster || wouldViolateSides || !hasRoadNeighbor)
                    {
                        if (touchesOtherCluster) _patchDemotedByOtherCluster++;
                        if (wouldViolateSides) _rejectedByConsecutiveSides++;
                        if (!hasRoadNeighbor) _patchDemotedNoRoadNeighbor++;
                        AssignStopTile(tileData, coord, tiles);
                        stopDemoted++;
                    }
                    else
                    {
                        ResetTile(tileData, TileType.Road, hpRestore: 0, moneteGained: 0);
                        tileData.DifficultyLevel = 0;
                        roadExtended++;
                    }

                    globalClaimed.Add(coord);
                    tileOwner[coord] = extendOwner.Value;
                    tileData.PathClusterId = extendOwner.Value;
                    patchCountPerPathCluster[extendOwner.Value] =
                        (patchCountPerPathCluster.TryGetValue(extendOwner.Value, out int u2) ? u2 : 0) + 1;
                }
                else
                {
                    AssignSingleTile(tiles[coord], coord, tiles);
                    tiles[coord].EventPlacementId = nextEventId++;
                    eventOccupied.Add(coord);
                    singleFallback++;
                }
            }

            UnityEngine.Debug.Log($"[MapCluster] Rammendo (PatchResidualGaps): {roadExtended} tessere estese a Strada, {stopDemoted} demote a Stop, {singleFallback} tessere singole di ripiego (nessun PathCluster adiacente)");
        }

        /// <summary>
        /// Raccoglie i bordi da cui la mesh PathCluster puo' partire, in due gruppi
        /// ordinati per priorita': prima i vicini dei semi Road di ogni cluster
        /// (clusterRoadSeeds — un seme garantito per cluster, vedi
        /// TryBuildProceduralCluster/PlaceEventClusters), poi tutti gli altri bordi
        /// (EventCluster generico + vicini di Start). Un HashSet "seen" condiviso tra i
        /// due gruppi garantisce che ogni coordinata compaia una volta sola nel risultato
        /// finale, senza bisogno di deduplicare a valle: se un vicino di un seme Road
        /// coincide con un bordo generico, resta nel gruppo prioritario e il gruppo
        /// generico lo salta. Ciascun gruppo e' mescolato (Fisher-Yates) SEPARATAMENTE e
        /// solo dopo concatenato — mescolare tutto insieme in un solo passaggio
        /// vanificherebbe la precedenza dei semi Road sul resto.
        /// </summary>
        private List<(HexCoord tile, int direction)> CollectClusterBorders(
            Dictionary<HexCoord, HexTileData> tiles, HashSet<HexCoord> eventOccupied,
            HashSet<HexCoord> clusterRoadSeeds, HexCoord start, HexCoord end, Random rng)
        {
            var seen = new HashSet<HexCoord>();
            var priorityBorders = new List<(HexCoord, int)>();
            var borders = new List<(HexCoord, int)>();

            // Priorita': vicini del seme Road di ogni cluster. Un cluster deve sempre
            // produrre un tentativo di strada propria, prima di ogni altro bordo — vedi
            // doc di classe punto 12.
            foreach (var roadCoord in clusterRoadSeeds)
            {
                for (int dir = 0; dir < 6; dir++)
                {
                    var neighbor = roadCoord.GetNeighbor(dir);
                    if (!tiles.ContainsKey(neighbor)) continue;
                    if (eventOccupied.Contains(neighbor)) continue;
                    if (neighbor.Equals(start) || neighbor.Equals(end)) continue;
                    if (!seen.Add(neighbor)) continue;
                    priorityBorders.Add((neighbor, dir));
                }
            }

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

            // Start e' sempre Strada (vedi Generate) ma, a differenza degli EventCluster,
            // non ha bordi innati da cui far nascere un PathCluster — senza questo seeding
            // potrebbe restare Strada isolata (zero vicini Strada), contro la regola "ogni
            // Strada confina con almeno un'altra Strada". Stesso identico meccanismo di
            // seeding degli EventCluster sopra, non uno nuovo: aumenta la probabilita' che un
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

            // Fisher-Yates separatamente per ciascun gruppo: mescolare tutto insieme in un
            // solo passaggio vanificherebbe la precedenza dei semi Road sul resto.
            for (int i = priorityBorders.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (priorityBorders[i], priorityBorders[j]) = (priorityBorders[j], priorityBorders[i]);
            }
            for (int i = borders.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (borders[i], borders[j]) = (borders[j], borders[i]);
            }

            priorityBorders.AddRange(borders);
            return priorityBorders;
        }

        /// <summary>
        /// Cresce un intero PathCluster (budget StradaNetwork: tessere totali, numero e
        /// lunghezza rami, pesi STOP/fork-3/fork-5 e di deviazione) a partire da una
        /// singola origine. Si blocca su: bordi griglia, Start/End, tessere di un
        /// EventCluster (muro fisso), tessere già usate da un altro PathCluster (muro che
        /// cresce).
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
                    // finalDir, non dir — vedi doc su GrowBranch/finalDir. EnqueueFork deve
                    // biforcare rispetto a come il ramo e' arrivato davvero all'endpoint,
                    // non a come e' partito.
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

        // ===== Vincolo "lati non consecutivi" =====
        //
        // Ogni tile Strada deve confinare con almeno un'altra Strada (mai isolata) e puo'
        // confinare con due o piu' Strada, ma MAI su due lati consecutivi dell'esagono —
        // due vicini su lati consecutivi sono a loro volta vicini tra loro, il che
        // produrrebbe un tratto largo 2 celle invece di 1 (un "blob" invece di un
        // percorso). Non vieta i fork: un fork a 3 vie (ramo in entrata + 2 nuovi rami,
        // vedi EnqueueFork con offset -1/+1) piazza per costruzione i 3 vicini su lati
        // alternati (differenza 2), sempre non consecutivi — MA SOLO SE l'offset e'
        // calcolato sulla direzione vera con cui il ramo arriva al punto di fork: vedi
        // GrowBranch.finalDir, passato a EnqueueFork da GrowOnePathCluster invece della
        // direzione di partenza (stantia dopo le deviazioni di DeviateDirection). Un fork
        // a 5 vie invece NON puo' mai rispettare la regola in ogni caso (5 vicini su 6
        // lati significano che almeno 4 coppie sono consecutive per pigeonhole), quindi
        // con questo vincolo attivo i rami oltre il terzo di un Fork5 vengono naturalmente
        // scartati da WouldViolateConsecutiveSides invece che creare un incrocio a 5 vie:
        // degrado organico, stesso principio "mai crashare, degrada silenziosamente" del
        // resto del generatore (vedi ResolveDifficulty, ApplyElementStats).
        // Fork3Weight/Fork5Weight in MapGenerationConfig non richiedono alcun trattamento
        // speciale: la regola li rende semplicemente auto-limitanti.

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
                    UnityEngine.Debug.LogWarning($"[MapCluster] Regola adiacenza Strada violata: {kvp.Key} e' Strada isolata (zero vicini Strada).");
                    continue;
                }

                for (int i = 0; i < roadDirs.Count; i++)
                for (int j = i + 1; j < roadDirs.Count; j++)
                    if (AreConsecutiveSides(roadDirs[i], roadDirs[j]))
                        UnityEngine.Debug.LogWarning($"[MapCluster] Regola adiacenza Strada violata: {kvp.Key} ha due vicini Strada su lati consecutivi ({roadDirs[i]}/{roadDirs[j]}).");
            }
        }

        /// <summary>
        /// finalDir: la direzione con cui il ramo arriva davvero all'endpoint dopo le
        /// deviazioni di DeviateDirection, distinta dalla direzione di PARTENZA (heading).
        /// Senza restituirla, il chiamante non ha modo di sapere come il ramo ha
        /// effettivamente svoltato — e la dimostrazione che un fork a 3 vie e' sempre
        /// geometricamente valido rispetto al vincolo lati-non-consecutivi vale solo se il
        /// fork usa la direzione VERA di ingresso al punto di biforcazione, non quella di
        /// partenza. Ritorna heading invariata se il ramo e' vuoto/di sola origine (nessun
        /// passo fatto, nessuna deviazione da riportare).
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