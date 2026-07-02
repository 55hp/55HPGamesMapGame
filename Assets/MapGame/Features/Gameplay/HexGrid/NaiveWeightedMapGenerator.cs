using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Serialization;

namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    /// <summary>
    /// Generazione semplice: tipo e magnitudo assegnati random-weighted per cella,
    /// nessun clustering/coerenza spaziale. Ritenta seed successivi finché la mappa
    /// non ha almeno MinRequiredPaths percorsi validi verso l'obiettivo di test
    /// (default 1 = solo "beatable"); PathsFound nel risultato serve anche a
    /// classificare la difficoltà del seed usato.
    /// </summary>
    public sealed class NaiveWeightedMapGenerator : IMapGenerator
    {
        [System.Serializable]
        public struct Config
        {
            [FormerlySerializedAs("BattagliaFallbackWeight")] [FormerlySerializedAs("EmptyWeight")] public float BattleFallbackWeight;
            public int MinFoodReward;
            public int MaxFoodReward; // esclusivo
            public int MinRequiredPaths;
            public int MaxGenerationAttempts;
            public int MaxPathsToClassify;

            public static Config Default => new Config
            {
                BattleFallbackWeight = 0.25f,
                MinFoodReward = 1,
                MaxFoodReward = 6, // 1-5 inclusi
                MinRequiredPaths = 1,
                MaxGenerationAttempts = 50,
                MaxPathsToClassify = 10
            };
        }

        private readonly Config _config;

        public NaiveWeightedMapGenerator(Config config)
        {
            _config = config;
        }

        public MapGenerationResult Generate(int width, int height, int startingFood, int seed)
        {
            MapGenerationResult lastAttempt = null;

            for (int attempt = 0; attempt < _config.MaxGenerationAttempts; attempt++)
            {
                int trySeed = seed + attempt;
                var result = GenerateSingleAttempt(width, height, startingFood, trySeed);
                lastAttempt = result;

                if (result.PathsFound >= _config.MinRequiredPaths)
                    return result;
            }

            Debug.LogWarning(
                $"[NaiveWeightedMapGenerator] Nessun seed tra {seed} e {seed + _config.MaxGenerationAttempts - 1} " +
                $"ha raggiunto MinRequiredPaths={_config.MinRequiredPaths}. " +
                $"Uso l'ultimo tentativo (PathsFound={lastAttempt?.PathsFound}).");

            return lastAttempt;
        }

        private MapGenerationResult GenerateSingleAttempt(int width, int height, int startingFood, int seed)
        {
            var tiles = new Dictionary<HexCoord, HexTileData>();
            var rng = new System.Random(seed);
            var types = new[] { TileType.Battaglia, TileType.NPC, TileType.Mistery, TileType.Risorsa };

            for (int row = 0; row < height; row++)
            {
                for (int col = 0; col < width; col++)
                {
                    var coord = HexCoord.FromOffsetOddR(col, row);
                    var tile = new HexTileData(coord);

                    tile.Type = rng.NextDouble() < _config.BattleFallbackWeight
                        ? TileType.Battaglia
                        : types[rng.Next(types.Length)];
                    tile.Magnitude = rng.Next(1, 4); // 1-3 inclusi, sempre valido: ogni tile ha ora un tipo reale
                    tile.FoodReward = tile.Type == TileType.Risorsa
                        ? rng.Next(_config.MinFoodReward, _config.MaxFoodReward)
                        : 0;

                    tiles[coord] = tile;
                }
            }

            var start = HexCoord.FromOffsetOddR(width / 2, height / 2);
            if (tiles.TryGetValue(start, out var startTile))
            {
                startTile.State = TileState.Scoperta;
                startTile.Type = TileType.None;
                startTile.Magnitude = 0;
                startTile.FoodReward = 0;
            }

            var objective = FindFarthestTile(start, tiles);
            if (tiles.TryGetValue(objective, out var objectiveTile))
                objectiveTile.IsObjective = true;

            var counter = new MapPathCounter(tiles, _config.MaxPathsToClassify);
            int pathsFound = counter.CountPaths(start, startingFood, objective);

            return new MapGenerationResult
            {
                Tiles = tiles,
                StartCoord = start,
                ObjectiveCoord = objective,
                SeedUsed = seed,
                PathsFound = pathsFound
            };
        }

        /// <summary>Obiettivo di test = la tile più lontana dallo start (non è il sistema missioni reale).</summary>
        private static HexCoord FindFarthestTile(HexCoord start, Dictionary<HexCoord, HexTileData> tiles)
        {
            HexCoord farthest = start;
            int maxDist = -1;

            foreach (var coord in tiles.Keys)
            {
                int dist = coord.DistanceTo(start);
                if (dist > maxDist)
                {
                    maxDist = dist;
                    farthest = coord;
                }
            }

            return farthest;
        }
    }
}
