using hp55games.MapGame.Features.Configs;

namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    /// <summary>
    /// Proprietario unico della logica di generazione mappa. Sa quale IMapGenerator
    /// istanziare e come tradurre MapGenerationConfig nei suoi parametri — così la
    /// logica di costruzione del generator non si disperde tra HexGridController e i
    /// tool Editor.
    ///
    /// Usato sia a runtime (risolto via ServiceRegistry, vedi MapGenerationServiceInstaller)
    /// sia in Editor per il preview (MapGenerationConfigEditor), istanziato direttamente lì
    /// perché il ServiceRegistry non è popolato fuori Play Mode.
    /// </summary>
    public interface IMapGenerationService
    {
        MapGenerationResult GenerateMap(MapGenerationConfig config, LevelConfig level, ElementCatalog elementCatalog, int seed);
    }

    public sealed class MapGenerationService : IMapGenerationService
    {
        public MapGenerationResult GenerateMap(MapGenerationConfig config, LevelConfig level, ElementCatalog elementCatalog, int seed)
        {
            // Il vecchio bilanciamento hardcoded/random (PlaceholderBalanceSettings) e'
            // stato sostituito dalla revisione 2026-08-05 con la risoluzione via
            // ElementCatalog dentro AestheticClusterMapGenerator, e il campo — rimasto
            // inutilizzato su MapGenerationConfig da allora — e' stato rimosso del tutto
            // il 2026-08-07 insieme al resto della roba deprecata (vedi MapGenerationConfig.cs).
            IMapGenerator generator = new AestheticClusterMapGenerator(
                config.EndDistanceWeights,
                config.StartMinBorderDistance,
                config.ClusterMinDistanceFromStartEnd,
                config.StradaNetwork,
                config.EventClusters,
                level,
                elementCatalog);

            return generator.Generate(config.Width, config.Height, seed);
        }
    }
}
