namespace hp55games.MapGame.Rules
{
    /// <summary>
    /// Unica fonte di casualità per le regole. Stesso seed + stesse chiamate = stessa sequenza.
    /// </summary>
    public interface IRandomSource
    {
        /// <summary>Prossimo intero a 64 bit senza segno.</summary>
        ulong NextULong();

        /// <summary>Intero uniforme in [minInclusive, maxExclusive).</summary>
        int Range(int minInclusive, int maxExclusive);

        /// <summary>Double uniforme in [0, 1).</summary>
        double NextDouble();
    }
}
