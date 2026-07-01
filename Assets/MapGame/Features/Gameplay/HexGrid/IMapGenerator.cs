using System.Collections.Generic;

namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    /// <summary>
    /// Risultato di una generazione di mappa: le tile e quale coordinata è l'obiettivo di test.
    /// </summary>
    public sealed class MapGenerationResult
    {
        public Dictionary<HexCoord, HexTileData> Tiles;
        public HexCoord StartCoord;
        public HexCoord ObjectiveCoord;
        public int SeedUsed;

        /// <summary>
        /// Numero di percorsi validi trovati verso l'obiettivo (capped, vedi MapPathCounter).
        /// Utile per classificare la difficoltà della mappa generata da un dato seed.
        /// </summary>
        public int PathsFound;
    }

    /// <summary>
    /// Strategia di generazione mappa. Implementazioni concrete (naive, noise-based, ecc.)
    /// vanno dietro questa interfaccia per restare intercambiabili senza toccare
    /// HexGridController o la hint mechanic.
    /// </summary>
    public interface IMapGenerator
    {
        MapGenerationResult Generate(int width, int height, int startingFood, int seed);
    }
}
