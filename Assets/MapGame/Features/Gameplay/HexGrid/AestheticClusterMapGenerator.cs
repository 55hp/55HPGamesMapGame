using System;
using System.Collections.Generic;
using hp55games.MapGame.Features.Configs;

namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    /// <summary>
    /// Generatore di mappa, pipeline "macchie di leopardo" (GDD, rev. 2026-07-07), completa:
    /// 1. Piazzamento Start/End (weighted distance)
    /// 2. Piazzamento EventCluster (forme dal catalogo) e tessere singole via rejection
    ///    sampling, con almeno una tessera di distacco tra due piazzamenti qualunque.
    /// 3. Mesh di PathCluster: parte da ogni bordo di ogni EventCluster/singola piazzata,
    ///    cresce a budget (15 tessere / 5 rami / rami 3-7) per ciascun PathCluster, si
    ///    ferma sui bordi degli EventCluster (fanno da muro, nessuna Neutra necessaria lì)
    ///    e diventa Neutra dove tocca un PathCluster diverso già piazzato (per non farli
    ///    fondere in un'unica cascata).
    /// 4. Rammendo: ogni tessera ancora priva di contenuto a questo punto diventa Strada
    ///    extra su un PathCluster confinante (massimo 2 per PathCluster) oppure, se non
    ///    c'è un PathCluster a cui attaccarsi, una tessera singola. Nessuna tessera resta
    ///    senza contenuto esplicito — niente più riempimento casuale di base.
    /// 5. Rivelazione iniziale (Start ed End già Scoperte)
    /// </summary>
    public sealed class AestheticClusterMapGenerator : IMapGenerator
    {
        /// <summary>
        /// Tipi ammessi per le tessere singole (variante di un EventCluster da 1 tessera).
        /// Niente Strada: la Strada è la mesh separata, non un contenuto di singola.
        /// </summary>
        private static readonly TileType[] SingleTilePool =
        {
            TileType.Battaglia, TileType.Trappola, TileType.Risorsa, TileType.NPC, TileType.Mistery,
        };

        private readonly DistanceWeight[] _endDistanceWeights;
        private readonly int _startMinBorderDistance;
        private readonly int _clusterMinDistanceFromStartEnd;
        private readonly PlaceholderBalanceSettings _balance;
        private readonly StradaNetworkSettings _strada;
        private readonly EventClusterPlacementSettings _eventClusters;

        public AestheticClusterMapGenerator(
            DistanceWeight[] endDistanceWeights,
            int startMinBorderDistance,
            int clusterMinDistanceFromStartEnd,
            PlaceholderBalanceSettings balance,
            StradaNetworkSettings strada,
            EventClusterPlacementSettings eventClusters)
        {
            _endDistanceWeights = endDistanceWeights;
            _startMinBorderDistance = startMinBorderDistance;
            _clusterMinDistanceFromStartEnd = clusterMinDistanceFromStartEnd;
            _balance = balance;
            _strada = strada;
            _eventClusters = eventClusters;
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

            ResetTile(tiles[startCoord], TileType.Strada, hpRestore: 0, moneteGained: 0);
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
            tile.HpRestore = type switch
            {
                TileType.Risorsa   =>  rng.Next(_balance.RisorsaHpRestoreMin, _balance.RisorsaHpRestoreMax + 1),
                TileType.Battaglia => -rng.Next(_balance.BattagliaHpLossMin, _balance.BattagliaHpLossMax + 1),
                TileType.Trappola  => -rng.Next(_balance.TrappolaHpLossMin, _balance.TrappolaHpLossMax + 1),
                _                  =>  0,
            };
            tile.MoneteGained = type == TileType.Battaglia
                ? rng.Next(_balance.BattagliaMoneteMin, _balance.BattagliaMoneteMax + 1)
                : 0;
        }

        private void ResetTile(HexTileData tile, TileType type, int hpRestore, int moneteGained)
        {
            tile.Type = type;
            tile.HpRestore = hpRestore;
            tile.MoneteGained = moneteGained;
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
                    ApplyPlaceholderBalance(tiles[origin], SingleTilePool[rng.Next(SingleTilePool.Length)], rng);
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

        private void ApplyEventClusterShape(Dictionary<HexCoord, HexTileData> tiles, HexCoord origin, EventClusterShape shape, Random rng)
        {
            foreach (var tileSpec in shape.Tiles)
            {
                var coord = origin + new HexCoord(tileSpec.RelativeQ, tileSpec.RelativeR);
                ApplyPlaceholderBalance(tiles[coord], tileSpec.Type, rng);
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
                // questo PathCluster confina con una tessera di un altro, diventa Neutra
                // invece di Strada, così CascadeStrada non li fonde in un'unica cascata.
                var neutraOverride = new HashSet<HexCoord>();
                foreach (var coord in pathTiles)
                {
                    for (int dir = 0; dir < 6; dir++)
                    {
                        var neighbor = coord.GetNeighbor(dir);
                        if (tileOwner.TryGetValue(neighbor, out int ownerIdx) && ownerIdx != pathClusterIndex)
                        {
                            neutraOverride.Add(coord);
                            break;
                        }
                    }
                }

                foreach (var coord in pathTiles)
                {
                    var type = neutraOverride.Contains(coord) ? TileType.Neutra : TileType.Strada;
                    ResetTile(tiles[coord], type, hpRestore: 0, moneteGained: 0);
                    globalClaimed.Add(coord);
                    tileOwner[coord] = pathClusterIndex;
                    tiles[coord].PathClusterId = pathClusterIndex;
                }

                pathClusterIndex++;
            }
        }

        /// <summary>
        /// Ogni tessera ancora priva di contenuto dopo EventCluster e mesh PathCluster
        /// (né Start, né End, né occupata, né già Strada/Neutra) viene risolta qui:
        /// se confina con un PathCluster che non ha ancora esaurito la sua quota di 2
        /// tessere extra, diventa Strada e si aggiunge a quel PathCluster; altrimenti
        /// diventa una tessera singola. Con questa passata nessuna tessera della griglia
        /// resta senza contenuto esplicito.
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
                    ResetTile(tiles[coord], TileType.Strada, hpRestore: 0, moneteGained: 0);
                    globalClaimed.Add(coord);
                    tileOwner[coord] = extendOwner.Value;
                    tiles[coord].PathClusterId = extendOwner.Value;
                    patchCountPerPathCluster[extendOwner.Value] =
                        (patchCountPerPathCluster.TryGetValue(extendOwner.Value, out int u2) ? u2 : 0) + 1;
                }
                else
                {
                    ApplyPlaceholderBalance(tiles[coord], SingleTilePool[rng.Next(SingleTilePool.Length)], rng);
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
            startTile.State = TileState.Scoperta;
            endTile.State   = TileState.Conosciuta;
        }
    }
}
