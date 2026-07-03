using System;
using System.Collections.Generic;

namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    /// <summary>
    /// Generatore di mappa per la fase prototipo.
    /// Assegna tipi casuali uniformi da un pool fisso (Boss escluso).
    /// Il tile di partenza è sempre Strada/Scoperta al centro della griglia.
    /// Il tile più lontano dalla partenza diventa Boss e obiettivo di missione.
    /// Nessun retry, nessun conteggio percorsi: con movimento libero ogni tile
    /// è raggiungibile per definizione.
    /// </summary>
    public sealed class RandomTileTypeGenerator : IMapGenerator
    {
        private static readonly TileType[] RandomPool =
        {
            TileType.Strada,
            TileType.Battaglia,
            TileType.Trappola,
            TileType.Risorsa,
            TileType.NPC,
            TileType.Mistery,
        };

        public MapGenerationResult Generate(int width, int height, int seed)
        {
            var rng = new Random(seed);
            var tiles = new Dictionary<HexCoord, HexTileData>(width * height);

            // Build all tiles with random types.
            for (int row = 0; row < height; row++)
            {
                for (int col = 0; col < width; col++)
                {
                    var coord = HexCoord.FromOffsetOddR(col, row);
                    var tile = new HexTileData(coord);

                    var type = RandomPool[rng.Next(RandomPool.Length)];
                    tile.Type = type;

                    // PLACEHOLDER balance ranges for the readability prototype — not final design.
                    tile.HpRestore = type switch
                    {
                        TileType.Risorsa   =>  rng.Next(1, 7),   // +1..+6  heal
                        TileType.Battaglia => -rng.Next(1, 5),   // -1..-4  combat damage
                        TileType.Trappola  => -rng.Next(2, 7),   // -2..-6  trap damage (harsher: no loot)
                        _                  =>  0,                 // Strada, NPC, Mistery, Boss: no effect yet
                    };

                    // PLACEHOLDER Monete reward — balance is the designer's responsibility.
                    tile.MoneteGained = type switch
                    {
                        TileType.Battaglia => rng.Next(1, 6),    // +1..+5 monete per kill
                        _                  => 0,
                    };

                    tiles[coord] = tile;
                }
            }

            // Start tile: center of the grid, always Strada and already revealed.
            var startCoord = HexCoord.FromOffsetOddR(width / 2, height / 2);
            var startTile = tiles[startCoord];
            startTile.Type = TileType.Strada;
            startTile.State = TileState.Scoperta;
            startTile.HpRestore = 0;

            // Objective tile: farthest tile from start becomes Boss.
            HexCoord objectiveCoord = startCoord;
            int maxDist = 0;
            foreach (var coord in tiles.Keys)
            {
                if (coord.Equals(startCoord)) continue;
                int dist = coord.DistanceTo(startCoord);
                if (dist > maxDist)
                {
                    maxDist = dist;
                    objectiveCoord = coord;
                }
            }

            var objectiveTile = tiles[objectiveCoord];
            objectiveTile.Type = TileType.Boss;
            objectiveTile.IsObjective = true;
            objectiveTile.HpRestore = 0;
            objectiveTile.State = TileState.Conosciuta;

            return new MapGenerationResult
            {
                Tiles = tiles,
                StartCoord = startCoord,
                ObjectiveCoord = objectiveCoord,
                SeedUsed = seed,
            };
        }
    }
}
