using System.Collections.Generic;
using NUnit.Framework;

namespace hp55games.MapGame.Rules.Tests
{
    public class RandomSourceTests
    {
        private static List<ulong> Take(IRandomSource rng, int n)
        {
            var list = new List<ulong>(n);
            for (int i = 0; i < n; i++) list.Add(rng.NextULong());
            return list;
        }

        [Test]
        public void SameSeed_SameSequence()
        {
            CollectionAssert.AreEqual(Take(new SplitMix64Random(42), 100), Take(new SplitMix64Random(42), 100));
        }

        [Test]
        public void DifferentSeeds_DifferentSequences()
        {
            CollectionAssert.AreNotEqual(Take(new SplitMix64Random(1), 100), Take(new SplitMix64Random(2), 100));
        }

        [Test]
        public void KnownVector_Seed0()
        {
            // Valore di riferimento SplitMix64 per seed 0: garantisce lo stesso output su ogni piattaforma.
            Assert.AreEqual(0xE220A8397B1DCDAFUL, new SplitMix64Random(0).NextULong());
        }

        [Test]
        public void Range_StaysInBounds_AndCoversAll()
        {
            var rng = new SplitMix64Random(7);
            var seen = new HashSet<int>();
            for (int i = 0; i < 10000; i++)
            {
                int v = rng.Range(-3, 4);
                Assert.That(v, Is.InRange(-3, 3));
                seen.Add(v);
            }
            Assert.AreEqual(7, seen.Count);
        }

        [Test]
        public void NextDouble_InUnitInterval()
        {
            var rng = new SplitMix64Random(99);
            for (int i = 0; i < 10000; i++)
            {
                double d = rng.NextDouble();
                Assert.That(d, Is.GreaterThanOrEqualTo(0.0).And.LessThan(1.0));
            }
        }
    }
}
