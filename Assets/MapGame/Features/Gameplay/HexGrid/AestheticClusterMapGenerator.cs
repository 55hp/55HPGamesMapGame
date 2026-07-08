using System;
using System.Collections.Generic;
using hp55games.MapGame.Features.Configs;

namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    /// <summary>
    /// Generatore di mappa, pipeline "macchie di leopardo" (GDD, rev. 2026-07-07):
    /// 1. Riempimento procedurale di base — TEMPORANEO: verrà rimosso in fase 5 quando
    ///    la mesh Strada coprirà per intero le tessere non occupate da EventCluster/singole.
    /// 2. Piazzamento Start/End (weighted distance)
    /// 3. Piazzamento EventCluster (forme dal catalogo) e tessere singole via rejection
    ///    sampling, con almeno una tessera di distacco tra due piazzamenti qualunque.
    /// 4. Rivelazione iniziale (Start ed End già Scoperte)
    ///
    /// Mesh Strada (fase 5) non ancora ricablata su questo sistema — i metodi esistenti
    /// (GenerateStradaNetwork e affini) restano nel file ma non sono chiamati, verranno
    /// riscritti per attraversare più EventCluster invece di uno singolo.
    /// </summary>
    public sealed class AestheticClusterMapGenerator : IMapGenerator
    {
        private static readonly TileType[] FillerPool =
        {
            TileType.Strada, TileType.Battaglia, TileType.Trappola,
            TileType.Risorsa, TileType.NPC, TileType.Mistery,
        };

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
                tiles[coord] = BuildFillerTile(coord, rng);
            }

            var startCoord = PlaceStart(tiles, width, height, rng);
            var endCoord = PlaceEnd(tiles, startCoord, rng);

            ResetTile(tiles[startCoord], TileType.Strada, hpRestore: 0, moneteGained: 0);
            tiles[startCoord].IsObjective = false;

            ResetTile(tiles[endCoord], TileType.Boss, hpRestore: 0, moneteGained: 0);
            tiles[endCoord].IsObjective = true;

            PlaceEventClusters(tiles, width, height, startCoord, endCoord, rng);

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

        /// <summary>
        /// Piazza EventCluster (forme dal catalogo) e tessere singole via rejection
        /// sampling: tenta una posizione e una forma a caso, verifica i vincoli (dentro
        /// griglia, lontano da Start/End, almeno una tessera di distacco da ogni altro
        /// EventCluster/singola già piazzato), se valida la piazza, altrimenti riprova.
        /// Si ferma dopo _eventClusters.MaxConsecutiveFailures tentativi falliti di fila
        /// consecutivi — segnale che la griglia è piena. Il rapporto cluster:singola nasce
        /// da un contatore che si rigenera a ogni singola piazzata con un nuovo target
        /// casuale tra ClusterToSingleRatioMin/Max, non è imposto rigidamente sul totale.
        /// </summary>
        private void PlaceEventClusters(
            Dictionary<HexCoord, HexTileData> tiles, int width, int height,
            HexCoord start, HexCoord end, Random rng)
        {
            var occupied = new HashSet<HexCoord>();
            var catalog = _eventClusters.Catalog;
            bool hasShapes = catalog != null && catalog.Shapes != null && catalog.Shapes.Length > 0;

            int clustersUntilNextSingle = rng.Next(_eventClusters.ClusterToSingleRatioMin, _eventClusters.ClusterToSingleRatioMax + 1);
            int consecutiveFailures = 0;

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

                foreach (var coord in footprint) occupied.Add(coord);
                consecutiveFailures = 0;
            }
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

        // ===== Mesh Strada (fase 5) — definizioni tenute, non ancora chiamate =====
        // Pensate per un solo EventCluster a 6 posizioni fisse (vecchio fiore): andranno
        // riscritte per attaccarsi al bordo di forme arbitrarie e attraversare più
        // EventCluster incontrati lungo il cammino, come discusso.

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
                if (path.Count == 0) continue;

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
