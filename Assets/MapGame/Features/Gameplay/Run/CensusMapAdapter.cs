using System.Collections.Generic;
using hp55games.MapGame.Features.Gameplay.HexGrid;
using hp55games.MapGame.Rules;

namespace hp55games.MapGame.Features.Gameplay.Run
{
    /// <summary>
    /// IMapGenerator v2 (04 §4): chiama il generatore puro e traduce in HexTileData.
    /// Le dimensioni vengono dalla RulesConfig (il censimento somma a 10×10): width/height passati sono ignorati.
    /// </summary>
    public sealed class CensusMapAdapter : IMapGenerator
    {
        private readonly RulesConfig _config;

        public CensusMapAdapter(RulesConfig config) { _config = config; }

        public MapGenerationResult Generate(int width, int height, int seed)
        {
            var layout = CensusMapGenerator.Generate(_config, unchecked((ulong)seed));
            return FromLayout(_config, layout, seed);
        }

        public static MapGenerationResult FromLayout(RulesConfig config, MapLayout layout, int seed)
        {
            var result = new MapGenerationResult { SeedUsed = seed, Success = layout.Success, FailureReason = layout.FailureReason };
            if (!layout.Success) return result;

            var tiles = new Dictionary<HexCoord, HexTileData>(layout.Count);
            for (int i = 0; i < layout.Count; i++)
            {
                var kind = layout.Tiles[i];
                var def = config.Content(kind);
                var coord = ContentKindMap.ToHexCoord(i, layout.Width);
                bool open = kind == ContentKind.Start; // D-041: le Vuote nascono coperte
                tiles[coord] = new HexTileData(coord)
                {
                    Content = kind,
                    RulesIndex = i,
                    Type = ContentKindMap.ToTileType(kind),
                    DifficultyLevel = def.MaskLevel,
                    IsObjective = def.IsObjective,
                    Exploration = open ? ExplorationState.Explored : ExplorationState.Unexplored,
                    Spotting = open ? SpottingState.Spotted : SpottingState.Unspotted,
                };
            }
            result.Tiles = tiles;
            result.StartCoord = ContentKindMap.ToHexCoord(layout.StartIndex, layout.Width);
            result.ObjectiveCoord = ContentKindMap.ToHexCoord(layout.BossIndex, layout.Width);
            return result;
        }
    }
}
