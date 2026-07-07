using hp55games.MapGame.Features.Configs;

namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    /// <summary>
    /// Proprietario unico della logica di generazione mappa (gap segnalato nelle Bezi Rules,
    /// sezione "Map generation ownership"). Sa quale IMapGenerator istanziare e come tradurre
    /// MapGenerationConfig nei suoi parametri — così la logica di costruzione del generator
    /// non si disperde tra HexGridController e i tool Editor.
    ///
    /// Usato sia a runtime (risolto via ServiceRegistry, vedi MapGenerationServiceInstaller)
    /// sia in Editor per il preview (MapGenerationConfigEditor), istanziato direttamente lì
    /// perché il ServiceRegistry non è popolato fuori Play Mode.
    /// </summary>
    public interface IMapGenerationService
    {
        MapGenerationResult GenerateMap(MapGenerationConfig config, int seed);
    }

    public sealed class MapGenerationService : IMapGenerationService
    {
        public MapGenerationResult GenerateMap(MapGenerationConfig config, int seed)
        {
            IMapGenerator generator = new AestheticClusterMapGenerator(
                config.EndDistanceWeights,
                config.StartMinBorderDistance,
                config.ClusterMinDistanceFromStartEnd,
                config.ClusterPlacementMaxAttempts,
                config.PlaceholderBalance,
                config.StradaNetwork);

            return generator.Generate(config.Width, config.Height, seed);
        }
    }
}
