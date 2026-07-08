using System;
using System.Collections.Generic;
using hp55games.MapGame.Features.Configs;

namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    /// <summary>
    /// Generatore di mappa, pipeline GDD v2.2:
    /// 1. Riempimento procedurale di base (lo "spazio negativo" tra i cluster)
    /// 2. Piazzamento Start/End (weighted distance)
    /// 3. Piazzamento cluster estetico (l'hint implicito)
    /// 3b. Rete Strada auto-generata dal bordo del cluster (incroci, budget fisso)
    /// 4. Rivelazione iniziale (Start ed End già Scoperte)
    ///
    /// Nessun golden path: con movimento gratuito ogni tile è raggiungibile
    /// per definizione, e il contrasto cluster/riempimento crea già un percorso
    /// leggibile senza bisogno di disegnarlo esplicitamente (decisione di sessione).
    ///
    /// Parametri di piazzamento/bilanciamento/rete Strada ricevuti dal costruttore
    /// (letti da MapGenerationConfig tramite MapGenerationService) — non hardcoded,
    /// per restare configurabili da Editor senza ricompilare.
    /// </summary>
    public sealed class AestheticClusterMapGenerator : IMapGenerator
    {
        private static readonly TileType[] FillerPool =
        {
            TileType.Strada, TileType.Battaglia, TileType.Trappola,
            TileType.Risorsa, TileType.NPC, TileType.Mistery,
        };

        private readonly DistanceWeight[] _endDistanceWeights;
        private readonly int _startMinBorderDistance;
        private readonly int _clusterMinDistanceFromStartEnd;
        private readonly int _clusterPlacementMaxAttempts;
        private readonly PlaceholderBalanceSettings _balance;
        private readonly StradaNetworkSettings _strada;

        public AestheticClusterMapGenerator(
            DistanceWeight[] endDistanceWeights,
            int startMinBorderDistance,
            int clusterMinDistanceFromStartEnd,
            int clusterPlacementMaxAttempts,
            PlaceholderBalanceSettings balance,
            StradaNetworkSettings strada)
        {
            _endDistanceWeights = endDistanceWeights;
            _startMinBorderDistance = startMinBorderDistance;
            _clusterMinDistanceFromStartEnd = clusterMinDistanceFromStartEnd;
            _clusterPlacementMaxAttempts = clusterPlacementMaxAttempts;
            _balance = balance;
            _strada = strada;
        }

        public MapGenerationResult Generate(int width, int height, int seed)
        {
            var rng = new Random(seed);
            var tiles = new Dictionary<HexCoord, HexTileData>(width * height);

            for (int row = 0; row < height; row++)
            for (int col = 0; col < width; col++)
            {
                var coord = HexCoord.FromOffsetOddQ(col, row);
                tiles[coord] = BuildFillerTile(coord, rng);
            }

            var startCoord = PlaceStart(tiles, width, height, rng);
            var endCoord = PlaceEnd(tiles, startCoord, rng);

            ResetTile(tiles[startCoord], TileType.Strada, hpRestore: 0, moneteGained: 0);
            tiles[startCoord].IsObjective = false;

            ResetTile(tiles[endCoord], TileType.Boss, hpRestore: 0, moneteGained: 0);
            tiles[endCoord].IsObjective = true;

            var clusterPlacement = PlaceAestheticCluster(tiles, width, height, ClusterPatternCatalog.Foresta, startCoord, endCoord, rng);
            if (clusterPlacement.HasValue)
            {
                GenerateStradaNetwork(tiles, clusterPlacement.Value.center, clusterPlacement.Value.petals, startCoord, endCoord, rng);
            }

            RevealInitialTiles(tiles[startCoord], tiles[endCoord]);

            return new MapGenerationResult
            {
                Tiles = tiles,
                StartCoord = startCoord,
                ObjectiveCoord = endCoord,
                SeedUsed = seed,
            };
        }

        private HexTileData BuildFillerTile(HexCoord coord, Random rng)
        {
            var tile = new HexTileData(coord);
            ApplyPlaceholderBalance(tile, FillerPool[rng.Next(FillerPool.Length)], rng);
            return tile;
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

        private (HexCoord center, HexCoord[] petals)? PlaceAestheticCluster(
            Dictionary<HexCoord, HexTileData> tiles, int width, int height,
            ClusterPatternDefinition pattern, HexCoord start, HexCoord end, Random rng)
        {
            for (int attempt = 0; attempt < _clusterPlacementMaxAttempts; attempt++)
            {
                var center = HexCoord.FromOffsetOddQ(rng.Next(width), rng.Next(height));

                if (center.Equals(start) || center.Equals(end)) continue;
                if (center.DistanceTo(start) < _clusterMinDistanceFromStartEnd) continue;
                if (center.DistanceTo(end) < _clusterMinDistanceFromStartEnd) continue;

                var petalCoords = new HexCoord[6];
                bool allValid = true;
                for (int dir = 0; dir < 6; dir++)
                {
                    var neighbor = center.GetNeighbor(dir);
                    if (!tiles.ContainsKey(neighbor) || neighbor.Equals(start) || neighbor.Equals(end))
                    {
                        allValid = false;
                        break;
                    }
                    petalCoords[dir] = neighbor;
                }

                if (!allValid) continue;

                ApplyPattern(tiles, center, petalCoords, pattern);
                return (center, petalCoords);
            }

            return null;
        }

        private void ApplyPattern(
            Dictionary<HexCoord, HexTileData> tiles, HexCoord center,
            HexCoord[] petalCoords, ClusterPatternDefinition pattern)
        {
            ResetTile(tiles[center], pattern.Center.Type, pattern.Center.HpRestore, pattern.Center.MoneteGained);

            for (int dir = 0; dir < 6; dir++)
            {
                var spec = pattern.Petals[dir];
                ResetTile(tiles[petalCoords[dir]], spec.Type, spec.HpRestore, spec.MoneteGained);
            }
        }

        /// <summary>
        /// Rete Strada automatica attorno al cluster: cresce come un piccolo albero a
        /// incroci partendo da una tile di bordo (adiacente a un petalo, non parte del
        /// cluster, non Start/End). Vincoli da Franci (sessione 2026-07-07): ogni ramo
        /// tra _strada.MinBranchLength e _strada.MaxBranchLength tessere, al massimo
        /// _strada.MaxBranches rami totali, _strada.MaxTotalTiles tessere complessive.
        /// Solo tessere piane per ora — niente ponte rotto (deferito). Se il budget non
        /// permette nemmeno il ramo minimo, quel ramo/quella biforcazione viene scartata
        /// silenziosamente, stesso pattern di PlaceAestheticCluster.
        /// </summary>
        private void GenerateStradaNetwork(
            Dictionary<HexCoord, HexTileData> tiles, HexCoord clusterCenter, HexCoord[] clusterPetals,
            HexCoord start, HexCoord end, Random rng)
        {
            var border = FindClusterBorder(tiles, clusterCenter, clusterPetals, start, end);
            if (border.Count == 0) return;

            var claimed = new HashSet<HexCoord>();
            var (rootTile, rootDir) = border[rng.Next(border.Count)];

            int tilesBudget = _strada.MaxTotalTiles;
            int branchBudget = _strada.MaxBranches;

            var queue = new Queue<(HexCoord origin, int dir, bool includeOrigin)>();
            queue.Enqueue((rootTile, rootDir, true));

            while (queue.Count > 0 && tilesBudget >= _strada.MinBranchLength && branchBudget > 0)
            {
                var (origin, dir, includeOrigin) = queue.Dequeue();

                int maxLenHere = Math.Min(_strada.MaxBranchLength, tilesBudget);
                if (maxLenHere < _strada.MinBranchLength) continue;

                int length = rng.Next(_strada.MinBranchLength, maxLenHere + 1);
                var path = GrowBranch(tiles, origin, dir, length, includeOrigin, start, end, claimed, rng);
                if (path.Count < _strada.MinBranchLength) continue;

                foreach (var coord in path)
                    ResetTile(tiles[coord], TileType.Strada, hpRestore: 0, moneteGained: 0);

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
        }

        /// <summary>
        /// Tile adiacenti ai petali del cluster ma non appartenenti al cluster stesso
        /// (né Start/End): punto di attacco per la radice della rete Strada. La direzione
        /// associata è quella del petalo di provenienza rispetto al centro — usata come
        /// heading iniziale, così il ramo cresce "verso l'esterno" allontanandosi dal cluster.
        /// </summary>
        private List<(HexCoord tile, int direction)> FindClusterBorder(
            Dictionary<HexCoord, HexTileData> tiles, HexCoord center, HexCoord[] petalCoords,
            HexCoord start, HexCoord end)
        {
            var clusterSet = new HashSet<HexCoord> { center };
            foreach (var p in petalCoords) clusterSet.Add(p);

            var seen = new HashSet<HexCoord>();
            var border = new List<(HexCoord, int)>();

            for (int dir = 0; dir < 6; dir++)
            {
                var petal = petalCoords[dir];
                for (int nDir = 0; nDir < 6; nDir++)
                {
                    var neighbor = petal.GetNeighbor(nDir);
                    if (!tiles.ContainsKey(neighbor)) continue;
                    if (clusterSet.Contains(neighbor)) continue;
                    if (neighbor.Equals(start) || neighbor.Equals(end)) continue;
                    if (!seen.Add(neighbor)) continue;
                    border.Add((neighbor, dir));
                }
            }

            return border;
        }

        /// <summary>
        /// Cresce un ramo tile per tile con leggera deviazione random (DeviateDirection),
        /// fino a `length` tessere o finché non esce dalla griglia / tocca Start-End / tocca
        /// una tessera già rivendicata da un altro ramo. Se includeOrigin è true, `origin`
        /// stessa è la prima tessera del ramo (radice); se false, origin è solo il punto di
        /// aggancio condiviso con il ramo padre (biforcazione) e non viene ricontata nel budget.
        /// </summary>
        private List<HexCoord> GrowBranch(
            Dictionary<HexCoord, HexTileData> tiles, HexCoord origin, int heading, int length,
            bool includeOrigin, HexCoord start, HexCoord end, HashSet<HexCoord> claimed, Random rng)
        {
            var path = new List<HexCoord>();
            var current = origin;
            int dir = heading;
            int steps = length;

            if (includeOrigin)
            {
                path.Add(origin);
                claimed.Add(origin);
                steps -= 1;
            }

            for (int i = 0; i < steps; i++)
            {
                dir = DeviateDirection(dir, rng);
                var next = current.GetNeighbor(dir);
                if (!tiles.ContainsKey(next)) break;
                if (next.Equals(start) || next.Equals(end)) break;
                if (claimed.Contains(next)) break;

                path.Add(next);
                claimed.Add(next);
                current = next;
            }

            return path;
        }

        /// <summary>
        /// Applica una piccola probabilità di svolta rispetto alla direzione corrente, così
        /// il ramo non risulta perfettamente rettilineo (pesi in MapGenerationConfig.StradaNetwork).
        /// </summary>
        private int DeviateDirection(int dir, Random rng)
        {
            int total = _strada.KeepHeadingWeight + _strada.TurnWeight;
            int roll = rng.Next(total);

            if (roll < _strada.KeepHeadingWeight) return dir;

            int turnRoll = roll - _strada.KeepHeadingWeight;
            bool turnRight = turnRoll < _strada.TurnWeight / 2;
            return turnRight ? (dir + 1) % 6 : (dir + 5) % 6;
        }

        /// <summary>
        /// Sceglie un esito pesato tra le opzioni ancora possibili (STOP è sempre presente;
        /// le biforcazioni entrano in lista solo se il budget residuo le può sostenere) —
        /// così la probabilità si ridistribuisce automaticamente quando un esito non è
        /// sostenibile, invece di forzare STOP.
        /// </summary>
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

        /// <summary>
        /// Mette in coda `count` nuovi rami dal punto di biforcazione, con direzioni
        /// distribuite simmetricamente attorno alla direzione del ramo padre — evitando la
        /// direzione opposta (parentDir + 3) per non ricrescere dentro il cluster.
        /// </summary>
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

        /// <summary>
        /// GDD fix: Start is Scoperta (player is here, content resolved).
        /// End/Boss is Conosciuta (position known from mission brief, content NOT yet resolved).
        /// </summary>
        private void RevealInitialTiles(HexTileData startTile, HexTileData endTile)
        {
            startTile.State = TileState.Scoperta;
            endTile.State   = TileState.Conosciuta;
        }
    }
}
