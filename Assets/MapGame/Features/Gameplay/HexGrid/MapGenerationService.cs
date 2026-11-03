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
        MapGenerationResult GenerateMap(MapGenerationConfig config, LevelConfig level, int seed);
    }

    public sealed class MapGenerationService : IMapGenerationService
    {
        public MapGenerationResult GenerateMap(MapGenerationConfig config, LevelConfig level, int seed)
        {
            // v2 (04 §4): generatore a censimento. MapClusterGenerator non è più istanziato (rimozione: spec 0012).
            var rules = hp55games.MapGame.Rules.RulesConfigDefaults.Level1();
            rules.StartMinBorderDistance = config.StartMinBorderDistance;
            rules.EndDistanceWeights.Clear();
            foreach (var w in config.EndDistanceWeights)
                rules.EndDistanceWeights.Add(new hp55games.MapGame.Rules.DistanceWeight(w.Distance, w.Weight));
            IMapGenerator generator = new hp55games.MapGame.Features.Gameplay.Run.CensusMapAdapter(rules);
            return generator.Generate(config.Width, config.Height, seed);
        }
    }
}
