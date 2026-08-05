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
            // PlaceholderBalanceSettings non e' piu' passato al generatore (revisione
            // 2026-08-05): il vecchio bilanciamento hardcoded/random e' sostituito dalla
            // risoluzione via ElementCatalog dentro AestheticClusterMapGenerator. Il campo
            // resta su MapGenerationConfig per non rompere gli asset serializzati
            // esistenti, ma non e' piu' letto da nessuno.
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
