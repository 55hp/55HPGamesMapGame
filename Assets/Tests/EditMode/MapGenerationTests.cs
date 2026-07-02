using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;

using hp55games.MapGame.Features.Gameplay.HexGrid;

namespace hp55games.MapGame.Tests.EditMode
{
    /// <summary>
    /// Verifica che il generatore produca sempre mappe beatable e stampa una prima
    /// distribuzione di difficoltà (PathsFound) su un campione di seed, per calibrare
    /// i pesi del generatore PRIMA di iniziare il test della hint mechanic.
    ///
    /// Nessuna asserzione qui giudica cosa sia "una buona distribuzione": questo test
    /// è uno strumento di ispezione, la classificazione delle soglie di difficoltà
    /// resta una decisione di design.
    /// </summary>
    public class MapGenerationTests
    {
        private const int Width = 6;
        private const int Height = 6;
        private const int StartingFood = 12;
        private const int SeedSampleCount = 200;
        private const int BaseSeed = 1000;

        [Test]
        public void Generator_AlwaysProducesBeatableMap()
        {
            var generator = new NaiveWeightedMapGenerator(NaiveWeightedMapGenerator.Config.Default);

            for (int i = 0; i < SeedSampleCount; i++)
            {
                var result = generator.Generate(Width, Height, StartingFood, BaseSeed + i * 97);

                Assert.GreaterOrEqual(result.PathsFound, 1,
                    $"Seed {result.SeedUsed} non è beatable (PathsFound=0). " +
                    "Il generatore avrebbe dovuto ritentare seed successivi automaticamente.");
            }
        }

        [Test]
        public void Generator_DifficultyDistribution_IsLogged()
        {
            var config = NaiveWeightedMapGenerator.Config.Default;
            var generator = new NaiveWeightedMapGenerator(config);
            var buckets = new Dictionary<int, int>(); // PathsFound -> numero di seed

            for (int i = 0; i < SeedSampleCount; i++)
            {
                var result = generator.Generate(Width, Height, StartingFood, BaseSeed + i * 97);
                buckets.TryGetValue(result.PathsFound, out int count);
                buckets[result.PathsFound] = count + 1;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Distribuzione PathsFound su {SeedSampleCount} seed " +
                           $"({Width}x{Height}, food={StartingFood}):");

            foreach (var kvp in buckets.OrderBy(k => k.Key))
            {
                string label = kvp.Key >= config.MaxPathsToClassify ? $"{kvp.Key}+" : kvp.Key.ToString();
                float pct = 100f * kvp.Value / SeedSampleCount;
                sb.AppendLine($"  PathsFound={label} -> {kvp.Value} seed ({pct:F1}%)");
            }

            UnityEngine.Debug.Log(sb.ToString());

            Assert.Pass("Vedi Console per la distribuzione completa.");
        }
    }
}
