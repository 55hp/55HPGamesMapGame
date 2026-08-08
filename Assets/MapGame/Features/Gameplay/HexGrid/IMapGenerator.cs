using System.Collections.Generic;

namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    /// <summary>
    /// Risultato di una generazione di mappa.
    ///
    /// Success/FailureReason: a differenza degli altri vincoli del generatore
    /// (DifficultyLevel, adiacenza Strada) che degradano silenziosamente quando la
    /// posizione non li supporta, il numero minimo di PathCluster e' un requisito HARD —
    /// sotto soglia la mappa non e' considerata giocabile. Success=false non significa che
    /// Tiles sia vuoto o corrotto (la generazione completa comunque, per permettere debug),
    /// significa che il chiamante NON deve avviare il livello con questi Tiles — vedi
    /// HexGridController.BuildGrid.
    /// </summary>
    public sealed class MapGenerationResult
    {
        public Dictionary<HexCoord, HexTileData> Tiles;
        public HexCoord StartCoord;
        public HexCoord ObjectiveCoord;
        public int SeedUsed;

        public bool Success = true;
        public string FailureReason;
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
