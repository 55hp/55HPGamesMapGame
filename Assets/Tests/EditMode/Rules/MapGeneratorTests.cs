using System.Linq;
using NUnit.Framework;
using static hp55games.MapGame.Rules.Tests.RulesTestUtil;

namespace hp55games.MapGame.Rules.Tests
{
    public class MapGeneratorTests
    {
        [Test]
        public void R009_DefaultConfig_IsValid_AndSumsToGrid()
        {
            var c = Config();
            CollectionAssert.IsEmpty(c.Validate());
            Assert.AreEqual(c.Width * c.Height, c.CensusTotal);
        }

        [Test]
        public void R009_Census_CountsMatch_Over1000Seeds()
        {
            var c = Config();
            for (ulong seed = 0; seed < 1000; seed++)
            {
                var m = CensusMapGenerator.Generate(c, seed);
                Assert.IsTrue(m.Success, m.FailureReason);
                foreach (var kv in c.Census)
                    Assert.AreEqual(kv.Value, m.Tiles.Count(t => t == kv.Key), $"seed {seed} {kv.Key}");
                Assert.AreNotEqual(m.StartIndex, m.BossIndex);
                Assert.AreEqual(ContentKind.Boss, m.Tiles[m.BossIndex]);
                Assert.AreEqual(ContentKind.Start, m.Tiles[m.StartIndex]);
            }
        }

        [Test]
        public void R008_Start_FarFromBorder_BossAtWeightedDistance()
        {
            var c = Config();
            var allowed = c.EndDistanceWeights.Select(w => w.Distance).ToList();
            for (ulong seed = 0; seed < 1000; seed++)
            {
                var m = CensusMapGenerator.Generate(c, seed);
                int col = m.StartIndex % c.Width, row = m.StartIndex / c.Width;
                Assert.That(col, Is.InRange(c.StartMinBorderDistance, c.Width - 1 - c.StartMinBorderDistance));
                Assert.That(row, Is.InRange(c.StartMinBorderDistance, c.Height - 1 - c.StartMinBorderDistance));
                CollectionAssert.Contains(allowed, HexGridMath.Distance(m.StartIndex, m.BossIndex, c.Width), $"seed {seed}");
            }
        }

        [Test]
        public void R007_SameSeed_SameMap_DifferentSeeds_DifferentMaps()
        {
            var c = Config();
            CollectionAssert.AreEqual(CensusMapGenerator.Generate(c, 7).Tiles, CensusMapGenerator.Generate(c, 7).Tiles);
            CollectionAssert.AreNotEqual(CensusMapGenerator.Generate(c, 7).Tiles, CensusMapGenerator.Generate(c, 8).Tiles);
        }

        [Test]
        public void D029_BossPosition_NotAlwaysAboveStart()
        {
            var c = Config();
            int above = 0, below = 0;
            for (ulong seed = 0; seed < 500; seed++)
            {
                var m = CensusMapGenerator.Generate(c, seed);
                if (m.BossIndex / c.Width < m.StartIndex / c.Width) above++; else below++;
            }
            Assert.Greater(below, 50);
            Assert.Greater(above, 50);
        }

        [Test]
        public void R093_ClueCounts_MatchContent_Over1000Seeds()
        {
            var c = Config();
            for (ulong seed = 0; seed < 1000; seed += 7)
            {
                var m = CensusMapGenerator.Generate(c, seed);
                foreach (ClueTarget t in System.Enum.GetValues(typeof(ClueTarget)))
                {
                    int rowsSum = 0, colsSum = 0;
                    for (int r = 0; r < c.Height; r++)
                    {
                        int expect = 0;
                        for (int col = 0; col < c.Width; col++) if (CensusMapGenerator.Matches(c, m.At(col, r), t)) expect++;
                        Assert.AreEqual(expect, m.ClueCount(t, LineRef.Row(r)));
                        rowsSum += expect;
                    }
                    for (int col = 0; col < c.Width; col++) colsSum += m.ClueCount(t, LineRef.Col(col));
                    Assert.AreEqual(rowsSum, colsSum);
                }
            }
        }

        [Test]
        public void InvalidCensus_FailsExplicitly()
        {
            var c = Config();
            c.Census[ContentKind.Bush] += 1;
            var m = CensusMapGenerator.Generate(c, 1);
            Assert.IsFalse(m.Success);
            StringAssert.Contains("Censimento", m.FailureReason);
        }
    
        [Test]
        public void D036_SafeStart_Over1000Seeds()
        {
            var c = Config();
            for (ulong seed = 0; seed < 1000; seed++)
            {
                var m = CensusMapGenerator.Generate(c, seed);
                foreach (int nb in HexGridMath.Neighbors(m.StartIndex, c.Width, c.Height))
                {
                    int lvl = c.Content(m.Tiles[nb]).MaskLevel;
                    Assert.IsFalse(c.StartNeighborForbiddenMaskLevels.Contains(lvl), $"seed {seed}: {m.Tiles[nb]} accanto alla partenza");
                }
                bool coin = false;
                for (int i = 0; i < m.Tiles.Length; i++)
                    if (c.StartCoinSourceKinds.Contains(m.Tiles[i]) && HexGridMath.Distance(i, m.StartIndex, c.Width) <= c.StartCoinSourceMaxDistance) coin = true;
                Assert.IsTrue(coin, $"seed {seed}: nessuna fonte di monete vicina");
            }
        }
    }
}
