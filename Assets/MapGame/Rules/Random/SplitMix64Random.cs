using System;

namespace hp55games.MapGame.Rules
{
    /// <summary>
    /// Generatore SplitMix64 seedato. Deterministico su tutte le piattaforme
    /// (solo aritmetica intera a 64 bit, nessuna dipendenza da System.Random).
    /// </summary>
    public sealed class SplitMix64Random : IRandomSource
    {
        private ulong _state;

        public SplitMix64Random(ulong seed)
        {
            _state = seed;
        }

        public ulong NextULong()
        {
            unchecked
            {
                _state += 0x9E3779B97F4A7C15UL;
                ulong z = _state;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), "maxExclusive deve essere > minInclusive");

            ulong span = (ulong)((long)maxExclusive - minInclusive);
            // Rifiuto per evitare il bias del modulo.
            ulong limit = ulong.MaxValue - (ulong.MaxValue % span);
            ulong r;
            do { r = NextULong(); } while (r >= limit);
            return (int)((long)minInclusive + (long)(r % span));
        }

        public double NextDouble()
        {
            // 53 bit di mantissa.
            return (NextULong() >> 11) * (1.0 / (1UL << 53));
        }
    }
}
