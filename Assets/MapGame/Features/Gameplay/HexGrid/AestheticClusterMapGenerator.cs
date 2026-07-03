// ===== Assets/MapGame/Features/Gameplay/HexGrid/AestheticClusterMapGenerator.cs =====
using System;
using System.Collections.Generic;

namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    /// <summary>
    /// Generatore di mappa, pipeline GDD v2.2:
    /// 1. Riempimento procedurale di base (lo "spazio negativo" tra i cluster)
    /// 2. Piazzamento Start/End (weighted distance)
    /// 3. Piazzamento cluster estetico (l'hint implicito)
    /// 4. Rivelazione iniziale (Start ed End già Scoperte)
    ///
    /// Nessun golden path: con movimento gratuito ogni tile è raggiungibile
    /// per definizione, e il contrasto cluster/riempimento crea già un percorso
    /// leggibile senza bisogno di disegnarlo esplicitamente (decisione di sessione).
    ///
    /// Per Sessione 1: un solo cluster estetico (Foresta), nessun cluster Strada
    /// dedicato oltre al piazzamento casuale del filler.
    /// </summary>
    public sealed class AestheticClusterMapGenerator : IMapGenerator
    {
        private static readonly TileType[] FillerPool =
        {
            TileType.Strada, TileType.Battaglia, TileType.Trappola,
            TileType.Risorsa, TileType.NPC, TileType.Mistery,
        };

        // PLACEHOLDER: coppie <distanza, peso> per il piazzamento di End rispetto a Start.
        // Da esporre in MapGenerationConfig quando pronto il tool editor (prossimo step).
        private static readonly (int distance, int weight)[] EndDistanceWeights =
        {
            (4, 1), (5, 2), (6, 3), (7, 2), (8, 1),
        };

        private const int StartMinBorderDistance = 3;
        private const int ClusterMinDistanceFromStartEnd = 2;
        private const int ClusterPlacementMaxAttempts = 50;

        public MapGenerationResult Generate(int width, int height, int seed)
        {
            var rng = new Random(seed);
            var tiles = new Dictionary<HexCoord, HexTileData>(width * height);

            for (int row = 0; row < height; row++)
            for (int col = 0; col < width; col++)
            {
                var coord = HexCoord.FromOffsetOddR(col, row);
                tiles[coord] = BuildFillerTile(coord, rng);
            }

            var startCoord = PlaceStart(tiles, width, height, rng);
            var endCoord = PlaceEnd(tiles, startCoord, rng);

            ResetTile(tiles[startCoord], TileType.Strada, hpRestore: 0, moneteGained: 0);
            tiles[startCoord].IsObjective = false;

            ResetTile(tiles[endCoord], TileType.Boss, hpRestore: 0, moneteGained: 0);
            tiles[endCoord].IsObjective = true;

            PlaceAestheticCluster(tiles, width, height, ClusterPatternCatalog.Foresta, startCoord, endCoord, rng);

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
                TileType.Risorsa   =>  rng.Next(1, 7),
                TileType.Battaglia => -rng.Next(1, 5),
                TileType.Trappola  => -rng.Next(2, 7),
                _                  =>  0,
            };
            tile.MoneteGained = type == TileType.Battaglia ? rng.Next(1, 6) : 0;
        }

        private void ResetTile(HexTileData tile, TileType type, int hpRestore, int moneteGained)
        {
            tile.Type = type;
            tile.HpRestore = hpRestore;
            tile.MoneteGained = moneteGained;
        }

        /// <summary>Start: coordinata casuale ad almeno StartMinBorderDistance dai bordi.</summary>
        private HexCoord PlaceStart(Dictionary<HexCoord, HexTileData> tiles, int width, int height, Random rng)
        {
            var candidates = new List<HexCoord>();
            foreach (var coord in tiles.Keys)
            {
                coord.ToOffsetOddR(out int col, out int row);
                if (col < StartMinBorderDistance || col >= width - StartMinBorderDistance) continue;
                if (row < StartMinBorderDistance || row >= height - StartMinBorderDistance) continue;
                candidates.Add(coord);
            }

            // Griglia troppo piccola per il margine richiesto: fallback al centro.
            return candidates.Count == 0
                ? HexCoord.FromOffsetOddR(width / 2, height / 2)
                : candidates[rng.Next(candidates.Count)];
        }

        /// <summary>End: distanza pesata da Start, tile più vicina alla distanza estratta.</summary>
        private HexCoord PlaceEnd(Dictionary<HexCoord, HexTileData> tiles, HexCoord start, Random rng)
        {
            int targetDistance = WeightedPick(EndDistanceWeights, rng);

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

        private int WeightedPick((int distance, int weight)[] table, Random rng)
        {
            int totalWeight = 0;
            foreach (var entry in table) totalWeight += entry.weight;

            int roll = rng.Next(totalWeight);
            foreach (var entry in table)
            {
                if (roll < entry.weight) return entry.distance;
                roll -= entry.weight;
            }

            return table[^1].distance; // difensivo
        }

        /// <summary>
        /// Piazza il pattern su un centro casuale con tutti e sei i petali dentro
        /// la griglia e lontano da Start/End. Se non trova un centro valido entro
        /// ClusterPlacementMaxAttempts, il cluster viene omesso senza errore —
        /// segnale utile se capita spesso durante il playtest (griglia troppo piccola/densa).
        /// </summary>
        private void PlaceAestheticCluster(
            Dictionary<HexCoord, HexTileData> tiles, int width, int height,
            ClusterPatternDefinition pattern, HexCoord start, HexCoord end, Random rng)
        {
            for (int attempt = 0; attempt < ClusterPlacementMaxAttempts; attempt++)
            {
                var center = HexCoord.FromOffsetOddR(rng.Next(width), rng.Next(height));

                if (center.Equals(start) || center.Equals(end)) continue;
                if (center.DistanceTo(start) < ClusterMinDistanceFromStartEnd) continue;
                if (center.DistanceTo(end) < ClusterMinDistanceFromStartEnd) continue;

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
                return;
            }
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
        /// GDD fix: Start is Scoperta (player is here, content resolved).
        /// End/Boss is Conosciuta (position known from mission brief, content NOT yet resolved —
        /// it has not been interacted with and must not be treated as already played).
        /// </summary>
        private void RevealInitialTiles(HexTileData startTile, HexTileData endTile)
        {
            startTile.State = TileState.Scoperta;
            endTile.State   = TileState.Conosciuta;
        }
    }
}