using System;
using System.Collections.Generic;
using hp55games.MapGame.Features.Configs;

namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    /// <summary>
    /// Generatore di mappa, pipeline "macchie di leopardo" (GDD, rev. 2026-07-07),
    /// revisione 2026-07-10 per il sistema LevelConfig / DifficultyLevel:
    /// 1. Piazzamento Start/End (weighted distance)
    /// 2. Piazzamento EventCluster (forme dal catalogo, Type+DifficultyLevel gia' autorati
    ///    per cella) e tessere singole (pescate da LevelConfig.EventSingleTilesList) via
    ///    rejection sampling, con almeno una tessera di distacco tra due piazzamenti
    ///    qualunque.
    /// 3. Mesh di PathCluster: parte da ogni bordo di ogni EventCluster/singola piazzata,
    ///    cresce a budget (15 tessere / 5 rami / rami 3-7) per ciascun PathCluster, si
    ///    ferma sui bordi degli EventCluster (fanno da muro, nessuna separazione
    ///    necessaria lì) e pesca da LevelConfig.StopSingleTilesList dove tocca un
    ///    PathCluster diverso già piazzato (per non farli fondere in un'unica cascata).
    /// 4. Rammendo: ogni tessera ancora priva di contenuto diventa Strada extra su un
    ///    PathCluster confinante (massimo 2 per PathCluster, o una tessera di
    ///    StopSingleTilesList se confina anche con un PathCluster diverso) oppure, se non
    ///    c'è un PathCluster a cui attaccarsi, una tessera singola da
    ///    EventSingleTilesList. Nessuna tessera resta senza contenuto esplicito.
    /// 5. Vincolo DifficultyLevel: ogni tessera pescata da una lista LevelConfig porta con
    ///    se' un DifficultyLevel (1-6) dalla entry scelta; se la posizione finale non ha
    ///    abbastanza vicini validi in griglia per quel livello, il livello viene abbassato
    ///    fino al valore supportato (minimo 1), stesso TileType — vedi ResolveDifficulty.
    /// 6. Rivelazione iniziale (Start ed End già Scoperte)
    /// </summary>
    public sealed class AestheticClusterMapGenerator : IMapGenerator
    {
        private readonly DistanceWeight[] _endDistanceWeights;
        private readonly int _startMinBorderDistance;
        private readonly int _clusterMinDistanceFromStartEnd;
        private readonly PlaceholderBalanceSettings _balance;
        private readonly StradaNetworkSettings _strada;
        private readonly EventClusterPlacementSettings _eventClusters;
        private readonly LevelConfig _levelConfig;

        public AestheticClusterMapGenerator(
            DistanceWeight[] endDistanceWeights,
            int startMinBorderDistance,
            int clusterMinDistanceFromStartEnd,
            PlaceholderBalanceSettings balance,
            StradaNetworkSettings strada,
            EventClusterPlacementSettings eventClusters,
            LevelConfig levelConfig)
        {
            _endDistanceWeights = endDistanceWeights;
            _startMinBorderDistance = startMinBorderDistance;
            _clusterMinDistanceFromStartEnd = clusterMinDistanceFromStartEnd;
            _balance = balance;
            _strada = strada;
            _eventClusters = eventClusters;
            _levelConfig = levelConfig;
        }

        public MapGenerationResult Generate(int width, int height, int seed)
        {
            var rng = new Random(seed);
            var tiles = new Dictionary<HexCoord, HexTileData>(width * height);

            for (int row = 0; row < height; row++)
            for (int col = 0; col < width; col++)
            {
                var coord = HexCoord.FromOffsetOddQ(col, row);
                tiles[coord] = new HexTileData(coord);
            }

            var startCoord = PlaceStart(tiles, width, height, rng);
            var endCoord = PlaceEnd(tiles, startCoord, rng);

            ResetTile(tiles[startCoord], TileType.Path, hpRestore: 0, moneteGained: 0);
            tiles[startCoord].IsObjective = false;

            ResetTile(tiles[endCoord], TileType.Boss, hpRestore: 0, moneteGained: 0);
            tiles[endCoord].IsObjective = true;

            var eventOccupied = PlaceEventClusters(tiles, width, height, startCoord, endCoord, rng, out int nextEventId);
            GeneratePathClusterMesh(tiles, startCoord, endCoord, eventOccupied, rng, out var globalClaimed, out var tileOwner);
            PatchResidualGaps(tiles, startCoord, endCoord, eventOccupied, globalClaimed, tileOwner, rng, nextEventId);

            RevealInitialTiles(tiles[startCoord], tiles[endCoord]);

            return new MapGenerationResult
            {
                Tiles = tiles,
                StartCoord = startCoord,
                ObjectiveCoord = endCoord,
                SeedUsed = seed,
            };
        }

        private void ApplyPlaceholderBalance(HexTileData tile, TileType type, Random rng)
        {
            tile.Type = type;
            // Goods non cura più HP direttamente dal 2026-07-10 (introduzione del costo
            // movimento): rifornisce la scorta di Cibo, che il costo movimento consuma,
            // invece di curare sul colpo. Vedi FoodRestore sotto.
            // Enemy (2026-07-17, reintegrazione XP): il danno NON viene piu' bruciato qui.
            // Effetto Enemy = perdita HP pari al DifficultyLevel + guadagno XP pari al
            // DifficultyLevel, applicato al reveal in HexGridController dove il livello e'
            // gia' risolto (ResolveDifficulty gira DOPO questo metodo, qui non e' ancora
            // noto). Quindi HpRestore resta 0 per Enemy come per tutti gli altri content.
            // Chance assorbe il vecchio Trappola (tabella esiti non ancora definita, impatto
            // zero). Shop/Miniboss: bilanciamento non ancora definito.
            tile.HpRestore = 0;
            tile.FoodRestore = type == TileType.Goods
                ? rng.Next(_balance.GoodsFoodRestoreMin, _balance.GoodsFoodRestoreMax + 1)
                : 0;
            // Monete in pausa: l'economia Shop/Monete e' sospesa finche' non viene
            // ridisegnata. Enemy da' XP, non Monete. Nessun tipo assegna Monete per ora.
            tile.MoneteGained = 0;
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
        /// Sceglie una entry a caso da pool. False se pool e' null o vuoto (LevelConfig
        /// non popolato per quella lista) — il chiamante decide il fallback.
        /// </summary>
        private bool TryPickEntry(LevelTileEntry[] pool, Random rng, out LevelTileEntry entry)
        {
            if (pool == null || pool.Length == 0)
            {
                entry = default;
                return false;
            }

            entry = pool[rng.Next(pool.Length)];
            return true;
        }

        /// <summary>
        /// Assegna una tessera singola (EventCluster da 1 tessera, o rammendo senza
        /// PathCluster adiacente) pescando da EventSingleTilesList. Se la lista e' vuota o
        /// il LevelConfig manca, la tile resta al default (Strada) invece di crashare —
        /// degrado silenzioso, coerente con "LevelConfig puo' restare parzialmente
        /// autorato" descritto sul LevelConfig stesso.
        /// </summary>
        private void AssignSingleTile(HexTileData tile, HexCoord coord, Dictionary<HexCoord, HexTileData> tiles, Random rng)
        {
            var pool = _levelConfig != null ? _levelConfig.EventSingleTilesList : null;
            if (TryPickEntry(pool, rng, out var entry))
            {
                ApplyPlaceholderBalance(tile, entry.Type, rng);
                tile.DifficultyLevel = ResolveDifficulty(entry.DifficultyLevel, coord, tiles);
            }
        }

        /// <summary>
        /// Assegna una tessera di separazione PathCluster pescando da
        /// StopSingleTilesList (contenuto reale riskinnato, es. npc vestito da ponte
        /// rotto — vedi GDD "StopSingleTilesList"). Se la lista e' vuota o il LevelConfig
        /// manca, ripiega su TileType.Void (vero no-op, DifficultyLevel 0). In entrambi i
        /// casi la separazione funziona identicamente: CascadeStrada si ferma su
        /// qualunque tessera non-Strada, a prescindere da cosa faccia quella tessera.
        /// </summary>
        private void AssignStopTile(HexTileData tile, HexCoord coord, Dictionary<HexCoord, HexTileData> tiles, Random rng)
        {
            var pool = _levelConfig != null ? _levelConfig.StopSingleTilesList : null;
            if (TryPickEntry(pool, rng, out var entry))
            {
                ApplyPlaceholderBalance(tile, entry.Type, rng);
                tile.DifficultyLevel = ResolveDifficulty(entry.DifficultyLevel, coord, tiles);
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
        /// Piazza EventCluster (forme dal catalogo) e tessere singole via rejection
        /// sampling. Ritorna l'insieme di tutte le tessere occupate, usato dalla mesh
        /// PathCluster come muro invalicabile. nextEventId è l'indice progressivo da usare
        /// per i piazzamenti successivi (es. PatchResidualGaps) per garantire ID unici.
        /// </summary>
        private HashSet<HexCoord> PlaceEventClusters(
            Dictionary<HexCoord, HexTileData> tiles, int width, int height,
            HexCoord start, HexCoord end, Random rng, out int nextEventId)
        {
            var occupied = new HashSet<HexCoord>();
            var catalog = _eventClusters.Catalog;
            bool hasShapes = catalog != null && catalog.Shapes != null && catalog.Shapes.Length > 0;

            int clustersUntilNextSingle = rng.Next(_eventClusters.ClusterToSingleRatioMin, _eventClusters.ClusterToSingleRatioMax + 1);
            int consecutiveFailures = 0;
            int eventPlacementIndex = 0;

            while (consecutiveFailures < _eventClusters.MaxConsecutiveFailures)
            {
                bool wantCluster = hasShapes && clustersUntilNextSingle > 0;

                var origin = HexCoord.FromOffsetOddQ(rng.Next(width), rng.Next(height));
                EventClusterShape shape = wantCluster ? catalog.Shapes[rng.Next(catalog.Shapes.Length)] : null;

                var footprint = wantCluster
                    ? ComputeFootprint(origin, shape)
                    : new List<HexCoord> { origin };

                if (!IsValidEventClusterPlacement(footprint, tiles, start, end, occupied))
                {
                    consecutiveFailures++;
                    continue;
                }

                if (wantCluster)
                {
                    ApplyEventClusterShape(tiles, origin, shape, rng);
                    clustersUntilNextSingle--;
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

        private List<HexCoord> ComputeFootprint(HexCoord origin, EventClusterShape shape)
        {
            var footprint = new List<HexCoord>(shape.Tiles.Length);
            foreach (var tileSpec in shape.Tiles)
            {
                footprint.Add(origin + new HexCoord(tileSpec.RelativeQ, tileSpec.RelativeR));
            }
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
        /// Applica una EventClusterShape autorata a mano: Type e DifficultyLevel sono
        /// gia' fissati per-cella nello Shape Editor (letti da EventClusterTilesList al
        /// momento dell'autoria, non ripescati qui). Il DifficultyLevel autorato viene
        /// comunque passato per ResolveDifficulty, perche' la posizione finale in griglia
        /// e' scelta a runtime (rejection sampling) e potrebbe non avere abbastanza vicini
        /// per il livello autorato.
        /// </summary>
        private void ApplyEventClusterShape(Dictionary<HexCoord, HexTileData> tiles, HexCoord origin, EventClusterShape shape, Random rng)
        {
            foreach (var tileSpec in shape.Tiles)
            {
                var coord = origin + new HexCoord(tileSpec.RelativeQ, tileSpec.RelativeR);
                ApplyPlaceholderBalance(tiles[coord], tileSpec.Type, rng);
                tiles[coord].DifficultyLevel = ResolveDifficulty(tileSpec.DifficultyLevel, coord, tiles);
            }
        }

        // ===== Mesh PathCluster =====

        /// <summary>
        /// Punto di partenza: ogni tessera di bordo di ogni EventCluster/singola piazzata
        /// (adiacente a una tessera occupata, non occupata essa stessa, non Start/End),
        /// mescolate in ordine casuale. Da ognuna, se ancora libera al suo turno, cresce
        /// un intero PathCluster a budget. Nessuna soglia di tentativi da tarare: la lista
        /// di partenza è finita per costruzione, quindi il processo termina da sé.
        /// </summary>
        private void GeneratePathClusterMesh(
            Dictionary<HexCoord, HexTileData> tiles, HexCoord start, HexCoord end,
            HashSet<HexCoord> eventOccupied, Random rng,
            out HashSet<HexCoord> globalClaimed, out Dictionary<HexCoord, int> tileOwner)
        {
            var origins = CollectClusterBorders(tiles, eventOccupied, start, end, rng);

            globalClaimed = new HashSet<HexCoord>();
            tileOwner = new Dictionary<HexCoord, int>();
            int pathClusterIndex = 0;

            foreach (var (originTile, originDir) in origins)
            {
                if (eventOccupied.Contains(originTile) || globalClaimed.Contains(originTile)) continue;

                var pathTiles = GrowOnePathCluster(tiles, originTile, originDir, start, end, eventOccupied, globalClaimed, rng);
                if (pathTiles.Count == 0) continue;

                // Separazione da un PathCluster diverso già piazzato: se una tessera di
                // questo PathCluster confina con una tessera di un altro, pesca da
                // StopSingleTilesList (o Void) invece di restare Strada, così CascadeStrada
                // non li fonde in un'unica cascata.
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
                        ResetTile(tileData, TileType.Path, hpRestore: 0, moneteGained: 0);
                        tileData.DifficultyLevel = 0;
                    }

                    globalClaimed.Add(coord);
                    tileOwner[coord] = pathClusterIndex;
                    tileData.PathClusterId = pathClusterIndex;
                }

                pathClusterIndex++;
            }
        }

        /// <summary>
        /// Ogni tessera ancora priva di contenuto dopo EventCluster e mesh PathCluster
        /// (né Start, né End, né occupata, né già Strada/StopSingleTilesList) viene
        /// risolta qui: se confina con un PathCluster che non ha ancora esaurito la sua
        /// quota di 2 tessere extra, diventa Strada (o una tessera di StopSingleTilesList
        /// se confina anche con un PathCluster diverso) e si aggiunge a quel PathCluster;
        /// altrimenti diventa una tessera singola da EventSingleTilesList. Con questa
        /// passata nessuna tessera della griglia resta senza contenuto esplicito.
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
                    // dall'extendOwner, pesca da StopSingleTilesList per non fare da ponte
                    // tra i due cluster (CascadeStrada li fonderebbe in un'unica cascata).
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

                    var tileData = tiles[coord];
                    if (touchesOtherCluster)
                    {
                        AssignStopTile(tileData, coord, tiles, rng);
                    }
                    else
                    {
                        ResetTile(tileData, TileType.Path, hpRestore: 0, moneteGained: 0);
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
                if (includeOrigin && IsBlocked(origin, tiles, start, end, eventOccupied, globalClaimed, localClaimed)) continue;

                int maxLenHere = Math.Min(_strada.MaxBranchLength, tilesBudget);
                if (maxLenHere < _strada.MinBranchLength) continue;

                int length = rng.Next(_strada.MinBranchLength, maxLenHere + 1);
                var path = GrowBranch(tiles, origin, dir, length, includeOrigin, start, end, eventOccupied, globalClaimed, localClaimed, rng);
                if (path.Count < _strada.MinBranchLength) continue;

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
                    EnqueueFork(queue, endpoint, dir, forkCount, rng);
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

        private List<HexCoord> GrowBranch(
            Dictionary<HexCoord, HexTileData> tiles, HexCoord origin, int heading, int length, bool includeOrigin,
            HexCoord start, HexCoord end, HashSet<HexCoord> eventOccupied, HashSet<HexCoord> globalClaimed,
            HashSet<HexCoord> localClaimed, Random rng)
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
                if (IsBlocked(next, tiles, start, end, eventOccupied, globalClaimed, localClaimed)) break;

                path.Add(next);
                localClaimed.Add(next);
                current = next;
            }

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
