using System.Collections.Generic;

namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    /// <summary>
    /// Risultato di una generazione di mappa.
    /// </summary>
    public sealed class MapGenerationResult
    {
        public Dictionary<HexCoord, HexTileData> Tiles;
        public HexCoord StartCoord;
        public HexCoord ObjectiveCoord;
        public int SeedUsed;
    }

    /// <summary>
    /// Strategia di generazione mappa. Implementazioni concrete vanno dietro questa
    /// interfaccia per restare intercambiabili senza toccare HexGridController.
    /// </summary>
    public interface IMapGenerator
    {
        MapGenerationResult Generate(int width, int height, int seed);
    }
}
